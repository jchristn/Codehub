namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to create or update a custom action.
    /// </summary>
    public class CustomActionRequest
    {
        #region Public-Members

        /// <summary>
        /// Display name shown in the actions menu.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Agent to launch: claude, codex, mux, or opencode.
        /// </summary>
        public string Agent { get; set; }

        /// <summary>
        /// Whether to pass the agent's dangerous flag.
        /// </summary>
        public bool Dangerous { get; set; }

        /// <summary>
        /// Default prompt to pass to the agent.
        /// </summary>
        public string Prompt { get; set; }

        #endregion
    }
}
