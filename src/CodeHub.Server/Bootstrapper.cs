namespace CodeHub.Server
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core;
    using CodeHub.Core.Database;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Serialization;
    using CodeHub.Core.Services;
    using CodeHub.Core.Settings;
    using CodeHub.Server.Services;
    using SyslogLogging;

    /// <summary>
    /// Application composition root and lifecycle host.
    /// </summary>
    public static class Bootstrapper
    {
        #region Private-Members

        private static LoggingModule _Logging;
        private static CodeHubSettings _Settings;
        private static DatabaseDriverBase _Db;
        private static CodeHubServer _Server;
        private static ScanService _Scan;
        private static string _SettingsFile;
        private static readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private static readonly ManualResetEventSlim _Exit = new ManualResetEventSlim(false);
        private static int _ShuttingDown;
        private const string Header = "[CodeHub] ";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run the application.
        /// </summary>
        /// <param name="args">Command-line arguments.</param>
        public static void Run(string[] args)
        {
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                // Server is exiting because of an unhandled exception on the main path.
                string message = Header + "server exiting due to an unhandled exception:\n" + e.ToString();
                if (_Logging != null) _Logging.Error(message);
                else Console.Error.WriteLine(message);
                Environment.ExitCode = 1;
            }
        }

        #endregion

        #region Private-Methods

        private static async Task RunAsync(string[] args)
        {
            LoadSettings();
            ApplyCommandLineArguments(args);
            InitializeLogging();

            // Log any exception that would otherwise take the process down silently.
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            Console.WriteLine(Constants.ProductName + " " + Constants.SoftwareVersion);
            _Logging.Info(Header + "starting " + Constants.ProductName + " " + Constants.SoftwareVersion
                + " (pid " + Environment.ProcessId + ", " + RuntimeInformation.FrameworkDescription + ", "
                + RuntimeInformation.OSDescription.Trim() + ")");

            _Db = await DatabaseDriverFactory.CreateAndInitializeAsync(_Settings.Database, _Cts.Token).ConfigureAwait(false);
            _Logging.Info(Header + "database initialized (" + _Settings.Database.Type + ")");

            Serializer serializer = new Serializer();
            GitHubService gitHub = new GitHubService(_Settings.GitHub);
            ScoringService scoring = new ScoringService();
            _Scan = new ScanService(_Db, _Settings, _Logging, gitHub, scoring);

            SelectionService selection = new SelectionService(_Db);

            ServiceContext ctx = new ServiceContext
            {
                Db = _Db,
                Selection = selection,
                Serializer = serializer,
                Settings = _Settings,
                Scan = _Scan,
                GitHub = gitHub,
                Logging = _Logging,
                Launcher = new LauncherService(_Logging),
                SettingsWriter = new SettingsWriter(_Settings, _SettingsFile),
                StaticContent = new StaticContentService(_Settings.Webserver.DashboardDirectory, _Logging)
            };

            AuthenticationService auth = new AuthenticationService(_Settings.Authentication, serializer, _Logging);
            RequestHistoryCaptureService capture = new RequestHistoryCaptureService(_Db, _Settings.RequestHistory, _Logging);

            _Server = new CodeHubServer(ctx, auth, capture);
            _Server.Start();

            StartBackgroundLoops();

            Console.CancelKeyPress += (s, e) => { e.Cancel = true; Shutdown("Ctrl+C / SIGINT received"); };
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Shutdown("process exit");

            _Logging.Info(Header + "startup complete; listening on " + _Settings.Webserver.Hostname + ":" + _Settings.Webserver.Port
                + ", dashboard at /dashboard");

            _Exit.Wait();

            _Logging.Info(Header + "run loop exited; server has stopped");
        }

        private static void LoadSettings()
        {
            string settingsFile = "codehub.json";
            string envFile = Environment.GetEnvironmentVariable("CODEHUB_SETTINGS_FILE");
            if (!String.IsNullOrEmpty(envFile)) settingsFile = envFile;

            _SettingsFile = settingsFile;
            _Settings = CodeHubSettings.FromFile(settingsFile);
            _Settings.ApplyEnvironmentOverrides();
        }

        private static void ApplyCommandLineArguments(string[] args)
        {
            if (args == null) return;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string inline = null;
                int eq = arg.IndexOf('=');
                if (eq > 0)
                {
                    inline = arg.Substring(eq + 1);
                    arg = arg.Substring(0, eq);
                }

                switch (arg.ToLowerInvariant())
                {
                    case "--port":
                    case "-p":
                        string portValue = inline ?? NextArg(args, ref i);
                        if (Int32.TryParse(portValue, out int port)) _Settings.Webserver.Port = port;
                        break;
                    case "--hostname":
                    case "-h":
                        string host = inline ?? NextArg(args, ref i);
                        if (!String.IsNullOrEmpty(host)) _Settings.Webserver.Hostname = host;
                        break;
                    case "--root":
                        string root = inline ?? NextArg(args, ref i);
                        if (!String.IsNullOrEmpty(root))
                            _Settings.Directories.RootPaths = new System.Collections.Generic.List<string> { root };
                        break;
                }
            }
        }

        private static string NextArg(string[] args, ref int i)
        {
            return (i + 1 < args.Length) ? args[++i] : null;
        }

        private static void InitializeLogging()
        {
            _Logging = new LoggingModule();
            _Logging.Settings.EnableConsole = _Settings.Logging.ConsoleLogging;
            _Logging.Settings.EnableColors = _Settings.Logging.EnableColors;

            if (_Settings.Logging.FileLogging && !String.IsNullOrEmpty(_Settings.Logging.LogDirectory))
            {
                if (!Directory.Exists(_Settings.Logging.LogDirectory)) Directory.CreateDirectory(_Settings.Logging.LogDirectory);
                _Logging.Settings.LogFilename = Path.Combine(_Settings.Logging.LogDirectory, _Settings.Logging.LogFilename);
                _Logging.Settings.FileLogging = FileLoggingMode.SingleLogFile;
            }
        }

        private static void StartBackgroundLoops()
        {
            if (_Settings.Scan.ScanOnStartup)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _Scan.RunAsync(ScanTriggerEnum.Startup, null, _Cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(Header + "startup scan failed: " + e.Message);
                    }
                });
            }

            if (_Settings.Scan.IntervalHours > 0)
            {
                _ = Task.Run(async () =>
                {
                    TimeSpan interval = TimeSpan.FromHours(_Settings.Scan.IntervalHours);
                    while (!_Cts.IsCancellationRequested)
                    {
                        try
                        {
                            _Scan.NextScheduledScanUtc = DateTime.UtcNow.Add(interval);
                            await Task.Delay(interval, _Cts.Token).ConfigureAwait(false);
                            await _Scan.RunAsync(ScanTriggerEnum.Scheduled, null, _Cts.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception e)
                        {
                            _Logging.Warn(Header + "scheduled scan failed: " + e.Message);
                        }
                    }
                });
            }

            _ = Task.Run(async () =>
            {
                while (!_Cts.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), _Cts.Token).ConfigureAwait(false);
                        DateTime cutoff = DateTime.UtcNow.AddDays(-_Settings.RequestHistory.RetentionDays);
                        await _Db.RequestHistory.PruneAsync(cutoff, _Cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            });
        }

        private static void Shutdown(string reason)
        {
            // Only the first caller performs (and logs) shutdown; ProcessExit + Ctrl+C can both fire.
            if (Interlocked.Exchange(ref _ShuttingDown, 1) != 0) return;

            try
            {
                _Logging?.Info(Header + "shutting down (reason: " + (reason ?? "unspecified") + ")");
                _Cts.Cancel();
                _Server?.Stop();
                _Db?.Dispose();
                _Logging?.Info(Header + "shutdown complete");
            }
            catch (Exception e)
            {
                _Logging?.Warn(Header + "error during shutdown: " + e.Message);
            }
            finally
            {
                _Exit.Set();
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            string detail = ex != null ? ex.ToString() : Convert.ToString(e.ExceptionObject);
            string message = Header + "UNHANDLED EXCEPTION (terminating=" + e.IsTerminating + "):\n" + detail;
            if (_Logging != null) _Logging.Error(message);
            else Console.Error.WriteLine(message);

            // If the runtime is tearing the process down, record why we're exiting.
            if (e.IsTerminating) _Logging?.Info(Header + "process is terminating due to an unhandled exception");
        }

        private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            _Logging?.Error(Header + "UNOBSERVED TASK EXCEPTION:\n" + e.Exception.ToString());
            e.SetObserved(); // prevent this from escalating to a process-terminating exception
        }

        #endregion
    }
}
