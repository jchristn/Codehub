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
        /// Default prompt to pass to whichever agent runs the action.
        /// </summary>
        public string Prompt { get; set; }

        #endregion
    }
}
