namespace CodeHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;

    /// <summary>
    /// Computes repository signals and overall health from collected facts. Pure logic, no I/O.
    /// </summary>
    public class ScoringService
    {
        #region Public-Methods

        /// <summary>
        /// Score a repository, returning its signals and setting its overall health.
        /// </summary>
        /// <param name="repository">Repository (OverallHealth is set).</param>
        /// <param name="projects">Projects in the repository.</param>
        /// <param name="dependencies">Flagged dependencies.</param>
        /// <param name="gitHub">GitHub snapshot, or null.</param>
        /// <param name="gitHubConfigured">Whether a GitHub token is configured.</param>
        /// <returns>Computed signals.</returns>
        public List<Signal> Score(
            Repository repository,
            List<Project> projects,
            List<Dependency> dependencies,
            GitHubSnapshot gitHub,
            bool gitHubConfigured)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));
            projects = projects ?? new List<Project>();
            dependencies = dependencies ?? new List<Dependency>();

            List<Signal> signals = new List<Signal>
            {
                ScoreTestInfra(repository, projects),
                ScoreTelemetry(repository, projects),
                ScoreOutdated(repository, projects, dependencies),
                ScoreVulnerabilities(repository, projects, dependencies, gitHub),
                ScoreIssues(repository, gitHub, gitHubConfigured)
            };

            repository.OverallHealth = ComputeOverall(signals);
            return signals;
        }

        #endregion

        #region Private-Methods

        private static Signal ScoreTestInfra(Repository repository, List<Project> projects)
        {
            List<Project> csharp = projects.Where(p => p.Type == ProjectTypeEnum.CSharp && !p.IsTestProject).ToList();
            Signal signal = New(repository, SignalTypeEnum.TestInfra);

            if (csharp.Count == 0)
            {
                signal.Status = HealthStatusEnum.NotApplicable;
                signal.Detail = "No C# source projects.";
                return signal;
            }

            List<Project> tests = projects.Where(p => p.IsTestProject).ToList();
            bool hasTouchstone = projects.Any(p => p.HasTouchstone);
            bool hasAutomated = projects.Any(p => p.Name != null && p.Name.IndexOf("Test.Automated", StringComparison.OrdinalIgnoreCase) >= 0);

            if (hasTouchstone && hasAutomated)
            {
                signal.Status = HealthStatusEnum.Green;
                signal.Detail = "Touchstone suite present (Test.Shared + Test.Automated).";
            }
            else if (tests.Count > 0)
            {
                signal.Status = HealthStatusEnum.Yellow;
                signal.Detail = hasTouchstone
                    ? "Touchstone referenced but no Test.Automated console runner found."
                    : tests.Count + " test project(s) present, but not Touchstone-shaped.";
            }
            else
            {
                signal.Status = HealthStatusEnum.Red;
                signal.Detail = "No test projects found for " + csharp.Count + " C# project(s).";
            }

            return signal;
        }

        private static Signal ScoreTelemetry(Repository repository, List<Project> projects)
        {
            List<Project> webServices = projects.Where(p => p.Type == ProjectTypeEnum.CSharp && p.IsWebService).ToList();
            Signal signal = New(repository, SignalTypeEnum.Telemetry);

            if (webServices.Count == 0)
            {
                signal.Status = HealthStatusEnum.NotApplicable;
                signal.Detail = "No C# web services.";
                return signal;
            }

            bool anyRadiant = webServices.Any(p => p.HasRadiant);
            bool anyWatson = webServices.Any(p => p.HasWatson7);

            if (anyRadiant)
            {
                signal.Status = HealthStatusEnum.Green;
                signal.Detail = "Web service wires Radiant telemetry.";
            }
            else if (anyWatson)
            {
                signal.Status = HealthStatusEnum.Yellow;
                signal.Detail = "Watson 7 web service with no Radiant telemetry host.";
            }
            else
            {
                signal.Status = HealthStatusEnum.Red;
                signal.Detail = "Web service with no detected telemetry.";
            }

            return signal;
        }

        private static Signal ScoreOutdated(Repository repository, List<Project> projects, List<Dependency> dependencies)
        {
            List<Project> csharp = projects.Where(p => p.Type == ProjectTypeEnum.CSharp && !p.IsTestProject).ToList();
            Signal signal = New(repository, SignalTypeEnum.OutdatedDependencies);

            if (csharp.Count == 0)
            {
                signal.Status = HealthStatusEnum.NotApplicable;
                signal.Detail = "Dependency checks apply to C# projects.";
                return signal;
            }

            List<Dependency> outdated = dependencies.Where(d => d.Drift != DriftLevelEnum.None).ToList();
            if (outdated.Count == 0)
            {
                signal.Status = HealthStatusEnum.Green;
                signal.Detail = "No outdated dependencies detected.";
                return signal;
            }

            bool anyMajor = outdated.Any(d => d.Drift == DriftLevelEnum.Major);
            signal.Status = anyMajor ? HealthStatusEnum.Red : HealthStatusEnum.Yellow;
            signal.Detail = outdated.Count + " outdated package(s)" +
                (anyMajor ? " (at least one major version behind)." : " (minor/patch updates available).");
            return signal;
        }

        private static Signal ScoreVulnerabilities(Repository repository, List<Project> projects, List<Dependency> dependencies, GitHubSnapshot gitHub)
        {
            Signal signal = New(repository, SignalTypeEnum.Vulnerabilities);

            bool hasCode = projects.Any(p => p.Type == ProjectTypeEnum.CSharp && !p.IsTestProject);
            bool hasGitHub = gitHub != null && String.IsNullOrEmpty(gitHub.Error);
            if (!hasCode && !hasGitHub)
            {
                signal.Status = HealthStatusEnum.NotApplicable;
                signal.Detail = "No analyzable dependencies.";
                return signal;
            }

            List<Dependency> vulnerable = dependencies.Where(d => d.IsVulnerable).ToList();
            VulnerabilitySeverityEnum worst = VulnerabilitySeverityEnum.None;
            foreach (Dependency dependency in vulnerable)
            {
                if (dependency.Severity > worst) worst = dependency.Severity;
            }

            int dependabotOpen = gitHub?.DependabotOpen ?? 0;
            int dependabotHigh = gitHub?.DependabotHigh ?? 0;
            int dependabotCritical = gitHub?.DependabotCritical ?? 0;

            if (dependabotCritical > 0) worst = VulnerabilitySeverityEnum.Critical;
            else if (dependabotHigh > 0 && worst < VulnerabilitySeverityEnum.High) worst = VulnerabilitySeverityEnum.High;
            else if (dependabotOpen > 0 && worst < VulnerabilitySeverityEnum.Moderate) worst = VulnerabilitySeverityEnum.Moderate;

            int total = vulnerable.Count + dependabotOpen;
            if (total == 0)
            {
                signal.Status = HealthStatusEnum.Green;
                signal.Detail = "No known vulnerabilities or Dependabot alerts.";
                return signal;
            }

            if (worst >= VulnerabilitySeverityEnum.High) signal.Status = HealthStatusEnum.Red;
            else signal.Status = HealthStatusEnum.Yellow;

            signal.Detail = vulnerable.Count + " vulnerable package(s), " + dependabotOpen +
                " open Dependabot alert(s); worst severity " + worst + ".";
            return signal;
        }

        private static Signal ScoreIssues(Repository repository, GitHubSnapshot gitHub, bool gitHubConfigured)
        {
            Signal signal = New(repository, SignalTypeEnum.IssuesAndPullRequests);

            if (!gitHubConfigured)
            {
                signal.Status = HealthStatusEnum.Unknown;
                signal.Detail = "GitHub token not configured.";
                return signal;
            }

            if (gitHub == null || !String.IsNullOrEmpty(gitHub.Error))
            {
                signal.Status = HealthStatusEnum.Unknown;
                signal.Detail = gitHub?.Error ?? "No GitHub data.";
                return signal;
            }

            if (gitHub.OpenPullRequests > 0)
            {
                signal.Status = HealthStatusEnum.Red;
            }
            else if (gitHub.OpenIssues > 0)
            {
                signal.Status = HealthStatusEnum.Yellow;
            }
            else
            {
                signal.Status = HealthStatusEnum.Green;
            }

            signal.Detail = gitHub.OpenIssues + " open issue(s), " + gitHub.OpenPullRequests + " open pull request(s).";
            return signal;
        }

        private static HealthStatusEnum ComputeOverall(List<Signal> signals)
        {
            // Overall health is driven by the maintenance signals; issues/PRs are informational.
            List<HealthStatusEnum> driving = signals
                .Where(s => s.SignalType != SignalTypeEnum.IssuesAndPullRequests)
                .Select(s => s.Status)
                .Where(s => s != HealthStatusEnum.NotApplicable && s != HealthStatusEnum.Unknown)
                .ToList();

            if (driving.Count == 0) return HealthStatusEnum.Unknown;
            if (driving.Contains(HealthStatusEnum.Red)) return HealthStatusEnum.Red;
            if (driving.Contains(HealthStatusEnum.Yellow)) return HealthStatusEnum.Yellow;
            return HealthStatusEnum.Green;
        }

        private static Signal New(Repository repository, SignalTypeEnum type)
        {
            return new Signal
            {
                RepositoryId = repository.Id,
                SignalType = type
            };
        }

        #endregion
    }
}
