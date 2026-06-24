using System;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks
{
    /// <summary>
    /// Represents a background check from MinistrySafe API v3
    /// </summary>
    public class BackgroundCheckV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the order date
        /// </summary>
        [JsonProperty( "order_date" )]
        public DateTime? OrderDate { get; set; }

        /// <summary>
        /// Gets or sets the status (e.g., "ordered", "complete", "consider")
        /// </summary>
        [JsonProperty( "status" )]
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the level (screening tier)
        /// </summary>
        [JsonProperty( "level" )]
        public int? Level { get; set; }

        /// <summary>
        /// Gets or sets the applicant interface URL
        /// </summary>
        [JsonProperty( "applicant_interface_url" )]
        public string ApplicantInterfaceUrl { get; set; }

        /// <summary>
        /// Gets or sets the payment status
        /// </summary>
        [JsonProperty( "payment_status" )]
        public string PaymentStatus { get; set; }
    }
}
