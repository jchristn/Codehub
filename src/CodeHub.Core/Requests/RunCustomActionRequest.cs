namespace CodeHub.Core.Requests
{
    using System.Collections.Generic;

    /// <summary>
    /// Request to run a custom action against one or more repositories with a chosen agent.
    /// </summary>
    public class RunCustomActionRequest
    {
        #region Public-Members

        /// <summary>
        /// Repositories to run the action in; each opens its own terminal.
        /// </summary>
        public List<string> RepositoryIds { get; set; }

        /// <summary>
        /// Agent to run the action with: claude, codex, mux, or opencode.
        /// </summary>
        public string Agent { get; set; }

        /// <summary>
        /// Whether to pass the agent's dangerous flag (ignored by agents that have none, e.g. opencode).
        /// </summary>
        public bool Dangerous { get; set; }

        /// <summary>
        /// Prompt override. When null, the action's stored prompt is used.
        /// </summary>
        public string Prompt { get; set; }

        #endregion
    }
}
