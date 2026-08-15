namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// A parsed GitHub owner/repo reference.
    /// </summary>
    public class GitHubRepoRef
    {
        /// <summary>
        /// Repository owner.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Repository name.
        /// </summary>
        public string Repo { get; set; }

        /// <summary>
        /// Parse an owner/repo from a git remote URL, or return null.
        /// </summary>
        /// <param name="remoteUrl">Git remote URL (https or ssh form).</param>
        /// <returns>Parsed reference or null.</returns>
        public static GitHubRepoRef Parse(string remoteUrl)
        {
            if (String.IsNullOrEmpty(remoteUrl)) return null;
            if (remoteUrl.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) < 0) return null;

            Match match = Regex.Match(remoteUrl, "github\\.com[/:]([^/]+)/([^/]+?)(?:\\.git)?/?$",
                RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            return new GitHubRepoRef
            {
                Owner = match.Groups[1].Value,
                Repo = match.Groups[2].Value
            };
        }
    }
}
