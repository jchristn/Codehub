namespace CodeHub.Core.Models
{
    /// <summary>
    /// Per-repository progress within an in-flight scan.
    /// </summary>
    public class ScanRepoProgress
    {
        #region Public-Members

        /// <summary>
        /// Repository name (directory name).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Repository path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Status: Pending, Scanning, Done, or Failed.
        /// </summary>
        public string Status { get; set; } = "Pending";

        #endregion
    }
}
