using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTableJs.ServerSide.Models.Requests
{
    /// <summary>
    /// Represents a search condition, either global or per-column.
    /// </summary>
    public sealed class DataTableSearchRequest
    {
        /// <summary>
        /// The search value entered by the user. Can be null or empty.
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Indicates whether the search term should be interpreted as a regular expression.
        /// </summary>
        [JsonProperty("regex")]
        public bool Regex { get; set; }
    }
}
