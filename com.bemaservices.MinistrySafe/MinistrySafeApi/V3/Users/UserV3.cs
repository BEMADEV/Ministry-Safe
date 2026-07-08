using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Users
{
    /// <summary>
    /// Represents a user type from MinistrySafe API v3
    /// </summary>
    public class UserV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int? Id { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "first_name" )]
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "last_name" )]
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "email" )]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user type
        /// </summary>
        [JsonProperty( "user_type" )]
        public string UserType { get; set; }

        /// <summary>
        /// Gets or sets the role
        /// </summary>
        [JsonProperty( "role" )]
        public string Role { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "external_id" )]
        public string ExternalId { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "tags" )]
        public string[] Tags { get; set; }
    }
}
