namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to select or deselect a directory for scanning.
    /// </summary>
    public class SelectionRequest
    {
        #region Public-Members

        /// <summary>
        /// Absolute directory path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// True to select (include), false to deselect (uncheck / exclude).
        /// </summary>
        public bool Selected { get; set; }

        #endregion
    }
}
