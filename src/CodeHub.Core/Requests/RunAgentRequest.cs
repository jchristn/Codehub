namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to launch an agent in a single repository with an ad-hoc prompt.
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
        /// Prompt to pass to the agent.
        /// </summary>
        public string Prompt { get; set; }

        #endregion
    }
}
