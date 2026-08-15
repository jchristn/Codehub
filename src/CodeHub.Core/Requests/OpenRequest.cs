namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to open a repository in an external tool on the server host.
    /// </summary>
    public class OpenRequest
    {
        #region Public-Members

        /// <summary>
        /// Target tool: explorer, terminal, claude, or codex.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Whether to pass the tool's dangerous flag (Claude: --dangerously-skip-permissions,
        /// Codex: --yolo).
        /// </summary>
        public bool Dangerous { get; set; }

        #endregion
    }
}
