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
    public static class BackgroundCheckStatuses
    {
        public static string[] AWAITING_APPLICANT = {
            "billing",
            "ordered"
        };

        public static string[] SUBMITTED = {
            "submitted"
        };

        public static string[] CANCELLED = {
            "applicant_withdrawn",
            "archived",
            "cancelled",
            "expired"
        };

        public static string[] COMPLETED_NEEDS_REVIEW = {
            "adverse",
            "adverse_deny_employment",
            "adverse_rescind_offer",
            "approved",
            "pre_adverse",
            "ready",
            "rejected",
            "complete"
        };

        public static string[] COMPLETED_CLEARED = {
            "clear"
        };

        public static string[] DISPUTED = {
            "dispute"
        };

        public static string[] ERROR = {
            "error"
        };

        public static string[] PROCESSING = {
            "pending"
        };
    }
}