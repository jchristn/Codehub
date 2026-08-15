namespace CodeHub.Core.Services
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Services.Collectors;
    using CodeHub.Core.Settings;

    /// <summary>
    /// Fetches issues, pull requests, Dependabot alerts, and visibility from the GitHub REST API.
    /// Returns a snapshot with an Error message when no token is configured or a call fails.
    /// </summary>
    public class GitHubService
    {
        #region Private-Members

        private readonly GitHubSettings _Settings;
        private readonly HttpClient _Http;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">GitHub settings.</param>
        public GitHubService(GitHubSettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Http = new HttpClient
            {
                BaseAddress = new Uri("https://api.github.com/"),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _Http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CodeHub", "0.1"));
            _Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            _Http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
            if (_Settings.IsConfigured())
            {
                _Http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", _Settings.PersonalAccessToken);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Whether a GitHub token is configured.
        /// </summary>
        /// <returns>True when configured.</returns>
        public bool IsConfigured()
        {
            return _Settings.IsConfigured();
        }

        /// <summary>
        /// Fetch a snapshot for a repository.
        /// </summary>
        /// <param name="repository">Repository.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GitHub snapshot.</returns>
        public async Task<GitHubSnapshot> FetchAsync(Repository repository, CancellationToken token = default)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            GitHubSnapshot snapshot = new GitHubSnapshot { RepositoryId = repository.Id };

            if (!_Settings.IsConfigured())
            {
                snapshot.Error = "GitHub token not configured.";
                return snapshot;
            }

            GitHubRepoRef repoRef = GitHubRepoRef.Parse(repository.RemoteUrl);
            if (repoRef == null && !String.IsNullOrEmpty(_Settings.Owner))
            {
                repoRef = new GitHubRepoRef { Owner = _Settings.Owner, Repo = repository.Name };
            }
            if (repoRef == null)
            {
                snapshot.Error = "No GitHub remote resolved for this repository.";
                return snapshot;
            }

            snapshot.Owner = repoRef.Owner;
            snapshot.Repo = repoRef.Repo;

            try
            {
                JsonElement? repoJson = await GetJsonAsync("repos/" + repoRef.Owner + "/" + repoRef.Repo, token).ConfigureAwait(false);
                if (repoJson == null)
                {
                    snapshot.Error = "Repository not found on GitHub.";
                    return snapshot;
                }

                JsonElement repoElement = repoJson.Value;
                if (repoElement.TryGetProperty("private", out JsonElement priv)) snapshot.IsPrivate = priv.GetBoolean();
                int openIssuesAndPrs = repoElement.TryGetProperty("open_issues_count", out JsonElement oi) ? oi.GetInt32() : 0;

                int openPrs = await CountPagedAsync(
                    "repos/" + repoRef.Owner + "/" + repoRef.Repo + "/pulls?state=open&per_page=100", token).ConfigureAwait(false);
                snapshot.OpenPullRequests = openPrs;
                snapshot.OpenIssues = Math.Max(0, openIssuesAndPrs - openPrs);

                await FetchDependabotAsync(repoRef, snapshot, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                snapshot.Error = e.Message;
            }

            return snapshot;
        }

        #endregion

        #region Private-Methods

        private async Task FetchDependabotAsync(GitHubRepoRef repoRef, GitHubSnapshot snapshot, CancellationToken token)
        {
            try
            {
                HttpResponseMessage response = await _Http.GetAsync(
                    "repos/" + repoRef.Owner + "/" + repoRef.Repo + "/dependabot/alerts?state=open&per_page=100",
                    token).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.NotFound)
                {
                    // Dependabot not enabled or token lacks security scope; leave counts at zero.
                    return;
                }
                if (!response.IsSuccessStatusCode) return;

                string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Array) return;
                    foreach (JsonElement alert in doc.RootElement.EnumerateArray())
                    {
                        snapshot.DependabotOpen++;
                        string severity = null;
                        if (alert.TryGetProperty("security_advisory", out JsonElement advisory) &&
                            advisory.TryGetProperty("severity", out JsonElement sev))
                        {
                            severity = sev.GetString();
                        }
                        if (String.Equals(severity, "high", StringComparison.OrdinalIgnoreCase)) snapshot.DependabotHigh++;
                        else if (String.Equals(severity, "critical", StringComparison.OrdinalIgnoreCase)) snapshot.DependabotCritical++;
                    }
                }
            }
            catch (Exception)
            {
                // best effort
            }
        }

        private async Task<JsonElement?> GetJsonAsync(string path, CancellationToken token)
        {
            HttpResponseMessage response = await _Http.GetAsync(path, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return null;
            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            JsonDocument doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }

        private async Task<int> CountPagedAsync(string path, CancellationToken token)
        {
            HttpResponseMessage response = await _Http.GetAsync(path, token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return 0;
            string body = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            using (JsonDocument doc = JsonDocument.Parse(body))
            {
                if (doc.RootElement.ValueKind != JsonValueKind.Array) return 0;
                return doc.RootElement.GetArrayLength();
            }
        }

        #endregion
    }
}
