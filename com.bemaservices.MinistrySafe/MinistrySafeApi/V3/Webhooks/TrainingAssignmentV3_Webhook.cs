using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings
{
    /// <summary>
    /// Represents a training assignment webhook payload from MinistrySafe API v3.
    /// </summary>
    public class TrainingAssignmentV3_Webhook
    {
        /// <summary>
        /// Gets or sets the event value (completed or module_completed).
        /// </summary>
        [JsonProperty( "event" )]
        public string Event { get; set; }

        /// <summary>
        /// Gets or sets the training attempt identifier.
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the training is completed.
        /// </summary>
        [JsonProperty( "completed" )]
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user passed.
        /// </summary>
        [JsonProperty( "passed" )]
        public bool? Passed { get; set; }

        /// <summary>
        /// Gets or sets the quiz score.
        /// </summary>
        [JsonProperty( "score" )]
        public int? Score { get; set; }

        /// <summary>
        /// Gets or sets when the training was assigned.
        /// </summary>
        [JsonProperty( "created_at" )]
        public string CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the training due date.
        /// </summary>
        [JsonProperty( "due_date" )]
        public string DueDate { get; set; }

        /// <summary>
        /// Gets or sets the payment status.
        /// </summary>
        [JsonProperty( "payment_status" )]
        public string PaymentStatus { get; set; }

        /// <summary>
        /// Gets or sets the direct link to the certificate.
        /// </summary>
        [JsonProperty( "certificate_url" )]
        public string CertificateUrl { get; set; }

        /// <summary>
        /// Gets or sets the participant object.
        /// </summary>
        [JsonProperty( "participant" )]
        public TrainingAssignmentParticipantV3_Webhook Participant { get; set; }

        /// <summary>
        /// Gets or sets the training object.
        /// </summary>
        [JsonProperty( "training" )]
        public TrainingAssignmentTrainingV3_Webhook Training { get; set; }

        /// <summary>
        /// Gets or sets field names that changed.
        /// </summary>
        [JsonProperty( "changes" )]
        public List<string> Changes { get; set; }
    }

    public class TrainingAssignmentParticipantV3_Webhook
    {
        /// <summary>
        /// Gets or sets the participant identifier.
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the participant first name.
        /// </summary>
        [JsonProperty( "first_name" )]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the participant last name.
        /// </summary>
        [JsonProperty( "last_name" )]
        public string LastName { get; set; }
    }

    public class TrainingAssignmentTrainingV3_Webhook
    {
        /// <summary>
        /// Gets or sets the training short name.
        /// </summary>
        [JsonProperty( "short_name" )]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the training language.
        /// </summary>
        [JsonProperty( "language" )]
        public string Language { get; set; }
    }
}
