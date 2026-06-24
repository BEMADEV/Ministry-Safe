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
        /// The ministrysafe users URL
        /// </summary>
        public const string MINISTRYSAFE_USERS_URL = "v3/users";

        /// <summary>
        /// The packages URL (V2 only - may not exist in V3)
        /// </summary>
        public const string MINISTRYSAFE_PACKAGES_URL = "v2/custom_background_check_packages";

        /// <summary>
        /// The ministrysafe tags URL (V2 only - may not exist in V3)
        /// </summary>
        public const string MINISTRYSAFE_TAGS_URL = "v2/tags";

        /// <summary>
        /// The ministrysafe survey codes URL (V2 only - may not exist in V3)
        /// </summary>
        public const string MINISTRYSAFE_SURVEY_CODES_URL = "v2/survey_codes";

        /// <summary>
        /// The ministrysafe trainings URL
        /// </summary>
        public const string MINISTRYSAFE_TRAININGS_URL = "v3/trainings";

        /// <summary>
        /// The ministrysafe backgroundcheck URL
        /// </summary>
        public const string MINISTRYSAFE_BACKGROUNDCHECK_URL = "v3/background_checks";

        /// <summary>
        /// The ministrysafe available levels URL
        /// </summary>
        public const string MINISTRYSAFE_AVAILABLE_LEVELS_URL = "v3/background_checks/levels";

        /// <summary>
        /// The ministrysafe documents URL (V3)
        /// </summary>
        public const string MINISTRYSAFE_DOCUMENTS_URL = "v3/documents";

        /// <summary>
        /// The ministrysafe training attempts URL (V3)
        /// </summary>
        public const string MINISTRYSAFE_TRAINING_ATTEMPTS_URL = "v3/trainings/attempts";

        /// <summary>
        /// The ministrysafe workflow type name
        /// </summary>
        public const string MINISTRYSAFE_WORKFLOW_TYPE_NAME = "MinistrySafe Safe Training";

        /// <summary>
        /// The ministrysafe attribute access token
        /// </summary>
        public const string MINISTRYSAFE_ATTRIBUTE_ACCESS_TOKEN = "AccessToken";

        /// <summary>
        /// The ministrysafe attribute access token
        /// </summary>
        public const string MINISTRYSAFE_ATTRIBUTE_ENABLE_DEBUGGING = "EnableDebugging";

        /// <summary>
        /// The ministrysafe attribute server URL
        /// </summary>
        public const string MINISTRYSAFE_ATTRIBUTE_SERVER_URL = "MinistrySafeServerUrl";
    }
}