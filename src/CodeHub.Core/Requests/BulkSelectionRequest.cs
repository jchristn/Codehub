namespace CodeHub.Core.Requests
{
    using System.Collections.Generic;

    /// <summary>
    /// Request to include multiple directories for scanning in one operation.
    /// </summary>
    public class BulkSelectionRequest
    {
        #region Public-Members

        /// <summary>
        /// Absolute directory paths to include. Empty lines, duplicates, and non-existent
        /// directories are ignored by the server.
        /// </summary>
        public List<string> Paths { get; set; } = new List<string>();

        #endregion
    }
}
