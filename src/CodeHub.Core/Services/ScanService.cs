namespace CodeHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Services.Collectors;
    using CodeHub.Core.Settings;
    using SyslogLogging;

    /// <summary>
    /// Orchestrates repository discovery, collection, scoring, and persistence.
    /// </summary>
    public class ScanService
    {
        #region Public-Members

        /// <summary>
        /// Whether a scan is currently running.
        /// </summary>
        public bool IsScanning
        {
            get { return _IsScanning; }
        }

        /// <summary>
        /// The in-flight scan run, if any.
        /// </summary>
        public ScanRun CurrentRun
        {
            get { return _CurrentRun; }
        }

        /// <summary>
        /// When the next automatic scan is scheduled, or null when scheduling is disabled.
        /// </summary>
        public DateTime? NextScheduledScanUtc { get; set; } = null;

        /// <summary>
        /// Per-repository progress for the in-flight scan, or null when idle.
        /// </summary>
        public List<ScanRepoProgress> CurrentRepositories
        {
            get { return _CurrentRepos; }
        }

        #endregion

        #region Private-Members

        private readonly DatabaseDriverBase _Db;
        private readonly CodeHubSettings _Settings;
        private readonly LoggingModule _Logging;
        private readonly GitHubService _GitHub;
        private readonly ScoringService _Scoring;
        private readonly SelectionService _Selection;
        private readonly SemaphoreSlim _RunGate = new SemaphoreSlim(1, 1);
        private readonly string _Header = "[ScanService] ";

        private volatile bool _IsScanning = false;
        private volatile ScanRun _CurrentRun = null;
        private volatile List<ScanRepoProgress> _CurrentRepos = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Database driver.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="logging">Logging module.</param>
        /// <param name="gitHub">GitHub service.</param>
        /// <param name="scoring">Scoring service.</param>
        public ScanService(
            DatabaseDriverBase db,
            CodeHubSettings settings,
            LoggingModule logging,
            GitHubService gitHub,
            ScoringService scoring)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _GitHub = gitHub ?? throw new ArgumentNullException(nameof(gitHub));
            _Scoring = scoring ?? throw new ArgumentNullException(nameof(scoring));
            _Selection = new SelectionService(db);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run a scan across all included repositories, or a single repository when specified.
        /// </summary>
        /// <param name="trigger">What triggered the scan.</param>
        /// <param name="repositoryId">Optional single repository to rescan.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The scan run record.</returns>
        public async Task<ScanRun> RunAsync(ScanTriggerEnum trigger, string repositoryId = null, CancellationToken token = default)
        {
            if (!await _RunGate.WaitAsync(0, token).ConfigureAwait(false))
            {
                _Logging.Warn(_Header + "scan already in progress; ignoring new request");
                return _CurrentRun;
            }

            ScanRun run = new ScanRun { Trigger = trigger, Status = ScanStatusEnum.Running };

            try
            {
                _IsScanning = true;
                _CurrentRun = run;
                await _Db.ScanRuns.CreateAsync(run, token).ConfigureAwait(false);

                SelectionSets sets = await _Selection.GetSetsAsync(token).ConfigureAwait(false);
                List<string> roots = await ResolveRootsAsync(repositoryId, sets, token).ConfigureAwait(false);
                run.ReposTotal = roots.Count;
                await _Db.ScanRuns.UpdateAsync(run, token).ConfigureAwait(false);
                _Logging.Info(_Header + "scanning " + roots.Count + " repository(ies)");

                List<string> excluded = sets.Excluded;

                // Manual scans (Scan Now / Rescan Now) and single-repository scans force a full
                // re-collection so every column is repopulated, bypassing the git-HEAD delta skip.
                bool force = trigger == ScanTriggerEnum.Manual || !String.IsNullOrEmpty(repositoryId);

                // Per-repository progress, exposed on the scan status for the Scans page.
                List<ScanRepoProgress> progress = new List<ScanRepoProgress>();
                Dictionary<string, ScanRepoProgress> progressByPath = new Dictionary<string, ScanRepoProgress>();
                foreach (string root in roots)
                {
                    ScanRepoProgress p = new ScanRepoProgress
                    {
                        Path = root,
                        Name = Path.GetFileName(root.TrimEnd('\\', '/')),
                        Status = "Pending"
                    };
                    progress.Add(p);
                    progressByPath[root] = p;
                }
                _CurrentRepos = progress;

                int scanned = 0;
                SemaphoreSlim gate = new SemaphoreSlim(_Settings.Scan.MaxConcurrency);
                List<Task> tasks = new List<Task>();

                foreach (string root in roots)
                {
                    await gate.WaitAsync(token).ConfigureAwait(false);
                    ScanRepoProgress repoProgress = progressByPath.TryGetValue(root, out ScanRepoProgress rp) ? rp : null;
                    tasks.Add(Task.Run(async () =>
                    {
                        if (repoProgress != null) repoProgress.Status = "Scanning";
                        try
                        {
                            await ProcessRepositoryAsync(root, excluded, force, token).ConfigureAwait(false);
                            if (repoProgress != null) repoProgress.Status = "Done";
                        }
                        catch (Exception e)
                        {
                            if (repoProgress != null) repoProgress.Status = "Failed";
                            _Logging.Warn(_Header + "failed to scan " + root + ": " + e.Message);
                        }
                        finally
                        {
                            int done = Interlocked.Increment(ref scanned);
                            run.ReposScanned = done;
                            gate.Release();
                        }
                    }, token));
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                run.Status = ScanStatusEnum.Completed;
                run.CompletedUtc = DateTime.UtcNow;
                await _Db.ScanRuns.UpdateAsync(run, token).ConfigureAwait(false);
                _Logging.Info(_Header + "scan complete (" + run.ReposScanned + " repositories)");
            }
            catch (Exception e)
            {
                run.Status = ScanStatusEnum.Failed;
                run.Error = e.Message;
                run.CompletedUtc = DateTime.UtcNow;
                try { await _Db.ScanRuns.UpdateAsync(run, CancellationToken.None).ConfigureAwait(false); }
                catch (Exception) { /* ignore */ }
                _Logging.Error(_Header + "scan failed: " + e.Message);
            }
            finally
            {
                _IsScanning = false;
                _CurrentRun = null;
                _CurrentRepos = null;
                _RunGate.Release();
            }

            return run;
        }

        #endregion

        #region Private-Methods

        private static bool IsExcluded(string root, List<string> excluded)
        {
            if (excluded == null || excluded.Count == 0) return false;
            foreach (string path in excluded)
            {
                if (SelectionService.PathEquals(root, path) || SelectionService.IsStrictlyUnder(root, path)) return true;
            }
            return false;
        }

        private async Task<List<string>> ResolveRootsAsync(string repositoryId, SelectionSets sets, CancellationToken token)
        {
            if (!String.IsNullOrEmpty(repositoryId))
            {
                Repository existing = await _Db.Repositories.ReadAsync(repositoryId, token).ConfigureAwait(false);
                if (existing != null && !String.IsNullOrEmpty(existing.Path))
                    return new List<string> { existing.Path };
                return new List<string>();
            }

            // Scan nothing until directories are selected.
            if (sets.Included.Count == 0) return new List<string>();

            DiscoveryCollector discovery = new DiscoveryCollector(_Settings.Directories);
            return discovery.FindRepositoryRootsFromSelection(sets.Included);
        }

        private async Task ProcessRepositoryAsync(string root, List<string> excluded, bool force, CancellationToken token)
        {
            Repository existing = await _Db.Repositories.ReadByPathAsync(root, token).ConfigureAwait(false);

            if (IsExcluded(root, excluded))
            {
                if (existing != null && existing.IsIncluded)
                {
                    existing.IsIncluded = false;
                    await _Db.Repositories.UpsertAsync(existing, token).ConfigureAwait(false);
                }
                return;
            }

            bool isGit = Directory.Exists(Path.Combine(root, ".git"));

            // Delta: skip a git repository whose HEAD is unchanged since its last scan.
            // Forced (manual / single-repo) scans always re-collect so every column is repopulated.
            if (!force && existing != null && existing.LastScannedUtc.HasValue && isGit && !String.IsNullOrEmpty(existing.LastCommitHash))
            {
                string currentHash = await GitCollector.GetHeadHashAsync(root, token).ConfigureAwait(false);
                if (!String.IsNullOrEmpty(currentHash) && currentHash == existing.LastCommitHash)
                {
                    existing.IsIncluded = true;
                    existing.LastScannedUtc = DateTime.UtcNow;
                    await _Db.Repositories.UpsertAsync(existing, token).ConfigureAwait(false);
                    return;
                }
            }

            DiscoveryCollector discovery = new DiscoveryCollector(_Settings.Directories);
            DiscoveredRepository discovered = discovery.Discover(root);
            Repository repository = discovered.Repository;
            repository.IsIncluded = true;

            // Skip noise folders: no projects and not a git repository (e.g. assets, logs).
            if (discovered.Projects.Count == 0 && !repository.IsGitRepository) return;

            GitCollector git = new GitCollector();
            await git.PopulateAsync(repository, discovered.Projects, token).ConfigureAwait(false);

            List<Branch> branches = new List<Branch>();
            if (repository.IsGitRepository)
            {
                branches = await git.GetBranchesAsync(repository.Path, repository.BaseBranch, token).ConfigureAwait(false);
                repository.BranchCount = branches.Count;
            }

            List<Dependency> dependencies = new List<Dependency>();
            if (_Settings.Scan.DependencyCheck)
            {
                DependencyCollector deps = new DependencyCollector();
                dependencies = await deps.CollectAsync(repository, discovered.Projects, token).ConfigureAwait(false);
            }

            GitHubSnapshot snapshot = null;
            if (_GitHub.IsConfigured())
            {
                snapshot = await _GitHub.FetchAsync(repository, token).ConfigureAwait(false);
                if (snapshot != null && !snapshot.IsPrivate && String.IsNullOrEmpty(snapshot.Error))
                {
                    repository.Visibility = SourceVisibilityEnum.Open;
                }
                else if (snapshot != null && snapshot.IsPrivate)
                {
                    repository.Visibility = SourceVisibilityEnum.Closed;
                }
            }

            List<Signal> signals = _Scoring.Score(repository, discovered.Projects, dependencies, snapshot, _GitHub.IsConfigured());
            repository.LastScannedUtc = DateTime.UtcNow;

            Repository saved = await _Db.Repositories.UpsertAsync(repository, token).ConfigureAwait(false);
            await _Db.Projects.ReplaceForRepositoryAsync(saved.Id, discovered.Projects, token).ConfigureAwait(false);
            await _Db.Dependencies.ReplaceForRepositoryAsync(saved.Id, dependencies, token).ConfigureAwait(false);
            await _Db.Signals.ReplaceForRepositoryAsync(saved.Id, signals, token).ConfigureAwait(false);
            await _Db.Branches.ReplaceForRepositoryAsync(saved.Id, branches, token).ConfigureAwait(false);

            if (snapshot != null)
            {
                snapshot.RepositoryId = saved.Id;
                await _Db.GitHubSnapshots.UpsertAsync(snapshot, token).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
