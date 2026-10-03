namespace CodeHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using CodeHub.Core.Helpers;
    using SyslogLogging;

    /// <summary>
    /// Launches external tools (the file manager, a terminal, Claude, Codex, mux, OpenCode) at a
    /// repository path on the server host. Intended for the local single-operator model. Supports
    /// Windows (Explorer, Windows Terminal or cmd), macOS (Finder, Terminal), and Linux (xdg-open,
    /// the first terminal emulator found on PATH).
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
        /// <param name="target">explorer (the file manager), terminal, claude, codex, mux, or opencode.</param>
        /// <param name="path">Repository path.</param>
        /// <param name="dangerous">Whether to pass the tool's dangerous flag.</param>
        public void Open(string target, string path, bool dangerous)
        {
            if (String.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Repository path no longer exists: " + path);

            bool isExplorer = String.Equals(target.Trim(), "explorer", StringComparison.OrdinalIgnoreCase);
            string command = isExplorer ? null : LaunchHelper.TargetCommand(target, dangerous);

            if (OperatingSystem.IsWindows())
            {
                // Snapshot existing windows so the one we're about to open can be pulled to the
                // foreground (the server is a background process, so new windows open behind).
                HashSet<IntPtr> windowsBefore = Interop.WindowForeground.Snapshot();

                if (isExplorer) StartExplorer(path);
                else OpenWindowsTerminal(path, command);

                // Pull the newly-opened window to the foreground once it appears.
                Interop.WindowForeground.BringNewWindowToForegroundAsync(windowsBefore, 3000);
            }
            else
            {
                if (isExplorer) OpenFileManager(path);
                else OpenPosixTerminal(path, command);
            }

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
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("Repository path no longer exists: " + path);

            // The prompt is written verbatim to a sidecar file (newlines preserved) and read back by
            // the launch script, which passes it to the agent as a single argument. Putting the
            // prompt directly on a command line would mangle newlines and quotes.
            if (OperatingSystem.IsWindows())
            {
                HashSet<IntPtr> windowsBefore = Interop.WindowForeground.Snapshot();
                string batch = WriteWindowsLaunchFiles(agent, dangerous, path, prompt);
                OpenWindowsTerminal(path, "\"" + batch + "\"");
                Interop.WindowForeground.BringNewWindowToForegroundAsync(windowsBefore, 3000);
            }
            else
            {
                string promptFile = null;
                if (!String.IsNullOrWhiteSpace(prompt))
                {
                    promptFile = NewTempStem() + ".txt";
                    File.WriteAllText(promptFile, prompt.Trim(), new System.Text.UTF8Encoding(false));
                }
                OpenPosixTerminal(path, LaunchHelper.PosixAgentCommand(agent, dangerous, promptFile));
            }

            _Logging.Info(_Header + "launched custom action (" + agent + ") at " + path + (dangerous ? " (dangerous)" : String.Empty));
        }

        #endregion

        #region Private-Methods

        // Escape a string for embedding inside a PowerShell single-quoted literal.
        private static string PsLiteral(string value)
        {
            return value.Replace("'", "''");
        }

        // Write the three temp files that drive a Windows custom action and return the .cmd entry point:
        //   .txt  the prompt, byte-for-byte with newlines intact
        //   .ps1  reads the prompt raw and launches the agent with it as one argument
        //   .cmd  cd's to the repo and invokes the .ps1 (keeps the existing wt/cmd /k window flow)
        private static string WriteWindowsLaunchFiles(string agent, bool dangerous, string path, string prompt)
        {
            LaunchHelper.ResolveAgent(agent, out string binary, out string dangerFlag, out string promptFlag);

            string stem = NewTempStem();
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

        private static string NewTempStem()
        {
            string dir = Path.Combine(Path.GetTempPath(), "codehub");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "action_" + Guid.NewGuid().ToString("N"));
        }

        private void OpenWindowsTerminal(string path, string command)
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

        private static void OpenFileManager(string path)
        {
            if (OperatingSystem.IsMacOS())
            {
                StartProcess("/usr/bin/open", path);
                return;
            }

            string opener = LaunchHelper.FindOnPath("xdg-open")
                ?? throw new NotSupportedException("No file manager opener (xdg-open) was found on the CodeHub server.");
            StartProcess(opener, path);
        }

        // Write the inner script (cd, run, keep a shell open) and the launcher that runs it from the
        // user's login shell, then open the launcher in a new terminal window.
        private void OpenPosixTerminal(string path, string command)
        {
            string stem = NewTempStem();
            string inner = stem + ".sh";
            string launcher = stem + (OperatingSystem.IsMacOS() ? ".command" : "_launch.sh");
            WriteExecutable(inner, LaunchHelper.PosixInnerScript(path, command));
            WriteExecutable(launcher, LaunchHelper.PosixLauncherScript(inner));

            if (OperatingSystem.IsMacOS())
            {
                // Terminal runs a .command file in a new window and comes to the front.
                StartProcess("/usr/bin/open", "-a", "Terminal", launcher);
                return;
            }

            foreach (KeyValuePair<string, string[]> terminal in LaunchHelper.LinuxTerminals)
            {
                string binary = LaunchHelper.FindOnPath(terminal.Key);
                if (binary == null) continue;
                StartProcess(binary, LaunchHelper.LinuxTerminalArgs(terminal.Value, launcher).ToArray());
                _Logging.Debug(_Header + "opened " + terminal.Key + " for " + path);
                return;
            }

            throw new NotSupportedException(
                "No supported terminal emulator was found on the CodeHub server. Install one of: " +
                String.Join(", ", LaunchHelper.LinuxTerminals.Select(t => t.Key)) + ".");
        }

        private static void WriteExecutable(string file, string contents)
        {
            File.WriteAllText(file, contents, new System.Text.UTF8Encoding(false));
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(file,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }

        private static void StartProcess(string fileName, params string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false
            };
            foreach (string arg in args) info.ArgumentList.Add(arg);
            using (Process.Start(info)) { }
        }

        #endregion
    }
}
