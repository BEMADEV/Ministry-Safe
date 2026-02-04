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
    [MigrationNumber( 14, "1.16.0" )]
    public partial class SurveyCodeUpdates : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedTypeAttribute( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "3EE69CBC-35CE-4496-88CC-8327A447603F", "Price", "Price", "", 2, "", "FAC63431-7DA4-41AB-82CC-257E2BFB9A0E" );
            RockMigrationHelper.AddDefinedTypeAttribute( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Code", "Code", "", 1, "", "FDFB5F3F-4A2B-435C-972D-C0D3243E2634" );
            RockMigrationHelper.AddDefinedTypeAttribute( "95EF81D2-C192-4B9E-A7A3-5E1E90BDA3CE", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Type", "Type", "", 0, "", "0368DB7A-CBB2-41DC-9072-F01D4E6776F1" );

            RockMigrationHelper.UpdateWorkflowTypeAttribute( "5876314A-FC4F-4A07-8CA0-A02DE26E55BE", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Survey Type", "SurveyType", "Value should be the type of MinistrySafe training to request from the vendor.", 9, @"", "FC5D6AD1-5003-4E75-B297-59675444113A", false ); // Awareness Training (MinistrySafe):Survey Type
            RockMigrationHelper.UpdateAttributeQualifier( "FC5D6AD1-5003-4E75-B297-59675444113A", "displaydescription", @"False", "8133162D-3079-4ACD-8D9A-253A1D5F985F" ); // Awareness Training (MinistrySafe):Survey Type:displaydescription

            RockMigrationHelper.UpdateAttributeQualifier( "39A5D0FF-BA35-4CA1-B8DD-1FB40F4660E9", "displaydescription", @"False", "9F879C77-8960-4D70-B849-BF0A25BFD191" ); // MinistrySafe Request Launcher:Trainings:displaydescription
            RockMigrationHelper.UpdateAttributeQualifier( "05E50A5A-DFB8-4656-9210-9027565D7864", "displaydescription", @"False", "FD10024C-D776-4F05-B535-F67026DD5C67" ); // Person Attribute

            List<string> errorMessages = new List<string>();
            MinistrySafe.UpdateSurveyTypes( errorMessages );
        }



        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
