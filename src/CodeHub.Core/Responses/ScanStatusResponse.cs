namespace CodeHub.Core.Responses
{
    using System;
    using System.Collections.Generic;
    using CodeHub.Core.Models;

    /// <summary>
    /// Current and last scan state.
    /// </summary>
    public class ScanStatusResponse
    {
        #region Public-Members

        /// <summary>
        /// Whether a scan is currently running.
        /// </summary>
        public bool IsScanning { get; set; } = false;

        /// <summary>
        /// The in-flight scan run, if any.
        /// </summary>
        public ScanRun Current { get; set; } = null;

        /// <summary>
        /// The most recent completed scan run, if any.
        /// </summary>
        public ScanRun Latest { get; set; } = null;

        /// <summary>
        /// Timestamp of the last completed scan.
        /// </summary>
        public DateTime? LastScannedUtc { get; set; } = null;

        /// <summary>
        /// Timestamp of the next scheduled scan, or null when scheduling is disabled.
        /// </summary>
        public DateTime? NextScanUtc { get; set; } = null;

        /// <summary>
        /// Number of repositories known to the system.
        /// </summary>
        public int RepositoryCount { get; set; } = 0;

        /// <summary>
        /// Per-repository progress for the in-flight scan, or null when idle.
        /// </summary>
        public List<ScanRepoProgress> Repositories { get; set; } = null;

        #endregion
    }
}
