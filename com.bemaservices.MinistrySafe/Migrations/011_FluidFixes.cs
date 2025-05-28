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
    /// Class FluidFixes.
    /// Implements the <see cref="Migration" />
    /// </summary>
    /// <seealso cref="Migration" />
    [MigrationNumber( 11, "1.13.0" )]
    public partial class FluidFixes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdateWorkflowActionForm( @"<h1>Background Request Details</h1> <p> {{CurrentPerson.NickName}}, please complete the form below to start the background request process. </p> 
{% assign WarnOfRecent = Workflow | Attribute:'WarnOfRecent' %}
{% if WarnOfRecent == 'Yes' %}    <div class='alert alert-warning'>         Notice: It's been less than a year since this person's last background check was processed.         Please make sure you want to continue with this request!     </div> {% endif %} <hr />", @"", "Submit^fdc397cd-8b4a-436e-bea1-bce2e6717c03^47ddefda-d5c2-42d3-a872-e271f9d71d89^Your request has been submitted successfully.|Cancel^5683E775-B9F3-408C-80AC-94DE0E51CF3A^ab8ed98a-20f9-4e74-912b-430db21e069c^The request has been canceled.", "", false, "", "33264364-ACC6-48F9-B57B-3E4ADEDE84C8" ); // Background Check (MinistrySafe):Initial Request:Get Details
            RockMigrationHelper.UpdateWorkflowActionType( "C68D3D90-AB1A-4732-AE89-B49656859173", "Get Details", 7, "486DC4FA-FCBC-425F-90B0-E606DA8A9F68", true, true, "33264364-ACC6-48F9-B57B-3E4ADEDE84C8", "", 1, "", "F14042CB-7887-4449-9541-DE2E21FE5CE2" ); // Background Check (MinistrySafe):Initial Request:Get Details       
        }
        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
