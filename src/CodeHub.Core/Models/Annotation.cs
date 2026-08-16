namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A manual override of a repository column/signal value, with a note explaining why.
    /// The override value is shown (and filtered/sorted on) instead of the computed value.
    /// </summary>
    public class Annotation
    {
        #region Public-Members

        /// <summary>
        /// Annotation identifier (prefix "ann_").
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
        /// Owning repository identifier.
        /// </summary>
        public string RepositoryId { get; set; } = String.Empty;

        /// <summary>
        /// Column being overridden: a signal type (e.g. "OutdatedDependencies") or "Overall".
        /// </summary>
        public string Column { get; set; } = String.Empty;

        /// <summary>
        /// The health status to show instead (Green, Yellow, Red, NotApplicable, Unknown).
        /// </summary>
        public string Status { get; set; } = String.Empty;

        /// <summary>
        /// The note explaining the override.
        /// </summary>
        public string Note { get; set; } = String.Empty;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateAnnotationId();

        #endregion
    }
}
