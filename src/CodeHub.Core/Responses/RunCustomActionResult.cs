namespace CodeHub.Core.Responses
{
    /// <summary>
    /// Per-repository outcome of running a custom action.
    /// </summary>
    public class RunCustomActionResult
    {
        #region Public-Members

        /// <summary>
        /// Repository identifier.
        /// </summary>
        public string RepositoryId { get; set; }

        /// <summary>
        /// Repository name, when the repository was found.
        /// </summary>
        public string RepositoryName { get; set; }

        /// <summary>
        /// Whether a terminal was launched.
        /// </summary>
        public bool Launched { get; set; }

        /// <summary>
        /// Failure reason when not launched.
        /// </summary>
        public string Error { get; set; }

        #endregion
    }
}
