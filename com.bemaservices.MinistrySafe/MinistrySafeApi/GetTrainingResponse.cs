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
    /// JSON return structure for the Get Report API Call's Response
    /// </summary>
    internal class GetTrainingResponse
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is winner.
        /// </summary>
        /// <value><c>null</c> if [is winner] contains no value, <c>true</c> if [is winner]; otherwise, <c>false</c>.</value>
        [JsonProperty( "winner" )]
        public bool? IsWinner { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        [JsonProperty( "score" )]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>The created date time.</value>
        [JsonProperty( "created_at" )]
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the complete date time.
        /// </summary>
        /// <value>The complete date time.</value>
        [JsonProperty( "complete_date" )]
        public DateTime? CompleteDateTime { get; set; }

        /// <summary>
        /// Gets or sets the name of the survey.
        /// </summary>
        /// <value>The name of the survey.</value>
        [JsonProperty( "survey_name" )]
        public string SurveyName { get; set; }

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