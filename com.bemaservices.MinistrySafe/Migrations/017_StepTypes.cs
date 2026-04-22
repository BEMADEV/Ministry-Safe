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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.bemaservices.MinistrySafe.Constants;
using Rock;
using Rock.Model;
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class RequestLauncher.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 17, "1.16.0" )]
    public partial class StepTypes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            AddStepProgram();
            AddNewWorkflow();
            UpdateBadge();
            AddStepUpdateJob();
            MigrateExistingTrainings();

        }

        private void MigrateExistingTrainings()
        {
            Sql( $@"
            IF NOT EXISTS (
                SELECT 1
                FROM [ServiceJob]
                WHERE [Guid] = 'B40B86F2-BBB6-4F56-8F53-CE587460EDF3'
            )
            BEGIN
                INSERT INTO [ServiceJob] (
                      [IsSystem]
                    , [IsActive]
                    , [Name]
                    , [Description]
                    , [Class]
                    , [CronExpression]
                    , [NotificationStatus]
                    , [Guid]
                ) VALUES (
                      1
                    , 1
                    , 'MinistrySafe Update Helper - Migrate Existing Trainings.'
                    , 'This job migrates existing trainings to the steps area.'
                    , 'com.bemaservices.MinistrySafe.Jobs.MigrateExistingTrainings'
                    , '0 0/5 * 1/1 * ? *'
                    , 1
                    , 'B40B86F2-BBB6-4F56-8F53-CE587460EDF3'
                );
            END" );
        }

        private void AddStepUpdateJob()
        {
            // add ServiceJob: Update Steps
            // Code Generated using Rock\Dev Tools\Sql\CodeGen_ServiceJobWithAttributes_ForAJob.sql
            Sql( @"IF NOT EXISTS( SELECT [Id] FROM [ServiceJob] WHERE [Class] = 'com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses' AND [Guid] = '01D4C579-13CC-4734-A99C-174C05D8DF71' )
            BEGIN
               INSERT INTO [ServiceJob] (
                  [IsSystem]
                  ,[IsActive]
                  ,[Name]
                  ,[Description]
                  ,[Class]
                  ,[CronExpression]
                  ,[NotificationStatus]
                  ,[Guid] )
               VALUES ( 
                  0
                  ,1
                  ,'Update Steps'
                  ,''
                  ,'com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses'
                  ,'0 0 5 1/1 * ? *'
                  ,1
                  ,'01D4C579-13CC-4734-A99C-174C05D8DF71'
                  );
            END" );
            // Attribute: com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses: Days Until Expiring
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Days Until Expiring", "Days Until Expiring", @"The number of days after completion when a step should be marked as Expiring.", 0, @"700", "4499C1EE-EAC5-4818-9557-3B1478F8A4D6", "DaysUntilExpiring" );
            // Attribute: com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses: Days Until Expired
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ServiceJob", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Class", "com.bemaservices.MinistrySafe.Jobs.UpdateStepStatuses", "Days Until Expired", "Days Until Expired", @"The number of days after completion when a step should be marked as Expired.", 1, @"730", "266B1A12-468D-4EA5-B29A-5377891659ED", "DaysUntilExpired" );

            var serviceJobId = SqlScalar( "Select Top 1 Id From ServiceJob Where [Guid] = '01D4C579-13CC-4734-A99C-174C05D8DF71'" ).ToStringSafe().AsInteger();
            RockMigrationHelper.AddAttributeValue( "4499C1EE-EAC5-4818-9557-3B1478F8A4D6", serviceJobId, @"700", "4499C1EE-EAC5-4818-9557-3B1478F8A4D6" ); // Update Steps: Days Until Expiring
            RockMigrationHelper.AddAttributeValue( "266B1A12-468D-4EA5-B29A-5377891659ED", serviceJobId, @"730", "266B1A12-468D-4EA5-B29A-5377891659ED" ); // Update Steps: Days Until Expired
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
                " );

        }

        private void AddNewWorkflow()
        {
            #region FieldTypes

            RockMigrationHelper.UpdateFieldType( "Adaptive Message", "", "Rock", "Rock.Field.Types.AdaptiveMessageFieldType", "55CC533A-99D2-418F-B93E-73EA098ABDAE" );

            #endregion

            #region EntityTypes

            RockMigrationHelper.UpdateEntityType( "Rock.Model.Workflow", "3540E9A7-FE30-43A9-8B0A-A372B63DFC93", true, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActivity", "2CB52ED0-CB06-4D62-9E2C-73B60AFA4C9F", true, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowActionType", "23E3273A-B137-48A3-9AFF-C8DC832DDCA6", true, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.ActivateActivity", "38907A90-1634-4A93-8017-619326A4A582", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.ActivateWorkflow", "9E3C42B5-792A-4694-8ACE-B84E5E87C800", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.CompleteWorkflow", "EEDA4318-F014-4A46-9C76-4C052EF81AA1", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.PersistWorkflow", "F1A39347-6FE0-43D4-89FB-544195088ECF", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.RunLava", "BC21E57A-1477-44B3-A7C2-61A806118945", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.SetAttributeFromEntity", "972F19B9-598B-474B-97A4-50E56E7B59D2", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.SetAttributeToCurrentPerson", "24B7D5E6-C30F-48F4-9D7E-AF45A342CF3A", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.SetAttributeValue", "C789E457-0783-44B3-9D8F-2EBAB5F11110", false, true );
            RockMigrationHelper.UpdateEntityType( "Rock.Workflow.Action.UserEntryForm", "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", false, true );
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24B7D5E6-C30F-48F4-9D7E-AF45A342CF3A", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "DE9CB292-4785-4EA3-976D-3826F91E9E98" ); // Rock.Workflow.Action.SetAttributeToCurrentPerson:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24B7D5E6-C30F-48F4-9D7E-AF45A342CF3A", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Person Attribute", "PersonAttribute", "The attribute to set to the currently logged in person.", 0, @"", "BBED8A83-8BB2-4D35-BAFB-05F67DCAD112" ); // Rock.Workflow.Action.SetAttributeToCurrentPerson:Person Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "24B7D5E6-C30F-48F4-9D7E-AF45A342CF3A", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "89E9BCED-91AB-47B0-AD52-D78B0B7CB9E8" ); // Rock.Workflow.Action.SetAttributeToCurrentPerson:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "E8ABD802-372C-47BE-82B1-96F50DB5169E" ); // Rock.Workflow.Action.ActivateActivity:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "739FD425-5B8C-4605-B775-7E4D9D4C11DB", "Activity", "Activity", "The activity type to activate", 0, @"", "02D5A7A5-8781-46B4-B9FC-AF816829D240" ); // Rock.Workflow.Action.ActivateActivity:Activity
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "38907A90-1634-4A93-8017-619326A4A582", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "3809A78C-B773-440C-8E3F-A8E81D0DAE08" ); // Rock.Workflow.Action.ActivateActivity:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "234910F2-A0DB-4D7D-BAF7-83C880EF30AE" ); // Rock.Workflow.Action.UserEntryForm:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "C178113D-7C86-4229-8424-C6D0CF4A7E23" ); // Rock.Workflow.Action.UserEntryForm:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Lava Template", "LavaTemplate", "By default this action will set the attribute value equal to the guid (or id) of the entity that was passed in for processing. If you include a lava template here, the action will instead set the attribute value to the output of this template. The mergefield to use for the entity is 'Entity.' For example, use {{ Entity.Name }} if the entity has a Name property. <span class='tip tip-lava'></span>", 4, @"", "7D79FC31-D0ED-4DB0-AB7D-60F4F98A1199" ); // Rock.Workflow.Action.SetAttributeFromEntity:Lava Template
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "9392E3D7-A28B-4CD8-8B03-5E147B102EF1" ); // Rock.Workflow.Action.SetAttributeFromEntity:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Entity Is Required", "EntityIsRequired", "Should an error be returned if the entity is missing or not a valid entity type?", 2, @"True", "B524B00C-29CB-49E9-9896-8BB60F209783" ); // Rock.Workflow.Action.SetAttributeFromEntity:Entity Is Required
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Use Id instead of Guid", "UseId", "Most entity attribute field types expect the Guid of the entity (which is used by default). Select this option if the entity's Id should be used instead (should be rare).", 3, @"False", "1246C53A-FD92-4E08-ABDE-9A6C37E70C7B" ); // Rock.Workflow.Action.SetAttributeFromEntity:Use Id instead of Guid
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Attribute", "Attribute", "The attribute to set the value of.", 1, @"", "61E6E1BC-E657-4F00-B2E9-769AAA25B9F7" ); // Rock.Workflow.Action.SetAttributeFromEntity:Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "972F19B9-598B-474B-97A4-50E56E7B59D2", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "AD4EFAC4-E687-43DF-832F-0DC3856ABABB" ); // Rock.Workflow.Action.SetAttributeFromEntity:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "B00F5144-1E44-4A1C-9C27-8C30DEEC7B70" ); // Rock.Workflow.Action.ActivateWorkflow:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Workflow Attribute", "WorkflowAttribute", "The attribute to hold the new activated workflow. ", 5, @"", "AEF2316E-0136-4634-9ED3-F2309D08036C" ); // Rock.Workflow.Action.ActivateWorkflow:Workflow Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Workflow Type from Attribute", "WorkflowTypefromAttribute", "The workflow type to activate. Either this or Workflow Type must be set.", 3, @"", "8725F9F8-4B1B-4D80-AEAA-B3C6FBB1D1C4" ); // Rock.Workflow.Action.ActivateWorkflow:Workflow Type from Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "The workflow type to activate.  To set the Workflow Type from an Attribute, leave this blank and set Workflow Type from Attribute.", 2, @"", "B2009E3F-02ED-4F7E-9099-5C403653BAFB" ); // Rock.Workflow.Action.ActivateWorkflow:Workflow Type
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "73B02051-0D38-4AD9-BF81-A2D477DE4F70", "Workflow Attribute Key", "WorkflowAttributeKey", "Used to match the current workflow's attribute keys to the keys of the new workflow. The new workflow will inherit the attribute values of the keys provided.", 4, @"", "A4AAAC2A-070B-4EA7-9331-EF7859DCB5DC" ); // Rock.Workflow.Action.ActivateWorkflow:Workflow Attribute Key
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Name", "WorkflowName", "The name of your new workflow", 1, @"", "3322E917-D124-41D1-A941-B27DA247C175" ); // Rock.Workflow.Action.ActivateWorkflow:Workflow Name
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "9E3C42B5-792A-4694-8ACE-B84E5E87C800", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "86DEF4D0-0D2B-4E9F-BF6F-976DE63FAF1E" ); // Rock.Workflow.Action.ActivateWorkflow:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "BC21E57A-1477-44B3-A7C2-61A806118945", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Lava", "Value", "The <span class='tip tip-lava'></span> to run.", 0, @"", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4" ); // Rock.Workflow.Action.RunLava:Lava
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "BC21E57A-1477-44B3-A7C2-61A806118945", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "F1924BDC-9B79-4018-9D4A-C3516C87A514" ); // Rock.Workflow.Action.RunLava:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "BC21E57A-1477-44B3-A7C2-61A806118945", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Attribute", "Attribute", "The attribute to store the result in.", 1, @"", "431273C6-342D-4030-ADC7-7CDEDC7F8B27" ); // Rock.Workflow.Action.RunLava:Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "BC21E57A-1477-44B3-A7C2-61A806118945", "4BD9088F-5CC6-89B1-45FC-A2AAFFC7CC0D", "Enabled Lava Commands", "EnabledLavaCommands", "The Lava commands that should be enabled for this action.", 2, @"", "F3E380BF-AAC8-4015-9ADC-0DF56B5462F5" ); // Rock.Workflow.Action.RunLava:Enabled Lava Commands
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "BC21E57A-1477-44B3-A7C2-61A806118945", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "1B833F48-EFC2-4537-B1E3-7793F6863EAA" ); // Rock.Workflow.Action.RunLava:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "C789E457-0783-44B3-9D8F-2EBAB5F11110", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "D7EAA859-F500-4521-9523-488B12EAA7D2" ); // Rock.Workflow.Action.SetAttributeValue:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "C789E457-0783-44B3-9D8F-2EBAB5F11110", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Attribute", "Attribute", "The attribute to set the value of.", 0, @"", "44A0B977-4730-4519-8FF6-B0A01A95B212" ); // Rock.Workflow.Action.SetAttributeValue:Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "C789E457-0783-44B3-9D8F-2EBAB5F11110", "3B1D93D7-9414-48F9-80E5-6A3FC8F94C20", "Text Value|Attribute Value", "Value", "The text or attribute to set the value from. <span class='tip tip-lava'></span>", 1, @"", "E5272B11-A2B8-49DC-860D-8D574E2BC15C" ); // Rock.Workflow.Action.SetAttributeValue:Text Value|Attribute Value
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "C789E457-0783-44B3-9D8F-2EBAB5F11110", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "57093B41-50ED-48E5-B72B-8829E62704C8" ); // Rock.Workflow.Action.SetAttributeValue:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "EEDA4318-F014-4A46-9C76-4C052EF81AA1", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C" ); // Rock.Workflow.Action.CompleteWorkflow:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "EEDA4318-F014-4A46-9C76-4C052EF81AA1", "3B1D93D7-9414-48F9-80E5-6A3FC8F94C20", "Status|Status Attribute", "Status", "The status to set the workflow to when marking the workflow complete. <span class='tip tip-lava'></span>", 0, @"Completed", "385A255B-9F48-4625-862B-26231DBAC53A" ); // Rock.Workflow.Action.CompleteWorkflow:Status|Status Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "EEDA4318-F014-4A46-9C76-4C052EF81AA1", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "25CAD4BE-5A00-409D-9BAB-E32518D89956" ); // Rock.Workflow.Action.CompleteWorkflow:Order
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "50B01639-4938-40D2-A791-AA0EB4F86847" ); // Rock.Workflow.Action.PersistWorkflow:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Persist Immediately", "PersistImmediately", "This action will normally cause the workflow to be persisted (saved) once all the current activities/actions have completed processing. Set this flag to true, if the workflow should be persisted immediately. This is only required if a subsequent action needs a persisted workflow with a valid id.", 0, @"False", "82744A46-0110-4728-BD3D-66C85C5FCB2F" ); // Rock.Workflow.Action.PersistWorkflow:Persist Immediately
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "F1A39347-6FE0-43D4-89FB-544195088ECF", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "86F795B0-0CB6-4DA4-9CE4-B11D0922F361" ); // Rock.Workflow.Action.PersistWorkflow:Order

            #endregion

            #region Categories

            RockMigrationHelper.UpdateCategory( "C9F3C4A5-1526-474D-803F-D6C7A45CBBAE", "Safety & Security", "fa fa-medkit", "", "6F8A431C-BEBD-4D33-AAD6-1D70870329C2", 0 ); // Safety & Security

            #endregion

            #region MinistrySafe Request Launcher

            RockMigrationHelper.UpdateWorkflowType( false, true, "MinistrySafe Request Launcher", "", "6F8A431C-BEBD-4D33-AAD6-1D70870329C2", "Work", "fa fa-list-ol", 28800, true, 0, "2F932123-F1BC-4841-A6C0-DE5238A11272", 0 ); // MinistrySafe Request Launcher
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "E4EAB7B2-0B76-429B-AFE4-AD86D7428C70", "Requester", "Requester", "", 0, @"", "C78CA83A-A75F-4350-80BA-CCEFFD047674", false ); // MinistrySafe Request Launcher:Requester
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "E4EAB7B2-0B76-429B-AFE4-AD86D7428C70", "Person", "Person", "", 1, @"", "6C45C0C2-CE20-41D6-8946-DF145E485751", false ); // MinistrySafe Request Launcher:Person
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Background Check Types", "BackgroundCheckTypes", "", 2, @"", "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", false ); // MinistrySafe Request Launcher:Background Check Types
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Trainings", "Trainings", "", 3, @"", "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", false ); // MinistrySafe Request Launcher:Trainings
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Will the applicant be serving with children?", "ChildServing", "", 4, @"False", "6C4FEC7F-2EDC-4E5F-8285-AD20E671DA19", false ); // MinistrySafe Request Launcher:Will the applicant be serving with children?
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Is the applicant over 13 years of age?", "AgeOver13", "", 5, @"False", "6765ECDB-795D-40D5-B70D-47A7F5991A5D", false ); // MinistrySafe Request Launcher:Is the applicant over 13 years of age?
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Employee Type", "EmployeeType", "", 6, @"", "3123793C-2679-4049-87AF-836EFBC39307", false ); // MinistrySafe Request Launcher:Employee Type
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "What is the salary range or the applicant?", "SalaryRange", "", 7, @"", "047569CD-A974-41E2-9505-F91648D6C07C", false ); // MinistrySafe Request Launcher:What is the salary range or the applicant?
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Background Type Index", "BackgroundTypeIndex", "", 8, @"", "42FD7BF6-C45A-4B91-B4E7-6CF6C04C1E25", false ); // MinistrySafe Request Launcher:Background Type Index
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Training Index", "TrainingIndex", "", 9, @"", "521658C7-9442-48C1-90AD-306031A3051A", false ); // MinistrySafe Request Launcher:Training Index
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "1B71FEF4-201F-4D53-8C60-2DF21F1985ED", "Campus", "Campus1", "", 10, @"", "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", false ); // MinistrySafe Request Launcher:Campus
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "C28C7BF3-A552-4D77-9408-DEDCF760CED0", "Reason", "Reason", "", 11, @"", "CD395462-9C89-4E6F-A522-682546948AC4", false ); // MinistrySafe Request Launcher:Reason
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Package Type", "PackageType", "Used by the Activate Workflow action.", 12, @"", "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", false ); // MinistrySafe Request Launcher:Package Type
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Survey Type", "SurveyType", "Used by the Activate Workflow action.", 13, @"", "C1A6FF40-AED5-423C-891E-43FBAEACC136", false ); // MinistrySafe Request Launcher:Survey Type
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Skip Initial Entry", "SkipInitialEntry", "Used by the Activate Workflow action.", 14, @"True", "B82E2FFA-3856-4C5A-8EF8-4D9E4BF0C3A4", false ); // MinistrySafe Request Launcher:Skip Initial Entry
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Training User Type", "UserType", "", 15, @"", "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", false ); // MinistrySafe Request Launcher:Training User Type
            RockMigrationHelper.UpdateWorkflowTypeAttribute( "2F932123-F1BC-4841-A6C0-DE5238A11272", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Person Id", "PersonId", "", 16, @"", "6C8C1786-EB9E-443D-BB72-F77CFB732C9B", false ); // MinistrySafe Request Launcher:Person Id
            RockMigrationHelper.AddAttributeQualifier( "C78CA83A-A75F-4350-80BA-CCEFFD047674", "EnableSelfSelection", @"False", "DC15BAC3-D5E0-461D-98E0-77692EC2AE56" ); // MinistrySafe Request Launcher:Requester:EnableSelfSelection
            RockMigrationHelper.AddAttributeQualifier( "6C45C0C2-CE20-41D6-8946-DF145E485751", "EnableSelfSelection", @"False", "B85B0EEF-335A-40FC-B5DE-5E60E40B14D0" ); // MinistrySafe Request Launcher:Person:EnableSelfSelection
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "AllowAddingNewValues", @"False", "4A0CC333-0C6B-4F6A-B5B6-92FFDF2042C0" ); // MinistrySafe Request Launcher:Background Check Types:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "allowmultiple", @"True", "61F663AF-DFFA-43BD-97DF-5729BDF50362" ); // MinistrySafe Request Launcher:Background Check Types:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "definedtypeguid", @"BC2FDF9A-93B8-4325-8DE9-2F7B1943BFDF", "2DCEB4C5-E975-446F-B0FE-5964B0F84A16" ); // MinistrySafe Request Launcher:Background Check Types:definedtype
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "displaydescription", @"False", "43A67565-467D-4072-88FD-17F7969DB9BF" ); // MinistrySafe Request Launcher:Background Check Types:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "enhancedselection", @"True", "A6A804C1-328B-4E8B-AA77-45FAC23F5B75" ); // MinistrySafe Request Launcher:Background Check Types:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "includeInactive", @"False", "874DDF21-3FD2-4584-BA02-E9B7A093378C" ); // MinistrySafe Request Launcher:Background Check Types:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "RepeatColumns", @"", "ABD0AA5D-FFD4-4418-A29C-E1C6AC0197D4" ); // MinistrySafe Request Launcher:Background Check Types:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", "SelectableDefinedValuesId", @"", "A942B8F3-95CE-4A46-BB18-9F9B25869DB6" ); // MinistrySafe Request Launcher:Background Check Types:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "AllowAddingNewValues", @"False", "8452BE9A-92B5-4066-B503-1EFC6E67FE1E" ); // MinistrySafe Request Launcher:Trainings:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "allowmultiple", @"True", "787B88F9-998E-4804-B2B1-B62365B8C211" ); // MinistrySafe Request Launcher:Trainings:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "definedtypeguid", @"95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "ECE40DE1-FB57-47D4-BFE7-F875ACEFE3CB" ); // MinistrySafe Request Launcher:Trainings:definedtype
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "displaydescription", @"False", "9F879C77-8960-4D70-B849-BF0A25BFD191" ); // MinistrySafe Request Launcher:Trainings:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "enhancedselection", @"True", "27363278-6702-4BE2-867A-7E8753D404E5" ); // MinistrySafe Request Launcher:Trainings:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "includeInactive", @"False", "B0EBA221-0717-41BB-983A-987C5FE336DD" ); // MinistrySafe Request Launcher:Trainings:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "RepeatColumns", @"", "DDC91552-9904-4E73-B0B1-21F27F8AA57B" ); // MinistrySafe Request Launcher:Trainings:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "SelectableDefinedValuesId", @"", "67F60D3C-A270-4C63-B804-661EB1406600" ); // MinistrySafe Request Launcher:Trainings:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "6C4FEC7F-2EDC-4E5F-8285-AD20E671DA19", "BooleanControlType", @"2", "4EEA3C48-9CFE-42AB-875D-01557A9FD47A" ); // MinistrySafe Request Launcher:Will the applicant be serving with children?:BooleanControlType
            RockMigrationHelper.AddAttributeQualifier( "6C4FEC7F-2EDC-4E5F-8285-AD20E671DA19", "falsetext", @"No", "F41388A1-1C73-46DA-971E-A5E1EB9B75A9" ); // MinistrySafe Request Launcher:Will the applicant be serving with children?:falsetext
            RockMigrationHelper.AddAttributeQualifier( "6C4FEC7F-2EDC-4E5F-8285-AD20E671DA19", "truetext", @"Yes", "E9A66472-BAD4-4A1A-ADA4-FF625928D38E" ); // MinistrySafe Request Launcher:Will the applicant be serving with children?:truetext
            RockMigrationHelper.AddAttributeQualifier( "6765ECDB-795D-40D5-B70D-47A7F5991A5D", "BooleanControlType", @"2", "F28ED86A-0141-43D0-B718-E26AB9836B25" ); // MinistrySafe Request Launcher:Is the applicant over 13 years of age?:BooleanControlType
            RockMigrationHelper.AddAttributeQualifier( "6765ECDB-795D-40D5-B70D-47A7F5991A5D", "falsetext", @"No", "16028628-4442-4311-B5B2-708F65228285" ); // MinistrySafe Request Launcher:Is the applicant over 13 years of age?:falsetext
            RockMigrationHelper.AddAttributeQualifier( "6765ECDB-795D-40D5-B70D-47A7F5991A5D", "truetext", @"Yes", "EE4407C6-A12F-445B-AD39-7BAA48CAA5BA" ); // MinistrySafe Request Launcher:Is the applicant over 13 years of age?:truetext
            RockMigrationHelper.AddAttributeQualifier( "3123793C-2679-4049-87AF-836EFBC39307", "fieldtype", @"rb", "ABCFE7B7-0695-4BE1-B717-53BA67411130" ); // MinistrySafe Request Launcher:Employee Type:fieldtype
            RockMigrationHelper.AddAttributeQualifier( "3123793C-2679-4049-87AF-836EFBC39307", "repeatColumns", @"", "CCCC8F9A-F60A-4270-B771-E112160054B6" ); // MinistrySafe Request Launcher:Employee Type:repeatColumns
            RockMigrationHelper.AddAttributeQualifier( "3123793C-2679-4049-87AF-836EFBC39307", "values", @"current^Current,prospective^Prospective", "46734131-2FEC-48D3-AE7D-348C6AA991FA" ); // MinistrySafe Request Launcher:Employee Type:values
            RockMigrationHelper.AddAttributeQualifier( "047569CD-A974-41E2-9505-F91648D6C07C", "fieldtype", @"rb", "B66A9FF6-9C13-4341-8735-91933301ED6C" ); // MinistrySafe Request Launcher:What is the salary range or the applicant?:fieldtype
            RockMigrationHelper.AddAttributeQualifier( "047569CD-A974-41E2-9505-F91648D6C07C", "repeatColumns", @"", "205F480F-D208-400B-90B7-209DF9C5F443" ); // MinistrySafe Request Launcher:What is the salary range or the applicant?:repeatColumns
            RockMigrationHelper.AddAttributeQualifier( "047569CD-A974-41E2-9505-F91648D6C07C", "values", @"under_20k^Under $20K, 20k_25k^$20-25K, 25k_75k^$25-75K, 75k_plus^$75K+", "926FAB02-649F-4310-A4EF-A46136F36F41" ); // MinistrySafe Request Launcher:What is the salary range or the applicant?:values
            RockMigrationHelper.AddAttributeQualifier( "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", "filterCampusStatus", @"", "F3C4E859-33C7-48C3-AC9B-7F7BCD00656E" ); // MinistrySafe Request Launcher:Campus:filterCampusStatus
            RockMigrationHelper.AddAttributeQualifier( "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", "filterCampusTypes", @"", "3457CAC6-F9BF-4DEB-B3CD-F733C90E8AE7" ); // MinistrySafe Request Launcher:Campus:filterCampusTypes
            RockMigrationHelper.AddAttributeQualifier( "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", "includeInactive", @"False", "DB69F68A-1224-4885-98C9-837E6782D188" ); // MinistrySafe Request Launcher:Campus:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", "SelectableCampusIds", @"", "236F2EE6-269A-4878-96E2-2E3F5457AC05" ); // MinistrySafe Request Launcher:Campus:SelectableCampusIds
            RockMigrationHelper.AddAttributeQualifier( "CD395462-9C89-4E6F-A522-682546948AC4", "allowhtml", @"False", "63ED411E-3A55-4FF9-82C1-DF5E99F42E89" ); // MinistrySafe Request Launcher:Reason:allowhtml
            RockMigrationHelper.AddAttributeQualifier( "CD395462-9C89-4E6F-A522-682546948AC4", "maxcharacters", @"", "BB925F6F-9CA7-4594-9A03-6091DE4F6485" ); // MinistrySafe Request Launcher:Reason:maxcharacters
            RockMigrationHelper.AddAttributeQualifier( "CD395462-9C89-4E6F-A522-682546948AC4", "numberofrows", @"3", "237BBD36-738B-4127-9CB0-58912586ED90" ); // MinistrySafe Request Launcher:Reason:numberofrows
            RockMigrationHelper.AddAttributeQualifier( "CD395462-9C89-4E6F-A522-682546948AC4", "showcountdown", @"False", "95D9D374-7EF8-4D05-968D-73BB6D60EA92" ); // MinistrySafe Request Launcher:Reason:showcountdown
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "AllowAddingNewValues", @"False", "DBCC8505-4771-47B6-B140-DEE89385D006" ); // MinistrySafe Request Launcher:Package Type:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "allowmultiple", @"False", "ED542F0E-5EAC-407E-BF91-C58796FE6433" ); // MinistrySafe Request Launcher:Package Type:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "definedtypeguid", @"BC2FDF9A-93B8-4325-8DE9-2F7B1943BFDF", "16CD6A18-264C-4171-B538-141A296B1E50" ); // MinistrySafe Request Launcher:Package Type:definedtype
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "displaydescription", @"False", "3AD114BB-54A7-48B6-A624-06E870E487F7" ); // MinistrySafe Request Launcher:Package Type:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "enhancedselection", @"False", "FE7655AC-64B4-45D4-A2D6-2B2487380CC4" ); // MinistrySafe Request Launcher:Package Type:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "includeInactive", @"False", "03E5C65D-8A77-4CE1-8028-D03557627163" ); // MinistrySafe Request Launcher:Package Type:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "RepeatColumns", @"", "309BBC32-FC39-491D-8ED5-FB67C3DBA94C" ); // MinistrySafe Request Launcher:Package Type:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", "SelectableDefinedValuesId", @"", "5AD239F1-49A0-40F1-8F46-0F181CEF82CB" ); // MinistrySafe Request Launcher:Package Type:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "AllowAddingNewValues", @"False", "CF974604-81FA-4EAB-AEEB-DE93F5F6C977" ); // MinistrySafe Request Launcher:Survey Type:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "allowmultiple", @"False", "CF62D639-1CD5-47F3-819D-F72962512B1C" ); // MinistrySafe Request Launcher:Survey Type:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "definedtypeguid", @"95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "7C6390BF-A38E-4ACA-B349-86C60FA480A8" ); // MinistrySafe Request Launcher:Survey Type:definedtype
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "displaydescription", @"False", "67106497-5E7A-41A1-951C-1C29214CF604" ); // MinistrySafe Request Launcher:Survey Type:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "enhancedselection", @"False", "02ABAA50-4888-448E-9F2B-81B277802105" ); // MinistrySafe Request Launcher:Survey Type:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "includeInactive", @"False", "D0B767E4-F7FA-4A72-BB1B-D2F741DF2DD0" ); // MinistrySafe Request Launcher:Survey Type:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "RepeatColumns", @"", "2E35A520-E443-4C81-B986-417227E88F76" ); // MinistrySafe Request Launcher:Survey Type:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "C1A6FF40-AED5-423C-891E-43FBAEACC136", "SelectableDefinedValuesId", @"", "2AC647E2-2452-4F4A-925C-2798B78C9756" ); // MinistrySafe Request Launcher:Survey Type:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "B82E2FFA-3856-4C5A-8EF8-4D9E4BF0C3A4", "ispassword", @"False", "3D049A48-E868-4C82-AB80-7C97C6E42B4B" ); // MinistrySafe Request Launcher:Skip Initial Entry:ispassword
            RockMigrationHelper.AddAttributeQualifier( "B82E2FFA-3856-4C5A-8EF8-4D9E4BF0C3A4", "maxcharacters", @"", "8D48A8A2-4362-44C6-A25B-D9C10B755BA3" ); // MinistrySafe Request Launcher:Skip Initial Entry:maxcharacters
            RockMigrationHelper.AddAttributeQualifier( "B82E2FFA-3856-4C5A-8EF8-4D9E4BF0C3A4", "showcountdown", @"False", "2BA192EB-ED61-480A-97AC-60ADB697AD57" ); // MinistrySafe Request Launcher:Skip Initial Entry:showcountdown
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "AllowAddingNewValues", @"False", "9D4E6714-FEA1-414C-96F6-4453C29900BE" ); // MinistrySafe Request Launcher:Training User Type:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "allowmultiple", @"False", "45990F4F-B578-4139-9B18-B569C78AB67A" ); // MinistrySafe Request Launcher:Training User Type:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "definedtypeguid", @"559E79C6-2EAB-4A0D-A16F-59D9B63F002F", "905949B4-21DA-44A9-9FE0-30939B9591EB" ); // MinistrySafe Request Launcher:Training User Type:definedtype
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "displaydescription", @"True", "2B995EEB-BC35-4156-B15A-0431CCD78991" ); // MinistrySafe Request Launcher:Training User Type:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "enhancedselection", @"False", "6AF7333C-B7AB-4410-964F-6D03D36A5429" ); // MinistrySafe Request Launcher:Training User Type:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "includeInactive", @"False", "103277E3-0F37-44CA-910D-546DAE1F9E9D" ); // MinistrySafe Request Launcher:Training User Type:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "RepeatColumns", @"", "7DA48890-03DD-46B6-9096-07BE8BB92E4D" ); // MinistrySafe Request Launcher:Training User Type:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", "SelectableDefinedValuesId", @"", "E1225BCA-27DB-46CF-8731-CC919295572F" ); // MinistrySafe Request Launcher:Training User Type:SelectableDefinedValuesId
            RockMigrationHelper.UpdateWorkflowActivityType( "2F932123-F1BC-4841-A6C0-DE5238A11272", true, "Start", "", true, 0, "0D56A890-186B-473B-8E2C-59AAEE937AE2" ); // MinistrySafe Request Launcher:Start
            RockMigrationHelper.UpdateWorkflowActivityType( "2F932123-F1BC-4841-A6C0-DE5238A11272", true, "Background Check Loop", "", false, 1, "7C2EE748-9537-4F07-9CCF-DF928DA11174" ); // MinistrySafe Request Launcher:Background Check Loop
            RockMigrationHelper.UpdateWorkflowActivityType( "2F932123-F1BC-4841-A6C0-DE5238A11272", true, "Training Loop", "", false, 2, "211678E4-EDA1-4647-96D4-1958B118867D" ); // MinistrySafe Request Launcher:Training Loop
            RockMigrationHelper.UpdateWorkflowActivityType( "2F932123-F1BC-4841-A6C0-DE5238A11272", true, "Complete Workflow", "", false, 3, "87769844-3E8D-4AD3-9923-68302CEA734B" ); // MinistrySafe Request Launcher:Complete Workflow
            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Background Check Type", "BackgroundCheckType", "", 0, @"", "A073EDF3-C7B9-4BA6-8589-3087A61C2D24" ); // MinistrySafe Request Launcher:Background Check Loop:Background Check Type
            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "0F72D8FD-983F-41FB-A7A3-E6403EB04EDB", "Launched Workflow", "LaunchedWorkflow", "", 1, @"", "8D26F1D9-3519-4978-BF12-C81D524AB4BC" ); // MinistrySafe Request Launcher:Background Check Loop:Launched Workflow
            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "211678E4-EDA1-4647-96D4-1958B118867D", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Training Type", "TrainingType", "", 0, @"", "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E" ); // MinistrySafe Request Launcher:Training Loop:Training Type
            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "211678E4-EDA1-4647-96D4-1958B118867D", "0F72D8FD-983F-41FB-A7A3-E6403EB04EDB", "Launched Workflow", "LaunchedWorkflow", "", 1, @"", "8ACD9463-FD35-4561-8F58-DDF255745443" ); // MinistrySafe Request Launcher:Training Loop:Launched Workflow
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "AllowAddingNewValues", @"False", "D071BEBA-6F9C-40E2-A33B-333A723343DB" ); // MinistrySafe Request Launcher:Background Check Type:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "allowmultiple", @"False", "60622681-0627-46B5-A20C-A8BA3C96F762" ); // MinistrySafe Request Launcher:Background Check Type:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "definedtype", @"59", "E905362D-62BF-4AD4-97A1-B1A512F0395E" ); // MinistrySafe Request Launcher:Background Check Type:definedtype
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "displaydescription", @"False", "EA850AFC-ADA0-4D03-B083-CB3B51939C55" ); // MinistrySafe Request Launcher:Background Check Type:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "enhancedselection", @"False", "BCAEC8A2-44EE-47EF-8736-0BB2BD754456" ); // MinistrySafe Request Launcher:Background Check Type:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "includeInactive", @"False", "AECE145C-603B-4368-B803-6324C0CC3D1E" ); // MinistrySafe Request Launcher:Background Check Type:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "RepeatColumns", @"", "2464D116-800D-49C2-B390-0F5107F227B8" ); // MinistrySafe Request Launcher:Background Check Type:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", "SelectableDefinedValuesId", @"", "50455E8F-909E-4DB9-B71F-B853BEA7C32B" ); // MinistrySafe Request Launcher:Background Check Type:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "8D26F1D9-3519-4978-BF12-C81D524AB4BC", "workflowtype", @"", "B14BE028-9A71-4052-AB14-114463EB4821" ); // MinistrySafe Request Launcher:Launched Workflow:workflowtype
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "AllowAddingNewValues", @"False", "FCFDBA2E-F034-4A06-A1CF-F51BC6B10058" ); // MinistrySafe Request Launcher:Training Type:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "allowmultiple", @"False", "0D5BC24B-A620-4722-A155-749EB00CBFAF" ); // MinistrySafe Request Launcher:Training Type:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "definedtype", @"115", "CE32651A-4A8B-404B-8A6C-5ABD50689AD0" ); // MinistrySafe Request Launcher:Training Type:definedtype
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "displaydescription", @"False", "522F294D-5DB5-4158-B6F3-290F89BF31A8" ); // MinistrySafe Request Launcher:Training Type:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "enhancedselection", @"False", "4876D49A-39E7-4765-B5A0-FBE3702F2D80" ); // MinistrySafe Request Launcher:Training Type:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "includeInactive", @"False", "5352884F-965A-4235-B61D-9EE12C793EEC" ); // MinistrySafe Request Launcher:Training Type:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "RepeatColumns", @"", "D6066915-CC0B-43DE-A10F-608752E4CDFE" ); // MinistrySafe Request Launcher:Training Type:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", "SelectableDefinedValuesId", @"", "BE92C97D-5C9C-410C-8EE5-7C417BD47CF7" ); // MinistrySafe Request Launcher:Training Type:SelectableDefinedValuesId
            RockMigrationHelper.AddAttributeQualifier( "8ACD9463-FD35-4561-8F58-DDF255745443", "workflowtype", @"", "DFACB496-D1F4-487C-90C1-6F3FBFC1010C" ); // MinistrySafe Request Launcher:Launched Workflow:workflowtype
            RockMigrationHelper.UpdateWorkflowActionForm( @"", @"", "Submit^^^Your information has been submitted successfully.", "", true, "", "A8603959-669A-4F08-930D-C333536389B8" ); // MinistrySafe Request Launcher:Start:Initial Entry
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "C78CA83A-A75F-4350-80BA-CCEFFD047674", 11, false, true, false, false, @"", @"", "A4361B09-81CF-4949-9A7F-891D6CA17729" ); // MinistrySafe Request Launcher:Start:Initial Entry:Requester
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "6C45C0C2-CE20-41D6-8946-DF145E485751", 0, true, false, true, false, @"", @"", "5168F339-1D9E-4B2D-9181-DA4462D36375" ); // MinistrySafe Request Launcher:Start:Initial Entry:Person
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "D1C4DE7F-6719-45B0-A8AD-D1D344F42D94", 1, true, false, false, false, @"", @"", "12C4987A-C3E5-4977-96A2-410DF621337E" ); // MinistrySafe Request Launcher:Start:Initial Entry:Background Check Types
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", 7, true, false, false, false, @"", @"", "180036DE-A097-4E59-BD10-A85A3BDCD43F" ); // MinistrySafe Request Launcher:Start:Initial Entry:Trainings
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "6C4FEC7F-2EDC-4E5F-8285-AD20E671DA19", 2, true, false, true, false, @"", @"", "6517041F-63DD-4144-8CC0-9800CBCB7F27" ); // MinistrySafe Request Launcher:Start:Initial Entry:Will the applicant be serving with children?
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "6765ECDB-795D-40D5-B70D-47A7F5991A5D", 3, true, false, true, false, @"", @"", "01E55154-8DD5-4EE6-8FF6-B72BA94504FD" ); // MinistrySafe Request Launcher:Start:Initial Entry:Is the applicant over 13 years of age?
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "3123793C-2679-4049-87AF-836EFBC39307", 5, true, false, false, false, @"", @"", "D4DE26C7-5A16-4656-BFEA-D204E268C251" ); // MinistrySafe Request Launcher:Start:Initial Entry:Employee Type
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "047569CD-A974-41E2-9505-F91648D6C07C", 4, true, false, false, false, @"", @"", "5A17672D-B797-48EB-8537-C44849B3A60B" ); // MinistrySafe Request Launcher:Start:Initial Entry:What is the salary range or the applicant?
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "42FD7BF6-C45A-4B91-B4E7-6CF6C04C1E25", 9, false, true, false, false, @"", @"", "602BB123-99F7-470D-99A1-731C52C8B5B3" ); // MinistrySafe Request Launcher:Start:Initial Entry:Background Type Index
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "521658C7-9442-48C1-90AD-306031A3051A", 10, false, true, false, false, @"", @"", "B60D007D-FC44-4124-89E0-E9545D4E3402" ); // MinistrySafe Request Launcher:Start:Initial Entry:Training Index
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "98CED0C5-A6F8-43E6-AAA8-EE0DEDC88F00", 6, true, false, false, false, @"", @"", "518CA412-8961-421B-9251-2D7C6F7E2A84" ); // MinistrySafe Request Launcher:Start:Initial Entry:Campus
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "CD395462-9C89-4E6F-A522-682546948AC4", 12, true, false, false, false, @"", @"", "5260AC82-0E57-4EA3-9BA0-2D1123050425" ); // MinistrySafe Request Launcher:Start:Initial Entry:Reason
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "E1FC8DB7-6C85-4635-8FCB-1ADE44D50F70", 13, false, true, false, false, @"", @"", "B5FF5F89-3111-4FE7-92F0-82AEB3E1F0F2" ); // MinistrySafe Request Launcher:Start:Initial Entry:Package Type
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "C1A6FF40-AED5-423C-891E-43FBAEACC136", 14, false, true, false, false, @"", @"", "EDB156C3-5B1E-44AF-93BB-A1DD09E6C184" ); // MinistrySafe Request Launcher:Start:Initial Entry:Survey Type
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "B82E2FFA-3856-4C5A-8EF8-4D9E4BF0C3A4", 15, false, true, false, false, @"", @"", "5061B234-606F-4732-8C46-CA07A39E09AC" ); // MinistrySafe Request Launcher:Start:Initial Entry:Skip Initial Entry
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "1097BB5D-8A75-4BE5-9EE4-CE8EF6EDBB64", 8, true, false, true, false, @"", @"", "43D648AD-3CA6-4BA0-92DC-553F540C5B3D" ); // MinistrySafe Request Launcher:Start:Initial Entry:Training User Type
            RockMigrationHelper.UpdateWorkflowActionFormAttribute( "A8603959-669A-4F08-930D-C333536389B8", "6C8C1786-EB9E-443D-BB72-F77CFB732C9B", 16, false, true, false, false, @"", @"", "4962808D-AAB5-4F65-997C-AAB62C17653B" ); // MinistrySafe Request Launcher:Start:Initial Entry:Person Id
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Requester to Current Person", 0, "24B7D5E6-C30F-48F4-9D7E-AF45A342CF3A", true, false, "", "", 1, "", "AAE29791-4FB8-4738-B379-7A8459B8BD22" ); // MinistrySafe Request Launcher:Start:Set Requester to Current Person
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Person from Person Id", 1, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "6C45C0C2-CE20-41D6-8946-DF145E485751", 32, "", "85F71A55-78E8-4AA7-BD31-947C234FD813" ); // MinistrySafe Request Launcher:Start:Set Person from Person Id
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Person From Connection Request", 2, "972F19B9-598B-474B-97A4-50E56E7B59D2", true, false, "", "6C45C0C2-CE20-41D6-8946-DF145E485751", 32, "", "0EBC23E4-6D72-4C32-A3B3-3E127421A00A" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Person From Group Member", 3, "972F19B9-598B-474B-97A4-50E56E7B59D2", true, false, "", "6C45C0C2-CE20-41D6-8946-DF145E485751", 32, "", "7F2695CF-1451-4E26-B57E-90F3F5B90F16" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Person", 4, "972F19B9-598B-474B-97A4-50E56E7B59D2", true, false, "", "6C45C0C2-CE20-41D6-8946-DF145E485751", 32, "", "BB177428-E6D4-43EA-BDCC-C344CAF6F238" ); // MinistrySafe Request Launcher:Start:Set Person
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Initial Entry", 5, "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", true, false, "A8603959-669A-4F08-930D-C333536389B8", "", 1, "", "7A065044-1BD9-4B86-B9ED-A08D4A3F8A06" ); // MinistrySafe Request Launcher:Start:Initial Entry
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Persist Workflow", 6, "F1A39347-6FE0-43D4-89FB-544195088ECF", true, false, "", "", 1, "", "C5776383-9581-453B-9FBD-1D70646187B6" ); // MinistrySafe Request Launcher:Start:Persist Workflow
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Background Type Index", 7, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "4AE36C99-2288-4DB0-A3AC-45E6995AE7A8" ); // MinistrySafe Request Launcher:Start:Set Background Type Index
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Set Training Index", 8, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "A0F623AB-6A00-40AB-8AAD-B61D76260355" ); // MinistrySafe Request Launcher:Start:Set Training Index
            RockMigrationHelper.UpdateWorkflowActionType( "0D56A890-186B-473B-8E2C-59AAEE937AE2", "Launch Background Check Loop", 9, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "", 1, "", "5016E89A-8857-4974-9EC9-0A605BD96F6C" ); // MinistrySafe Request Launcher:Start:Launch Background Check Loop
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Activate Training Loop If Index < 0", 0, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "42FD7BF6-C45A-4B91-B4E7-6CF6C04C1E25", 512, "0", "A011DFB8-84B3-4B7E-B349-39E9C49EC956" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training Loop If Index < 0
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Get Next BackgroundCheckType", 1, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "973072A5-5173-45B6-9382-82F50CAB5876" ); // MinistrySafe Request Launcher:Background Check Loop:Get Next BackgroundCheckType
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Activate Training if BackgroundCheckType is Null", 2, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "A073EDF3-C7B9-4BA6-8589-3087A61C2D24", 32, "", "D44637BC-88C7-44D7-9027-8102B9A08A7F" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training if BackgroundCheckType is Null
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Set Package Type", 3, "C789E457-0783-44B3-9D8F-2EBAB5F11110", true, false, "", "", 1, "", "A1F30B6D-32B9-499E-A941-133491484AAD" ); // MinistrySafe Request Launcher:Background Check Loop:Set Package Type
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Launch Background Check Request Workflow", 4, "9E3C42B5-792A-4694-8ACE-B84E5E87C800", true, false, "", "", 1, "", "55864E51-0F8A-4AA7-90F5-FE5F633EC80B" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Iterate Index", 5, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "4BA54392-3F8C-4F3C-8D5C-8CE12EB35E20" ); // MinistrySafe Request Launcher:Background Check Loop:Iterate Index
            RockMigrationHelper.UpdateWorkflowActionType( "7C2EE748-9537-4F07-9CCF-DF928DA11174", "Launch Loop Again", 6, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "", 1, "", "E2BB3A94-40E7-4570-A70F-F1B6ACD41C31" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Loop Again
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Activate Complete Workflow if Index < 0", 0, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "521658C7-9442-48C1-90AD-306031A3051A", 512, "0", "966B4B70-FC9E-4F78-A8BD-6C2ED2E509DF" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Index < 0
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Get Next Training Type", 1, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "2467A3C5-5E3D-44C6-977E-59529E7B0A9D" ); // MinistrySafe Request Launcher:Training Loop:Get Next Training Type
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Activate Complete Workflow if Training Type is Null", 2, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "F2B0F0C4-0A6E-4DFF-AA86-63D8AF250C6E", 32, "", "5724FBCA-97EF-41E0-B53C-6B4B50814DB5" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Training Type is Null
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Set Survey Type", 3, "C789E457-0783-44B3-9D8F-2EBAB5F11110", true, false, "", "", 1, "", "7AFA9845-341A-4E21-9F6F-DF170F81289D" ); // MinistrySafe Request Launcher:Training Loop:Set Survey Type
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Launch Training Workflow", 4, "9E3C42B5-792A-4694-8ACE-B84E5E87C800", true, false, "", "", 1, "", "6E78A9E5-6B49-4EFC-8442-3B15771FB50E" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Iterate Index", 5, "BC21E57A-1477-44B3-A7C2-61A806118945", true, false, "", "", 1, "", "F0793F74-B92E-4BE8-87D3-1EC6AB756965" ); // MinistrySafe Request Launcher:Training Loop:Iterate Index
            RockMigrationHelper.UpdateWorkflowActionType( "211678E4-EDA1-4647-96D4-1958B118867D", "Launch Loop Again", 6, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "", 1, "", "5BF81A85-C34D-436F-874C-1F4A5839D279" ); // MinistrySafe Request Launcher:Training Loop:Launch Loop Again
            RockMigrationHelper.UpdateWorkflowActionType( "87769844-3E8D-4AD3-9923-68302CEA734B", "Complete Workflow", 0, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, false, "", "", 1, "", "AA9CB786-C754-4A57-8689-506F398FC424" ); // MinistrySafe Request Launcher:Complete Workflow:Complete Workflow
            RockMigrationHelper.AddActionTypeAttributeValue( "AAE29791-4FB8-4738-B379-7A8459B8BD22", "DE9CB292-4785-4EA3-976D-3826F91E9E98", @"False" ); // MinistrySafe Request Launcher:Start:Set Requester to Current Person:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "AAE29791-4FB8-4738-B379-7A8459B8BD22", "BBED8A83-8BB2-4D35-BAFB-05F67DCAD112", @"c78ca83a-a75f-4350-80ba-cceffd047674" ); // MinistrySafe Request Launcher:Start:Set Requester to Current Person:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "85F71A55-78E8-4AA7-BD31-947C234FD813", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{% assign personId = 'Global' | PageParameter:'PersonId' | Default:'0' | AsInteger %}
{% assign person = personId | PersonById %}
{{person.PrimaryAlias.Guid}}" ); // MinistrySafe Request Launcher:Start:Set Person from Person Id:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "85F71A55-78E8-4AA7-BD31-947C234FD813", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Start:Set Person from Person Id:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "85F71A55-78E8-4AA7-BD31-947C234FD813", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"6c45c0c2-ce20-41d6-8946-df145e485751" ); // MinistrySafe Request Launcher:Start:Set Person from Person Id:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "0EBC23E4-6D72-4C32-A3B3-3E127421A00A", "9392E3D7-A28B-4CD8-8B03-5E147B102EF1", @"False" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "0EBC23E4-6D72-4C32-A3B3-3E127421A00A", "61E6E1BC-E657-4F00-B2E9-769AAA25B9F7", @"6c45c0c2-ce20-41d6-8946-df145e485751" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "0EBC23E4-6D72-4C32-A3B3-3E127421A00A", "B524B00C-29CB-49E9-9896-8BB60F209783", @"True" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request:Entity Is Required
            RockMigrationHelper.AddActionTypeAttributeValue( "0EBC23E4-6D72-4C32-A3B3-3E127421A00A", "1246C53A-FD92-4E08-ABDE-9A6C37E70C7B", @"False" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request:Use Id instead of Guid
            RockMigrationHelper.AddActionTypeAttributeValue( "0EBC23E4-6D72-4C32-A3B3-3E127421A00A", "7D79FC31-D0ED-4DB0-AB7D-60F4F98A1199", @"{{ Entity.PersonAlias.Guid}}" ); // MinistrySafe Request Launcher:Start:Set Person From Connection Request:Lava Template
            RockMigrationHelper.AddActionTypeAttributeValue( "7F2695CF-1451-4E26-B57E-90F3F5B90F16", "9392E3D7-A28B-4CD8-8B03-5E147B102EF1", @"False" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "7F2695CF-1451-4E26-B57E-90F3F5B90F16", "61E6E1BC-E657-4F00-B2E9-769AAA25B9F7", @"6c45c0c2-ce20-41d6-8946-df145e485751" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "7F2695CF-1451-4E26-B57E-90F3F5B90F16", "B524B00C-29CB-49E9-9896-8BB60F209783", @"True" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member:Entity Is Required
            RockMigrationHelper.AddActionTypeAttributeValue( "7F2695CF-1451-4E26-B57E-90F3F5B90F16", "1246C53A-FD92-4E08-ABDE-9A6C37E70C7B", @"False" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member:Use Id instead of Guid
            RockMigrationHelper.AddActionTypeAttributeValue( "7F2695CF-1451-4E26-B57E-90F3F5B90F16", "7D79FC31-D0ED-4DB0-AB7D-60F4F98A1199", @"{{ Entity.Person.PrimaryAlias.Guid }}" ); // MinistrySafe Request Launcher:Start:Set Person From Group Member:Lava Template
            RockMigrationHelper.AddActionTypeAttributeValue( "BB177428-E6D4-43EA-BDCC-C344CAF6F238", "9392E3D7-A28B-4CD8-8B03-5E147B102EF1", @"False" ); // MinistrySafe Request Launcher:Start:Set Person:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "BB177428-E6D4-43EA-BDCC-C344CAF6F238", "61E6E1BC-E657-4F00-B2E9-769AAA25B9F7", @"6c45c0c2-ce20-41d6-8946-df145e485751" ); // MinistrySafe Request Launcher:Start:Set Person:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "BB177428-E6D4-43EA-BDCC-C344CAF6F238", "B524B00C-29CB-49E9-9896-8BB60F209783", @"True" ); // MinistrySafe Request Launcher:Start:Set Person:Entity Is Required
            RockMigrationHelper.AddActionTypeAttributeValue( "BB177428-E6D4-43EA-BDCC-C344CAF6F238", "1246C53A-FD92-4E08-ABDE-9A6C37E70C7B", @"False" ); // MinistrySafe Request Launcher:Start:Set Person:Use Id instead of Guid
            RockMigrationHelper.AddActionTypeAttributeValue( "7A065044-1BD9-4B86-B9ED-A08D4A3F8A06", "234910F2-A0DB-4D7D-BAF7-83C880EF30AE", @"False" ); // MinistrySafe Request Launcher:Start:Initial Entry:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "C5776383-9581-453B-9FBD-1D70646187B6", "50B01639-4938-40D2-A791-AA0EB4F86847", @"False" ); // MinistrySafe Request Launcher:Start:Persist Workflow:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "C5776383-9581-453B-9FBD-1D70646187B6", "82744A46-0110-4728-BD3D-66C85C5FCB2F", @"False" ); // MinistrySafe Request Launcher:Start:Persist Workflow:Persist Immediately
            RockMigrationHelper.AddActionTypeAttributeValue( "4AE36C99-2288-4DB0-A3AC-45E6995AE7A8", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{% assign arraySize = Workflow | Attribute:'BackgroundCheckTypes' | Split:',' | Size %}{% if arraySize == 0 %}-1{%else%}0{% endif %}" ); // MinistrySafe Request Launcher:Start:Set Background Type Index:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "4AE36C99-2288-4DB0-A3AC-45E6995AE7A8", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Start:Set Background Type Index:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "4AE36C99-2288-4DB0-A3AC-45E6995AE7A8", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"42fd7bf6-c45a-4b91-b4e7-6cf6c04c1e25" ); // MinistrySafe Request Launcher:Start:Set Background Type Index:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "A0F623AB-6A00-40AB-8AAD-B61D76260355", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{% assign arraySize = Workflow | Attribute:'Trainings' | Split:',' | Size %}{% if arraySize == 0 %}-1{%else%}0{% endif %}" ); // MinistrySafe Request Launcher:Start:Set Training Index:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "A0F623AB-6A00-40AB-8AAD-B61D76260355", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Start:Set Training Index:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "A0F623AB-6A00-40AB-8AAD-B61D76260355", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"521658c7-9442-48c1-90ad-306031a3051a" ); // MinistrySafe Request Launcher:Start:Set Training Index:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "5016E89A-8857-4974-9EC9-0A605BD96F6C", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Start:Launch Background Check Loop:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "5016E89A-8857-4974-9EC9-0A605BD96F6C", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"7C2EE748-9537-4F07-9CCF-DF928DA11174" ); // MinistrySafe Request Launcher:Start:Launch Background Check Loop:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "A011DFB8-84B3-4B7E-B349-39E9C49EC956", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training Loop If Index < 0:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "A011DFB8-84B3-4B7E-B349-39E9C49EC956", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"211678E4-EDA1-4647-96D4-1958B118867D" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training Loop If Index < 0:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "973072A5-5173-45B6-9382-82F50CAB5876", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{% assign options = Workflow | Attribute:'BackgroundCheckTypes','RawValue' | Trim %}
{% assign optionList =  options | Split:',' %}
{% assign loopIndex = Workflow | Attribute:'BackgroundTypeIndex' | AsInteger %}
{% assign optionSize = optionList | Size %}
{% if optionSize > loopIndex and options != empty %}
{{ optionList | Index:loopIndex }}
{% endif %}" ); // MinistrySafe Request Launcher:Background Check Loop:Get Next BackgroundCheckType:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "973072A5-5173-45B6-9382-82F50CAB5876", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Get Next BackgroundCheckType:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "973072A5-5173-45B6-9382-82F50CAB5876", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"a073edf3-c7b9-4ba6-8589-3087a61c2d24" ); // MinistrySafe Request Launcher:Background Check Loop:Get Next BackgroundCheckType:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "D44637BC-88C7-44D7-9027-8102B9A08A7F", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training if BackgroundCheckType is Null:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "D44637BC-88C7-44D7-9027-8102B9A08A7F", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"211678E4-EDA1-4647-96D4-1958B118867D" ); // MinistrySafe Request Launcher:Background Check Loop:Activate Training if BackgroundCheckType is Null:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "A1F30B6D-32B9-499E-A941-133491484AAD", "D7EAA859-F500-4521-9523-488B12EAA7D2", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Set Package Type:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "A1F30B6D-32B9-499E-A941-133491484AAD", "44A0B977-4730-4519-8FF6-B0A01A95B212", @"e1fc8db7-6c85-4635-8fcb-1ade44d50f70" ); // MinistrySafe Request Launcher:Background Check Loop:Set Package Type:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "A1F30B6D-32B9-499E-A941-133491484AAD", "E5272B11-A2B8-49DC-860D-8D574E2BC15C", @"a073edf3-c7b9-4ba6-8589-3087a61c2d24" ); // MinistrySafe Request Launcher:Background Check Loop:Set Package Type:Text Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "55864E51-0F8A-4AA7-90F5-FE5F633EC80B", "B00F5144-1E44-4A1C-9C27-8C30DEEC7B70", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "55864E51-0F8A-4AA7-90F5-FE5F633EC80B", "3322E917-D124-41D1-A941-B27DA247C175", @"Background Check Request" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow:Workflow Name
            RockMigrationHelper.AddActionTypeAttributeValue( "55864E51-0F8A-4AA7-90F5-FE5F633EC80B", "B2009E3F-02ED-4F7E-9099-5C403653BAFB", @"21637ed6-b25b-4e00-88d4-c42425279d86" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow:Workflow Type
            RockMigrationHelper.AddActionTypeAttributeValue( "55864E51-0F8A-4AA7-90F5-FE5F633EC80B", "A4AAAC2A-070B-4EA7-9331-EF7859DCB5DC", @"Requester^Requester|Person^Person|Campus1^Campus|PackageType^PackageType|Reason^Reason|ChildServing^ChildServing|SalaryRange^SalaryRange|EmployeeType^EmployeeType|SkipInitialEntry^SkipInitialEntry|AgeOver13^AgeOver13" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow:Workflow Attribute Key
            RockMigrationHelper.AddActionTypeAttributeValue( "55864E51-0F8A-4AA7-90F5-FE5F633EC80B", "AEF2316E-0136-4634-9ED3-F2309D08036C", @"8d26f1d9-3519-4978-bf12-c81d524ab4bc" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Background Check Request Workflow:Workflow Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "4BA54392-3F8C-4F3C-8D5C-8CE12EB35E20", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{{ Workflow | Attribute:'BackgroundTypeIndex' | AsInteger | Plus:1 }}" ); // MinistrySafe Request Launcher:Background Check Loop:Iterate Index:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "4BA54392-3F8C-4F3C-8D5C-8CE12EB35E20", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Iterate Index:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "4BA54392-3F8C-4F3C-8D5C-8CE12EB35E20", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"42fd7bf6-c45a-4b91-b4e7-6cf6c04c1e25" ); // MinistrySafe Request Launcher:Background Check Loop:Iterate Index:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "E2BB3A94-40E7-4570-A70F-F1B6ACD41C31", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Loop Again:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "E2BB3A94-40E7-4570-A70F-F1B6ACD41C31", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"7C2EE748-9537-4F07-9CCF-DF928DA11174" ); // MinistrySafe Request Launcher:Background Check Loop:Launch Loop Again:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "966B4B70-FC9E-4F78-A8BD-6C2ED2E509DF", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Index < 0:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "966B4B70-FC9E-4F78-A8BD-6C2ED2E509DF", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"87769844-3E8D-4AD3-9923-68302CEA734B" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Index < 0:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "2467A3C5-5E3D-44C6-977E-59529E7B0A9D", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{% assign options = Workflow | Attribute:'Trainings','RawValue' | Trim %}
{% assign optionList =  options | Split:',' %}
{% assign loopIndex = Workflow | Attribute:'TrainingIndex' | AsInteger %}
{% assign optionSize = optionList | Size %}
{% if optionSize > loopIndex and options != empty %}
{{ optionList | Index:loopIndex }}
{% endif %}" ); // MinistrySafe Request Launcher:Training Loop:Get Next Training Type:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "2467A3C5-5E3D-44C6-977E-59529E7B0A9D", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Training Loop:Get Next Training Type:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "2467A3C5-5E3D-44C6-977E-59529E7B0A9D", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"f2b0f0c4-0a6e-4dff-aa86-63d8af250c6e" ); // MinistrySafe Request Launcher:Training Loop:Get Next Training Type:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "5724FBCA-97EF-41E0-B53C-6B4B50814DB5", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Training Type is Null:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "5724FBCA-97EF-41E0-B53C-6B4B50814DB5", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"87769844-3E8D-4AD3-9923-68302CEA734B" ); // MinistrySafe Request Launcher:Training Loop:Activate Complete Workflow if Training Type is Null:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "7AFA9845-341A-4E21-9F6F-DF170F81289D", "D7EAA859-F500-4521-9523-488B12EAA7D2", @"False" ); // MinistrySafe Request Launcher:Training Loop:Set Survey Type:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "7AFA9845-341A-4E21-9F6F-DF170F81289D", "44A0B977-4730-4519-8FF6-B0A01A95B212", @"c1a6ff40-aed5-423c-891e-43fbaeacc136" ); // MinistrySafe Request Launcher:Training Loop:Set Survey Type:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "7AFA9845-341A-4E21-9F6F-DF170F81289D", "E5272B11-A2B8-49DC-860D-8D574E2BC15C", @"f2b0f0c4-0a6e-4dff-aa86-63d8af250c6e" ); // MinistrySafe Request Launcher:Training Loop:Set Survey Type:Text Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "6E78A9E5-6B49-4EFC-8442-3B15771FB50E", "B00F5144-1E44-4A1C-9C27-8C30DEEC7B70", @"False" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "6E78A9E5-6B49-4EFC-8442-3B15771FB50E", "3322E917-D124-41D1-A941-B27DA247C175", @"Training Request" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow:Workflow Name
            RockMigrationHelper.AddActionTypeAttributeValue( "6E78A9E5-6B49-4EFC-8442-3B15771FB50E", "B2009E3F-02ED-4F7E-9099-5C403653BAFB", @"5876314a-fc4f-4a07-8ca0-a02de26e55be" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow:Workflow Type
            RockMigrationHelper.AddActionTypeAttributeValue( "6E78A9E5-6B49-4EFC-8442-3B15771FB50E", "A4AAAC2A-070B-4EA7-9331-EF7859DCB5DC", @"Requester^Requester|Person^Person|SurveyType^SurveyType|Reason^Reason|UserType^UserType|SkipInitialEntry^SkipInitialEntry" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow:Workflow Attribute Key
            RockMigrationHelper.AddActionTypeAttributeValue( "6E78A9E5-6B49-4EFC-8442-3B15771FB50E", "AEF2316E-0136-4634-9ED3-F2309D08036C", @"8acd9463-fd35-4561-8f58-ddf255745443" ); // MinistrySafe Request Launcher:Training Loop:Launch Training Workflow:Workflow Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "F0793F74-B92E-4BE8-87D3-1EC6AB756965", "F1F6F9D6-FDC5-489C-8261-4B9F45B3EED4", @"{{ Workflow | Attribute:'TrainingIndex' | AsInteger | Plus:1 }}" ); // MinistrySafe Request Launcher:Training Loop:Iterate Index:Lava
            RockMigrationHelper.AddActionTypeAttributeValue( "F0793F74-B92E-4BE8-87D3-1EC6AB756965", "F1924BDC-9B79-4018-9D4A-C3516C87A514", @"False" ); // MinistrySafe Request Launcher:Training Loop:Iterate Index:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "F0793F74-B92E-4BE8-87D3-1EC6AB756965", "431273C6-342D-4030-ADC7-7CDEDC7F8B27", @"521658c7-9442-48c1-90ad-306031a3051a" ); // MinistrySafe Request Launcher:Training Loop:Iterate Index:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "5BF81A85-C34D-436F-874C-1F4A5839D279", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // MinistrySafe Request Launcher:Training Loop:Launch Loop Again:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "5BF81A85-C34D-436F-874C-1F4A5839D279", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"211678E4-EDA1-4647-96D4-1958B118867D" ); // MinistrySafe Request Launcher:Training Loop:Launch Loop Again:Activity
            RockMigrationHelper.AddActionTypeAttributeValue( "AA9CB786-C754-4A57-8689-506F398FC424", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C", @"False" ); // MinistrySafe Request Launcher:Complete Workflow:Complete Workflow:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "AA9CB786-C754-4A57-8689-506F398FC424", "385A255B-9F48-4625-862B-26231DBAC53A", @"Completed" ); // MinistrySafe Request Launcher:Complete Workflow:Complete Workflow:Status|Status Attribute

            #endregion

            #region DefinedValue AttributeType qualifier helper

            Sql( @"
			UPDATE [aq] SET [key] = 'definedtype', [Value] = CAST( [dt].[Id] as varchar(5) )
			FROM [AttributeQualifier] [aq]
			INNER JOIN [Attribute] [a] ON [a].[Id] = [aq].[AttributeId]
			INNER JOIN [FieldType] [ft] ON [ft].[Id] = [a].[FieldTypeId]
			INNER JOIN [DefinedType] [dt] ON CAST([dt].[guid] AS varchar(50) ) = [aq].[value]
			WHERE [ft].[class] = 'Rock.Field.Types.DefinedValueFieldType'
			AND [aq].[key] = 'definedtypeguid'
            And ( Select top 1 Id from AttributeQualifier aq1 Where aq1.[Key] = 'definedtype' and aq1.AttributeId = aq.AttributeId ) is null
		" );

            #endregion
        }

        private void AddStepProgram()
        {
            // Add StepProgram: MinistrySafe Trainings
            Sql( @"
                INSERT INTO dbo.StepProgram (
                    Name,
                    IconCssClass,
                    DefaultListView,
                    IsActive,
                    [Order],
                    Guid,
                    StepTerm
                )
                VALUES (
                    'MinistrySafe Trainings',
                    'fa fa-eye',       
                    0,                     
                    1,                    
                    0,                     
                    'F821FE74-214A-4583-9ECB-FF7B6193F57F',               
                    'Training'                   
                );
            " );

            var stepProgramId = SqlScalar( "SELECT [Id] FROM [StepProgram] WHERE [Guid] = 'F821FE74-214A-4583-9ECB-FF7B6193F57F'" ).ToStringSafe();

            // Entity: Rock.Model.StepType Attribute: Code
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.StepType", "9C204CD0-1233-41C5-818A-C5DA439445AA", "StepProgramId", stepProgramId, "Code", "Code", @"", 0, @"", "71C7690F-61D0-4961-BD6E-DEBA99826E0F", "Code" );
            // Entity: Rock.Model.StepType Attribute: Price
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.StepType", "3EE69CBC-35CE-4496-88CC-8327A447603F", "StepProgramId", stepProgramId, "Price", "Price", @"", 1, @"", "8FC938A3-D9B3-40D3-9905-439BD14452C8", "Price" );
            // Entity: Rock.Model.StepType Attribute: Type
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.StepType", "9C204CD0-1233-41C5-818A-C5DA439445AA", "StepProgramId", stepProgramId, "Type", "Type", @"", 2, @"", "2EBC9EDF-4D92-45DA-86B2-3B14F0396962", "Type" );

            // Qualifier for attribute: Code
            RockMigrationHelper.UpdateAttributeQualifier( "71C7690F-61D0-4961-BD6E-DEBA99826E0F", "ispassword", @"False", "DC58FD8B-C9F5-4E51-BE02-FE054371DD25" );
            // Qualifier for attribute: Code
            RockMigrationHelper.UpdateAttributeQualifier( "71C7690F-61D0-4961-BD6E-DEBA99826E0F", "maxcharacters", @"", "8C888D26-5E4E-494A-8458-174F6955C822" );
            // Qualifier for attribute: Code
            RockMigrationHelper.UpdateAttributeQualifier( "71C7690F-61D0-4961-BD6E-DEBA99826E0F", "showcountdown", @"False", "DECA9426-1F4D-4531-BAD1-5DA41A9BC01C" );
            // Qualifier for attribute: Type
            RockMigrationHelper.UpdateAttributeQualifier( "2EBC9EDF-4D92-45DA-86B2-3B14F0396962", "ispassword", @"False", "E9C09662-AFE8-4D14-AB05-3BBC4EAAC411" );
            // Qualifier for attribute: Type
            RockMigrationHelper.UpdateAttributeQualifier( "2EBC9EDF-4D92-45DA-86B2-3B14F0396962", "maxcharacters", @"", "C3AB3BE1-9E05-4F74-BA8D-6D9571C94EE7" );
            // Qualifier for attribute: Type
            RockMigrationHelper.UpdateAttributeQualifier( "2EBC9EDF-4D92-45DA-86B2-3B14F0396962", "showcountdown", @"False", "BB34E8E6-CF63-4A52-918F-BCEF8BE12E13" );


            RockMigrationHelper.AddDefinedTypeAttribute( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "B00149C7-08D6-448C-AF21-948BF453DF7E", "Step Type", "StepType", "", 15311, "", "829CCCC8-2D70-43B4-90CF-136012A8A756" );
            RockMigrationHelper.AddAttributeQualifier( "829CCCC8-2D70-43B4-90CF-136012A8A756", "DefaultStepProgramGuid", stepProgramId, "0BCAF840-0E84-48EA-A090-3D16EB3F15BA" );

            Sql( @"
            -- Declare variables
            DECLARE @StepProgramId INT;
            DECLARE @CurrentDateTime DATETIME = GETDATE();

            -- Get StepProgramId by GUID
            SET @StepProgramId = (SELECT Id FROM dbo.StepProgram WHERE Guid = 'F821FE74-214A-4583-9ECB-FF7B6193F57F');

            -- Insert Step Statuses
            INSERT INTO dbo.StepStatus
            (
                Name,
                StepProgramId,
                IsCompleteStatus,
                StatusColor,
                IsActive,
                [Order],
                Guid,
                CreatedDateTime,
                ModifiedDateTime
            )
            VALUES
            -- Status 1: Requested
            (
                'Requested',
                @StepProgramId,
                0,                                -- Not a completion status
                'rgb(33,150,243)',                -- Blue
                1,
                0,
                '10B95BB8-E5F8-48BA-9D5A-77349EDC32AE',
                @CurrentDateTime,
                @CurrentDateTime
            ),
            -- Status 2: Passed
            (
                'Passed',
                @StepProgramId,
                1,                                -- Completion status
                'rgb(76,175,80)',                 -- Green
                1,
                1,
                'B1300BFA-19C8-40EB-A1FE-00E023C5F7BB',
                @CurrentDateTime,
                @CurrentDateTime
            ),
            -- Status 3: Expiring
            (
                'Expiring',
                @StepProgramId,
                1,                                -- Completion status
                'rgb(240,223,80)',                -- Yellow
                1,
                2,
                'C27A2C45-3F07-4862-8E98-91DB17957916',
                @CurrentDateTime,
                @CurrentDateTime
            ),
            -- Status 4: Expired
            (
                'Expired',
                @StepProgramId,
                1,                                -- Completion status
                'rgb(163,163,163)',               -- Gray
                1,
                3,
                '5E79DB95-F60B-4448-960A-165751713164',
                @CurrentDateTime,
                @CurrentDateTime
            ),
            -- Status 5: Failed
            (
                'Failed',
                @StepProgramId,
                1,                                -- Completion status
                'rgb(244,67,54)',                 -- Red
                1,
                4,
                '4FC55334-30A9-47C9-BC31-E36BB32C1113',
                @CurrentDateTime,
                @CurrentDateTime
            );" );

        }



        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
