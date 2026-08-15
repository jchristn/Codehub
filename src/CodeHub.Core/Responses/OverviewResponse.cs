namespace CodeHub.Core.Responses
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Aggregate health for the Home/Overview page.
    /// </summary>
    public class OverviewResponse
    {
        #region Public-Members

        /// <summary>
        /// Total number of included repositories.
        /// </summary>
        public int TotalRepositories { get; set; } = 0;

        /// <summary>
        /// Number of repositories with at least one red signal.
        /// </summary>
        public int NeedsAttention { get; set; } = 0;

        /// <summary>
        /// Repositories with overall green health.
        /// </summary>
        public int GreenCount { get; set; } = 0;

        /// <summary>
        /// Repositories with overall yellow health.
        /// </summary>
        public int YellowCount { get; set; } = 0;

        /// <summary>
        /// Repositories with overall red health.
        /// </summary>
        public int RedCount { get; set; } = 0;

        /// <summary>
        /// C# repositories with no automated tests.
        /// </summary>
        public int ReposWithoutTests { get; set; } = 0;

        /// <summary>
        /// Web-service repositories with no telemetry.
        /// </summary>
        public int WebServicesWithoutTelemetry { get; set; } = 0;

        /// <summary>
        /// Repositories with high or critical vulnerabilities.
        /// </summary>
        public int ReposWithHighCves { get; set; } = 0;

        /// <summary>
        /// Repositories with outdated dependencies.
        /// </summary>
        public int ReposWithOutdatedDeps { get; set; } = 0;

        /// <summary>
        /// Timestamp of the last completed scan.
        /// </summary>
        public DateTime? LastScannedUtc { get; set; } = null;

        /// <summary>
        /// Whether a scan is currently running.
        /// </summary>
        public bool IsScanning { get; set; } = false;

        /// <summary>
        /// The worst-scoring repositories, for the attention list.
        /// </summary>
        public List<RepositoryListItem> AttentionList { get; set; } = new List<RepositoryListItem>();

        #endregion
    }
}
