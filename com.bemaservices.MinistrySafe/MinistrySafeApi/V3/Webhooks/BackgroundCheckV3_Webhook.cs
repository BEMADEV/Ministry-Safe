using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks
{
    /// <summary>
    /// Represents a background check from MinistrySafe API v3
    /// </summary>
    public class BackgroundCheckV3_Webhook
    {
        /// <summary>
        /// Gets or sets the background check identifier.
        /// </summary>      
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the MinistrySafe user identifier.
        /// </summary>
        [JsonProperty( "user_id" )]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the user email.
        /// </summary>
        [JsonProperty( "email" )]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the external user identifier.
        /// </summary>
        [JsonProperty( "external_id" )]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the URL to view results.
        /// </summary>
        [JsonProperty( "results_url" )]
        public string ResultsUrl { get; set; }

        /// <summary>
        /// Gets or sets the completion timestamp.
        /// </summary>
        [JsonProperty( "complete_date" )]
        public string CompleteDate { get; set; }

        /// <summary>
        /// Gets or sets the new status event value (all-changes mode).
        /// </summary>
        [JsonProperty( "event" )]
        public string Event { get; set; }

        /// <summary>
        /// Gets or sets the current background check status (all-changes mode).
        /// </summary>
        [JsonProperty( "status" )]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the field names that changed (all-changes mode).
        /// </summary>
        [JsonProperty( "changes" )]
        public List<string> Changes { get; set; }
    }
}
