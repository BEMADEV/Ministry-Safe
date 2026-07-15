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
    [MigrationNumber( 18, "1.16.0" )]
    public partial class Hotfixes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddActionTypeAttributeValue( "7602EB3D-C02E-4D4B-8D1C-EBB843DF3BF2", "3DF1CCE7-5A18-4DC5-9DD5-6F34CEAB40B6", @"{{ 'Now' | Date:'yyyy-MM-ddTHH:mm:ss' }}" ); // Awareness Training (MinistrySafe):Submit Request:Add Step:Start Date|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "07BB3139-EFF7-432A-9AFD-D060C19F81A5", "94689BDE-493E-4869-A614-2D54822D747C", @"{{ 'Now' | Date:'yyyy-MM-ddTHH:mm:ss' }}" ); // Awareness Training (MinistrySafe):Submit Request:Set Training Request Date:Value|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "44B246E6-5EA9-4562-B4A5-6EC14340C350", "3DF1CCE7-5A18-4DC5-9DD5-6F34CEAB40B6", @"{{ 'Now' | Date:'yyyy-MM-ddTHH:mm:ss' }}" ); // Awareness Training (MinistrySafe):Process Result:Add Step if Does Not Exist:Start Date|Attribute Value
            RockMigrationHelper.AddActionTypeAttributeValue( "15C6C2E1-99F0-4A40-91E4-F2175FF9A7B9", "0415C959-BF89-4D19-9C47-3AB1098E1FBA", @"{{ 'Now' | Date:'yyyy-MM-ddTHH:mm:ss' }}" ); // Awareness Training (MinistrySafe):Process Result:Complete Step:Property Value|Property Value Attribute
            RockMigrationHelper.AddActionTypeAttributeValue( "2C33553C-CF30-47A2-B1AF-21C73DDBA83E", "94689BDE-493E-4869-A614-2D54822D747C", @"{{ 'Now' | Date:'yyyy-MM-ddTHH:mm:ss' }}" ); // Awareness Training (MinistrySafe):Send Reminder and Close Workflow:Set Training Request Date:Value|Attribute Value
        }



        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
        }
    }
}
