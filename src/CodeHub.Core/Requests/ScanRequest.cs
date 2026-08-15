namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request body for triggering a scan.
    /// </summary>
    public class ScanRequest
    {
        #region Public-Members

        /// <summary>
        /// Optional repository identifier to scan a single repository. When null, all
        /// included repositories are scanned.
        /// </summary>
        public string RepositoryId { get; set; } = null;

        #endregion
    }
}
