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
using Rock.Workflow.Action;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class RequestLauncher.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 19, "1.18.0" )]
    public partial class BackgroundCheckType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            AddPersonAttribute();
            UpdateWorkflow();
            ReaddWorkflowsToActions();
        }

        private void ReaddWorkflowsToActions()
        {
            // Add Workflow to Action Dropdown
            RockMigrationHelper.AddBlockAttributeValue(
                "1E6AF671-9C1A-4C6C-8156-36B6D7589F34",
                "B8419489-84A9-40AB-B85D-DD5E07255B17",
                MinistrySafeSystemGuid.MINISTRYSAFE_TRAINING_WORKFLOW_TYPE,
                true );

            RockMigrationHelper.AddBlockAttributeValue(
                "1E6AF671-9C1A-4C6C-8156-36B6D7589F34",
                "B8419489-84A9-40AB-B85D-DD5E07255B17",
                MinistrySafeSystemGuid.MINISTRYSAFE_BACKGROUNDCHECK_WORKFLOW_TYPE,
                true );

        }

        private void UpdateWorkflow()
        {
            var ministrySafeStatusId = SqlScalar( "Select Top 1 Id From DefinedType Where Guid = 'A19A48A7-8EC9-4FAC-8433-B9346C790175'" ).ToStringSafe();

            // Add new action and update order for existing actions
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Background Check Type", 0, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "63DA7BF3-B290-4832-B7DD-E1F0D89F373F", 64, "", "266C9220-9A40-4CCF-92FD-7C1822A09866" ); // Background Check:Complete Request:Update Background Check Type
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Date", 1, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "80373383-4BC4-44AC-9F08-AA6E8469AD00" ); // Background Check:Complete Request:Update Date
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Report", 2, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "708B2D92-E00E-4856-9DAA-9877624B47D9" ); // Background Check:Complete Request:Update Report
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Attribute Status", 3, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "40A3FF6C-D093-4F78-A245-8219D1A8CABE" ); // Background Check:Complete Request:Update Attribute Status
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Background Check Passed", 4, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 8, "Pass", "CB472BFF-058D-4544-938C-13CA981CEE9F" ); // Background Check:Complete Request:Background Check Passed
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Background Check Failed", 5, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 8, "Fail", "0D38493F-A0E3-4777-9180-A4D81D7805AD" ); // Background Check:Complete Request:Background Check Failed
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Archive Background Check", 6, "F6F75A2C-200F-4A7F-B6BC-79DFD2F34A13", true, false, "", "58154ED1-4288-41BB-B37D-1F0458C2D220", 1, "c2978654-2d24-4ccb-825b-43892b73ee96", "F1382B37-800C-47CA-8396-CF0E20707335" ); // Background Check:Complete Request:Archive Background Check
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Ministry Safe Status", 7, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "6F00CAD3-75A8-4FB3-AB1B-3894432781EF" ); // Background Check:Complete Request:Update Ministry Safe Status
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Notify Requester", 8, "66197B01-D1F0-4924-A315-47AD54E030DE", true, false, "", "", 1, "", "C1197EEF-0422-47BD-A68F-646C5CD09AC5" ); // Background Check:Complete Request:Notify Requester
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Complete Workflow", 9, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, false, "", "", 1, "", "8B5CA90C-AFB5-4AB7-93D1-8C9BE3DFCCF9" ); // Background Check:Complete Request:Complete Workflow

            RockMigrationHelper.AddActionTypeAttributeValue( "266C9220-9A40-4CCF-92FD-7C1822A09866", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Update Background Check Type:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "266C9220-9A40-4CCF-92FD-7C1822A09866", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Update Background Check Type:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "266C9220-9A40-4CCF-92FD-7C1822A09866", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"115dbc06-715e-40dc-bc91-587a255c973b" ); // Background Check:Complete Request:Update Background Check Type:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "266C9220-9A40-4CCF-92FD-7C1822A09866", "94689BDE-493E-4869-A614-2D54822D747C", @"63da7bf3-b290-4832-b7dd-e1f0d89f373f" ); // Background Check:Complete Request:Update Background Check Type:Value|Attribute Value


            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "5603DF2D-C429-4230-AEA1-3B6B1A19EF71", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Cancelled Status", "CancelledStatus", "", 4, @"3c64dcf1-9ded-4387-bbc1-d243d7c054ee", "983FFB55-F489-49C8-BBAD-A413BF11622E" ); // Background Check:Review Result:Cancelled Status
            RockMigrationHelper.UpdateWorkflowActivityTypeAttribute( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Passed MinistrySafe Status", "PassedMinistrySafeStatus", "", 0, @"b5da923a-6d2b-433c-9e9f-fcd0cad4b5e9", "794D5AAD-79DB-4732-BE58-45999FE9658B" ); // Background Check:Process Result:Passed MinistrySafe Status

            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "AllowAddingNewValues", @"False", "CAD690DA-A747-4711-9512-7A942192C542" ); // Background Check:Cancelled Status:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "allowmultiple", @"False", "AEEF40F4-D30A-46D8-94F8-7908730C0DE1" ); // Background Check:Cancelled Status:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "definedtype", ministrySafeStatusId, "6EB354F7-E013-4313-B9C0-B0CCBEA18092" ); // Background Check:Cancelled Status:definedtype
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "displaydescription", @"False", "A6B4B653-193F-48D1-AF87-8CB780B1C099" ); // Background Check:Cancelled Status:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "enhancedselection", @"False", "ADCF5E7C-E741-49FD-A498-AAAD1B5BF911" ); // Background Check:Cancelled Status:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "includeInactive", @"False", "45C698B6-6455-420B-860A-D3A138650EA3" ); // Background Check:Cancelled Status:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "RepeatColumns", @"", "EEB0ECDF-FAC3-45B8-845F-3069C9EB4B68" ); // Background Check:Cancelled Status:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "983FFB55-F489-49C8-BBAD-A413BF11622E", "SelectableDefinedValuesId", @"", "7BD292B6-5D54-44B2-9804-7A6CD2AAF65A" ); // Background Check:Cancelled Status:SelectableDefinedValuesId

            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "AllowAddingNewValues", @"False", "EF7872AE-0F1B-49AF-AE1F-CC3E29139918" ); // Background Check:Passed MinistrySafe Status:AllowAddingNewValues
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "allowmultiple", @"False", "A18286C5-772A-4440-95A2-BC835DC095E8" ); // Background Check:Passed MinistrySafe Status:allowmultiple
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "definedtype", ministrySafeStatusId, "BDBF6A4C-5BFA-46A8-86B6-398B1487F5AA" ); // Background Check:Passed MinistrySafe Status:definedtype
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "displaydescription", @"False", "33FC3F55-5ADE-4C6C-ABBD-45857EA1BA3A" ); // Background Check:Passed MinistrySafe Status:displaydescription
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "enhancedselection", @"False", "392A7A8E-1C42-47CE-BDF4-CE0829F47C6B" ); // Background Check:Passed MinistrySafe Status:enhancedselection
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "includeInactive", @"False", "D5361F8A-C9FD-43B5-8659-0E90332F9E79" ); // Background Check:Passed MinistrySafe Status:includeInactive
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "RepeatColumns", @"", "CC42F2B8-CAA2-472F-A57D-1003D0E38F2D" ); // Background Check:Passed MinistrySafe Status:RepeatColumns
            RockMigrationHelper.AddAttributeQualifier( "794D5AAD-79DB-4732-BE58-45999FE9658B", "SelectableDefinedValuesId", @"", "4A32DC5A-9D95-4EEA-B8DF-B2B3106CC86C" ); // Background Check:Passed MinistrySafe Status:SelectableDefinedValuesId


            RockMigrationHelper.UpdateWorkflowActionType( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "Set MinistrySafe Status if Passed", 3, "C789E457-0783-44B3-9D8F-2EBAB5F11110", true, false, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 1, "Pass", "066094B6-A543-47D2-B456-66ADA0A8AB4C" ); // Background Check:Process Result:Set MinistrySafe Status if Passed
            RockMigrationHelper.UpdateWorkflowActionType( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "Activate Complete", 4, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 1, "Pass", "4C0F1458-9C90-4191-B807-DBA2A9328E62" ); // Background Check:Process Result:Activate Complete

            RockMigrationHelper.UpdateWorkflowActionType( "5603DF2D-C429-4230-AEA1-3B6B1A19EF71", "Activate Cancelled", 6, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "58154ED1-4288-41BB-B37D-1F0458C2D220", 1, "983ffb55-f489-49c8-bbad-a413bf11622e", "F697BDC1-9359-4475-BA46-CCC46B05B373" ); // Background Check:Review Result:Activate Cancelled
            RockMigrationHelper.UpdateWorkflowActionType( "5603DF2D-C429-4230-AEA1-3B6B1A19EF71", "Activate Complete", 7, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "", 1, "", "BD069F33-F6B2-4169-92F4-BA8F9885E4DB" ); // Background Check:Review Result:Activate Complete

            RockMigrationHelper.AddActionTypeAttributeValue( "066094B6-A543-47D2-B456-66ADA0A8AB4C", "D7EAA859-F500-4521-9523-488B12EAA7D2", @"False" ); // Background Check:Process Result:Set MinistrySafe Status if Passed:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "066094B6-A543-47D2-B456-66ADA0A8AB4C", "44A0B977-4730-4519-8FF6-B0A01A95B212", @"58154ed1-4288-41bb-b37d-1f0458c2d220" ); // Background Check:Process Result:Set MinistrySafe Status if Passed:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "066094B6-A543-47D2-B456-66ADA0A8AB4C", "E5272B11-A2B8-49DC-860D-8D574E2BC15C", @"794d5aad-79db-4732-be58-45999fe9658b" ); // Background Check:Process Result:Set MinistrySafe Status if Passed:Text Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "F697BDC1-9359-4475-BA46-CCC46B05B373", "E8ABD802-372C-47BE-82B1-96F50DB5169E", @"False" ); // Background Check:Review Result:Activate Cancelled:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "F697BDC1-9359-4475-BA46-CCC46B05B373", "02D5A7A5-8781-46B4-B9FC-AF816829D240", @"ED871E22-E218-44EA-819E-0E120E5ED97F" ); // Background Check:Review Result:Activate Cancelled:Activity

        }

        private void AddPersonAttribute()
        {
            // Entity: Rock.Model.Person Attribute: Background Check Type
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.Person", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "", "", "Background Check Type", "Background Check Type", @"", 15312, @"", "115DBC06-715E-40DC-BC91-587A255C973B", "BackgroundCheckType" );
            var backgroundCheckTypeId = SqlScalar( String.Format( "Select Top 1 Id From DefinedType Where Guid = '{0}'", Rock.SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES ) ).ToStringSafe();

            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "AllowAddingNewValues", @"False", "B98A7A16-4661-4B51-8E71-3C9D1DDC4E6C" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "allowmultiple", @"False", "81AF8919-B26F-4CB7-A4F2-BB7E87637F8E" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "definedtype", backgroundCheckTypeId, "72F12255-320A-462D-B56D-4C5BE6379DF9" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "displaydescription", @"False", "DD26C253-C5D7-4A34-BC7A-BB38E90FB49C" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "enhancedselection", @"False", "7733C1A3-4843-4E7E-B701-994A9852752E" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "includeInactive", @"False", "32591889-0D0F-43EF-9758-965B458257EF" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "RepeatColumns", @"", "97DBC462-B114-4AD5-9F64-B340832CB663" );
            // Qualifier for attribute: BackgroundCheckType
            RockMigrationHelper.UpdateAttributeQualifier( "115DBC06-715E-40DC-BC91-587A255C973B", "SelectableDefinedValuesId", @"", "C90F9146-9766-4898-AE92-8188D1A59100" );

        }



        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "115DBC06-715E-40DC-BC91-587A255C973B" ); // Rock.Model.Person: Background Check Type

        }
    }
}
