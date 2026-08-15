namespace CodeHub.Core.Settings
{
    using System;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using CodeHub.Core.Database;

    /// <summary>
    /// Root application settings. Every value lives inside a named section — there are no
    /// top-level discrete values. Loaded from JSON and overridable via environment variables.
    /// </summary>
    public class CodeHubSettings
    {
        #region Public-Members

        /// <summary>
        /// HTTP hosting settings.
        /// </summary>
        public WebserverSettings Webserver { get; set; } = new WebserverSettings();

        /// <summary>
        /// CORS settings.
        /// </summary>
        public CorsSettings Cors { get; set; } = new CorsSettings();

        /// <summary>
        /// Static-key authentication settings.
        /// </summary>
        public AuthSettings Authentication { get; set; } = new AuthSettings();

        /// <summary>
        /// Database settings.
        /// </summary>
        public DatabaseSettings Database { get; set; } = new DatabaseSettings();

        /// <summary>
        /// Directory settings (base roots + skipped directory names).
        /// </summary>
        public DirectorySettings Directories { get; set; } = new DirectorySettings();

        /// <summary>
        /// Scan scheduling settings.
        /// </summary>
        public ScanSettings Scan { get; set; } = new ScanSettings();

        /// <summary>
        /// GitHub integration settings.
        /// </summary>
        public GitHubSettings GitHub { get; set; } = new GitHubSettings();

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging { get; set; } = new LoggingSettings();

        /// <summary>
        /// Request-history capture settings.
        /// </summary>
        public RequestHistorySettings RequestHistory { get; set; } = new RequestHistorySettings();

        /// <summary>
        /// Model-runner settings (reserved for a future feature).
        /// </summary>
        public ModelRunnerSettings ModelRunner { get; set; } = new ModelRunnerSettings();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Load settings from a file, creating a default file if none exists.
        /// </summary>
        /// <param name="filename">Settings file path.</param>
        /// <returns>Loaded settings.</returns>
        public static CodeHubSettings FromFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            if (!File.Exists(filename))
            {
                CodeHubSettings defaults = new CodeHubSettings();
                defaults.ToFile(filename);
                return defaults;
            }

            string json = File.ReadAllText(filename);
            CodeHubSettings settings = JsonSerializer.Deserialize<CodeHubSettings>(json, GetOptions());
            return settings ?? new CodeHubSettings();
        }

        /// <summary>
        /// Persist settings to a file.
        /// </summary>
        /// <param name="filename">Settings file path.</param>
        public void ToFile(string filename)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            string json = JsonSerializer.Serialize(this, GetOptions());
            File.WriteAllText(filename, json);
        }

        /// <summary>
        /// Apply environment-variable overrides for secrets and key paths.
        /// </summary>
        public void ApplyEnvironmentOverrides()
        {
            string apiKey = Environment.GetEnvironmentVariable("CODEHUB_AUTH_API_KEY");
            if (!String.IsNullOrEmpty(apiKey)) Authentication.ApiKey = apiKey;

            string pat = Environment.GetEnvironmentVariable("CODEHUB_GITHUB_PAT");
            if (!String.IsNullOrEmpty(pat)) GitHub.PersonalAccessToken = pat;

            string root = Environment.GetEnvironmentVariable("CODEHUB_SCAN_ROOT");
            if (!String.IsNullOrEmpty(root))
            {
                Directories.RootPaths = new System.Collections.Generic.List<string>(
                    root.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            string hostname = Environment.GetEnvironmentVariable("CODEHUB_HOSTNAME");
            if (!String.IsNullOrEmpty(hostname)) Webserver.Hostname = hostname;

            string port = Environment.GetEnvironmentVariable("CODEHUB_PORT");
            if (!String.IsNullOrEmpty(port) && Int32.TryParse(port, out int portInt)) Webserver.Port = portInt;

            string dbFile = Environment.GetEnvironmentVariable("CODEHUB_DB_FILENAME");
            if (!String.IsNullOrEmpty(dbFile)) Database.Filename = dbFile;
        }

        #endregion

        #region Private-Methods

        private static JsonSerializerOptions GetOptions()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        #endregion
    }
}
