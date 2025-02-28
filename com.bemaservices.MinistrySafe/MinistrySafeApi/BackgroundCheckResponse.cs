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
    /// JSON return structure for the create candidate API call's response.
    /// </summary>
    internal class BackgroundCheckResponse
    {
        /// <summary>
        /// Gets or sets the candidate ID.
        /// </summary>
        /// <value>The candidate ID.</value>
        [JsonProperty( "id" )]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the order date.
        /// </summary>
        /// <value>The order date.</value>
        [JsonProperty( "order_date" )]
        public string OrderDate { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        [JsonProperty( "status" )]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the applicant interface URL.
        /// </summary>
        /// <value>The applicant interface URL.</value>
        [JsonProperty( "applicant_interface_url" )]
        public string ApplicantInterfaceUrl { get; set; }

        /// <summary>
        /// Gets or sets the results URL.
        /// </summary>
        /// <value>The results URL.</value>
        [JsonProperty( "results_url" )]
        public string ResultsUrl { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        [JsonProperty( "user_id" )]
        public int? UserId { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonProperty( "level" )]
        public int? Level { get; set; }

        /// <summary>
        /// Gets or sets the custom background check package code.
        /// </summary>
        /// <value>The custom background check package code.</value>
        [JsonProperty( "custom_background_check_package_code" )]
        public string CustomBackgroundCheckPackageCode { get; set; }

        /// <summary>
        /// Gets or sets the complete date.
        /// </summary>
        /// <value>The complete date.</value>
        [JsonProperty( "complete_date" )]
        public string CompleteDate { get; set; }

        /// <summary>
        /// Gets or sets the complete date.
        /// </summary>
        /// <value>The complete date.</value>
        [JsonProperty( "tazwork_flagged" )]
        public bool? TazworkFlagged { get; set; }
    }
}