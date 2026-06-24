using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Response
{
    /// <summary>
    /// Represents an error response from MinistrySafe API v3
    /// </summary>
    public class ErrorResponseV3
    {
        /// <summary>
        /// Gets or sets the error message
        /// </summary>
        [JsonProperty( "error" )]
        public string Error { get; set; }

        /// <summary>
        /// Gets or sets the detailed message
        /// </summary>
        [JsonProperty( "message" )]
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the array of error messages (for validation errors)
        /// </summary>
        [JsonProperty( "errors" )]
        public string[] Errors { get; set; }
    }
}
