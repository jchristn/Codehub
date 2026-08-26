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

            System.Collections.Generic.HashSet<IntPtr> windowsBefore = Interop.WindowForeground.Snapshot();

            // The prompt is written verbatim to a sidecar file (newlines preserved) and read back
            // by a PowerShell launcher that passes it to the agent as a single argument. Putting the
            // prompt on a cmd command line would force it onto one physical line, losing newlines.
            string batch = WriteLaunchFiles(agent, dangerous, path, prompt);
            OpenTerminal(path, "\"" + batch + "\"");

            Interop.WindowForeground.BringNewWindowToForegroundAsync(windowsBefore, 3000);
            _Logging.Info(_Header + "launched custom action (" + agent + ") at " + path + (dangerous ? " (dangerous)" : String.Empty));
        }

        #endregion

        #region Private-Methods

        private static void ResolveAgent(string agent, out string binary, out string dangerFlag, out string promptFlag)
        {
            promptFlag = null; // flag preceding the prompt; null means pass it positionally
            switch (agent.Trim().ToLowerInvariant())
            {
                case "claude": binary = "claude"; dangerFlag = "--dangerously-skip-permissions"; break;
                case "codex": binary = "codex"; dangerFlag = "--yolo"; break;
                // mux needs --prompt to skip the splash screen and stay interactive.
                case "mux": binary = "mux"; dangerFlag = "--yolo"; promptFlag = "--prompt"; break;
                case "opencode": binary = "opencode"; dangerFlag = null; break;
                default: throw new ArgumentException("Unknown agent: " + agent);
            }
        }

        // Escape a string for embedding inside a PowerShell single-quoted literal.
        private static string PsLiteral(string value)
        {
            return value.Replace("'", "''");
        }

        // Write the three temp files that drive a custom action and return the .cmd entry point:
        //   .txt  the prompt, byte-for-byte with newlines intact
        //   .ps1  reads the prompt raw and launches the agent with it as one argument
        //   .cmd  cd's to the repo and invokes the .ps1 (keeps the existing wt/cmd /k window flow)
        private static string WriteLaunchFiles(string agent, bool dangerous, string path, string prompt)
        {
            ResolveAgent(agent, out string binary, out string dangerFlag, out string promptFlag);

            string dir = Path.Combine(Path.GetTempPath(), "codehub");
            Directory.CreateDirectory(dir);
            string stem = Path.Combine(dir, "action_" + Guid.NewGuid().ToString("N"));
            string promptFile = stem + ".txt";
            string scriptFile = stem + ".ps1";
            string cmdFile = stem + ".cmd";

            bool hasPrompt = !String.IsNullOrWhiteSpace(prompt);
            if (hasPrompt) File.WriteAllText(promptFile, prompt.Trim(), new System.Text.UTF8Encoding(false));

            // Build the PowerShell launcher. The agent is invoked via the call operator so a PATH
            // shim (e.g. claude.cmd) resolves, and the prompt keeps its newlines as a single argument.
            System.Text.StringBuilder script = new System.Text.StringBuilder();
            string invoke = "& '" + binary + "'";
            if (dangerous && dangerFlag != null) invoke += " " + dangerFlag;
            if (hasPrompt)
            {
                // Read the prompt verbatim, then escape embedded double quotes as \" so Windows
                // PowerShell 5.1's native-argument handling delivers them intact to the agent.
                script.Append("$p = (Get-Content -Raw -LiteralPath '")
                      .Append(PsLiteral(promptFile))
                      .Append("') -replace '\"','\\\"'\r\n");
                invoke += (promptFlag != null ? " " + promptFlag : String.Empty) + " $p";
            }
            script.Append(invoke).Append("\r\n");
            File.WriteAllText(scriptFile, script.ToString(), new System.Text.UTF8Encoding(false));

            string cmd = "@echo off\r\ncd /d \"" + path + "\"\r\n"
                + "powershell -NoProfile -ExecutionPolicy Bypass -File \"" + scriptFile + "\"\r\n";
            File.WriteAllText(cmdFile, cmd);
            return cmdFile;
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
