using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Response
{
    /// <summary>
    /// Response model for a simple success message from MinistrySafe API v3
    /// </summary>
    public class MessageResponseV3
    {
        /// <summary>
        /// Gets or sets the message
        /// </summary>
        [JsonProperty( "message" )]
        public string Message { get; set; }
    }
}
