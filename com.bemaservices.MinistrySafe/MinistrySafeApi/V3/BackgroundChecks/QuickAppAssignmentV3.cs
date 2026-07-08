using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks
{
    /// <summary>
    /// Request model for assigning a quick app background check in MinistrySafe API v3
    /// </summary>
    public class QuickAppAssignmentV3 : BackgroundCheckAssignmentV3
    {
        /// <summary>
        /// Gets or sets whether the applicant is over 13 years old
        /// </summary>
        [JsonProperty( "age_over_13" )]
        public bool AgeOver13 { get; set; }

        /// <summary>
        /// Gets or sets whether upfront payment is required
        /// </summary>
        [JsonProperty( "upfront_payment" )]
        public bool UpfrontPayment { get; set; }
    }
}
