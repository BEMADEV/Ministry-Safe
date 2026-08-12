using System.Collections.Generic;
using Newtonsoft.Json;

namespace com.bemaservices.MinistrySafe.MinistrySafeApi.V3.Trainings
{
    /// <summary>
    /// Represents a step type from MinistrySafe API v3
    /// </summary>
    public class TrainingTypeV3
    {
        /// <summary>
        /// Gets or sets the step type name
        /// </summary>
        [JsonProperty( "short_name" )]
        public string ShortName { get; set; }

        //TODO: Not implemented in API v3 yet
        /// <summary>
        /// Gets or sets the step type name
        /// </summary>
        [JsonProperty( "description" )]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the step type code
        /// </summary>
        [JsonProperty( "languages" )]
        public List<TrainingTypeVariantV3> Languages { get; set; }
    }

    public class TrainingTypeVariantV3
    {
        /// <summary>
        /// Gets or sets the unique identifier
        /// </summary>
        [JsonProperty( "id" )]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the step type name
        /// </summary>
        [JsonProperty( "name" )]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the step type code
        /// </summary>
        [JsonProperty( "language" )]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets the step type code
        /// </summary>
        [JsonProperty( "description" )]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the step type code
        /// </summary>
        [JsonProperty( "price" )]
        public decimal Price { get; set; }
    }
}
