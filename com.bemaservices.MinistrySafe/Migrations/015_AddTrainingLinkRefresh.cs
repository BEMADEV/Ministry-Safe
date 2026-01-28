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
using Rock.Model;
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class RequestLauncher.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 15, "1.16.0" )]
    public partial class AddTrainingLinkRefresh : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateEntityType( "com.bemaservices.MinistrySafe.Workflow.Action.RefreshTraining", "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", false, true );                       
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Active", "Active", "Should Service be used?", 0, @"False", "7F6B8D47-5098-418A-9DC2-B8AC20A9424D" ); // com.bemaservices.MinistrySafe.Workflow.Action.RefreshTraining:Active
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Direct Login Url", "DirectLoginUrl", "The attribute to save the MinistrySafe direct login URL.", 2, @"", "A03289C0-0068-4F7E-9D3A-2932F649AE27" ); // com.bemaservices.MinistrySafe.Workflow.Action.RefreshTraining:Direct Login Url
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", "33E6DF69-BDFA-407A-9744-C175B60643AE", "Person Attribute", "PersonAttribute", "The Person attribute that contains the person who the training should be submitted for.", 1, @"", "DC3661CA-4018-4AE5-83B2-CCE9BB26840C" ); // com.bemaservices.MinistrySafe.Workflow.Action.RefreshTraining:Person Attribute
            RockMigrationHelper.UpdateWorkflowActionEntityAttribute( "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Order", "Order", "The order that this service should be used (priority)", 0, @"", "93454031-BA08-4377-88A3-5DE6A9B253BD" ); // com.bemaservices.MinistrySafe.Workflow.Action.RefreshTraining:Order

            RockMigrationHelper.UpdateWorkflowActionType( "47CAD594-D718-4C71-B71C-28FC9413B86B", "Refresh Training Link", 2, "CE2B15F5-37A6-48E0-92D3-E2DD923596CE", true, false, "", "", 1, "", "92BC59A9-D9AE-4ACB-B50B-27E84B2E62CD" ); // Awareness Training (MinistrySafe):Send Reminder and Close Workflow:Refresh Training Link
            
            RockMigrationHelper.AddActionTypeAttributeValue( "92BC59A9-D9AE-4ACB-B50B-27E84B2E62CD", "7F6B8D47-5098-418A-9DC2-B8AC20A9424D", @"True" ); // Awareness Training (MinistrySafe):Send Reminder and Close Workflow:Refresh Training Link:Active
            RockMigrationHelper.AddActionTypeAttributeValue( "92BC59A9-D9AE-4ACB-B50B-27E84B2E62CD", "DC3661CA-4018-4AE5-83B2-CCE9BB26840C", @"ddd130f7-2d6c-4473-bdc0-3a67a92dbacd" ); // Awareness Training (MinistrySafe):Send Reminder and Close Workflow:Refresh Training Link:Person Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "92BC59A9-D9AE-4ACB-B50B-27E84B2E62CD", "A03289C0-0068-4F7E-9D3A-2932F649AE27", @"96aaf10c-a1c5-4e25-9570-d7ab5477a638" ); // Awareness Training (MinistrySafe):Send Reminder and Close Workflow:Refresh Training Link:Direct Login Url
        }



        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
