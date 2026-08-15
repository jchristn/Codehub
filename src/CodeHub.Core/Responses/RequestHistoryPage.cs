namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;
    using CodeHub.Core.Models;

    /// <summary>
    /// A page of request-history entries (bodies omitted).
    /// </summary>
    public class RequestHistoryPage
    {
        #region Public-Members

        /// <summary>
        /// Entries on this page.
        /// </summary>
        public List<RequestHistoryEntry> Items { get; set; } = new List<RequestHistoryEntry>();

        /// <summary>
        /// Page number (1-based).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Total number of matching entries.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        #endregion
    }
}
