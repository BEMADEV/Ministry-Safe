using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks
{
    /// <summary>
    /// Base class for background check assignment requests in MinistrySafe API v3
    /// </summary>
    public abstract class BackgroundCheckAssignmentV3
    {
        /// <summary>
        /// Gets or sets the user ID (required)
        /// </summary>
        [JsonProperty( "user_id" )]
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the external ID
        /// </summary>
        [JsonProperty( "external_id" )]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the PCO ID
        /// </summary>
        [JsonProperty( "pco_id" )]
        public string PcoId { get; set; }

        /// <summary>
        /// Gets or sets the level
        /// </summary>
        [JsonProperty( "level" )]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the custom background check package code
        /// </summary>
        [JsonProperty( "custom_background_check_package_code" )]
        public string CustomBackgroundCheckPackageCode { get; set; }

        /// <summary>
        /// Gets or sets whether the position serves children
        /// </summary>
        [JsonProperty( "child_serving" )]
        public bool ChildServing { get; set; }

        /// <summary>
        /// Gets or sets the employee type
        /// </summary>
        [JsonProperty( "employee_type" )]
        public string EmployeeType { get; set; }

        /// <summary>
        /// Gets or sets the user type
        /// </summary>
        [JsonProperty( "user_type" )]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the salary range
        /// </summary>
        [JsonProperty( "salary_range" )]
        public string SalaryRange { get; set; }

        /// <summary>
        /// Gets or sets whether the applicant self disclosed
        /// </summary>
        [JsonProperty( "applicant_self_disclosed" )]
        public bool ApplicantSelfDisclosed { get; set; }

        /// <summary>
        /// Gets or sets the applicant self disclosed notes
        /// </summary>
        [JsonProperty( "applicant_self_disclosed_notes" )]
        public string ApplicantSelfDisclosedNotes { get; set; }

    }
}
