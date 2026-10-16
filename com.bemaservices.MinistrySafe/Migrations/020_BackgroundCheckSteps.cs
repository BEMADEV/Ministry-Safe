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
using com.bemaservices.MinistrySafe.Constants;
using Rock;
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Adds BackgroundCheck type tracking and MinistrySafe background-check Steps.
    /// </summary>
    [MigrationNumber( 20, "1.19.0" )]
    public partial class BackgroundCheckSteps : Migration
    {
        /// <summary>
        /// Applies the migration.
        /// </summary>
        public override void Up()
        {
            AddBackgroundCheckStepProgram();
            AddBackgroundCheckTypeAttribute();
            AddBackgroundCheckStepWorkflowActions();
            UpdateStepStatusJobAttributes();
            AddBackgroundCheckMigrationJob();
            UpdateBadge();
        }

        private void AddBackgroundCheckStepProgram()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT 1 FROM dbo.StepProgram WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_PROGRAM}' )
BEGIN
    INSERT INTO dbo.StepProgram ( Name, IconCssClass, DefaultListView, IsActive, [Order], Guid, StepTerm )
    VALUES ( 'MinistrySafe Background Checks', 'ti ti-shield', 0, 1, 1, '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_PROGRAM}', 'Background Check' );
END;

DECLARE @StepProgramId INT = ( SELECT TOP 1 Id FROM dbo.StepProgram WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_PROGRAM}' );
DECLARE @MinistrySafeStatusTypeId INT = ( SELECT TOP 1 Id FROM dbo.DefinedType WHERE Guid = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STATUS_TYPE}' );
DECLARE @CurrentDateTime DATETIME = GETDATE();
DECLARE @StepStatusMap TABLE (
    DefinedValueGuid UNIQUEIDENTIFIER,
    StepStatusGuid UNIQUEIDENTIFIER,
    IsCompleteStatus BIT,
    StatusColor NVARCHAR(50),
    [Order] INT
);

INSERT INTO @StepStatusMap ( DefinedValueGuid, StepStatusGuid, IsCompleteStatus, StatusColor, [Order] )
VALUES
    ( 'C8887B07-A7DB-4AF1-B56C-543BB7137F2A', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_ADVERSE}', 1, 'rgb(244,67,54)', 0 ),
    ( '807D3AC0-95A0-44BB-BF9B-CE729606FEC7', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_APPLICANT_WITHDRAWN}', 1, 'rgb(158,158,158)', 1 ),
    ( 'B5DA923A-6D2B-433C-9E9F-FCD0CAD4B5E9', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_APPROVED}', 1, 'rgb(76,175,80)', 2 ),
    ( '8947127B-315A-4F34-AFFC-6F155C669D5B', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_ARCHIVED}', 1, 'rgb(158,158,158)', 3 ),
    ( '468996A3-8810-46C0-BEB5-43F9AEDC2147', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_AWAITING_APPLICANT}', 0, 'rgb(33,150,243)', 4 ),
    ( 'EF69198D-6A9B-4E84-BD58-E72355117CA9', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_AWAITING_MINISTRYSAFE_REVIEW}', 0, 'rgb(33,150,243)', 5 ),
    ( '1EE72961-11B7-4473-A4F3-9091987BF687', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_AWAITING_SECURITY_REVIEW}', 0, 'rgb(240,223,80)', 6 ),
    ( '3C64DCF1-9DED-4387-BBC1-D243D7C054EE', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_CANCELLED}', 1, 'rgb(158,158,158)', 7 ),
    ( '4E475DBA-94F4-44F1-A842-B12F4E20BF7D', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_FAILED_PENDING_REVIEW}', 0, 'rgb(244,67,54)', 8 ),
    ( 'BA89FC63-DE39-4A5C-80E5-FB804974BF50', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_PENDING_SECURITY_APPROVAL}', 0, 'rgb(240,223,80)', 9 ),
    ( 'A6593B42-3119-4D81-9C3E-68F044410BD1', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_PENDING}', 0, 'rgb(33,150,243)', 10 ),
    ( '7D947D35-5617-4509-8804-341B84F3CEFA', '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_STATUS_PRE_ADVERSE}', 0, 'rgb(244,140,0)', 11 );

INSERT INTO dbo.StepStatus ( Name, StepProgramId, IsCompleteStatus, StatusColor, IsActive, [Order], Guid, CreatedDateTime, ModifiedDateTime )
SELECT dv.Value, @StepProgramId, statusMap.IsCompleteStatus, statusMap.StatusColor, 1, statusMap.[Order], statusMap.StepStatusGuid, @CurrentDateTime, @CurrentDateTime
FROM @StepStatusMap statusMap
INNER JOIN dbo.DefinedValue dv ON dv.Guid = statusMap.DefinedValueGuid AND dv.DefinedTypeId = @MinistrySafeStatusTypeId
WHERE NOT EXISTS ( SELECT 1 FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = dv.Value );

INSERT INTO dbo.StepStatus ( Name, StepProgramId, IsCompleteStatus, StatusColor, IsActive, [Order], Guid, CreatedDateTime, ModifiedDateTime )
SELECT 'Expiring', @StepProgramId, 1, 'rgb(240,223,80)', 1, 12, '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STATUS_EXPIRING}', @CurrentDateTime, @CurrentDateTime
WHERE NOT EXISTS ( SELECT 1 FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Expiring' );

INSERT INTO dbo.StepStatus ( Name, StepProgramId, IsCompleteStatus, StatusColor, IsActive, [Order], Guid, CreatedDateTime, ModifiedDateTime )
SELECT 'Expired', @StepProgramId, 1, 'rgb(163,163,163)', 1, 13, '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STATUS_EXPIRED}', @CurrentDateTime, @CurrentDateTime
WHERE NOT EXISTS ( SELECT 1 FROM dbo.StepStatus WHERE StepProgramId = @StepProgramId AND Name = 'Expired' );
" );
        }

        private void AddBackgroundCheckTypeAttribute()
        {
            RockMigrationHelper.AddOrUpdateEntityAttribute(
                "Rock.Model.BackgroundCheck",
                "59D5A94C-94A0-4630-B80A-BB25697D74C7",
                "",
                "",
                "Background Check Type",
                "Background Check Type",
                "The MinistrySafe background-check type selected for this request.",
                0,
                "",
                MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE,
                "BackgroundCheckType" );

            var backgroundCheckTypeId = SqlScalar( $"SELECT TOP 1 Id FROM DefinedType WHERE Guid = '{Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES}'" ).ToStringSafe();

            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "AllowAddingNewValues", "False", "45A7B1D6-83DE-43B5-801F-ECA3162DBEFC" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "allowmultiple", "False", "B90AE807-CB7D-4677-9E57-BA0E18FB62E3" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "definedtype", backgroundCheckTypeId, "2276CC8C-3F9B-403D-AE78-4150C1C09AEE" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "displaydescription", "False", "5FC57ECC-559B-4832-8EB8-17B2A70C6723" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "enhancedselection", "False", "5647A455-C250-4189-9D42-4A21170E8178" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "includeInactive", "False", "F92B97C6-EC35-4D77-BBD3-487E3F2A6791" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "RepeatColumns", "", "071B5629-A4CF-4430-9BC6-DBF9F747FAC3" );
            RockMigrationHelper.UpdateAttributeQualifier( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE, "SelectableDefinedValuesId", "", "0A5DB7DA-79D7-4BC9-AD37-1586A3055303" );

            Sql( $"UPDATE [Attribute] SET [IsGridColumn] = 1 WHERE [Guid] = '{MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_TYPE_ATTRIBUTE}'" );
        }

        private void AddBackgroundCheckStepWorkflowActions()
        {
            RockMigrationHelper.UpdateWorkflowTypeAttribute(
                MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_WORKFLOW_TYPE,
                "9C204CD0-1233-41C5-818A-C5DA439445AA",
                "Step",
                "Step",
                "The Rock Step associated with this background check.",
                50,
                "",
                MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_STEP_ATTRIBUTE,
                false );

        }

        private void UpdateStepStatusJobAttributes()
        {
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Training Days Until Expiring", "Training Days Until Expiring", @"The number of days after completion when a training step should be marked as Expiring.", 0, @"700", "4499C1EE-EAC5-4818-9557-3B1478F8A4D6", "DaysUntilExpiring" );
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Training Days Until Expired", "Training Days Until Expired", @"The number of days after completion when a training step should be marked as Expired.", 1, @"730", "266B1A12-468D-4EA5-B29A-5377891659ED", "DaysUntilExpired" );
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Background Check Days Until Expiring", "Background Check Days Until Expiring", @"The number of days after approval when a background check should be marked as Expiring.", 2, @"700", MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_DAYS_UNTIL_EXPIRING_ATTRIBUTE, "BackgroundCheckDaysUntilExpiring" );
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Background Check Days Until Expired", "Background Check Days Until Expired", @"The number of days after approval when a background check should be marked as Expired.", 3, @"730", MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_DAYS_UNTIL_EXPIRED_ATTRIBUTE, "BackgroundCheckDaysUntilExpired" );

            var serviceJobId = SqlScalar( "SELECT TOP 1 Id FROM ServiceJob WHERE [Class] = 'com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses'" ).ToStringSafe().AsInteger();
            RockMigrationHelper.AddAttributeValue( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_DAYS_UNTIL_EXPIRING_ATTRIBUTE, serviceJobId, @"700", "2B9DF891-6D24-459A-B817-FF7F2444E886" );
            RockMigrationHelper.AddAttributeValue( MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_DAYS_UNTIL_EXPIRED_ATTRIBUTE, serviceJobId, @"730", "7D265B55-550E-48A8-8211-C60D5EF6BFC3" );
        }

        private void AddBackgroundCheckMigrationJob()
        {
            Sql( $@"
IF NOT EXISTS ( SELECT [Id] FROM [ServiceJob] WHERE [Guid] = '{MinistrySafeSystemGuid.MINISTRYSAFE_MIGRATE_BACKGROUNDCHECKS_JOB}' )
BEGIN
    INSERT INTO [ServiceJob] ( [IsSystem], [IsActive], [Name], [Description], [Class], [CronExpression], [NotificationStatus], [Guid] )
    VALUES ( 1, 1, 'MinistrySafe Update Helper - Migrate Existing Background Checks.', 'This job migrates existing background checks to MinistrySafe Steps.', 'com.bemaservices.MinistrySafe.Jobs.MigrateExistingBackgroundChecks', '0 0/5 * 1/1 * ? *', 1, '{MinistrySafeSystemGuid.MINISTRYSAFE_MIGRATE_BACKGROUNDCHECKS_JOB}' );
END" );
        }

        private void UpdateBadge()
        {
            RockMigrationHelper.AddBadgeAttributeValue( "9E9B9FAF-C7B8-40AA-B0C9-24177058943B", "01C9BA59-D8D4-4137-90A6-B3C06C70BBC3", @"
                {% assign yearsToHidden = 5 %}
                {% assign currentDate = 'Now' | Date %}
                {% assign trainingSteps = Person | Steps:'F821FE74-214A-4583-9ECB-FF7B6193F57F' %}
                {% assign groupedStepTypes = trainingSteps | OrderBy:'StepStatus.Order, StartDateTime desc' | GroupBy:'StepType.Guid' %}
                {% for groupedStepType in groupedStepTypes %}
                    {% assign stepTypeKvp = groupedStepType | PropertyToKeyValue %}
                    {% assign stepTypeSteps = stepTypeKvp.Value %}
                    {% assign latestStepTypeStep = stepTypeSteps | First %}
                    {% assign latestStepAge = currentDate | DateDiff:latestStepTypeStep.StartDateTime,'Y' %}
                    {% if yearsToHidden > latestStepAge %}
                        {% assign stepType = latestStepTypeStep.StepType %}
                        {% assign latestStepStatusColor = latestStepTypeStep.StepStatus.StatusColor | Default:'#16c98d' %}
                        {% assign badgeStyle = 'color:' | Append:latestStepStatusColor %}
                        {% assign latestRequestedStepSortDate = '' %}
                        {% assign latestPassedStepSortDate = '' %}
                        {% assign requestedStatusColor = '' %}
                        {% assign passedStatusColor = '' %}

                        {% for step in stepTypeSteps %}
                            {% if latestRequestedStepSortDate == '' and step.StepStatus.Name == 'Requested' %}
                                {% assign latestRequestedStepDateTime = step.StartDateTime | Default:step.CompletedDateTime %}
                                {% assign latestRequestedStepSortDate = latestRequestedStepDateTime | Date:'yyyyMMddHHmmss' %}
                                {% assign requestedStatusColor = step.StepStatus.StatusColor | Default:latestStepStatusColor %}
                            {% endif %}

                            {% if latestPassedStepSortDate == '' and step.StepStatus.Name == 'Passed' %}
                                {% assign latestPassedStepDateTime = step.CompletedDateTime | Default:step.StartDateTime %}
                                {% assign latestPassedStepSortDate = latestPassedStepDateTime | Date:'yyyyMMddHHmmss' %}
                                {% assign passedStatusColor = step.StepStatus.StatusColor | Default:latestStepStatusColor %}
                            {% endif %}
                        {% endfor %}

                        {% if latestRequestedStepSortDate != '' and latestPassedStepSortDate != '' and latestRequestedStepSortDate > latestPassedStepSortDate %}
                            {% capture badgeStyle %}color:color-mix(in srgb, {{ requestedStatusColor }} 50%, {{ passedStatusColor }} 50%);{% endcapture %}
                        {% endif %}

                        {% capture tooltipText %}
                        {{ stepType.Name }}
                        <ul>
                            {% for step in stepTypeSteps %}
                                {% assign stepScore = step | Attribute:'Score' %}
                                <li>{{step.StepStatus.Name}} on {{step.CompletedDateTime | Default:step.StartDateTime | Date:'MM/dd/yyyy' }} {% if stepScore > 0 %} (Score: {{stepScore}}) {% endif %}</li>
                            {% endfor %}
                        </ul>
                        {% endcapture %}

                        <div class=""rockbadge rockbadge-icon rockbadge-step""
                            style=""{{ badgeStyle }}""
                            data-toggle=""tooltip""
                            data-html=""true""
                            data-original-title=""{{ tooltipText }}"">
                                <i class=""badge-icon {{stepType.IconCssClass | Default: 'fa fa-eye'}}""></i>
                        </div>
                    {% endif %}
                {% endfor %}

                {% assign backgroundCheckSteps = Person | Steps:'7B3B0BD2-07B7-4B3A-AB7C-E6A904E3E8C0' %}
                {% assign groupedBackgroundCheckTypes = backgroundCheckSteps | OrderBy:'StepStatus.Order, StartDateTime desc' | GroupBy:'StepType.Guid' %}
                {% for groupedBackgroundCheckType in groupedBackgroundCheckTypes %}
                    {% assign backgroundCheckTypeKvp = groupedBackgroundCheckType | PropertyToKeyValue %}
                    {% assign backgroundCheckTypeSteps = backgroundCheckTypeKvp.Value %}
                    {% assign latestBackgroundCheckStep = backgroundCheckTypeSteps | First %}
                    {% assign latestBackgroundCheckAge = currentDate | DateDiff:latestBackgroundCheckStep.StartDateTime,'Y' %}
                    {% if yearsToHidden > latestBackgroundCheckAge %}
                        {% assign backgroundCheckStepType = latestBackgroundCheckStep.StepType %}
                        {% assign backgroundCheckStatusColor = latestBackgroundCheckStep.StepStatus.StatusColor | Default:'#16c98d' %}
                        {% capture backgroundCheckTooltip %}
                            {{ backgroundCheckStepType.Name }}
                            <ul>
                                {% for step in backgroundCheckTypeSteps %}
                                    <li>{{step.StepStatus.Name}} on {{step.CompletedDateTime | Default:step.StartDateTime | Date:'MM/dd/yyyy' }}</li>
                                {% endfor %}
                            </ul>
                        {% endcapture %}
                        <div class=""rockbadge rockbadge-icon rockbadge-step""
                            style=""color:{{ backgroundCheckStatusColor }}""
                            data-toggle=""tooltip""
                            data-html=""true""
                            data-original-title=""{{ backgroundCheckTooltip }}"">
                                <i class=""badge-icon {{backgroundCheckStepType.IconCssClass | Default: 'ti ti-shield'}}""></i>
                        </div>
                    {% endif %}
                {% endfor %}
                " );
        }

        /// <summary>
        /// Reverts the migration.
        /// </summary>
        public override void Down()
        {
        }
    }
}