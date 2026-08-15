namespace CodeHub.Server.Services
{
    using System;
    using CodeHub.Core.Settings;

    /// <summary>
    /// Holds the live settings object and its backing file, and applies edits from the dashboard,
    /// mutating the existing section objects (so live references keep working) and persisting to disk.
    /// </summary>
    public class SettingsWriter
    {
        #region Public-Members

        /// <summary>
        /// The live settings instance.
        /// </summary>
        public CodeHubSettings Settings { get; }

        /// <summary>
        /// Backing file path.
        /// </summary>
        public string FilePath { get; }

        #endregion

        #region Private-Members

        private readonly object _Lock = new object();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Live settings.</param>
        /// <param name="filePath">Backing file path.</param>
        public SettingsWriter(CodeHubSettings settings, string filePath)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Apply an incoming settings object onto the live settings and persist. Section objects are
        /// mutated in place so services holding references observe the changes.
        /// </summary>
        /// <param name="incoming">Incoming settings.</param>
        public void ApplyAndSave(CodeHubSettings incoming)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            lock (_Lock)
            {
                if (incoming.Webserver != null)
                {
                    Settings.Webserver.Hostname = incoming.Webserver.Hostname;
                    Settings.Webserver.Port = incoming.Webserver.Port;
                    Settings.Webserver.Ssl = incoming.Webserver.Ssl;
                    Settings.Webserver.EnableOpenApi = incoming.Webserver.EnableOpenApi;
                }
                if (incoming.Cors != null)
                {
                    Settings.Cors.Enable = incoming.Cors.Enable;
                    Settings.Cors.AllowOrigin = incoming.Cors.AllowOrigin;
                    Settings.Cors.AllowMethods = incoming.Cors.AllowMethods;
                    Settings.Cors.AllowHeaders = incoming.Cors.AllowHeaders;
                }
                if (incoming.Authentication != null && !String.IsNullOrWhiteSpace(incoming.Authentication.ApiKey))
                {
                    Settings.Authentication.ApiKey = incoming.Authentication.ApiKey;
                }
                if (incoming.Database != null)
                {
                    Settings.Database.Type = incoming.Database.Type;
                    Settings.Database.Filename = incoming.Database.Filename;
                }
                if (incoming.Directories != null)
                {
                    if (incoming.Directories.RootPaths != null) Settings.Directories.RootPaths = incoming.Directories.RootPaths;
                    if (incoming.Directories.ExcludeDirectories != null) Settings.Directories.ExcludeDirectories = incoming.Directories.ExcludeDirectories;
                }
                if (incoming.Scan != null)
                {
                    Settings.Scan.IntervalHours = incoming.Scan.IntervalHours;
                    Settings.Scan.ScanOnStartup = incoming.Scan.ScanOnStartup;
                    Settings.Scan.MaxConcurrency = incoming.Scan.MaxConcurrency;
                    Settings.Scan.DependencyCheck = incoming.Scan.DependencyCheck;
                }
                if (incoming.GitHub != null)
                {
                    Settings.GitHub.PersonalAccessToken = incoming.GitHub.PersonalAccessToken ?? Settings.GitHub.PersonalAccessToken;
                    Settings.GitHub.Owner = incoming.GitHub.Owner;
                }
                if (incoming.Logging != null)
                {
                    Settings.Logging.ConsoleLogging = incoming.Logging.ConsoleLogging;
                    Settings.Logging.EnableColors = incoming.Logging.EnableColors;
                    Settings.Logging.FileLogging = incoming.Logging.FileLogging;
                    Settings.Logging.LogDirectory = incoming.Logging.LogDirectory;
                    Settings.Logging.LogFilename = incoming.Logging.LogFilename;
                    Settings.Logging.MinimumSeverity = incoming.Logging.MinimumSeverity;
                }
                if (incoming.RequestHistory != null)
                {
                    Settings.RequestHistory.Enabled = incoming.RequestHistory.Enabled;
                    Settings.RequestHistory.MaxRequestBodyBytes = incoming.RequestHistory.MaxRequestBodyBytes;
                    Settings.RequestHistory.MaxResponseBodyBytes = incoming.RequestHistory.MaxResponseBodyBytes;
                    Settings.RequestHistory.RetentionDays = incoming.RequestHistory.RetentionDays;
                }
                if (incoming.ModelRunner != null)
                {
                    Settings.ModelRunner.EndpointBaseUrl = incoming.ModelRunner.EndpointBaseUrl;
                    Settings.ModelRunner.ApiType = incoming.ModelRunner.ApiType;
                    Settings.ModelRunner.ApiKey = incoming.ModelRunner.ApiKey ?? Settings.ModelRunner.ApiKey;
                    Settings.ModelRunner.ModelName = incoming.ModelRunner.ModelName;
                }

                Settings.ToFile(FilePath);
            }
        }

        #endregion
    }
}
