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
namespace com.bemaservices.MinistrySafe.Constants
{
    /// <summary>
    /// This class holds MinistrySafe settings.
    /// </summary>
    public static class MinistrySafeConstants
    {

        /// <summary>
        /// The URL where the token for the account is retrieved
        /// </summary>
        public const string MINISTRYSAFE_TOKEN_URL = "oauth/tokens";

        /// <summary>
        /// The typename prefix
        /// </summary>
        public const string MINISTRYSAFE_TYPENAME_PREFIX = "MinistrySafe - ";

        /// <summary>
        /// The login URL
        /// </summary>
        public const string MINISTRYSAFE_APISERVER = "https://safetysystem.abusepreventionsystems.com/api/";

        /// <summary>
        /// The staging login URL
        /// </summary>
        public const string MINISTRYSAFE_STAGING_APISERVER = "https://staging.ministrysafe.com/api/";

        /// <summary>
        /// The candidates URL
        /// </summary>
        public const string MINISTRYSAFE_USERS_URL = "v2/users";

        /// <summary>
        /// The packages URL
        /// </summary>
        public const string MINISTRYSAFE_PACKAGES_URL = "v2/custom_background_check_packages";

        /// <summary>
        /// The ministrysafe tags URL
        /// </summary>
        public const string MINISTRYSAFE_TAGS_URL = "v2/tags";

        /// <summary>
        /// The ministrysafe tags URL
        /// </summary>
        public const string MINISTRYSAFE_TRAININGS_URL = "v2/trainings";

        /// <summary>
        /// The report URL
        /// </summary>
        public const string MINISTRYSAFE_BACKGROUNDCHECK_URL = "v2/background_checks";

        /// <summary>
        /// The default checkr workflow type name
        /// </summary>
        public const string MINISTRYSAFE_WORKFLOW_TYPE_NAME = "MinistrySafe Safe Training";
    }
}