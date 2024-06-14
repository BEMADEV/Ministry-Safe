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
    internal class CreateUserRequest
    {
        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>The email.</value>
        public string email { get; set; }
        /// <summary>
        /// Gets or sets the employee identifier.
        /// </summary>
        /// <value>The employee identifier.</value>
        public string employee_id { get; set; }
        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        public string external_id { get; set; }
        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        public string first_name { get; set; }
        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        public string last_name { get; set; }
        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        public int? score { get; set; }
        /// <summary>
        /// Gets or sets the complete date.
        /// </summary>
        /// <value>The complete date.</value>
        public object complete_date { get; set; }
        /// <summary>
        /// Gets or sets the direct login URL.
        /// </summary>
        /// <value>The direct login URL.</value>
        public string direct_login_url { get; set; }
        /// <summary>
        /// Gets or sets the type of the user.
        /// </summary>
        /// <value>The type of the user.</value>
        public string user_type { get; set; }
    }
}