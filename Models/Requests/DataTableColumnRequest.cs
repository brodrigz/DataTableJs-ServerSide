using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTableJs.ServerSide.Models.Requests
{
    public sealed class DataTableColumnRequest
    {
        /// <summary>
        /// The property name or field bound to the column's data in the source.
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }

        /// <summary>
        /// Optional column name used for more advanced setups. Often blank.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the column is considered when performing global search via the search bar.
        /// <para>Even when disabled, the column can still be filtered using the column-specific search</para>
        /// </summary>
        [JsonProperty("searchable")]
        public bool GlobalSearchable { get; set; }

        /// <summary>
        /// Indicates whether the column is orderable (sortable).
        /// </summary>
        [JsonProperty("orderable")]
        public bool Orderable { get; set; }

        /// <summary>
        /// Column-specific search term and options. Can be null if no per-column search is applied.
        /// </summary>
        [JsonProperty("search")]
        public DataTableSearchRequest Search { get; set; }
    }
}
