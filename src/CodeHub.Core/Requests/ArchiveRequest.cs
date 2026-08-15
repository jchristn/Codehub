namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to archive or unarchive a repository on GitHub.
    /// </summary>
    public class ArchiveRequest
    {
        #region Public-Members

        /// <summary>
        /// True to archive, false to unarchive.
        /// </summary>
        public bool Archived { get; set; }

        #endregion
    }
}
