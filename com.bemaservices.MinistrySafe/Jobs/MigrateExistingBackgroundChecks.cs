// <copyright>
// Copyright by BEMA Software Services
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Linq;
using com.bemaservices.MinistrySafe.Constants;
using Quartz;
using Rock;
using Rock.Jobs;
using Rock.Jobs.PostUpdateJobs;
using Rock.Data;
using Rock.Model;

namespace com.bemaservices.MinistrySafe.Jobs
{
    /// <summary>
    /// Migrates existing MinistrySafe Background Check person attributes to Rock Steps.
    /// </summary>
    [DisallowConcurrentExecution]
    public class MigrateExistingBackgroundChecks : PostUpdateJob
    {
        /// <summary>
        /// Executes the migration job.
        /// </summary>
        public override void Execute()
        {
            using ( var rockContext = new RockContext() )
            {
                rockContext.Database.CommandTimeout = 14400;

                var stepProgram = new StepProgramService( rockContext ).Queryable()
                    .FirstOrDefault( program => program.Guid == MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_PROGRAM.AsGuid() );
                var backgroundCheckTypeAttribute = new AttributeService( rockContext )
                    .Get( MinistrySafeSystemGuid.MINISTRYSAFE_PERSON_BACKGROUNDCHECK_TYPE_ATTRIBUTE.AsGuid() );
                var backgroundCheckTypeDefinedType = new DefinedTypeService( rockContext ).Queryable()
                    .FirstOrDefault( definedType => definedType.Guid == Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES.AsGuid() );

                if ( stepProgram == null || backgroundCheckTypeAttribute == null || backgroundCheckTypeDefinedType == null )
                {
                    this.Result = "Background Check Step Program or person attributes were not found. The migration job will retry.";
                    return;
                }

                var personBackgroundCheckTypeValues = new AttributeValueService( rockContext )
                    .Queryable()
                    .Where( value => value.AttributeId == backgroundCheckTypeAttribute.Id && value.Value != null )
                    .Select( value => value.Value )
                    .Distinct()
                    .ToList();

                var definedValueService = new DefinedValueService( rockContext );
                var stepTypeService = new StepTypeService( rockContext );
                foreach ( var backgroundCheckTypeValue in personBackgroundCheckTypeValues )
                {
                    var backgroundCheckTypeGuid = backgroundCheckTypeValue.AsGuidOrNull();
                    if ( !backgroundCheckTypeGuid.HasValue )
                    {
                        continue;
                    }

                    var backgroundCheckType = definedValueService.Get( backgroundCheckTypeGuid.Value );
                    if ( backgroundCheckType == null ||
                        backgroundCheckType.DefinedTypeId != backgroundCheckTypeDefinedType.Id ||
                        backgroundCheckType.ForeignId != 4 )
                    {
                        continue;
                    }

                    if ( !stepTypeService.Queryable().Any( type =>
                        type.StepProgramId == stepProgram.Id && type.Name == backgroundCheckType.Value ) )
                    {
                        stepTypeService.Add( new StepType
                        {
                            IsActive = true,
                            StepProgramId = stepProgram.Id,
                            ForeignId = 4,
                            Name = backgroundCheckType.Value,
                            IconCssClass = "ti ti-shield"
                        } );
                    }
                }

                rockContext.SaveChanges();

                var sqlQuery = $@"
SET NOCOUNT ON;

DECLARE @BackgroundCheckTypeAttributeId INT = ( SELECT TOP 1 Id FROM dbo.Attribute WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_PERSON_BACKGROUNDCHECK_TYPE_ATTRIBUTE}' );
DECLARE @BackgroundCheckDateAttributeId INT = ( SELECT TOP 1 Id FROM dbo.Attribute WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_PERSON_BACKGROUNDCHECK_DATE_ATTRIBUTE}' );
DECLARE @BackgroundCheckStatusAttributeId INT = ( SELECT TOP 1 Id FROM dbo.Attribute WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_PERSON_BACKGROUNDCHECK_STATUS_ATTRIBUTE}' );
DECLARE @BackgroundCheckReportStatusAttributeId INT = ( SELECT TOP 1 Id FROM dbo.Attribute WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_PERSON_BACKGROUNDCHECK_REPORT_STATUS_ATTRIBUTE}' );
DECLARE @BackgroundCheckTypeDefinedTypeId INT = ( SELECT TOP 1 Id FROM dbo.DefinedType WHERE Guid = '{Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES}' );
DECLARE @MinistrySafeStatusDefinedTypeId INT = ( SELECT TOP 1 Id FROM dbo.DefinedType WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STATUS_TYPE}' );
DECLARE @StepProgramId INT = ( SELECT TOP 1 Id FROM dbo.StepProgram WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_PROGRAM}' );
DECLARE @ApprovedStepStatusId INT = ( SELECT TOP 1 Id FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Approved' );
DECLARE @FailedStepStatusId INT = ( SELECT TOP 1 Id FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Failed Pending Review' );
DECLARE @SecurityReviewStepStatusId INT = ( SELECT TOP 1 Id FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Awaiting Security Review' );
DECLARE @CancelledStepStatusId INT = ( SELECT TOP 1 Id FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Cancelled' );
DECLARE @PendingStepStatusId INT = ( SELECT TOP 1 Id FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Pending' );

DECLARE @ReferenceTable TABLE (
    PersonAliasId INT NOT NULL,
    StepTypeId INT NOT NULL,
    CompletedDateTime DATETIME NOT NULL,
    StepStatusId INT NOT NULL,
    PRIMARY KEY ( PersonAliasId, StepTypeId, CompletedDateTime )
);

INSERT INTO @ReferenceTable ( PersonAliasId, StepTypeId, CompletedDateTime, StepStatusId )
SELECT DISTINCT
    p.PrimaryAliasId,
    st.Id,
    avDate.ValueAsDateTime,
    COALESCE(
        ministrySafeStepStatus.Id,
        CASE
            WHEN avReportStatus.Value LIKE 'Pass%' THEN @ApprovedStepStatusId
            WHEN avReportStatus.Value LIKE 'Fail%' THEN @FailedStepStatusId
            WHEN avReportStatus.Value LIKE 'Review%' THEN @SecurityReviewStepStatusId
            WHEN avReportStatus.Value LIKE 'Cancel%' THEN @CancelledStepStatusId
            ELSE @PendingStepStatusId
        END
    )
FROM dbo.Person p
INNER JOIN dbo.PersonAlias pa ON pa.Id = p.PrimaryAliasId
INNER JOIN dbo.AttributeValue avType ON avType.EntityId = p.Id AND avType.AttributeId = @BackgroundCheckTypeAttributeId
INNER JOIN dbo.DefinedValue dvType ON dvType.Guid = TRY_CONVERT( UNIQUEIDENTIFIER, avType.Value ) AND dvType.DefinedTypeId = @BackgroundCheckTypeDefinedTypeId AND dvType.ForeignId = 4
INNER JOIN dbo.StepType st ON st.StepProgramId = @StepProgramId AND st.Name = dvType.Value
INNER JOIN dbo.AttributeValue avDate ON avDate.EntityId = p.Id AND avDate.AttributeId = @BackgroundCheckDateAttributeId
LEFT JOIN dbo.AttributeValue avStatus ON avStatus.EntityId = p.Id AND avStatus.AttributeId = @BackgroundCheckStatusAttributeId
LEFT JOIN dbo.DefinedValue dvStatus ON dvStatus.Guid = TRY_CONVERT( UNIQUEIDENTIFIER, avStatus.Value ) AND dvStatus.DefinedTypeId = @MinistrySafeStatusDefinedTypeId
LEFT JOIN dbo.StepStatus ministrySafeStepStatus ON ministrySafeStepStatus.StepProgramId = @StepProgramId AND ministrySafeStepStatus.Name = dvStatus.Value
LEFT JOIN dbo.AttributeValue avReportStatus ON avReportStatus.EntityId = p.Id AND avReportStatus.AttributeId = @BackgroundCheckReportStatusAttributeId
WHERE p.PrimaryAliasId IS NOT NULL
    AND avDate.ValueAsDateTime IS NOT NULL;

UPDATE step
SET step.StepStatusId = reference.StepStatusId,
    step.StartDateTime = COALESCE( step.StartDateTime, reference.CompletedDateTime ),
    step.CompletedDateTime = reference.CompletedDateTime,
    step.EndDateTime = COALESCE( step.EndDateTime, reference.CompletedDateTime )
FROM dbo.Step step
INNER JOIN @ReferenceTable reference ON reference.PersonAliasId = step.PersonAliasId
    AND reference.StepTypeId = step.StepTypeId
    AND step.CompletedDateTime >= CONVERT( DATE, reference.CompletedDateTime )
    AND step.CompletedDateTime < DATEADD( DAY, 1, CONVERT( DATE, reference.CompletedDateTime ) );
DECLARE @StepsUpdated INT = @@ROWCOUNT;

INSERT INTO dbo.Step ( StepTypeId, PersonAliasId, StepStatusId, StartDateTime, CompletedDateTime, EndDateTime, [Order], Guid )
SELECT reference.StepTypeId, reference.PersonAliasId, reference.StepStatusId, reference.CompletedDateTime, reference.CompletedDateTime, reference.CompletedDateTime, 0, NEWID()
FROM @ReferenceTable reference
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Step existingStep
    WHERE existingStep.StepTypeId = reference.StepTypeId
        AND existingStep.PersonAliasId = reference.PersonAliasId
        AND existingStep.CompletedDateTime >= CONVERT( DATE, reference.CompletedDateTime )
        AND existingStep.CompletedDateTime < DATEADD( DAY, 1, CONVERT( DATE, reference.CompletedDateTime ) )
);
DECLARE @StepsCreated INT = @@ROWCOUNT;

SELECT @StepsCreated + @StepsUpdated;
";

                var stepsProcessed = rockContext.Database.SqlQuery<int>( sqlQuery ).FirstOrDefault();
                this.Result = string.Format( "Migrated {0} MinistrySafe background check person attribute record{1} to Steps.",
                    stepsProcessed,
                    stepsProcessed == 1 ? "" : "s" );
            }

            DeleteJob();
        }

        private void DeleteJob()
        {
            using ( var rockContext = new RockContext() )
            {
                var jobService = new ServiceJobService( rockContext );
                var job = jobService.Get( GetJobId() );
                if ( job != null )
                {
                    jobService.Delete( job );
                    rockContext.SaveChanges();
                }
            }
        }
    }
}