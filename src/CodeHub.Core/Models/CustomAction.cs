namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A user-defined, agent-agnostic prompt that can be run against one or more repositories.
    /// The agent (and its dangerous flag) is chosen when the action is invoked, not stored here.
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
        /// Default prompt passed to the agent chosen at invoke time (editable at invoke time).
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
