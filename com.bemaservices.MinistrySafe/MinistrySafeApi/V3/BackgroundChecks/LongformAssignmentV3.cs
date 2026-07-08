using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.BackgroundChecks
{
    /// <summary>
    /// Request model for assigning a long form background check in MinistrySafe API v3
    /// </summary>
    public class LongFormAssignmentV3 : BackgroundCheckAssignmentV3
    {
        /// <summary>
        /// Gets or sets the first name
        /// </summary>
        [JsonProperty( "first_name" )]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the last name
        /// </summary>
        [JsonProperty( "last_name" )]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the address
        /// </summary>
        [JsonProperty( "address" )]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the city
        /// </summary>
        [JsonProperty( "city" )]
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the county
        /// </summary>
        [JsonProperty( "county" )]
        public string County { get; set; }

        /// <summary>
        /// Gets or sets the state
        /// </summary>
        [JsonProperty( "state" )]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the zip code
        /// </summary>
        [JsonProperty( "zip" )]
        public string Zip { get; set; }

        /// <summary>
        /// Gets or sets the social security number
        /// </summary>
        [JsonProperty( "ssn" )]
        public string Ssn { get; set; }

        /// <summary>
        /// Gets or sets the date of birth
        /// </summary>
        [JsonProperty( "dob" )]
        public string DateOfBirth { get; set; }

        /// <summary>
        /// Gets or sets the email address
        /// </summary>
        [JsonProperty( "email" )]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the driver license number
        /// </summary>
        [JsonProperty( "driver_license" )]
        public string DriverLicense { get; set; }

        /// <summary>
        /// Gets or sets the driver license state
        /// </summary>
        [JsonProperty( "driver_license_state" )]
        public string DriverLicenseState { get; set; }
    }
}
