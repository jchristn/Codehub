namespace CodeHub.Core.Helpers
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The agentic CLIs a custom action can run under. Custom actions are agent-agnostic prompts;
    /// the agent is chosen when an action is invoked.
    /// </summary>
    public static class AgentHelper
    {
        #region Public-Members

        /// <summary>
        /// Supported agent names, in display order.
        /// </summary>
        public static readonly IReadOnlyList<string> Names = new List<string> { "claude", "codex", "mux", "opencode" };

        #endregion

        #region Public-Methods

        /// <summary>
        /// Normalize an agent name (trimmed, lowercase). Returns null when the agent is not supported.
        /// </summary>
        /// <param name="agent">Agent name as supplied by the caller.</param>
        /// <returns>Normalized agent name, or null.</returns>
        public static string Normalize(string agent)
        {
            if (String.IsNullOrWhiteSpace(agent)) return null;
            string normalized = agent.Trim().ToLowerInvariant();
            foreach (string name in Names)
            {
                if (name == normalized) return normalized;
            }
            return null;
        }

        /// <summary>
        /// Validation message listing the supported agents.
        /// </summary>
        /// <returns>Message.</returns>
        public static string InvalidMessage()
        {
            return "Agent must be one of: " + String.Join(", ", Names) + ".";
        }

        #endregion
    }
}
