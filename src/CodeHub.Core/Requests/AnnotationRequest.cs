namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to set (upsert) a value override for a repository column.
    /// </summary>
    public class AnnotationRequest
    {
        #region Public-Members

        /// <summary>
        /// Column to override: a signal type (e.g. "OutdatedDependencies") or "Overall".
        /// </summary>
        public string Column { get; set; }

        /// <summary>
        /// The health status to show instead (Green, Yellow, Red, NotApplicable, Unknown).
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// The note explaining the override.
        /// </summary>
        public string Note { get; set; }

        #endregion
    }
}
