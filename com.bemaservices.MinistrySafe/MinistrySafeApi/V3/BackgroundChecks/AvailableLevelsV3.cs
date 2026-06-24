using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3
{
    /// <summary>
    /// Represents a background check type from MinistrySafe API v3
    /// </summary>
    public class AvailableLevelsV3
    {
        /// <summary>
        /// Gets or sets the candidate ID.
        /// </summary>
        /// <value>The candidate ID.</value>
        [JsonProperty( "levels" )]
        public List<BackgroundCheckLevelV3> Levels { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonProperty( "additional_notes" )]
        public List<String> AdditionalNotes { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonProperty( "upfront_payment_enabled" )]
        public bool UpfrontPaymentEnabled { get; set; }
    }
}
