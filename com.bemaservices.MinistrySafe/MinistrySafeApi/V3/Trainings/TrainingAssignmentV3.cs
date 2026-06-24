using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings
{
    /// <summary>
    /// Request model for assigning a training in MinistrySafe API v3
    /// </summary>
    public class TrainingAssignmentV3
    {
        /// <summary>
        /// Gets or sets the user ID (optional)
        /// </summary>
        [JsonProperty( "training_id" )]
        public int? TrainingId { get; set; }

        /// <summary>
        /// Gets or sets the user ID (required)
        /// </summary>
        [JsonProperty( "user_id" )]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the package ID (required)
        /// </summary>
        [JsonProperty( "external_id" )]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the package ID (required)
        /// </summary>
        [JsonProperty( "pco_id" )]
        public string PcoId { get; set; }

        /// <summary>
        /// Gets or sets the package ID (required)
        /// </summary>
        [JsonProperty( "send_email" )]
        public bool SendEmail { get; set; }

        /// <summary>
        /// Gets or sets the package ID (required)
        /// </summary>
        [JsonProperty( "upfront_payment" )]
        public bool UpfrontPayment { get; set; }
    }
}
