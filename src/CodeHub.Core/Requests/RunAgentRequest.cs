namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to launch an agent in a repository with a (possibly edited) prompt.
    /// </summary>
    public class RunAgentRequest
    {
        #region Public-Members

        /// <summary>
        /// Agent to launch: claude, codex, mux, or opencode.
        /// </summary>
        public string Agent { get; set; }

        /// <summary>
        /// Whether to pass the agent's dangerous flag.
        /// </summary>
        public bool Dangerous { get; set; }

        /// <summary>
        /// Prompt to pass to the agent (may be edited from the action's default).
        /// </summary>
        public string Prompt { get; set; }

        #endregion
    }
}
