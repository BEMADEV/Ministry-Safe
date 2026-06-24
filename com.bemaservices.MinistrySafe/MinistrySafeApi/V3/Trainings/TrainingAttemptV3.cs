using System;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings
{
    /// <summary>
    /// Represents a training from MinistrySafe API v3
    /// </summary>
    public class TrainingAttemptV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        [JsonProperty( "completed" )]
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets the training code
        /// </summary>
        [JsonProperty( "passed" )]
        public bool Passed { get; set; }

        /// <summary>
        /// Gets or sets the completion percentage (0-100)
        /// </summary>
        [JsonProperty( "score" )]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets the completion date
        /// </summary>
        [JsonProperty( "created_at" )]
        public DateTime? CreationDate { get; set; }

        /// <summary>
        /// Gets or sets the completion date
        /// </summary>
        [JsonProperty( "due_date" )]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the completion date
        /// </summary>
        [JsonProperty( "completed_at" )]
        public DateTime? CompletionDate { get; set; }

        /// <summary>
        /// Gets or sets the link to the training
        /// </summary>
        [JsonProperty( "payment_status" )]
        public string PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the training link expiration date
        /// </summary>
        [JsonProperty( "certificate_url" )]
        public string CertificateUrl { get; set; }

        /// <summary>
        /// Gets or sets the package ID
        /// </summary>
        [JsonProperty( "participant" )]
        public TrainingAttemptParticipantV3 User { get; set; }

        /// <summary>
        /// Gets or sets the user's first name
        /// </summary>
        [JsonProperty( "training" )]
        public TrainingAttemptTrainingV3 TrainingType { get; set; }
    }

    public class TrainingAttemptParticipantV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        [JsonProperty( "first_name" )]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the training code
        /// </summary>
        [JsonProperty( "last_name" )]
        public string LastName { get; set; }

    }

    public class TrainingAttemptTrainingV3
    {

        /// <summary>
        /// Gets or sets the user ID
        /// </summary>
        [JsonProperty( "short_name" )]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the training code
        /// </summary>
        [JsonProperty( "language" )]
        public string Language { get; set; }

    }
}
