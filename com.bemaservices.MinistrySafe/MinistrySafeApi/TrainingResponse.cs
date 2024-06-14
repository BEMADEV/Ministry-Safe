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
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi
{
    /// <summary>
    /// JSON return structure for the create invitation API call's response.
    /// </summary>
    internal class TrainingResponse
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty( "id" )]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        [JsonProperty( "first_name" )]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        [JsonProperty( "last_name" )]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>The email.</value>
        [JsonProperty( "email" )]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the person alias identifier.
        /// </summary>
        /// <value>The person alias identifier.</value>
        [JsonProperty( "external_id" )]
        public string PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        [JsonProperty( "score" )]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets the type of the user.
        /// </summary>
        /// <value>The type of the user.</value>
        [JsonProperty( "user_type" )]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the direct login URL.
        /// </summary>
        /// <value>The direct login URL.</value>
        [JsonProperty( "direct_login_url" )]
        public string DirectLoginUrl { get; set; }

        /// <summary>
        /// Gets or sets the completed date time.
        /// </summary>
        /// <value>The completed date time.</value>
        [JsonProperty( "complete_date" )]
        public string CompletedDateTime { get; set; }
    }
}