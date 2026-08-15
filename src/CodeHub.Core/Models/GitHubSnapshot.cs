namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A point-in-time snapshot of a repository's GitHub state.
    /// </summary>
    public class GitHubSnapshot
    {
        #region Public-Members

        /// <summary>
        /// Snapshot identifier (prefix "gh_").
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
        /// GitHub owner.
        /// </summary>
        public string Owner { get; set; } = null;

        /// <summary>
        /// GitHub repository name.
        /// </summary>
        public string Repo { get; set; } = null;

        /// <summary>
        /// Whether the GitHub repository is private.
        /// </summary>
        public bool IsPrivate { get; set; } = false;

        /// <summary>
        /// Count of open issues (excluding pull requests).
        /// </summary>
        public int OpenIssues { get; set; } = 0;

        /// <summary>
        /// Count of open pull requests.
        /// </summary>
        public int OpenPullRequests { get; set; } = 0;

        /// <summary>
        /// Count of open Dependabot alerts.
        /// </summary>
        public int DependabotOpen { get; set; } = 0;

        /// <summary>
        /// Count of open high-severity Dependabot alerts.
        /// </summary>
        public int DependabotHigh { get; set; } = 0;

        /// <summary>
        /// Count of open critical-severity Dependabot alerts.
        /// </summary>
        public int DependabotCritical { get; set; } = 0;

        /// <summary>
        /// Error message when the GitHub fetch failed or was skipped.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// UTC fetch timestamp.
        /// </summary>
        public DateTime FetchedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateGitHubSnapshotId();

        #endregion
    }
}
