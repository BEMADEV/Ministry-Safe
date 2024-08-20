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
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi
{
    /// <summary>
    /// Invitation webhook
    /// </summary>
    /// <seealso cref="com.bemaservices.MinistrySafe.MinistrySafeApi.TrainingWebhook" />
    internal class TrainingWebhook
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        [JsonProperty( "user_id" )]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the external identifier.
        /// </summary>
        /// <value>The external identifier.</value>
        [JsonProperty( "external_id" )]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        [JsonProperty( "score" )]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets the complete date time.
        /// </summary>
        /// <value>The complete date time.</value>
        [JsonProperty( "complete_date" )]
        public DateTime CompleteDateTime { get; set; }

        /// <summary>
        /// Gets or sets the survey code.
        /// </summary>
        /// <value>The survey code.</value>
        [JsonProperty( "survey_code" )]
        public string SurveyCode { get; set; }

        /// <summary>
        /// Gets or sets the certificate URL.
        /// </summary>
        /// <value>The certificate URL.</value>
        [JsonProperty( "certificate_url" )]
        public string CertificateUrl { get; set; }
    }
}
