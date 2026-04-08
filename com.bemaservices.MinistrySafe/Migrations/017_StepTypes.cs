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
            throw new NotImplementedException();
            AddStepProgram();
            AddNewWorkflow();
            AddBadge();
        }

        private void AddBadge()
        {
            throw new NotImplementedException();
        }

        private void AddNewWorkflow()
        {
            throw new NotImplementedException();
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
