namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A user-defined action that launches an agentic CLI in a repository with a default prompt.
    /// Stored in the database so it survives restarts and appears in the repository actions menu.
    /// </summary>
    public class CustomAction
    {
        #region Public-Members

        /// <summary>
        /// Custom action identifier (prefix "act_").
        /// </summary>
        public string Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Id));
                _Id = value;
            }
        }

        /// <summary>
        /// Display name shown in the actions menu.
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Agent to launch: claude, codex, mux, or opencode.
        /// </summary>
        public string Agent { get; set; } = "claude";

        /// <summary>
        /// Whether to pass the agent's dangerous flag (ignored by agents that have none, e.g. opencode).
        /// </summary>
        public bool Dangerous { get; set; } = false;

        /// <summary>
        /// Default prompt passed to the agent (editable at invoke time).
        /// </summary>
        public string Prompt { get; set; } = String.Empty;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateCustomActionId();

        #endregion
    }
}
