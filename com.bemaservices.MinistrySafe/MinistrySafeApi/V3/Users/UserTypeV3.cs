using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Users
{
    /// <summary>
    /// Represents a user type from MinistrySafe API v3
    /// </summary>
    public class UserTypeV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the localized name
        /// </summary>
        [JsonProperty( "name" )]
        public LocalizedNameV3 Name { get; set; }

        /// <summary>
        /// Gets or sets the background check type
        /// </summary>
        [JsonProperty( "background_check_type" )]
        public string BackgroundCheckType { get; set; }
    }

    /// <summary>
    /// Represents a localized name (English and Spanish)
    /// </summary>
    public class LocalizedNameV3
    {
        /// <summary>
        /// Gets or sets the English name
        /// </summary>
        [JsonProperty( "en" )]
        public string English { get; set; }

        /// <summary>
        /// Gets or sets the Spanish name
        /// </summary>
        [JsonProperty( "es" )]
        public string Spanish { get; set; }
    }
}
