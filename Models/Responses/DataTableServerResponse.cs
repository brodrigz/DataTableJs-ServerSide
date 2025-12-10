using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataTableJs.ServerSide.Models.Responses
{
    public static class DataTableServerResponse
    {
        /// <summary>
        /// Creates a success response for DataTables server-side processing.
        /// </summary>
        /// <param name="draw">The draw counter from the request.</param>
        /// <param name="recordsTotal">Total number of records before filtering.</param>
        /// <param name="recordsFiltered">Total number of records after filtering.</param>
        /// <param name="data">The data to be displayed.</param>
        /// <param name="continuationToken"></param>
        /// <returns>A configured DataTablesServerResponse.</returns>
        public static DataTableServerResponse<T> Success<T>(int draw, int recordsTotal, int recordsFiltered, IEnumerable<T> data, string continuationToken = null) where T : class
        {
            return new DataTableServerResponse<T>
            {
                Draw = draw,
                RecordsTotal = recordsTotal,
                RecordsFiltered = recordsFiltered,
                Data = data,
                ContinuationToken = continuationToken
            };
        }

        /// <summary>
        /// Creates an error response for DataTables server-side processing.
        /// </summary>
        /// <param name="draw">The draw counter from the request.</param>
        /// <param name="errorMessage">The error message to display.</param>
        /// <returns>A configured DataTablesServerResponse with the error message.</returns>
        public static DataTableServerResponse<object> Fail(int draw, string errorMessage)
        {
            return new DataTableServerResponse<object>
            {
                Draw = draw,
                Error = errorMessage
            };
        }
    }

    /// <summary>
    /// Represents a server response for DataTables server-side processing.
    /// </summary>
    public class DataTableServerResponse<T> where T : class
    {
        /// <summary>
        /// Gets or sets the draw counter that DataTables is expecting back from the server.
        /// </summary>
        [JsonProperty("draw")]
        public int Draw { get; set; }

        /// <summary>
        /// Gets or sets the total number of records before filtering.
        /// </summary>
        [JsonProperty("recordsTotal")]
        public int RecordsTotal { get; set; }

        /// <summary>
        /// Gets or sets the total number of records after filtering.
        /// </summary>
        [JsonProperty("recordsFiltered")]
        public int RecordsFiltered { get; set; }

        /// <summary>
        /// Gets or sets the data to be displayed in the table.
        /// </summary>
        [JsonProperty("data")]
        public IEnumerable<T> Data { get; set; }

        /// <summary>
        /// Gets or sets an optional error message to be displayed by DataTables.
        /// </summary>
        [JsonProperty("error")]
        public string Error { get; set; }

        /// <summary>
        /// If applicable, a storage continuation token that the client must send back
        /// on the next request. Typically <c>null</c> when the current page is the last page. Optional.
        /// </summary>
        [JsonProperty("continuationToken")]
        public string ContinuationToken { get; set; }
    }
}
