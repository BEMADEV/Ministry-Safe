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
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class RequestLauncher.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 13, "1.16.0" )]
    public partial class SecondBackgroundCheckFix : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            UpdateBackgroundCheckWorkflowType();
            AddEnableDebuggingAttribute();
        }

        private void AddEnableDebuggingAttribute()
        {
            RockMigrationHelper.AddOrUpdateEntityAttribute( "com.bemaservices.MinistrySafe.MinistrySafe", Rock.SystemGuid.FieldType.BOOLEAN, "", "", "Enable Debugging?", "Enable Debugging?", "", 2, "false", "9C7EF0A1-D3BD-42CC-B9B4-E5ED7940DD12", MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING );
        }

        /// <summary>
        /// Updates the type of the background check workflow.
        /// </summary>
        private void UpdateBackgroundCheckWorkflowType()
        {
            #region Background Check

            Sql( @"
                    Delete
                    From WorkflowAction
                    Where ActionTypeId in (
                        Select wat.Id
                        From WorkflowActionType wat
                        Where wat.[Guid] in (
                                    'CB94B493-0B95-4328-AB3C-874180FF1FEA',
                                    '304E76E2-BA98-4B72-8E37-B91917179E5B'
                                        )
                        )
                " );
            RockMigrationHelper.DeleteWorkflowActionType( "CB94B493-0B95-4328-AB3C-874180FF1FEA" ); // Background Check:Process Result:Save Date
            RockMigrationHelper.DeleteWorkflowActionType( "304E76E2-BA98-4B72-8E37-B91917179E5B" ); // Background Check:Process Result:Save Report

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "21637ED6-B25B-4E00-88D4-C42425279D86", "6B6AA175-4758-453F-8D83-FCD8044B5F36", "Completion Date", "CompletionDate", "", 22, @"", "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", false ); // Background Check:Completion Date
            RockMigrationHelper.AddAttributeQualifier( "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", "datePickerControlType", @"Date Picker", "7B5B9BD5-AA32-47DD-86FA-653135B9485A" ); // Background Check:Completion Date:datePickerControlType
            RockMigrationHelper.AddAttributeQualifier( "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", "displayCurrentOption", @"False", "DD36800A-A7FF-47E1-B26E-B001A771790E" ); // Background Check:Completion Date:displayCurrentOption
            RockMigrationHelper.AddAttributeQualifier( "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", "displayDiff", @"False", "65281C68-5B60-4CA5-89D0-BF379107AABA" ); // Background Check:Completion Date:displayDiff
            RockMigrationHelper.AddAttributeQualifier( "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", "format", @"", "9140B2FD-A561-40F2-A1ED-1A9CABD441DD" ); // Background Check:Completion Date:format
            RockMigrationHelper.AddAttributeQualifier( "789B55D9-7AA6-410C-8A61-A4AF2E84C3C0", "futureYearCount", @"", "01CD4B2A-499E-489B-B5AB-263870BA8312" ); // Background Check:Completion Date:futureYearCount

            RockMigrationHelper.UpdateWorkflowActivityType( "21637ED6-B25B-4E00-88D4-C42425279D86", true, "Process Result", "Evaluates the result of the background check received from the provider", false, 5, "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6" ); // Background Check:Process Result
            RockMigrationHelper.UpdateWorkflowActivityType( "21637ED6-B25B-4E00-88D4-C42425279D86", true, "Complete Request", "Notifies requester of result and updates person's record with result", false, 7, "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A" ); // Background Check:Complete Request

            RockMigrationHelper.UpdateWorkflowActionType( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "Get Completion Date", 0, "C789E457-0783-44B3-9D8F-2EBAB5F11110", true, false, "", "", 1, "", "CB94B493-0B95-4328-AB3C-874180FF1FEA" ); // Background Check:Process Result:Get Completion Date
            RockMigrationHelper.UpdateWorkflowActionType( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "Activate Review", 1, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 2, "Pass", "F7F8C93A-3C3B-4C5F-8B4D-8BF2560205FC" ); // Background Check:Process Result:Activate Review
            RockMigrationHelper.UpdateWorkflowActionType( "CA4F5173-4F0B-4A61-8AA7-E1C05F1098E6", "Activate Complete", 2, "38907A90-1634-4A93-8017-619326A4A582", true, true, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 1, "Pass", "4C0F1458-9C90-4191-B807-DBA2A9328E62" ); // Background Check:Process Result:Activate Complete

            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Date", 0, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "80373383-4BC4-44AC-9F08-AA6E8469AD00" ); // Background Check:Complete Request:Update Date
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Report", 1, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "708B2D92-E00E-4856-9DAA-9877624B47D9" ); // Background Check:Complete Request:Update Report
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Attribute Status", 2, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "40A3FF6C-D093-4F78-A245-8219D1A8CABE" ); // Background Check:Complete Request:Update Attribute Status
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Background Check Passed", 3, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 8, "Pass", "CB472BFF-058D-4544-938C-13CA981CEE9F" ); // Background Check:Complete Request:Background Check Passed
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Background Check Failed", 4, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "7D033FD0-1232-43A9-98FD-1A2F1C6C453B", 8, "Fail", "0D38493F-A0E3-4777-9180-A4D81D7805AD" ); // Background Check:Complete Request:Background Check Failed
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Archive Background Check", 5, "F6F75A2C-200F-4A7F-B6BC-79DFD2F34A13", true, false, "", "58154ED1-4288-41BB-B37D-1F0458C2D220", 1, "c2978654-2d24-4ccb-825b-43892b73ee96", "F1382B37-800C-47CA-8396-CF0E20707335" ); // Background Check:Complete Request:Archive Background Check
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Update Ministry Safe Status", 6, "320622DA-52E0-41AE-AF90-2BF78B488552", true, false, "", "", 1, "", "6F00CAD3-75A8-4FB3-AB1B-3894432781EF" ); // Background Check:Complete Request:Update Ministry Safe Status
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Notify Requester", 7, "66197B01-D1F0-4924-A315-47AD54E030DE", true, false, "", "", 1, "", "C1197EEF-0422-47BD-A68F-646C5CD09AC5" ); // Background Check:Complete Request:Notify Requester
            RockMigrationHelper.UpdateWorkflowActionType( "F4EC7AF1-4478-46DC-9C4B-D2B4924C9D3A", "Complete Workflow", 8, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, false, "", "", 1, "", "8B5CA90C-AFB5-4AB7-93D1-8C9BE3DFCCF9" ); // Background Check:Complete Request:Complete Workflow

            RockMigrationHelper.AddActionTypeAttributeValue( "CB94B493-0B95-4328-AB3C-874180FF1FEA", "D7EAA859-F500-4521-9523-488B12EAA7D2", @"False" ); // Background Check:Process Result:Get Completion Date:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "CB94B493-0B95-4328-AB3C-874180FF1FEA", "44A0B977-4730-4519-8FF6-B0A01A95B212", @"789b55d9-7aa6-410c-8a61-a4af2e84c3c0" ); // Background Check:Process Result:Get Completion Date:Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "CB94B493-0B95-4328-AB3C-874180FF1FEA", "E5272B11-A2B8-49DC-860D-8D574E2BC15C", @"{{ 'Now' | Date:'yyyy-MM-dd' }}T00:00:00" ); // Background Check:Process Result:Get Completion Date:Text Value|Attribute Value

            RockMigrationHelper.AddActionTypeAttributeValue( "80373383-4BC4-44AC-9F08-AA6E8469AD00", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Update Date:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "80373383-4BC4-44AC-9F08-AA6E8469AD00", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Update Date:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "80373383-4BC4-44AC-9F08-AA6E8469AD00", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"3daff000-7f74-47d7-8cb0-e4a4e6c81f5f" ); // Background Check:Complete Request:Update Date:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "80373383-4BC4-44AC-9F08-AA6E8469AD00", "94689BDE-493E-4869-A614-2D54822D747C", @"789b55d9-7aa6-410c-8a61-a4af2e84c3c0" ); // Background Check:Complete Request:Update Date:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "708B2D92-E00E-4856-9DAA-9877624B47D9", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Update Report:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "708B2D92-E00E-4856-9DAA-9877624B47D9", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Update Report:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "708B2D92-E00E-4856-9DAA-9877624B47D9", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"f3931952-460d-43e0-a6e0-eb6b5b1f9167" ); // Background Check:Complete Request:Update Report:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "708B2D92-E00E-4856-9DAA-9877624B47D9", "94689BDE-493E-4869-A614-2D54822D747C", @"31c4ec7c-8dec-4305-a363-428d7b07c300" ); // Background Check:Complete Request:Update Report:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "40A3FF6C-D093-4F78-A245-8219D1A8CABE", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Update Attribute Status:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "40A3FF6C-D093-4F78-A245-8219D1A8CABE", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Update Attribute Status:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "40A3FF6C-D093-4F78-A245-8219D1A8CABE", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"44490089-e02c-4e54-a456-454845abbc9d" ); // Background Check:Complete Request:Update Attribute Status:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "40A3FF6C-D093-4F78-A245-8219D1A8CABE", "94689BDE-493E-4869-A614-2D54822D747C", @"7d033fd0-1232-43a9-98fd-1a2f1c6c453b" ); // Background Check:Complete Request:Update Attribute Status:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "CB472BFF-058D-4544-938C-13CA981CEE9F", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Background Check Passed:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "CB472BFF-058D-4544-938C-13CA981CEE9F", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Background Check Passed:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "CB472BFF-058D-4544-938C-13CA981CEE9F", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"daf87b87-3d1e-463d-a197-52227fe4ea28" ); // Background Check:Complete Request:Background Check Passed:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "CB472BFF-058D-4544-938C-13CA981CEE9F", "94689BDE-493E-4869-A614-2D54822D747C", @"True" ); // Background Check:Complete Request:Background Check Passed:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "0D38493F-A0E3-4777-9180-A4D81D7805AD", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Background Check Failed:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "0D38493F-A0E3-4777-9180-A4D81D7805AD", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Background Check Failed:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "0D38493F-A0E3-4777-9180-A4D81D7805AD", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"daf87b87-3d1e-463d-a197-52227fe4ea28" ); // Background Check:Complete Request:Background Check Failed:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "0D38493F-A0E3-4777-9180-A4D81D7805AD", "94689BDE-493E-4869-A614-2D54822D747C", @"False" ); // Background Check:Complete Request:Background Check Failed:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "F1382B37-800C-47CA-8396-CF0E20707335", "936FC890-C56E-4ECD-A73D-7E1C3E146093", @"False" ); // Background Check:Complete Request:Archive Background Check:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "6F00CAD3-75A8-4FB3-AB1B-3894432781EF", "E5BAC4A6-FF7F-4016-BA9C-72D16CB60184", @"False" ); // Background Check:Complete Request:Update Ministry Safe Status:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "6F00CAD3-75A8-4FB3-AB1B-3894432781EF", "E456FB6F-05DB-4826-A612-5B704BC4EA13", @"120910c9-516d-48b5-8ce6-0e665ba1138a" ); // Background Check:Complete Request:Update Ministry Safe Status:Person
            RockMigrationHelper.AddActionTypeAttributeValue( "6F00CAD3-75A8-4FB3-AB1B-3894432781EF", "8F4BB00F-7FA2-41AD-8E90-81F4DFE2C762", @"16753ca6-a943-42f2-b7b5-c6a5dc8a397a" ); // Background Check:Complete Request:Update Ministry Safe Status:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "6F00CAD3-75A8-4FB3-AB1B-3894432781EF", "94689BDE-493E-4869-A614-2D54822D747C", @"58154ed1-4288-41bb-b37d-1f0458c2d220" ); // Background Check:Complete Request:Update Ministry Safe Status:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "C1197EEF-0422-47BD-A68F-646C5CD09AC5", "36197160-7D3D-490D-AB42-7E29105AFE91", @"False" ); // Background Check:Complete Request:Notify Requester:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "C1197EEF-0422-47BD-A68F-646C5CD09AC5", "0C4C13B8-7076-4872-925A-F950886B5E16", @"6a1412f4-569a-4dfd-b477-bdb99644c024" ); // Background Check:Complete Request:Notify Requester:Send To Email Addresses|To Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "C1197EEF-0422-47BD-A68F-646C5CD09AC5", "5D9B13B6-CD96-4C7C-86FA-4512B9D28386", @"Background Check for {{ Workflow | Attribute:'Person' }}" ); // Background Check:Complete Request:Notify Requester:Subject
            RockMigrationHelper.AddActionTypeAttributeValue( "C1197EEF-0422-47BD-A68F-646C5CD09AC5", "4D245B9E-6B03-46E7-8482-A51FBA190E4D", @"{{ 'Global' | Attribute:'EmailHeader' }}  <p>{{ Person.FirstName }},</p> <p>The background check for {{ Workflow | Attribute:'Person' }} has been completed.</p> <p>Result: {{ Workflow | Attribute:'ReportStatus' | Upcase }}<p/>  {{ 'Global' | Attribute:'EmailFooter' }}" ); // Background Check:Complete Request:Notify Requester:Body
            RockMigrationHelper.AddActionTypeAttributeValue( "C1197EEF-0422-47BD-A68F-646C5CD09AC5", "1BDC7ACA-9A0B-4C8A-909E-8B4143D9C2A3", @"False" ); // Background Check:Complete Request:Notify Requester:Save Communication History
            RockMigrationHelper.AddActionTypeAttributeValue( "8B5CA90C-AFB5-4AB7-93D1-8C9BE3DFCCF9", "0CA0DDEF-48EF-4ABC-9822-A05E225DE26C", @"False" ); // Background Check:Complete Request:Complete Workflow:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "8B5CA90C-AFB5-4AB7-93D1-8C9BE3DFCCF9", "385A255B-9F48-4625-862B-26231DBAC53A", @"Completed" ); // Background Check:Complete Request:Complete Workflow:Status|Status Attribute

            #endregion
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
