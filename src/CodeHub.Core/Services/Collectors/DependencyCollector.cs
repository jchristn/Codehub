namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;

    /// <summary>
    /// Collects outdated and vulnerable NuGet dependencies via the dotnet CLI.
    /// </summary>
    public class DependencyCollector
    {
        #region Private-Members

        private const int TimeoutMs = 120000;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Collect dependency freshness and vulnerability data for a repository's C# projects.
        /// Updates per-project counts and returns the flagged dependency rows.
        /// </summary>
        /// <param name="repository">Repository.</param>
        /// <param name="projects">All projects in the repository.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Flagged dependencies (outdated or vulnerable).</returns>
        public async Task<List<Dependency>> CollectAsync(Repository repository, List<Project> projects, CancellationToken token = default)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            List<Dependency> dependencies = new List<Dependency>();
            List<Project> csharp = projects.Where(p => p.Type == ProjectTypeEnum.CSharp && !p.IsTestProject).ToList();
            if (csharp.Count == 0) return dependencies;

            List<string> targets = ResolveTargets(repository, csharp);
            if (targets.Count == 0) return dependencies;

            Dictionary<string, Dependency> byKey = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);

            foreach (string target in targets)
            {
                await MergeOutdatedAsync(repository, projects, target, byKey, token).ConfigureAwait(false);
                await MergeVulnerableAsync(repository, projects, target, byKey, token).ConfigureAwait(false);
                if (token.IsCancellationRequested) break;
            }

            dependencies = byKey.Values.ToList();

            // Roll up per-project counts.
            foreach (Project project in projects)
            {
                project.OutdatedCount = dependencies.Count(d => d.ProjectId == project.Id && d.Drift != DriftLevelEnum.None);
                project.VulnerableCount = dependencies.Count(d => d.ProjectId == project.Id && d.IsVulnerable);
            }

            return dependencies;
        }

        #endregion

        #region Private-Methods

        private static List<string> ResolveTargets(Repository repository, List<Project> csharp)
        {
            List<string> slns = new List<string>();
            try
            {
                slns.AddRange(Directory.GetFiles(repository.Path, "*.sln", SearchOption.AllDirectories));
                slns.AddRange(Directory.GetFiles(repository.Path, "*.slnx", SearchOption.AllDirectories));
            }
            catch (Exception)
            {
                // ignore
            }

            // Prefer solutions; fall back to individual project files.
            if (slns.Count > 0 && slns.Count <= 5) return slns;
            return csharp.Select(p => p.Path).ToList();
        }

        private async Task MergeOutdatedAsync(
            Repository repository, List<Project> projects, string target,
            Dictionary<string, Dependency> byKey, CancellationToken token)
        {
            ProcessResult result = await ProcessRunner.RunAsync(
                "dotnet", "list \"" + target + "\" package --outdated --format json",
                repository.Path, TimeoutMs, token).ConfigureAwait(false);
            if (!result.Success || String.IsNullOrWhiteSpace(result.StandardOutput)) return;

            ParsePackages(result.StandardOutput, projects, byKey, false);
        }

        private async Task MergeVulnerableAsync(
            Repository repository, List<Project> projects, string target,
            Dictionary<string, Dependency> byKey, CancellationToken token)
        {
            ProcessResult result = await ProcessRunner.RunAsync(
                "dotnet", "list \"" + target + "\" package --vulnerable --format json",
                repository.Path, TimeoutMs, token).ConfigureAwait(false);
            if (!result.Success || String.IsNullOrWhiteSpace(result.StandardOutput)) return;

            ParsePackages(result.StandardOutput, projects, byKey, true);
        }

        private static void ParsePackages(
            string json, List<Project> projects, Dictionary<string, Dependency> byKey, bool vulnerableMode)
        {
            string trimmed = json.Trim();
            int braceIndex = trimmed.IndexOf('{');
            if (braceIndex < 0) return;
            trimmed = trimmed.Substring(braceIndex);

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(trimmed))
                {
                    if (!doc.RootElement.TryGetProperty("projects", out JsonElement projectsElement)) return;

                    foreach (JsonElement projectElement in projectsElement.EnumerateArray())
                    {
                        string projectPath = projectElement.TryGetProperty("path", out JsonElement pathEl) ? pathEl.GetString() : null;
                        Project owner = MatchProject(projects, projectPath);

                        if (!projectElement.TryGetProperty("frameworks", out JsonElement frameworks)) continue;
                        foreach (JsonElement framework in frameworks.EnumerateArray())
                        {
                            if (!framework.TryGetProperty("topLevelPackages", out JsonElement packages)) continue;
                            foreach (JsonElement package in packages.EnumerateArray())
                            {
                                string id = package.TryGetProperty("id", out JsonElement idEl) ? idEl.GetString() : null;
                                if (String.IsNullOrEmpty(id)) continue;
                                string resolved = package.TryGetProperty("resolvedVersion", out JsonElement rv) ? rv.GetString() : null;
                                string latest = package.TryGetProperty("latestVersion", out JsonElement lv) ? lv.GetString() : null;

                                string key = (owner != null ? owner.Id : "repo") + "|" + id;
                                if (!byKey.TryGetValue(key, out Dependency dep))
                                {
                                    dep = new Dependency
                                    {
                                        RepositoryId = owner != null ? owner.RepositoryId : null,
                                        ProjectId = owner != null ? owner.Id : null,
                                        Ecosystem = "nuget",
                                        PackageName = id,
                                        CurrentVersion = resolved
                                    };
                                    byKey[key] = dep;
                                }

                                if (!String.IsNullOrEmpty(resolved)) dep.CurrentVersion = resolved;

                                if (vulnerableMode)
                                {
                                    dep.IsVulnerable = true;
                                    if (package.TryGetProperty("vulnerabilities", out JsonElement vulns))
                                    {
                                        foreach (JsonElement vuln in vulns.EnumerateArray())
                                        {
                                            string severity = vuln.TryGetProperty("severity", out JsonElement sev) ? sev.GetString() : null;
                                            VulnerabilitySeverityEnum parsed = ParseSeverity(severity);
                                            if (parsed > dep.Severity) dep.Severity = parsed;
                                            if (dep.AdvisoryUrl == null && vuln.TryGetProperty("advisoryurl", out JsonElement adv))
                                                dep.AdvisoryUrl = adv.GetString();
                                        }
                                    }
                                }
                                else
                                {
                                    dep.LatestVersion = latest;
                                    dep.Drift = DriftCalculator.Compute(resolved, latest);
                                }
                            }
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // ignore malformed output
            }
        }

        private static Project MatchProject(List<Project> projects, string projectPath)
        {
            if (String.IsNullOrEmpty(projectPath)) return null;
            return projects.FirstOrDefault(p =>
                !String.IsNullOrEmpty(p.Path) &&
                (p.Path.Equals(projectPath, StringComparison.OrdinalIgnoreCase) ||
                 Path.GetFullPath(p.Path).Equals(TryFullPath(projectPath), StringComparison.OrdinalIgnoreCase)));
        }

        private static string TryFullPath(string path)
        {
            try
            {
                return Path.GetFullPath(path);
            }
            catch (Exception)
            {
                return path;
            }
        }

        private static VulnerabilitySeverityEnum ParseSeverity(string severity)
        {
            if (String.IsNullOrEmpty(severity)) return VulnerabilitySeverityEnum.Moderate;
            switch (severity.Trim().ToLowerInvariant())
            {
                case "critical": return VulnerabilitySeverityEnum.Critical;
                case "high": return VulnerabilitySeverityEnum.High;
                case "moderate": return VulnerabilitySeverityEnum.Moderate;
                case "medium": return VulnerabilitySeverityEnum.Moderate;
                case "low": return VulnerabilitySeverityEnum.Low;
                default: return VulnerabilitySeverityEnum.Moderate;
            }
        }

        #endregion
    }
}
