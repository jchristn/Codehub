namespace CodeHub.Server.Routes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Repository list, detail, overview, and include/exclude routes.
    /// </summary>
    public class RepositoryRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public RepositoryRoutes(ServiceContext ctx)
        {
            _Ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Register routes.
        /// </summary>
        /// <param name="server">Webserver.</param>
        public void Register(Webserver server)
        {
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/repositories", ListAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/overview", OverviewAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/api/repositories/{id}", DetailAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/api/repositories/{id}/branches", BranchesAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/include", IncludeAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/exclude", ExcludeAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/open", OpenAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/run-agent", RunAgentAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/archive", ArchiveAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/repositories/{id}/delete", DeleteRepoAsync);
        }

        #endregion

        #region Private-Methods

        private async Task ListAsync(HttpContextBase ctx)
        {
            List<Repository> repos = await _Ctx.Db.Repositories.EnumerateAsync(ctx.Token).ConfigureAwait(false);
            List<Signal> allSignals = await _Ctx.Db.Signals.EnumerateAllAsync(ctx.Token).ConfigureAwait(false);
            Dictionary<string, List<Signal>> signalsByRepo = allSignals
                .GroupBy(s => s.RepositoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            List<Project> allProjects = await _Ctx.Db.Projects.EnumerateAllAsync(ctx.Token).ConfigureAwait(false);
            Dictionary<string, List<string>> frameworksByRepo = allProjects
                .GroupBy(p => p.RepositoryId)
                .ToDictionary(g => g.Key, g => RollupFrameworks(g));

            List<GitHubSnapshot> allGitHub = await _Ctx.Db.GitHubSnapshots.EnumerateAllAsync(ctx.Token).ConfigureAwait(false);
            Dictionary<string, GitHubSnapshot> githubByRepo = allGitHub
                .GroupBy(g => g.RepositoryId)
                .ToDictionary(g => g.Key, g => g.First());

            string health = RouteHelper.Query(ctx, "health");
            string language = RouteHelper.Query(ctx, "language");
            string visibility = RouteHelper.Query(ctx, "visibility");
            string search = RouteHelper.Query(ctx, "q");
            string signalType = RouteHelper.Query(ctx, "signalType");
            string signalStatus = RouteHelper.Query(ctx, "signalStatus");
            bool includeExcluded = String.Equals(RouteHelper.Query(ctx, "includeExcluded"), "true", StringComparison.OrdinalIgnoreCase);
            string sort = RouteHelper.Query(ctx, "sort") ?? "name";
            string dir = RouteHelper.Query(ctx, "dir") ?? "asc";
            int pageNumber = Math.Max(1, RouteHelper.QueryInt(ctx, "pageNumber", 1));
            int pageSize = Math.Clamp(RouteHelper.QueryInt(ctx, "pageSize", 50), 1, 1000);

            IEnumerable<Repository> query = repos;
            if (!includeExcluded) query = query.Where(r => r.IsIncluded);
            if (!String.IsNullOrEmpty(health)) query = query.Where(r => r.OverallHealth.ToString().Equals(health, StringComparison.OrdinalIgnoreCase));
            if (!String.IsNullOrEmpty(language)) query = query.Where(r => r.Languages != null && r.Languages.Any(l => l.Equals(language, StringComparison.OrdinalIgnoreCase)));
            if (!String.IsNullOrEmpty(visibility)) query = query.Where(r => r.Visibility.ToString().Equals(visibility, StringComparison.OrdinalIgnoreCase));
            if (!String.IsNullOrEmpty(search)) query = query.Where(r =>
                (r.Name != null && r.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (r.Path != null && r.Path.Contains(search, StringComparison.OrdinalIgnoreCase)));

            if (!String.IsNullOrEmpty(signalType) && !String.IsNullOrEmpty(signalStatus))
            {
                query = query.Where(r =>
                    signalsByRepo.TryGetValue(r.Id, out List<Signal> sigs) &&
                    sigs.Any(s => s.SignalType.ToString().Equals(signalType, StringComparison.OrdinalIgnoreCase) &&
                                  s.Status.ToString().Equals(signalStatus, StringComparison.OrdinalIgnoreCase)));
            }

            // Per-column filters.
            string version = RouteHelper.Query(ctx, "version");
            if (!String.IsNullOrEmpty(version)) query = query.Where(r => r.CurrentVersion != null && r.CurrentVersion.Contains(version, StringComparison.OrdinalIgnoreCase));

            string branch = RouteHelper.Query(ctx, "branch");
            if (!String.IsNullOrEmpty(branch)) query = query.Where(r => r.CurrentBranch != null && r.CurrentBranch.Contains(branch, StringComparison.OrdinalIgnoreCase));

            string commits = RouteHelper.Query(ctx, "commits");
            if (!String.IsNullOrEmpty(commits)) query = query.Where(r => MatchCommits(r, commits));

            string updated = RouteHelper.Query(ctx, "updated");
            if (!String.IsNullOrEmpty(updated)) query = query.Where(r => MatchUpdated(r, updated));

            string overall = RouteHelper.Query(ctx, "overall");
            if (!String.IsNullOrEmpty(overall)) query = query.Where(r => r.OverallHealth.ToString().Equals(overall, StringComparison.OrdinalIgnoreCase));

            query = ApplySignalFilter(query, signalsByRepo, SignalTypeEnum.TestInfra, RouteHelper.Query(ctx, "tests"));
            query = ApplySignalFilter(query, signalsByRepo, SignalTypeEnum.Telemetry, RouteHelper.Query(ctx, "telemetry"));
            query = ApplySignalFilter(query, signalsByRepo, SignalTypeEnum.OutdatedDependencies, RouteHelper.Query(ctx, "dependencies"));
            query = ApplySignalFilter(query, signalsByRepo, SignalTypeEnum.Vulnerabilities, RouteHelper.Query(ctx, "security"));

            string issuepr = RouteHelper.Query(ctx, "issuepr");
            if (!String.IsNullOrEmpty(issuepr)) query = query.Where(r => MatchIssue(signalsByRepo, r.Id, issuepr));

            string frameworks = RouteHelper.Query(ctx, "frameworks");
            if (!String.IsNullOrEmpty(frameworks)) query = query.Where(r =>
                frameworksByRepo.TryGetValue(r.Id, out List<string> fw) &&
                fw.Any(f => f.Contains(frameworks, StringComparison.OrdinalIgnoreCase)));

            string archived = RouteHelper.Query(ctx, "archived");
            if (!String.IsNullOrEmpty(archived))
            {
                bool wantArchived = String.Equals(archived, "Yes", StringComparison.OrdinalIgnoreCase);
                query = query.Where(r => (githubByRepo.TryGetValue(r.Id, out GitHubSnapshot gh) && gh.IsArchived) == wantArchived);
            }

            List<Repository> filtered = OrderRepositories(query, sort, dir, signalsByRepo, frameworksByRepo, githubByRepo);
            int total = filtered.Count;

            List<Repository> pageRepos = filtered.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
            List<RepositoryListItem> items = new List<RepositoryListItem>();
            foreach (Repository repo in pageRepos)
            {
                RepositoryListItem item = new RepositoryListItem
                {
                    Repository = repo,
                    Signals = signalsByRepo.TryGetValue(repo.Id, out List<Signal> s) ? s : new List<Signal>(),
                    GitHub = githubByRepo.TryGetValue(repo.Id, out GitHubSnapshot ghSnap) ? ghSnap : null,
                    TargetFrameworks = frameworksByRepo.TryGetValue(repo.Id, out List<string> fw) ? fw : new List<string>()
                };
                items.Add(item);
            }

            PagedResponse<RepositoryListItem> response = new PagedResponse<RepositoryListItem>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = total
            };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
        }

        private async Task DetailAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            RepositoryDetailResponse response = new RepositoryDetailResponse
            {
                Repository = repo,
                Projects = await _Ctx.Db.Projects.EnumerateByRepositoryAsync(id, ctx.Token).ConfigureAwait(false),
                Dependencies = await _Ctx.Db.Dependencies.EnumerateByRepositoryAsync(id, ctx.Token).ConfigureAwait(false),
                Signals = await _Ctx.Db.Signals.EnumerateByRepositoryAsync(id, ctx.Token).ConfigureAwait(false),
                GitHub = await _Ctx.Db.GitHubSnapshots.ReadByRepositoryAsync(id, ctx.Token).ConfigureAwait(false)
            };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
        }

        private async Task IncludeAsync(HttpContextBase ctx)
        {
            await SetIncludedAsync(ctx, true).ConfigureAwait(false);
        }

        private async Task ExcludeAsync(HttpContextBase ctx)
        {
            await SetIncludedAsync(ctx, false).ConfigureAwait(false);
        }

        private async Task SetIncludedAsync(HttpContextBase ctx, bool included)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            await _Ctx.Db.Repositories.SetIncludedAsync(id, included, ctx.Token).ConfigureAwait(false);

            // Persist to the scan_selections table (the source of truth honored by scans).
            if (included) await _Ctx.Selection.SelectAsync(repo.Path, ctx.Token).ConfigureAwait(false);
            else await _Ctx.Selection.DeselectAsync(repo.Path, ctx.Token).ConfigureAwait(false);

            repo.IsIncluded = included;
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, repo).ConfigureAwait(false);
        }

        private async Task OpenAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            string body = ctx.Request.DataAsString;
            OpenRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<OpenRequest>(body);
            if (request == null || String.IsNullOrEmpty(request.Target))
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "A target is required.")).ConfigureAwait(false);
                return;
            }

            try
            {
                _Ctx.Launcher.Open(request.Target, repo.Path, request.Dangerous);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200,
                    new Dictionary<string, object> { { "launched", true }, { "target", request.Target } }).ConfigureAwait(false);
            }
            catch (NotSupportedException e)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("NotSupported", e.Message)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[RepositoryRoutes] open failed: " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 500, new ErrorResponse("LaunchFailed", e.Message)).ConfigureAwait(false);
            }
        }

        private async Task BranchesAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            List<Branch> branches = await _Ctx.Db.Branches.EnumerateByRepositoryAsync(id, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, branches).ConfigureAwait(false);
        }

        private async Task RunAgentAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            string body = ctx.Request.DataAsString;
            RunAgentRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<RunAgentRequest>(body);
            if (request == null || String.IsNullOrEmpty(request.Agent))
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "An agent is required.")).ConfigureAwait(false);
                return;
            }

            try
            {
                _Ctx.Launcher.OpenAgentPrompt(request.Agent, repo.Path, request.Dangerous, request.Prompt);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200,
                    new Dictionary<string, object> { { "launched", true }, { "agent", request.Agent } }).ConfigureAwait(false);
            }
            catch (NotSupportedException e)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("NotSupported", e.Message)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[RepositoryRoutes] run-agent failed: " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 500, new ErrorResponse("LaunchFailed", e.Message)).ConfigureAwait(false);
            }
        }

        private async Task ArchiveAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            string body = ctx.Request.DataAsString;
            ArchiveRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<ArchiveRequest>(body);
            bool archived = request?.Archived ?? false;

            try
            {
                await _Ctx.GitHub.SetArchivedAsync(repo, archived, ctx.Token).ConfigureAwait(false);

                // Reflect the new state immediately so the UI updates without a full rescan.
                GitHubSnapshot snap = await _Ctx.Db.GitHubSnapshots.ReadByRepositoryAsync(id, ctx.Token).ConfigureAwait(false)
                    ?? new GitHubSnapshot { RepositoryId = id };
                snap.IsArchived = archived;
                await _Ctx.Db.GitHubSnapshots.UpsertAsync(snap, ctx.Token).ConfigureAwait(false);

                _Ctx.Logging.Info("[RepositoryRoutes] " + (archived ? "archived " : "unarchived ") + repo.Name + " on GitHub");
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200,
                    new Dictionary<string, object> { { "archived", archived } }).ConfigureAwait(false);
            }
            catch (InvalidOperationException e)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("ArchiveFailed", e.Message)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[RepositoryRoutes] archive failed for " + repo.Name + ": " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 500, new ErrorResponse("ArchiveFailed", e.Message)).ConfigureAwait(false);
            }
        }

        private async Task DeleteRepoAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Repository repo = await _Ctx.Db.Repositories.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (repo == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Repository not found.")).ConfigureAwait(false);
                return;
            }

            string body = ctx.Request.DataAsString;
            DeleteRepositoryRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<DeleteRepositoryRequest>(body);
            bool recycle = request?.Recycle ?? false;

            try
            {
                // Remove from disk first; only touch the database if that succeeds so a failed
                // delete can be retried. A missing directory is treated as already-deleted.
                if (Directory.Exists(repo.Path))
                    Interop.FileOperations.DeleteDirectory(repo.Path, recycle);

                await _Ctx.Selection.DeselectAsync(repo.Path, ctx.Token).ConfigureAwait(false);
                await _Ctx.Db.Repositories.DeleteAsync(id, ctx.Token).ConfigureAwait(false);

                _Ctx.Logging.Info("[RepositoryRoutes] deleted " + repo.Path + (recycle ? " (recycle bin)" : " (permanent)"));
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200,
                    new Dictionary<string, object> { { "deleted", true }, { "recycled", recycle } }).ConfigureAwait(false);
            }
            catch (NotSupportedException e)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("NotSupported", e.Message)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[RepositoryRoutes] delete failed for " + repo.Path + ": " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 500, new ErrorResponse("DeleteFailed", e.Message)).ConfigureAwait(false);
            }
        }

        private async Task OverviewAsync(HttpContextBase ctx)
        {
            List<Repository> repos = await _Ctx.Db.Repositories.EnumerateAsync(ctx.Token).ConfigureAwait(false);
            List<Repository> included = repos.Where(r => r.IsIncluded).ToList();
            List<Signal> allSignals = await _Ctx.Db.Signals.EnumerateAllAsync(ctx.Token).ConfigureAwait(false);
            Dictionary<string, List<Signal>> signalsByRepo = allSignals
                .GroupBy(s => s.RepositoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            OverviewResponse overview = new OverviewResponse
            {
                TotalRepositories = included.Count,
                GreenCount = included.Count(r => r.OverallHealth == HealthStatusEnum.Green),
                YellowCount = included.Count(r => r.OverallHealth == HealthStatusEnum.Yellow),
                RedCount = included.Count(r => r.OverallHealth == HealthStatusEnum.Red),
                NeedsAttention = included.Count(r => r.OverallHealth == HealthStatusEnum.Red)
            };

            foreach (Repository repo in included)
            {
                if (!signalsByRepo.TryGetValue(repo.Id, out List<Signal> sigs)) continue;
                if (sigs.Any(s => s.SignalType == SignalTypeEnum.TestInfra && s.Status == HealthStatusEnum.Red)) overview.ReposWithoutTests++;
                if (sigs.Any(s => s.SignalType == SignalTypeEnum.Telemetry && s.Status == HealthStatusEnum.Red)) overview.WebServicesWithoutTelemetry++;
                if (sigs.Any(s => s.SignalType == SignalTypeEnum.Vulnerabilities && s.Status == HealthStatusEnum.Red)) overview.ReposWithHighCves++;
                if (sigs.Any(s => s.SignalType == SignalTypeEnum.OutdatedDependencies && (s.Status == HealthStatusEnum.Red || s.Status == HealthStatusEnum.Yellow))) overview.ReposWithOutdatedDeps++;
            }

            List<Repository> attention = included
                .Where(r => r.OverallHealth == HealthStatusEnum.Red || r.OverallHealth == HealthStatusEnum.Yellow)
                .OrderByDescending(r => r.OverallHealth == HealthStatusEnum.Red ? 1 : 0)
                .Take(10)
                .ToList();
            foreach (Repository repo in attention)
            {
                overview.AttentionList.Add(new RepositoryListItem
                {
                    Repository = repo,
                    Signals = signalsByRepo.TryGetValue(repo.Id, out List<Signal> s) ? s : new List<Signal>()
                });
            }

            ScanRun latest = await _Ctx.Db.ScanRuns.ReadLatestAsync(ctx.Token).ConfigureAwait(false);
            overview.LastScannedUtc = latest?.CompletedUtc;
            overview.IsScanning = _Ctx.Scan.IsScanning;

            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, overview).ConfigureAwait(false);
        }

        private static List<string> RollupFrameworks(IEnumerable<Project> projects)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (Project p in projects)
            {
                if (String.IsNullOrWhiteSpace(p.TargetFramework)) continue;
                foreach (string tf in p.TargetFramework.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = tf.Trim();
                    if (trimmed.Length > 0) set.Add(trimmed);
                }
            }
            List<string> list = set.ToList();
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

        private static List<Repository> OrderRepositories(
            IEnumerable<Repository> repos, string sort, string dir,
            Dictionary<string, List<Signal>> signalsByRepo, Dictionary<string, List<string>> frameworksByRepo,
            Dictionary<string, GitHubSnapshot> githubByRepo)
        {
            bool desc = String.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);
            switch ((sort ?? "name").ToLowerInvariant())
            {
                case "path": return ApplyString(repos, r => r.Path, desc);
                case "frameworks": return ApplyString(repos, r => frameworksByRepo.TryGetValue(r.Id, out List<string> fw) ? String.Join(",", fw) : String.Empty, desc);
                case "archived": return ApplyInt(repos, r => githubByRepo.TryGetValue(r.Id, out GitHubSnapshot gh) && gh.IsArchived ? 1 : 0, desc);
                case "language": return ApplyString(repos, r => r.PrimaryLanguage.ToString(), desc);
                case "visibility": return ApplyString(repos, r => r.Visibility.ToString(), desc);
                case "version": return ApplyString(repos, r => r.CurrentVersion, desc);
                case "branch": return ApplyString(repos, r => r.CurrentBranch, desc);
                case "ahead": return ApplyInt(repos, r => r.CommitsAhead, desc);
                case "behind": return ApplyInt(repos, r => r.CommitsBehind, desc);
                case "divergence": return ApplyInt(repos, r => r.CommitsAhead + r.CommitsBehind, desc);
                case "projectcount": return ApplyInt(repos, r => r.ProjectCount, desc);
                case "branchcount": return ApplyInt(repos, r => r.BranchCount, desc);
                case "lastupdate": return ApplyDate(repos, r => r.LastUpdateUtc, desc);
                case "lastscanned": return ApplyDate(repos, r => r.LastScannedUtc, desc);
                case "overall": return ApplyInt(repos, r => HealthRank(r.OverallHealth), desc);
                case "testinfra": return ApplyInt(repos, r => SignalRank(signalsByRepo, r.Id, SignalTypeEnum.TestInfra), desc);
                case "telemetry": return ApplyInt(repos, r => SignalRank(signalsByRepo, r.Id, SignalTypeEnum.Telemetry), desc);
                case "outdated":
                case "outdateddependencies": return ApplyInt(repos, r => SignalRank(signalsByRepo, r.Id, SignalTypeEnum.OutdatedDependencies), desc);
                case "cves":
                case "vulnerabilities": return ApplyInt(repos, r => SignalRank(signalsByRepo, r.Id, SignalTypeEnum.Vulnerabilities), desc);
                case "issues":
                case "issuesandpullrequests": return ApplyInt(repos, r => SignalRank(signalsByRepo, r.Id, SignalTypeEnum.IssuesAndPullRequests), desc);
                default: return ApplyString(repos, r => r.Name, desc);
            }
        }

        private static IEnumerable<Repository> ApplySignalFilter(
            IEnumerable<Repository> query, Dictionary<string, List<Signal>> signalsByRepo, SignalTypeEnum type, string status)
        {
            if (String.IsNullOrEmpty(status)) return query;
            return query.Where(r =>
                signalsByRepo.TryGetValue(r.Id, out List<Signal> sigs) &&
                sigs.Any(s => s.SignalType == type && s.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase)));
        }

        private static bool MatchCommits(Repository r, string commits)
        {
            switch (commits.Trim().ToLowerInvariant())
            {
                case "ahead": return r.CommitsAhead > 0;
                case "behind": return r.CommitsBehind > 0;
                case "even": return r.IsGitRepository && r.CommitsAhead == 0 && r.CommitsBehind == 0;
                default: return true;
            }
        }

        private static bool MatchUpdated(Repository r, string bucket)
        {
            string key = bucket.Trim().ToLowerInvariant();
            if (!r.LastUpdateUtc.HasValue) return key == "none";
            if (key == "none") return false;

            double days = (DateTime.UtcNow - r.LastUpdateUtc.Value).TotalDays;
            switch (key)
            {
                case "lastday": return days <= 1;
                case "lastweek": return days <= 7;
                case "lastmonth": return days <= 31;
                case "lastyear": return days <= 365;
                case "older": return days > 365;
                default: return true;
            }
        }

        private static bool MatchIssue(Dictionary<string, List<Signal>> signalsByRepo, string repoId, string value)
        {
            HealthStatusEnum status = HealthStatusEnum.Unknown;
            if (signalsByRepo.TryGetValue(repoId, out List<Signal> sigs))
            {
                Signal signal = sigs.FirstOrDefault(s => s.SignalType == SignalTypeEnum.IssuesAndPullRequests);
                if (signal != null) status = signal.Status;
            }

            bool hasOpen = status == HealthStatusEnum.Red || status == HealthStatusEnum.Yellow;
            switch (value.Trim().ToLowerInvariant())
            {
                case "yes": return hasOpen;
                case "no": return status == HealthStatusEnum.Green;
                default: return true;
            }
        }

        private static List<Repository> ApplyString(IEnumerable<Repository> repos, Func<Repository, string> selector, bool desc)
        {
            Func<Repository, string> safe = r => selector(r) ?? String.Empty;
            return (desc ? repos.OrderByDescending(safe, StringComparer.OrdinalIgnoreCase)
                         : repos.OrderBy(safe, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        private static List<Repository> ApplyInt(IEnumerable<Repository> repos, Func<Repository, int> selector, bool desc)
        {
            return (desc ? repos.OrderByDescending(selector) : repos.OrderBy(selector)).ToList();
        }

        private static List<Repository> ApplyDate(IEnumerable<Repository> repos, Func<Repository, DateTime?> selector, bool desc)
        {
            Func<Repository, DateTime> safe = r => selector(r) ?? DateTime.MinValue;
            return (desc ? repos.OrderByDescending(safe) : repos.OrderBy(safe)).ToList();
        }

        private static int HealthRank(HealthStatusEnum health)
        {
            switch (health)
            {
                case HealthStatusEnum.Green: return 1;
                case HealthStatusEnum.Yellow: return 2;
                case HealthStatusEnum.Red: return 3;
                default: return 0;
            }
        }

        private static int SignalRank(Dictionary<string, List<Signal>> signalsByRepo, string repoId, SignalTypeEnum type)
        {
            if (signalsByRepo.TryGetValue(repoId, out List<Signal> signals))
            {
                foreach (Signal signal in signals)
                {
                    if (signal.SignalType == type) return HealthRank(signal.Status);
                }
            }
            return 0;
        }

        #endregion
    }
}
