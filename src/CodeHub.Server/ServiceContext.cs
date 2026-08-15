namespace CodeHub.Server
{
    using CodeHub.Core.Database;
    using CodeHub.Core.Serialization;
    using CodeHub.Core.Services;
    using CodeHub.Core.Settings;
    using CodeHub.Server.Services;
    using SyslogLogging;

    /// <summary>
    /// Shared dependencies passed to route registrars.
    /// </summary>
    public class ServiceContext
    {
        /// <summary>
        /// External tool launcher (Explorer, terminal, Claude, Codex).
        /// </summary>
        public LauncherService Launcher { get; set; }

        /// <summary>
        /// Settings writer (applies dashboard edits and persists codehub.json).
        /// </summary>
        public SettingsWriter SettingsWriter { get; set; }

        /// <summary>
        /// Static content service serving the dashboard at /dashboard.
        /// </summary>
        public StaticContentService StaticContent { get; set; }

        /// <summary>
        /// Database driver.
        /// </summary>
        public DatabaseDriverBase Db { get; set; }

        /// <summary>
        /// Scan-selection service (DB-backed include/exclude directory set).
        /// </summary>
        public SelectionService Selection { get; set; }

        /// <summary>
        /// JSON serializer.
        /// </summary>
        public Serializer Serializer { get; set; }

        /// <summary>
        /// Application settings.
        /// </summary>
        public CodeHubSettings Settings { get; set; }

        /// <summary>
        /// Scan service.
        /// </summary>
        public ScanService Scan { get; set; }

        /// <summary>
        /// GitHub service.
        /// </summary>
        public GitHubService GitHub { get; set; }

        /// <summary>
        /// Logging module.
        /// </summary>
        public LoggingModule Logging { get; set; }
    }
}
