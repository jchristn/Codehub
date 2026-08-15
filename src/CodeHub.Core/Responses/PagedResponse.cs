namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// A page of results with paging metadata.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    public class PagedResponse<T>
    {
        #region Public-Members

        /// <summary>
        /// Items on this page.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>();

        /// <summary>
        /// Page number (1-based).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize { get; set; } = 25;

        /// <summary>
        /// Total number of matching items.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        #endregion
    }
}
