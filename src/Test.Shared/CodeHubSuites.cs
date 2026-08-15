namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Serialization;
    using CodeHub.Core.Services;
    using CodeHub.Core.Services.Collectors;
    using Touchstone.Core;

    /// <summary>
    /// Shared Touchstone test-suite descriptors for CodeHub core logic.
    /// </summary>
    public static class CodeHubSuites
    {
        #region Public-Members

        /// <summary>
        /// All suites.
        /// </summary>
        public static IReadOnlyList<TestSuiteDescriptor> All
        {
            get
            {
                return new List<TestSuiteDescriptor>
                {
                    ScoringSuite(),
                    DriftSuite(),
                    GitHubRefSuite(),
                    SerializerSuite(),
                    SelectionSuite()
                };
            }
        }

        #endregion

        #region Suites

        /// <summary>
        /// Scoring logic suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor ScoringSuite()
        {
            ScoringService scoring = new ScoringService();

            return new TestSuiteDescriptor(
                suiteId: "Scoring",
                displayName: "Scoring Service",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Scoring", "TouchstoneGreen", "Touchstone suite scores Test Infra green",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Widget" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Widget.Core", Type = ProjectTypeEnum.CSharp },
                                new Project { Name = "Test.Shared", Type = ProjectTypeEnum.CSharp, IsTestProject = true, HasTouchstone = true },
                                new Project { Name = "Test.Automated", Type = ProjectTypeEnum.CSharp, IsTestProject = true, HasTouchstone = true }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.TestInfra, HealthStatusEnum.Green);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "NoTestsRed", "C# project with no tests scores Test Infra red",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Widget" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Widget.Core", Type = ProjectTypeEnum.CSharp }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.TestInfra, HealthStatusEnum.Red);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryGreen", "Web service with Radiant scores Telemetry green",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Api" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Api.Server", Type = ProjectTypeEnum.CSharp, IsWebService = true, HasWatson7 = true, HasRadiant = true }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.Green);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryYellow", "Watson web service without Radiant scores Telemetry yellow",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Api" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Api.Server", Type = ProjectTypeEnum.CSharp, IsWebService = true, HasWatson7 = true, HasRadiant = false }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.Yellow);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryNaForLibrary", "Non-web C# library scores Telemetry N/A",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Lib" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Lib", Type = ProjectTypeEnum.CSharp }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.NotApplicable);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "OutdatedMajorRed", "A major-behind dependency scores Outdated red",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Widget" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Widget.Core", Type = ProjectTypeEnum.CSharp }
                            };
                            List<Dependency> deps = new List<Dependency>
                            {
                                new Dependency { PackageName = "Foo", Drift = DriftLevelEnum.Major }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, deps, null, false);
                            AssertStatus(signals, SignalTypeEnum.OutdatedDependencies, HealthStatusEnum.Red);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "VulnerableHighRed", "A high-severity vulnerability scores CVEs red",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Widget" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Widget.Core", Type = ProjectTypeEnum.CSharp }
                            };
                            List<Dependency> deps = new List<Dependency>
                            {
                                new Dependency { PackageName = "Foo", IsVulnerable = true, Severity = VulnerabilitySeverityEnum.High }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, deps, null, false);
                            AssertStatus(signals, SignalTypeEnum.Vulnerabilities, HealthStatusEnum.Red);
                            return Task.CompletedTask;
                        })
                });
        }

        /// <summary>
        /// Version drift suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor DriftSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Drift",
                displayName: "Drift Calculator",
                cases: new List<TestCaseDescriptor>
                {
                    Drift("Major", "1.2.3", "2.0.0", DriftLevelEnum.Major),
                    Drift("Minor", "1.2.3", "1.3.0", DriftLevelEnum.Minor),
                    Drift("Patch", "1.2.3", "1.2.4", DriftLevelEnum.Patch),
                    Drift("None", "1.2.3", "1.2.3", DriftLevelEnum.None),
                    Drift("Prerelease", "1.2.3", "1.2.4-beta", DriftLevelEnum.Patch)
                });
        }

        /// <summary>
        /// GitHub remote parsing suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor GitHubRefSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "GitHubRef",
                displayName: "GitHub Remote Parsing",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("GitHubRef", "Https", "Parses https remote",
                        executeAsync: _ =>
                        {
                            GitHubRepoRef parsed = GitHubRepoRef.Parse("https://github.com/jchristn/Radiant.git");
                            AssertTrue(parsed != null && parsed.Owner == "jchristn" && parsed.Repo == "Radiant", "https parse");
                            return Task.CompletedTask;
                        }),
                    new TestCaseDescriptor("GitHubRef", "Ssh", "Parses ssh remote",
                        executeAsync: _ =>
                        {
                            GitHubRepoRef parsed = GitHubRepoRef.Parse("git@github.com:jchristn/Watson.git");
                            AssertTrue(parsed != null && parsed.Owner == "jchristn" && parsed.Repo == "Watson", "ssh parse");
                            return Task.CompletedTask;
                        }),
                    new TestCaseDescriptor("GitHubRef", "NonGitHub", "Non-GitHub remote returns null",
                        executeAsync: _ =>
                        {
                            GitHubRepoRef parsed = GitHubRepoRef.Parse("https://gitlab.com/foo/bar.git");
                            AssertTrue(parsed == null, "non-github null");
                            return Task.CompletedTask;
                        })
                });
        }

        /// <summary>
        /// Serializer suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor SerializerSuite()
        {
            Serializer serializer = new Serializer();

            return new TestSuiteDescriptor(
                suiteId: "Serializer",
                displayName: "Serializer",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Serializer", "EnumAsString", "Enums serialize as camelCase strings",
                        executeAsync: _ =>
                        {
                            Signal signal = new Signal { SignalType = SignalTypeEnum.TestInfra, Status = HealthStatusEnum.Green };
                            string json = serializer.SerializeJson(signal);
                            AssertTrue(json.Contains("\"status\":\"Green\""), "status enum string");
                            AssertTrue(json.Contains("\"signalType\":\"TestInfra\""), "camelCase property");
                            return Task.CompletedTask;
                        }),
                    new TestCaseDescriptor("Serializer", "RoundTrip", "Round-trips a repository",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Widget", OverallHealth = HealthStatusEnum.Yellow };
                            string json = serializer.SerializeJson(repo);
                            Repository back = serializer.DeserializeJson<Repository>(json);
                            AssertTrue(back != null && back.Name == "Widget" && back.OverallHealth == HealthStatusEnum.Yellow, "round trip");
                            return Task.CompletedTask;
                        })
                });
        }

        /// <summary>
        /// Scan-selection path logic suite.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor SelectionSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Selection",
                displayName: "Selection Service",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Selection", "UnderTrue", "Child is strictly under parent",
                        executeAsync: _ =>
                        {
                            AssertTrue(SelectionService.IsStrictlyUnder("C:\\a\\b", "C:\\a"), "b under a");
                            AssertTrue(!SelectionService.IsStrictlyUnder("C:\\a", "C:\\a"), "a not under a");
                            AssertTrue(!SelectionService.IsStrictlyUnder("C:\\ab", "C:\\a"), "ab not under a (prefix trap)");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Selection", "SelectedAndPartial", "State resolves selected, partial, none",
                        executeAsync: _ =>
                        {
                            SelectionSets sets = new SelectionSets();
                            sets.Included.Add("C:\\code\\Dell");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Dell", sets) == SelectionStateEnum.Selected, "self selected");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Dell\\App", sets) == SelectionStateEnum.Selected, "descendant selected");
                            AssertTrue(SelectionService.StateFor("C:\\code", sets) == SelectionStateEnum.Partial, "ancestor partial");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Other", sets) == SelectionStateEnum.None, "sibling none");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Selection", "ExcludedWins", "Exclude under a selected branch wins",
                        executeAsync: _ =>
                        {
                            SelectionSets sets = new SelectionSets();
                            sets.Included.Add("C:\\code\\Dell");
                            sets.Excluded.Add("C:\\code\\Dell\\Legacy");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Dell\\Legacy", sets) == SelectionStateEnum.Excluded, "excluded self");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Dell\\Legacy\\Sub", sets) == SelectionStateEnum.Excluded, "excluded descendant");
                            AssertTrue(SelectionService.StateFor("C:\\code\\Dell\\Active", sets) == SelectionStateEnum.Selected, "sibling still selected");
                            return Task.CompletedTask;
                        })
                });
        }

        #endregion

        #region Private-Methods

        private static TestCaseDescriptor Drift(string caseId, string current, string latest, DriftLevelEnum expected)
        {
            return new TestCaseDescriptor("Drift", caseId, current + " -> " + latest + " = " + expected,
                executeAsync: _ =>
                {
                    DriftLevelEnum actual = DriftCalculator.Compute(current, latest);
                    AssertTrue(actual == expected, "expected " + expected + " but got " + actual);
                    return Task.CompletedTask;
                });
        }

        private static void AssertStatus(IReadOnlyList<Signal> signals, SignalTypeEnum type, HealthStatusEnum expected)
        {
            foreach (Signal signal in signals)
            {
                if (signal.SignalType == type)
                {
                    if (signal.Status != expected)
                        throw new Exception(type + " expected " + expected + " but was " + signal.Status + " (" + signal.Detail + ")");
                    return;
                }
            }
            throw new Exception("Signal " + type + " not found.");
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        #endregion
    }
}
