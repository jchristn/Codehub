namespace CodeHub.Core.Settings
{
    using System;

    /// <summary>
    /// GitHub integration settings. The token gates the issues/PRs/Dependabot columns.
    /// </summary>
    public class GitHubSettings
    {
        #region Public-Members

        /// <summary>
        /// GitHub Personal Access Token. Leave empty to disable GitHub columns.
        /// Override with the CODEHUB_GITHUB_PAT environment variable.
        /// </summary>
        public string PersonalAccessToken { get; set; } = "";

        /// <summary>
        /// Default GitHub owner used when a repository's remote does not resolve one.
        /// </summary>
        public string Owner { get; set; } = "";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Whether a token is configured.
        /// </summary>
        /// <returns>True when a token is present.</returns>
        public bool IsConfigured()
        {
            return !String.IsNullOrWhiteSpace(PersonalAccessToken);
        }

        #endregion
    }
}
