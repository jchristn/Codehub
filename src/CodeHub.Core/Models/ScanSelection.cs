namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A persisted scan-selection entry. Included entries are directories chosen to scan;
    /// excluded entries prune a specific path inside a selected branch. This table is the
    /// source of truth for what the scanner sweeps.
    /// </summary>
    public class ScanSelection
    {
        #region Public-Members

        /// <summary>
        /// Selection identifier (prefix "sel_").
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
        /// Absolute directory path.
        /// </summary>
        public string Path { get; set; } = String.Empty;

        /// <summary>
        /// True = include this directory in scans; false = exclude this path.
        /// </summary>
        public bool Included { get; set; } = true;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateSelectionId();

        #endregion
    }
}
