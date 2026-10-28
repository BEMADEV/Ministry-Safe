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
using Rock.Plugin;

namespace com.bemaservices.MinistrySafe.Migrations
{
    [MigrationNumber( 20, "1.18.0" )]
    public class WebhookAuthentication : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddOrUpdateEntityAttribute(
                "com.bemaservices.MinistrySafe.MinistrySafe",
                Rock.SystemGuid.FieldType.TEXT,
                "",
                "",
                "Webhook Authentication Username",
                "Webhook Authentication Username",
                @"Optional HTTP Basic Authentication username for incoming MinistrySafe webhooks.",
                3,
                @"",
                "742D6A81-BA39-43A0-8CE0-A66FBE0F2284",
                MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_WEBHOOK_USERNAME );

            RockMigrationHelper.AddOrUpdateEntityAttribute(
                "com.bemaservices.MinistrySafe.MinistrySafe",
                Rock.SystemGuid.FieldType.ENCRYPTED_TEXT,
                "",
                "",
                "Webhook Authentication Password",
                "Webhook Authentication Password",
                @"Optional HTTP Basic Authentication password for incoming MinistrySafe webhooks. Stored encrypted.",
                4,
                @"",
                "32BB2C00-EB64-4120-9096-8D117884DCFF",
                MinistrySafeConstants.MINISTRYSAFE_ATTRIBUTE_WEBHOOK_PASSWORD );
        }

        public override void Down()
        {
        }
    }
}
