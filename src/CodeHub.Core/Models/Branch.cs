namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A local git branch and its divergence from the repository's base branch, captured on scan.
    /// </summary>
    public class Branch
    {
        #region Public-Members

        /// <summary>
        /// Branch identifier (prefix "br_").
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
        /// Branch name.
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Whether this is the currently checked-out branch.
        /// </summary>
        public bool IsCurrent { get; set; } = false;

        /// <summary>
        /// Commits this branch is ahead of the base branch.
        /// </summary>
        public int Ahead { get; set; } = 0;

        /// <summary>
        /// Commits this branch is behind the base branch.
        /// </summary>
        public int Behind { get; set; } = 0;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateBranchId();

        #endregion
    }
}
