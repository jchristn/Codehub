namespace CodeHub.Core.Models
{
    using System;
    using System.Collections.Generic;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A repository discovered under the scan root. One repository is one row in the dashboard table.
    /// </summary>
    public class Repository
    {
        #region Public-Members

        /// <summary>
        /// Repository identifier (prefix "repo_").
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
        /// Absolute path to the repository root.
        /// </summary>
        public string Path { get; set; } = String.Empty;

        /// <summary>
        /// Repository display name (folder name).
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Open or closed source.
        /// </summary>
        public SourceVisibilityEnum Visibility { get; set; } = SourceVisibilityEnum.Unknown;

        /// <summary>
        /// Primary language of the repository.
        /// </summary>
        public ProjectTypeEnum PrimaryLanguage { get; set; } = ProjectTypeEnum.Unknown;

        /// <summary>
        /// Languages detected in the repository.
        /// </summary>
        public List<string> Languages { get; set; } = new List<string>();

        /// <summary>
        /// Current version of the repository's primary project.
        /// </summary>
        public string CurrentVersion { get; set; } = null;

        /// <summary>
        /// Whether the repository is under git.
        /// </summary>
        public bool IsGitRepository { get; set; } = false;

        /// <summary>
        /// Git remote origin URL, if any.
        /// </summary>
        public string RemoteUrl { get; set; } = null;

        /// <summary>
        /// Current checked-out branch, if a git repository.
        /// </summary>
        public string CurrentBranch { get; set; } = null;

        /// <summary>
        /// The base branch the ahead/behind counts are measured against (for example main or master).
        /// </summary>
        public string BaseBranch { get; set; } = null;

        /// <summary>
        /// Commits on the current branch that are not on the base branch.
        /// </summary>
        public int CommitsAhead { get; set; } = 0;

        /// <summary>
        /// Commits on the base branch that are not on the current branch.
        /// </summary>
        public int CommitsBehind { get; set; } = 0;

        /// <summary>
        /// The HEAD commit hash captured at the last scan. Used for delta detection:
        /// a git repository whose HEAD is unchanged is skipped on subsequent scans.
        /// </summary>
        public string LastCommitHash { get; set; } = null;

        /// <summary>
        /// Timestamp of the most recent commit (or newest project file mtime for non-git repos).
        /// </summary>
        public DateTime? LastUpdateUtc { get; set; } = null;

        /// <summary>
        /// Number of discrete projects discovered inside the repository.
        /// </summary>
        public int ProjectCount { get; set; } = 0;

        /// <summary>
        /// Rolled-up overall health.
        /// </summary>
        public HealthStatusEnum OverallHealth { get; set; } = HealthStatusEnum.Unknown;

        /// <summary>
        /// Whether the repository is included in scans (manual override honored).
        /// </summary>
        public bool IsIncluded { get; set; } = true;

        /// <summary>
        /// Timestamp of the last successful scan of this repository.
        /// </summary>
        public DateTime? LastScannedUtc { get; set; } = null;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateRepositoryId();

        #endregion
    }
}
