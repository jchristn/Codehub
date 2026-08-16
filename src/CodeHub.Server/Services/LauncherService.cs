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

        /// <summary>
        /// Open a terminal in a repository running an agent with a prompt (a custom action).
        /// </summary>
        /// <param name="agent">claude, codex, mux, or opencode.</param>
        /// <param name="path">Repository path.</param>
        /// <param name="dangerous">Whether to pass the agent's dangerous flag.</param>
        /// <param name="prompt">Prompt to pass to the agent.</param>
        public void OpenAgentPrompt(string agent, string path, bool dangerous, string prompt)
        {
            if (String.IsNullOrEmpty(agent)) throw new ArgumentNullException(nameof(agent));
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException(
                    "Custom actions launch on the machine running the CodeHub server, which is not Windows. " +
                    "Run the server on Windows to launch an agent in a terminal.");
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Repository path no longer exists: " + path);

            string command = BuildAgentCommand(agent, dangerous, prompt);

            System.Collections.Generic.HashSet<IntPtr> windowsBefore = Interop.WindowForeground.Snapshot();

            // The prompt goes into a temp batch file (not the wt/cmd command line), so its
            // spaces and special characters do not need shell-level escaping.
            string batch = WriteLaunchBatch(path, command);
            OpenTerminal(path, "\"" + batch + "\"");

            Interop.WindowForeground.BringNewWindowToForegroundAsync(windowsBefore, 3000);
            _Logging.Info(_Header + "launched custom action (" + agent + ") at " + path + (dangerous ? " (dangerous)" : String.Empty));
        }

        #endregion

        #region Private-Methods

        private static string BuildAgentCommand(string agent, bool dangerous, string prompt)
        {
            string binary;
            string dangerFlag;
            string promptFlag = null; // flag preceding the prompt; null means pass it positionally
            switch (agent.Trim().ToLowerInvariant())
            {
                case "claude": binary = "claude"; dangerFlag = "--dangerously-skip-permissions"; break;
                case "codex": binary = "codex"; dangerFlag = "--yolo"; break;
                // mux needs --prompt to skip the splash screen and stay interactive.
                case "mux": binary = "mux"; dangerFlag = "--yolo"; promptFlag = "--prompt"; break;
                case "opencode": binary = "opencode"; dangerFlag = null; break;
                default: throw new ArgumentException("Unknown agent: " + agent);
            }

            string command = binary;
            if (dangerous && dangerFlag != null) command += " " + dangerFlag;
            if (!String.IsNullOrWhiteSpace(prompt))
            {
                string quoted = "\"" + EscapePromptForBatch(prompt) + "\"";
                command += promptFlag != null ? " " + promptFlag + " " + quoted : " " + quoted;
            }
            return command;
        }

        private static string EscapePromptForBatch(string prompt)
        {
            // Flatten to a single line and escape for a double-quoted batch argument.
            string flat = prompt.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();
            flat = flat.Replace("%", "%%");    // percent is special in batch files
            flat = flat.Replace("\"", "\\\""); // pass embedded double quotes through to the agent
            return flat;
        }

        private static string WriteLaunchBatch(string path, string command)
        {
            string dir = Path.Combine(Path.GetTempPath(), "codehub");
            Directory.CreateDirectory(dir);
            string file = Path.Combine(dir, "action_" + Guid.NewGuid().ToString("N") + ".cmd");
            string content = "@echo off\r\ncd /d \"" + path + "\"\r\n" + command + "\r\n";
            File.WriteAllText(file, content);
            return file;
        }

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
