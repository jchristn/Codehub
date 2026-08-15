namespace CodeHub.Server.Services
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using SyslogLogging;

    /// <summary>
    /// Launches external tools (Explorer, a terminal, Claude, Codex) at a repository path on the
    /// server host. Intended for the local single-operator model; Windows only.
    /// </summary>
    public class LauncherService
    {
        #region Private-Members

        private readonly LoggingModule _Logging;
        private readonly string _Header = "[Launcher] ";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        public LauncherService(LoggingModule logging)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Open a repository path in the requested tool.
        /// </summary>
        /// <param name="target">explorer, terminal, claude, codex, mux, or opencode.</param>
        /// <param name="path">Repository path.</param>
        /// <param name="dangerous">Whether to pass the tool's dangerous flag.</param>
        public void Open(string target, string path, bool dangerous)
        {
            if (String.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException(
                    "These actions launch on the machine running the CodeHub server, which is not Windows. " +
                    "Run the server on Windows to open Explorer, a terminal, Claude, or Codex.");
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Repository path no longer exists: " + path);

            // Snapshot existing windows so the one we're about to open can be pulled to the
            // foreground (the server is a background process, so new windows open behind).
            System.Collections.Generic.HashSet<IntPtr> windowsBefore = Interop.WindowForeground.Snapshot();

            switch (target.Trim().ToLowerInvariant())
            {
                case "explorer":
                    StartExplorer(path);
                    break;
                case "terminal":
                    OpenTerminal(path, null);
                    break;
                case "claude":
                    OpenTerminal(path, "claude" + (dangerous ? " --dangerously-skip-permissions" : String.Empty));
                    break;
                case "codex":
                    OpenTerminal(path, "codex" + (dangerous ? " --yolo" : String.Empty));
                    break;
                case "mux":
                    OpenTerminal(path, "mux" + (dangerous ? " --yolo" : String.Empty));
                    break;
                case "opencode":
                    OpenTerminal(path, "opencode");
                    break;
                default:
                    throw new ArgumentException("Unknown launch target: " + target);
            }

            // Pull the newly-opened window to the foreground once it appears.
            Interop.WindowForeground.BringNewWindowToForegroundAsync(windowsBefore, 3000);

            _Logging.Info(_Header + "launched " + target + " at " + path + (dangerous ? " (dangerous)" : String.Empty));
        }

        #endregion

        #region Private-Methods

        private static void StartExplorer(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "\"" + path + "\"",
                UseShellExecute = true
            });
        }

        private void OpenTerminal(string path, string command)
        {
            // Prefer Windows Terminal; fall back to a cmd window if wt.exe is unavailable.
            try
            {
                string args = "-d \"" + path + "\"";
                if (!String.IsNullOrEmpty(command)) args += " cmd /k " + command;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "wt.exe",
                    Arguments = args,
                    UseShellExecute = true
                });
                return;
            }
            catch (Exception e)
            {
                _Logging.Debug(_Header + "wt.exe unavailable (" + e.Message + "); falling back to cmd");
            }

            string inner = "cd /d \"" + path + "\"";
            if (!String.IsNullOrEmpty(command)) inner += " && " + command;
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c start \"CodeHub\" cmd /k \"" + inner + "\"",
                UseShellExecute = true
            });
        }

        #endregion
    }
}
