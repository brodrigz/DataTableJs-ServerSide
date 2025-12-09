using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTableJs.ServerSide.Models.Requests
{
    /// <summary>
    /// Represents a single column ordering instruction sent by DataTables.
    /// </summary>
    public sealed class DataTableOrderRequest
    {
        /// <summary>
        /// The index of the column to apply ordering to (zero-based).
        /// </summary>
        [JsonProperty("column")]
        public int Column { get; set; }

        /// <summary>
        /// The direction of the sort: "asc" for ascending, "desc" for descending.
        /// </summary>
        [JsonProperty("dir")]
        public string Dir { get; set; }

        /// <summary>
        /// Name of the ordering column, as defined by columns.name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
