namespace CodeHub.Core.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    /// <summary>
    /// Platform-neutral building blocks for launching tools and agents at a repository: agent
    /// command lines, POSIX launch scripts, Linux terminal-emulator invocations, and quoting.
    /// Pure functions so they can be tested on any OS; the server's launcher does the process work.
    /// </summary>
    public static class LaunchHelper
    {
        #region Public-Members

        /// <summary>
        /// Linux terminal emulators to try, in order, with the arguments that precede the script
        /// to run. The first one found on PATH is used.
        /// </summary>
        public static readonly IReadOnlyList<KeyValuePair<string, string[]>> LinuxTerminals = new List<KeyValuePair<string, string[]>>
        {
            new KeyValuePair<string, string[]>("x-terminal-emulator", new[] { "-e" }),
            new KeyValuePair<string, string[]>("gnome-terminal", new[] { "--" }),
            new KeyValuePair<string, string[]>("konsole", new[] { "-e" }),
            new KeyValuePair<string, string[]>("xfce4-terminal", new[] { "-x" }),
            new KeyValuePair<string, string[]>("mate-terminal", new[] { "-x" }),
            new KeyValuePair<string, string[]>("tilix", new[] { "-e" }),
            new KeyValuePair<string, string[]>("kitty", new string[0]),
            new KeyValuePair<string, string[]>("alacritty", new[] { "-e" }),
            new KeyValuePair<string, string[]>("wezterm", new[] { "start", "--" }),
            new KeyValuePair<string, string[]>("foot", new string[0]),
            new KeyValuePair<string, string[]>("xterm", new[] { "-e" })
        };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Resolve an agent's binary and flags.
        /// </summary>
        /// <param name="agent">claude, codex, mux, or opencode.</param>
        /// <param name="binary">Executable name.</param>
        /// <param name="dangerFlag">Flag that skips permission prompts, or null if the agent has none.</param>
        /// <param name="promptFlag">Flag preceding the prompt, or null to pass the prompt positionally.</param>
        public static void ResolveAgent(string agent, out string binary, out string dangerFlag, out string promptFlag)
        {
            if (String.IsNullOrEmpty(agent)) throw new ArgumentNullException(nameof(agent));

            promptFlag = null;
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

        /// <summary>
        /// The command a terminal-based open target runs (no prompt). Returns null for a plain
        /// terminal. Throws for unknown targets; "explorer" (the file manager) is not a terminal target.
        /// </summary>
        /// <param name="target">terminal, claude, codex, mux, or opencode.</param>
        /// <param name="dangerous">Whether to pass the agent's dangerous flag.</param>
        /// <returns>Command line, or null.</returns>
        public static string TargetCommand(string target, bool dangerous)
        {
            if (String.IsNullOrEmpty(target)) throw new ArgumentNullException(nameof(target));

            string normalized = target.Trim().ToLowerInvariant();
            if (normalized == "terminal") return null;
            if (AgentHelper.Normalize(normalized) == null) throw new ArgumentException("Unknown launch target: " + target);

            ResolveAgent(normalized, out string binary, out string dangerFlag, out _);
            return binary + (dangerous && dangerFlag != null ? " " + dangerFlag : String.Empty);
        }

        /// <summary>
        /// POSIX shell command that runs an agent, passing the prompt read verbatim from a file as a
        /// single argument (newlines and quotes intact).
        /// </summary>
        /// <param name="agent">Agent name.</param>
        /// <param name="dangerous">Whether to pass the agent's dangerous flag.</param>
        /// <param name="promptFile">File holding the prompt, or null for no prompt.</param>
        /// <returns>Shell command.</returns>
        public static string PosixAgentCommand(string agent, bool dangerous, string promptFile)
        {
            ResolveAgent(agent, out string binary, out string dangerFlag, out string promptFlag);

            string command = binary;
            if (dangerous && dangerFlag != null) command += " " + dangerFlag;
            if (!String.IsNullOrEmpty(promptFile))
            {
                if (promptFlag != null) command += " " + promptFlag;
                command += " \"$(cat " + ShellQuote(promptFile) + ")\"";
            }
            return command;
        }

        /// <summary>
        /// POSIX script that changes to the repository, runs the command (if any), then leaves an
        /// interactive shell open — the equivalent of Windows' "cmd /k".
        /// </summary>
        /// <param name="path">Repository path.</param>
        /// <param name="command">Shell command, or null for a plain shell.</param>
        /// <returns>Script text.</returns>
        public static string PosixInnerScript(string path, string command)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            StringBuilder sb = new StringBuilder();
            sb.Append("#!/bin/sh\n");
            sb.Append("cd ").Append(ShellQuote(path)).Append(" || exit 1\n");
            if (!String.IsNullOrEmpty(command)) sb.Append(command).Append('\n');
            sb.Append("exec \"${SHELL:-/bin/sh}\" -l\n");
            return sb.ToString();
        }

        /// <summary>
        /// POSIX script a terminal window runs. It starts the inner script from the user's
        /// interactive login shell so PATH additions made in shell profiles (where agent CLIs such
        /// as claude usually live) are in effect.
        /// </summary>
        /// <param name="innerScript">Path to the inner script.</param>
        /// <returns>Script text.</returns>
        public static string PosixLauncherScript(string innerScript)
        {
            if (String.IsNullOrEmpty(innerScript)) throw new ArgumentNullException(nameof(innerScript));
            return "#!/bin/sh\nexec \"${SHELL:-/bin/sh}\" -l -i -c " + ShellQuote(ShellQuote(innerScript)) + "\n";
        }

        /// <summary>
        /// Arguments for a Linux terminal emulator to run a script.
        /// </summary>
        /// <param name="prefix">Arguments preceding the script (from <see cref="LinuxTerminals"/>).</param>
        /// <param name="script">Script path.</param>
        /// <returns>Argument list.</returns>
        public static List<string> LinuxTerminalArgs(string[] prefix, string script)
        {
            List<string> args = new List<string>(prefix ?? new string[0]);
            args.Add(script);
            return args;
        }

        /// <summary>
        /// Find an executable on a PATH-style list.
        /// </summary>
        /// <param name="name">Executable name.</param>
        /// <param name="pathVariable">PATH value (defaults to the PATH environment variable).</param>
        /// <returns>Full path, or null if not found.</returns>
        public static string FindOnPath(string name, string pathVariable = null)
        {
            if (String.IsNullOrEmpty(name)) return null;
            string value = pathVariable ?? Environment.GetEnvironmentVariable("PATH") ?? String.Empty;
            foreach (string dir in value.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    string candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate)) return candidate;
                }
                catch (Exception)
                {
                    // malformed PATH entry
                }
            }
            return null;
        }

        /// <summary>
        /// Quote a string as a single POSIX shell word.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Single-quoted word.</returns>
        public static string ShellQuote(string value)
        {
            return "'" + (value ?? String.Empty).Replace("'", "'\\''") + "'";
        }

        /// <summary>
        /// Quote a string as an AppleScript string literal.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Double-quoted, escaped literal.</returns>
        public static string AppleScriptQuote(string value)
        {
            return "\"" + (value ?? String.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        #endregion
    }
}
