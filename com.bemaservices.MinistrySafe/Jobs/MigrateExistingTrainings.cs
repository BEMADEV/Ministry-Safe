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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Jobs.PostUpdateJobs;
using Rock.Model;

namespace com.bemaservices.MinistrySafe.Jobs
{
    /// <summary>
    /// Job to migrate existing trainings - runs once during migration then deletes itself
    /// </summary>
    [DisplayName( "MinistrySafe Update Helper - Migrate Existing Trainings." )]
    [IntegerField(
        "Command Timeout",
        Key = AttributeKey.CommandTimeout,
        Description = "Maximum amount of time (in seconds) to wait for each SQL command to complete. On a large database with lots of transactions, this could take several minutes or more.",
        IsRequired = false,
        DefaultIntegerValue = 14400 )]
    [DisallowConcurrentExecution]
    public class MigrateExistingTrainings : PostUpdateJob
    {
        private static class AttributeKey
        {
            public const string CommandTimeout = "CommandTimeout";
        }

        public override void Execute()
        {
            var errorMessages = new List<String>();
            MinistrySafe.UpdateSurveyTypes( errorMessages );
            if ( errorMessages.Any() )
            {
                this.Result = errorMessages.JoinStrings( "," );
                return;
            }
            else
            {
                // get the configured timeout, or default to 240 minutes if it is blank
                var commandTimeout = GetAttributeValue( AttributeKey.CommandTimeout ).AsIntegerOrNull() ?? 14400;
                var rockContext = new RockContext();
                rockContext.Database.CommandTimeout = commandTimeout;

                var sqlQuery = @"
-- Old Data Points
Declare @TrainingTypePersonAttributeId int = (Select top 1 Id From Attribute Where Guid = '05E50A5A-DFB8-4656-9210-9027565D7864');
Declare @CompletionDatePersonAttributeId int = (Select top 1 Id From Attribute Where Guid = '0B1607AF-6900-406C-8F7F-8DC03FC253F3');
Declare @ResultPersonAttributeId int = (Select top 1 Id From Attribute Where Guid = 'C19F3842-7CEE-4772-B2ED-86B7968E2879');
Declare @ScorePersonAttributeId int = (Select top 1 Id From Attribute Where Guid = '937C7D10-74DD-4512-9E0D-79B5B989DEB7');

-- New Data Points
Declare @TrainingTypeStepTypeAttributeId int = (Select top 1 Id From Attribute Where Guid = '829CCCC8-2D70-43B4-90CF-136012A8A756');
Declare @StepProgramId int = (Select top 1 Id From StepProgram Where Guid = 'F821FE74-214A-4583-9ECB-FF7B6193F57F')
Declare @StepEntityTypeId int = (Select top 1 Id from EntityType Where Name = 'Rock.Model.Step' )
Declare @PassedStepStatusId int = ( Select top 1 Id from StepStatus Where StepProgramId = @StepProgramId and Name = 'Passed')
Declare @FailedStepStatusId int = ( Select top 1 Id from StepStatus Where StepProgramId = @StepProgramId and Name = 'Failed')

Declare @ReferenceTable table(
	PersonAliasId int,
	StepTypeId int,
	CompletedDateTime datetime,
	StepStatusId int,
	ScoreAttributeId int,
	Score int
	)
Insert into @ReferenceTable
Select pa.Id as PersonAliasId
	,st.Id as StepTypeId
	,avCompletionDate.ValueAsDateTime as CompletedDateTime
	, case when avResult.Value like 'Pass%' then @PassedStepStatusId
		when avResult.Value like 'Fail%' then @FailedStepStatusId
		else null end as StepStatusId
	,aScore.Id as ScoreAttributeId
	,avScore.Value as Score
From Person p
Join PersonAlias pa
	on pa.AliasPersonId = p.Id
Join AttributeValue avTrainingType 
	on avTrainingType.EntityId = p.Id 
	and avTrainingType.AttributeId = @TrainingTypePersonAttributeId
Join AttributeValue avCompletionDate 
	on avCompletionDate.EntityId = p.Id
	and avCompletionDate.AttributeId = @CompletionDatePersonAttributeId
Join AttributeValue avResult
	on avResult.EntityId = p.Id
	and avResult.AttributeId = @ResultPersonAttributeId
Join AttributeValue avScore
	on avScore.EntityId = p.Id
	and avScore.AttributeId = @ScorePersonAttributeId
Join DefinedValue dvTrainingType
	on dvTrainingType.Guid = try_cast(avTrainingType.Value as uniqueidentifier)
Join AttributeValue avStepType
	on avStepType.EntityId = dvTrainingType.Id
	and avStepType.AttributeId = @TrainingTypeStepTypeAttributeId
Join StepType st
	on st.Guid = try_cast(avStepType.Value as uniqueidentifier)
Join Attribute aScore
	on aScore.EntityTypeId = @StepEntityTypeId
	and EntityTypeQualifierColumn = 'StepTypeId'
	and EntityTypeQualifierValue = try_cast(st.Id as nvarchar(max))
	and [Key] = 'Score'
Left Join Step s
	on s.StepTypeId = st.Id
	and s.PersonAliasId = pa.Id
	and s.CompletedDateTime = avCompletionDate.ValueAsDateTime
Where s.Id is null

-- Insert Steps
INSERT INTO dbo.Step (
    StepTypeId,
    PersonAliasId,
    StepStatusId,
    CompletedDateTime,
	[Order],
    Guid
)
SELECT 
    StepTypeId,
    PersonAliasId,
    StepStatusId,
    CompletedDateTime,
	0,
    NEWID()
FROM @ReferenceTable;

INSERT INTO dbo.AttributeValue (
    IsSystem,
    AttributeId,
    EntityId,
    Value,
    Guid
)
SELECT 
    0 AS IsSystem,
    rt.ScoreAttributeId,
    s.Id AS EntityId,
    rt.Score AS Value,
    NEWID()
FROM @ReferenceTable rt
JOIN Step s 
	ON s.PersonAliasId = rt.PersonAliasId
    AND s.StepTypeId = rt.StepTypeId
    AND s.CompletedDateTime = rt.CompletedDateTime
WHERE rt.Score IS NOT NULL;


";
                rockContext.Database.ExecuteSqlCommand( sqlQuery );
                DeleteJob();
            }

            
        }

        /// <summary>
        /// Deletes the job.
        /// </summary>
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
