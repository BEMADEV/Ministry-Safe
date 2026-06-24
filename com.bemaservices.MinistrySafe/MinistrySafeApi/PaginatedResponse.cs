// <copyright>
// Copyright by BEMA Software Services
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi
{
    /// <summary>
    /// Generic wrapper for V3 API paginated responses
    /// </summary>
    /// <typeparam name="T">The type of data being returned</typeparam>
    internal class PaginatedResponse<T>
    {
        /// <summary>
        /// Gets or sets the data array
        /// </summary>
        [JsonProperty( "data" )]
        public List<T> Data { get; set; }

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
