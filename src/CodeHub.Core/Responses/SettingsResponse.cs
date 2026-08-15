namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// Redacted view of server configuration for the Settings page.
    /// </summary>
    public class SettingsResponse
    {
        #region Public-Members

        /// <summary>
        /// Base root paths the directory picker browses.
        /// </summary>
        public List<string> RootPaths { get; set; } = new List<string>();

        /// <summary>
        /// Number of included directory selections in the scan_selections table.
        /// </summary>
        public int SelectedCount { get; set; } = 0;

        /// <summary>
        /// Number of excluded directory entries in the scan_selections table.
        /// </summary>
        public int ExcludedCount { get; set; } = 0;

        /// <summary>
        /// Automatic scan interval in hours.
        /// </summary>
        public int IntervalHours { get; set; }

        /// <summary>
        /// Whether a scan runs at startup.
        /// </summary>
        public bool ScanOnStartup { get; set; }

        /// <summary>
        /// Maximum concurrent collection tasks.
        /// </summary>
        public int MaxConcurrency { get; set; }

        /// <summary>
        /// Whether dependency checks are enabled.
        /// </summary>
        public bool DependencyCheck { get; set; }

        /// <summary>
        /// Whether a GitHub token is configured (the token itself is never returned).
        /// </summary>
        public bool GitHubConfigured { get; set; }

        /// <summary>
        /// Default GitHub owner.
        /// </summary>
        public string GitHubOwner { get; set; }

        /// <summary>
        /// Database provider.
        /// </summary>
        public string DatabaseType { get; set; }

        /// <summary>
        /// Bind hostname.
        /// </summary>
        public string Hostname { get; set; }

        /// <summary>
        /// Bind port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Whether request history capture is enabled.
        /// </summary>
        public bool RequestHistoryEnabled { get; set; }

        /// <summary>
        /// Software version.
        /// </summary>
        public string Version { get; set; }

        #endregion
    }
}
