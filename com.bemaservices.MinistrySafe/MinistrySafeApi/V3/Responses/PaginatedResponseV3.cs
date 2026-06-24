using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Response
{
    /// <summary>
    /// Represents a paginated response from MinistrySafe API v3
    /// </summary>
    /// <typeparam name="T">The type of data in the response</typeparam>
    public class PaginatedResponseV3<T>
    {
        /// <summary>
        /// Gets or sets the data items for the current page
        /// </summary>
        [JsonProperty( "data" )]
        public T[] Data { get; set; }

        /// <summary>
        /// Gets or sets the current page number
        /// </summary>
        [JsonProperty( "page" )]
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the page size
        /// </summary>
        [JsonProperty( "page_size" )]
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of pages
        /// </summary>
        [JsonProperty( "total_pages" )]
        public int TotalPages { get; set; }
    }
}
