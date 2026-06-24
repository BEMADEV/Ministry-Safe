using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3
{
    /// <summary>
    /// Represents a background check type from MinistrySafe API v3
    /// </summary>
    public class BackgroundCheckLevelV3
    {
        /// <summary>
        /// Gets or sets the candidate ID.
        /// </summary>
        /// <value>The candidate ID.</value>
        [JsonProperty( "id" )]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonProperty( "name" )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        /// <value>The price.</value>
        [JsonProperty( "price" )]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [JsonProperty( "processing_time" )]
        public string ProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonProperty( "details" )]
        public List<String> Details { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [JsonProperty( "additional_notes" )]
        public List<String> AdditionalNotes { get; set; }
    }
}
