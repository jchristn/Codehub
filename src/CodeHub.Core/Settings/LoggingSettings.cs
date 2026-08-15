namespace CodeHub.Core.Settings
{
    /// <summary>
    /// Logging settings.
    /// </summary>
    public class LoggingSettings
    {
        #region Public-Members

        /// <summary>
        /// Whether to log to the console.
        /// </summary>
        public bool ConsoleLogging { get; set; } = true;

        /// <summary>
        /// Whether to enable colored console output.
        /// </summary>
        public bool EnableColors { get; set; } = true;

        /// <summary>
        /// Whether to log to a file.
        /// </summary>
        public bool FileLogging { get; set; } = true;

        /// <summary>
        /// Log directory.
        /// </summary>
        public string LogDirectory { get; set; } = "logs";

        /// <summary>
        /// Log filename.
        /// </summary>
        public string LogFilename { get; set; } = "codehub.log";

        /// <summary>
        /// Minimum severity (0=Debug .. 5=Alert), matching SyslogLogging.Severity.
        /// </summary>
        public int MinimumSeverity { get; set; } = 1;

        #endregion
    }
}
