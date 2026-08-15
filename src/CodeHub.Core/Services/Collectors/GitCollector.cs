namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;

    /// <summary>
    /// Populates git-derived repository fields: remote URL, last commit, and visibility heuristic.
    /// </summary>
    public class GitCollector
    {
        #region Private-Members

        private const int TimeoutMs = 15000;

        #endregion

        /// <summary>
        /// Read the current HEAD commit hash for a git repository, or null.
        /// </summary>
        /// <param name="repoPath">Repository path.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Commit hash, or null.</returns>
        public static async Task<string> GetHeadHashAsync(string repoPath, CancellationToken token = default)
        {
            ProcessResult result = await ProcessRunner.RunAsync(
                "git", "rev-parse HEAD", repoPath, TimeoutMs, token).ConfigureAwait(false);
            if (!result.Success) return null;
            string hash = result.StandardOutput.Trim();
            return String.IsNullOrEmpty(hash) ? null : hash;
        }

        #region Public-Methods

        /// <summary>
        /// Populate git-derived fields on a repository.
        /// </summary>
        /// <param name="repository">Repository to populate.</param>
        /// <param name="projects">Projects for the mtime fallback.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task PopulateAsync(Repository repository, List<Project> projects, CancellationToken token = default)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            if (repository.IsGitRepository)
            {
                repository.LastCommitHash = await GetHeadHashAsync(repository.Path, token).ConfigureAwait(false);

                ProcessResult remote = await ProcessRunner.RunAsync(
                    "git", "remote get-url origin", repository.Path, TimeoutMs, token).ConfigureAwait(false);
                if (remote.Success)
                {
                    string url = remote.StandardOutput.Trim();
                    if (!String.IsNullOrEmpty(url)) repository.RemoteUrl = url;
                }

                ProcessResult log = await ProcessRunner.RunAsync(
                    "git", "log -1 --format=%cI", repository.Path, TimeoutMs, token).ConfigureAwait(false);
                if (log.Success)
                {
                    string date = log.StandardOutput.Trim();
                    if (DateTime.TryParse(date, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
                    {
                        repository.LastUpdateUtc = parsed;
                    }
                }

                await PopulateBranchAndDivergenceAsync(repository, token).ConfigureAwait(false);
            }

            if (!repository.LastUpdateUtc.HasValue)
            {
                repository.LastUpdateUtc = NewestManifestTime(projects);
            }

            repository.Visibility = InferVisibility(repository);
        }

        #endregion

        #region Private-Methods

        private async Task PopulateBranchAndDivergenceAsync(Repository repository, CancellationToken token)
        {
            ProcessResult branch = await ProcessRunner.RunAsync(
                "git", "rev-parse --abbrev-ref HEAD", repository.Path, TimeoutMs, token).ConfigureAwait(false);
            if (branch.Success)
            {
                string name = branch.StandardOutput.Trim();
                if (!String.IsNullOrEmpty(name)) repository.CurrentBranch = name;
            }

            string baseRef = await ResolveBaseRefAsync(repository.Path, token).ConfigureAwait(false);
            if (String.IsNullOrEmpty(baseRef)) return;
            repository.BaseBranch = baseRef;

            ProcessResult counts = await ProcessRunner.RunAsync(
                "git", "rev-list --left-right --count " + baseRef + "...HEAD", repository.Path, TimeoutMs, token).ConfigureAwait(false);
            if (!counts.Success) return;

            string[] parts = counts.StandardOutput.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                if (Int32.TryParse(parts[0], out int behind)) repository.CommitsBehind = behind;
                if (Int32.TryParse(parts[1], out int ahead)) repository.CommitsAhead = ahead;
            }
        }

        /// <summary>
        /// Enumerate local branches and each branch's divergence (ahead/behind) from the base branch.
        /// </summary>
        /// <param name="repoPath">Repository path.</param>
        /// <param name="baseRef">Base ref to compare against; resolved automatically when null.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Branches with ahead/behind counts.</returns>
        public async Task<List<Branch>> GetBranchesAsync(string repoPath, string baseRef, CancellationToken token = default)
        {
            List<Branch> branches = new List<Branch>();

            ProcessResult list = await ProcessRunner.RunAsync(
                "git", "for-each-ref --format=%(refname:short) refs/heads", repoPath, TimeoutMs, token).ConfigureAwait(false);
            if (!list.Success) return branches;

            string current = null;
            ProcessResult head = await ProcessRunner.RunAsync(
                "git", "rev-parse --abbrev-ref HEAD", repoPath, TimeoutMs, token).ConfigureAwait(false);
            if (head.Success) current = head.StandardOutput.Trim();

            if (String.IsNullOrEmpty(baseRef))
                baseRef = await ResolveBaseRefAsync(repoPath, token).ConfigureAwait(false);

            foreach (string raw in list.StandardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string name = raw.Trim();
                if (String.IsNullOrEmpty(name)) continue;

                Branch b = new Branch { Name = name, IsCurrent = String.Equals(name, current, StringComparison.Ordinal) };
                if (!String.IsNullOrEmpty(baseRef))
                {
                    ProcessResult counts = await ProcessRunner.RunAsync(
                        "git", "rev-list --left-right --count " + baseRef + "..." + name, repoPath, TimeoutMs, token).ConfigureAwait(false);
                    if (counts.Success)
                    {
                        string[] parts = counts.StandardOutput.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 2)
                        {
                            if (Int32.TryParse(parts[0], out int behind)) b.Behind = behind;
                            if (Int32.TryParse(parts[1], out int ahead)) b.Ahead = ahead;
                        }
                    }
                }
                branches.Add(b);
            }
            return branches;
        }

        private static async Task<string> ResolveBaseRefAsync(string path, CancellationToken token)
        {
            string[] candidates = new[] { "origin/main", "origin/master", "main", "master" };
            foreach (string candidate in candidates)
            {
                ProcessResult verify = await ProcessRunner.RunAsync(
                    "git", "rev-parse --verify --quiet " + candidate, path, TimeoutMs, token).ConfigureAwait(false);
                if (verify.Success && !String.IsNullOrWhiteSpace(verify.StandardOutput)) return candidate;
            }
            return null;
        }

        private static DateTime? NewestManifestTime(List<Project> projects)
        {
            DateTime? newest = null;
            if (projects == null) return null;
            foreach (Project project in projects)
            {
                try
                {
                    if (String.IsNullOrEmpty(project.Path) || !File.Exists(project.Path)) continue;
                    DateTime mtime = File.GetLastWriteTimeUtc(project.Path);
                    if (!newest.HasValue || mtime > newest.Value) newest = mtime;
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            return newest;
        }

        private static SourceVisibilityEnum InferVisibility(Repository repository)
        {
            GitHubRepoRef gitHubRef = GitHubRepoRef.Parse(repository.RemoteUrl);
            if (gitHubRef == null)
            {
                return String.IsNullOrEmpty(repository.RemoteUrl) ? SourceVisibilityEnum.Closed : SourceVisibilityEnum.Unknown;
            }

            bool hasLicense =
                File.Exists(Path.Combine(repository.Path, "LICENSE.md")) ||
                File.Exists(Path.Combine(repository.Path, "LICENSE"));

            return hasLicense ? SourceVisibilityEnum.Open : SourceVisibilityEnum.Unknown;
        }

        #endregion
    }
}
