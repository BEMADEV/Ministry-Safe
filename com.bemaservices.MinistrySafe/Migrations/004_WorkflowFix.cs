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
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    /// <summary>
    /// Class WorkflowFix.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 4, "1.8.5" )]
    public partial class WorkflowFix : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateWorkflowActivityType( "5876314A-FC4F-4A07-8CA0-A02DE26E55BE", true, "Cancel Request", "Cancels the request prior to submitting to provider and completes the workflow.", false, 7, "748423B3-508B-4FDF-A200-3F3E86BF9182" ); // Ministry Safe Training Request:Cancel Request
            RockMigrationHelper.UpdateWorkflowActionType( "748423B3-508B-4FDF-A200-3F3E86BF9182", "Cancel Workflow", 0, "EEDA4318-F014-4A46-9C76-4C052EF81AA1", true, false, "", "", 1, "", "0C48DBA6-1F6D-4AD8-9A72-F09C135A1AF1" ); // Ministry Safe Training Request:Cancel Request:Cancel Workflow
            RockMigrationHelper.AddActionTypeAttributeValue( "0C48DBA6-1F6D-4AD8-9A72-F09C135A1AF1", "3327286F-C1A9-4624-949D-33E9F9049356", @"Cancelled" ); // Ministry Safe Training Request:Cancel Request:Cancel Workflow:Status|Status Attribute     
        }
        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
         }
    }
}
