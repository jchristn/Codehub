namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using CodeHub.Core.Database;
    using CodeHub.Core.Database.Sqlite;
    using CodeHub.Core.Database.Sqlite.Queries;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;
    using CodeHub.Core.Models;
    using CodeHub.Core.Serialization;
    using CodeHub.Core.Services;
    using CodeHub.Core.Services.Collectors;
    using Microsoft.Data.Sqlite;
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
                    SelectionSuite(),
                    CustomActionSuite(),
                    LaunchSuite()
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

                    new TestCaseDescriptor("Scoring", "TelemetryGreen", "C# project with metrics/traces scores Telemetry green",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Api" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Api.Server", Type = ProjectTypeEnum.CSharp, IsWebService = true, HasWatson7 = true, HasRadiant = true, HasTelemetry = true }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.Green);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryRedWatson", "Watson web service without metrics/traces scores Telemetry red",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Api" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Api.Server", Type = ProjectTypeEnum.CSharp, IsWebService = true, HasWatson7 = true, HasRadiant = false }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.Red);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryRedLibrary", "Non-Watson C# library without metrics/traces scores Telemetry red",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Lib" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "Lib", Type = ProjectTypeEnum.CSharp }
                            };
                            List<Signal> signals = scoring.Score(repo, projects, new List<Dependency>(), null, false);
                            AssertStatus(signals, SignalTypeEnum.Telemetry, HealthStatusEnum.Red);
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Scoring", "TelemetryNaWithoutCSharp", "Repo with only test or non-C# projects scores Telemetry N/A",
                        executeAsync: _ =>
                        {
                            Repository repo = new Repository { Name = "Web" };
                            List<Project> projects = new List<Project>
                            {
                                new Project { Name = "web", Type = ProjectTypeEnum.Node },
                                new Project { Name = "Test.Harness", Type = ProjectTypeEnum.CSharp, IsTestProject = true }
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
                        }),

                    new TestCaseDescriptor("Selection", "OnDiskCasing", "Paths resolve to their on-disk casing",
                        executeAsync: _ =>
                        {
                            string root = Path.Combine(Path.GetTempPath(), "codehub-case-" + Guid.NewGuid().ToString("N"));
                            string actual = Path.Combine(root, "MixedCase", "Inner");
                            Directory.CreateDirectory(actual);
                            try
                            {
                                string lowered = Path.Combine(root, "mixedcase", "inner");
                                if (!OperatingSystem.IsLinux()) // case-sensitive filesystem: lowered path does not exist
                                    AssertTrue(SelectionService.ResolveOnDiskCasing(lowered) == actual, "lowered path resolves to on-disk casing");
                                AssertTrue(SelectionService.ResolveOnDiskCasing(actual) == actual, "exact path unchanged");
                                string missing = Path.Combine(actual, "NotThere");
                                AssertTrue(SelectionService.ResolveOnDiskCasing(missing) == missing, "missing segment keeps given casing");
                            }
                            finally
                            {
                                Directory.Delete(root, true);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Selection", "NoCaseMigration", "Migration dedupes case variants and lookups ignore case",
                        executeAsync: async _ =>
                        {
                            string dir = Path.Combine(Path.GetTempPath(), "codehub-db-" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(dir);
                            string file = Path.Combine(dir, "codehub.db");
                            try
                            {
                                // Pre-migration schema holding case-variant duplicates.
                                using (SqliteDatabaseDriver legacy = new SqliteDatabaseDriver(new DatabaseSettings { Filename = file }))
                                {
                                    await legacy.ExecuteQueriesAsync(new List<string>
                                    {
                                        "CREATE TABLE scan_selections (id TEXT PRIMARY KEY, path TEXT NOT NULL UNIQUE, included INTEGER NOT NULL DEFAULT 1, createdutc TEXT NOT NULL);",
                                        "INSERT INTO scan_selections VALUES ('a', 'C:\\Code\\Pneuma', 1, '2026-01-01T00:00:00Z');",
                                        "INSERT INTO scan_selections VALUES ('b', 'c:\\code\\pneuma', 1, '2026-02-01T00:00:00Z');",
                                        "INSERT INTO scan_selections VALUES ('c', 'C:\\Code\\Other', 0, '2026-01-01T00:00:00Z');"
                                    }).ConfigureAwait(false);
                                }

                                using (DatabaseDriverBase db = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings { Filename = file }).ConfigureAwait(false))
                                {
                                    List<ScanSelection> rows = await db.Selections.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(rows.Count == 2, "duplicate removed (got " + rows.Count + ")");
                                    AssertTrue(rows.Exists(r => r.Id == "a" && r.Path == "C:\\Code\\Pneuma"), "earliest row kept");

                                    await db.Selections.UpsertAsync(new ScanSelection { Path = "c:\\code\\other", Included = true }).ConfigureAwait(false);
                                    rows = await db.Selections.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(rows.Count == 2, "case-variant upsert updates instead of inserting");
                                    AssertTrue(rows.Exists(r => r.Id == "c" && r.Path == "c:\\code\\other" && r.Included), "upsert rewrote path and included");

                                    bool rejected = false;
                                    try
                                    {
                                        await db.ExecuteQueryAsync("INSERT INTO scan_selections VALUES ('d', 'C:\\CODE\\OTHER', 1, '2026-03-01T00:00:00Z');").ConfigureAwait(false);
                                    }
                                    catch (SqliteException)
                                    {
                                        rejected = true;
                                    }
                                    AssertTrue(rejected, "schema rejects a case-variant duplicate insert");

                                    await db.Selections.DeleteByPathAsync("C:\\CODE\\PNEUMA").ConfigureAwait(false);
                                    AssertTrue(await db.Selections.CountAsync().ConfigureAwait(false) == 1, "delete matches case-insensitively");
                                }
                            }
                            finally
                            {
                                SqliteConnection.ClearAllPools();
                                Directory.Delete(dir, true);
                            }
                        }),

                    new TestCaseDescriptor("Selection", "RepositoryNoCaseMigration", "Migration merges case-variant repositories and annotations",
                        executeAsync: async _ =>
                        {
                            string dir = Path.Combine(Path.GetTempPath(), "codehub-db-" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(dir);
                            string file = Path.Combine(dir, "codehub.db");
                            try
                            {
                                // Pre-migration schema (case-sensitive keys) holding case-variant duplicates.
                                using (SqliteDatabaseDriver legacy = new SqliteDatabaseDriver(new DatabaseSettings { Filename = file }))
                                {
                                    List<string> setup = new List<string>();
                                    foreach (string create in TableQueries.All) setup.Add(create.Replace(" COLLATE NOCASE", ""));
                                    setup.Add("INSERT INTO repositories (id, path, name, createdutc) VALUES ('old', 'c:\\code\\armor', 'armor', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO repositories (id, path, name, createdutc) VALUES ('new', 'C:\\Code\\Armor', 'Armor', '2026-02-01T00:00:00Z');");
                                    setup.Add("INSERT INTO repositories (id, path, name, createdutc) VALUES ('solo', 'C:\\Code\\Solo', 'Solo', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO projects (id, repoid, path, name, createdutc) VALUES ('p-old', 'old', 'c:\\code\\armor\\a.csproj', 'A', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO projects (id, repoid, path, name, createdutc) VALUES ('p-new', 'new', 'C:\\Code\\Armor\\a.csproj', 'A', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO annotations (id, repoid, signalcolumn, status, createdutc) VALUES ('a1', 'old', 'Telemetry', 'Green', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO annotations (id, repoid, signalcolumn, status, createdutc) VALUES ('a2', 'new', 'Overall', 'Yellow', '2026-02-01T00:00:00Z');");
                                    setup.Add("INSERT INTO annotations (id, repoid, signalcolumn, status, createdutc) VALUES ('a3', 'solo', 'telemetry', 'Red', '2026-01-01T00:00:00Z');");
                                    setup.Add("INSERT INTO annotations (id, repoid, signalcolumn, status, createdutc) VALUES ('a4', 'solo', 'Telemetry', 'Green', '2026-02-01T00:00:00Z');");
                                    await legacy.ExecuteQueriesAsync(setup).ConfigureAwait(false);
                                }

                                using (DatabaseDriverBase db = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings { Filename = file }).ConfigureAwait(false))
                                {
                                    List<Repository> repos = await db.Repositories.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(repos.Count == 2, "duplicate repository removed (got " + repos.Count + ")");
                                    Repository armor = repos.Find(r => r.Id == "old");
                                    AssertTrue(armor != null && armor.Path == "C:\\Code\\Armor" && armor.Name == "Armor", "oldest row kept with newest casing");

                                    List<Project> projects = await db.Projects.EnumerateAllAsync().ConfigureAwait(false);
                                    AssertTrue(projects.Count == 1 && projects[0].Id == "p-old", "dropped repository's children removed");

                                    List<Annotation> armorAnn = await db.Annotations.EnumerateByRepositoryAsync("old").ConfigureAwait(false);
                                    AssertTrue(armorAnn.Count == 2, "overrides from both variants kept on the surviving repository");

                                    List<Annotation> soloAnn = await db.Annotations.EnumerateByRepositoryAsync("solo").ConfigureAwait(false);
                                    AssertTrue(soloAnn.Count == 1 && soloAnn[0].Status == "Green", "newest case-variant override kept");

                                    Repository byPath = await db.Repositories.ReadByPathAsync("c:\\CODE\\armor").ConfigureAwait(false);
                                    AssertTrue(byPath != null && byPath.Id == "old", "read by path ignores case");

                                    byPath.Path = "C:\\code\\ARMOR";
                                    await db.Repositories.UpsertAsync(byPath).ConfigureAwait(false);
                                    repos = await db.Repositories.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(repos.Count == 2 && repos.Exists(r => r.Id == "old" && r.Path == "C:\\code\\ARMOR"), "case-variant upsert updates in place");
                                }
                            }
                            finally
                            {
                                SqliteConnection.ClearAllPools();
                                Directory.Delete(dir, true);
                            }
                        })
                });
        }

        /// <summary>
        /// Custom action suite: actions are agent-agnostic prompts; the agent is chosen at run time.
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor CustomActionSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "CustomAction",
                displayName: "Custom Actions",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("CustomAction", "AgentNormalize", "Supported agents normalize; unknown agents are rejected",
                        executeAsync: _ =>
                        {
                            AssertTrue(AgentHelper.Normalize("claude") == "claude", "claude");
                            AssertTrue(AgentHelper.Normalize(" Codex ") == "codex", "trimmed and lowercased");
                            AssertTrue(AgentHelper.Normalize("MUX") == "mux", "mux");
                            AssertTrue(AgentHelper.Normalize("opencode") == "opencode", "opencode");
                            AssertTrue(AgentHelper.Normalize("gpt") == null, "unknown agent rejected");
                            AssertTrue(AgentHelper.Normalize("") == null && AgentHelper.Normalize(null) == null, "empty rejected");
                            AssertTrue(AgentHelper.InvalidMessage().Contains("claude, codex, mux, opencode"), "message lists agents");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("CustomAction", "RoundTrip", "Actions store only a name and prompt (newlines preserved)",
                        executeAsync: async _ =>
                        {
                            string dir = Path.Combine(Path.GetTempPath(), "codehub-db-" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(dir);
                            string file = Path.Combine(dir, "codehub.db");
                            try
                            {
                                using (DatabaseDriverBase db = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings { Filename = file }).ConfigureAwait(false))
                                {
                                    CustomAction action = new CustomAction { Name = "Review", Prompt = "Review this repo.\nList 'risks' first." };
                                    await db.CustomActions.UpsertAsync(action).ConfigureAwait(false);

                                    CustomAction read = await db.CustomActions.ReadAsync(action.Id).ConfigureAwait(false);
                                    AssertTrue(read != null && read.Name == "Review", "read back by id");
                                    AssertTrue(read.Prompt == "Review this repo.\nList 'risks' first.", "prompt preserved verbatim");

                                    read.Name = "Deep review";
                                    read.Prompt = "Updated";
                                    await db.CustomActions.UpsertAsync(read).ConfigureAwait(false);
                                    List<CustomAction> all = await db.CustomActions.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(all.Count == 1 && all[0].Name == "Deep review" && all[0].Prompt == "Updated", "upsert updates in place");

                                    await db.CustomActions.DeleteAsync(action.Id).ConfigureAwait(false);
                                    AssertTrue((await db.CustomActions.EnumerateAsync().ConfigureAwait(false)).Count == 0, "deleted");
                                }
                            }
                            finally
                            {
                                SqliteConnection.ClearAllPools();
                                Directory.Delete(dir, true);
                            }
                        }),

                    new TestCaseDescriptor("CustomAction", "DropAgentMigration", "Migration drops the legacy agent/dangerous columns and keeps actions",
                        executeAsync: async _ =>
                        {
                            string dir = Path.Combine(Path.GetTempPath(), "codehub-db-" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(dir);
                            string file = Path.Combine(dir, "codehub.db");
                            try
                            {
                                // Pre-migration schema, when each action was tied to one agent.
                                using (SqliteDatabaseDriver legacy = new SqliteDatabaseDriver(new DatabaseSettings { Filename = file }))
                                {
                                    await legacy.ExecuteQueriesAsync(new List<string>
                                    {
                                        "CREATE TABLE custom_actions (id TEXT PRIMARY KEY, name TEXT NOT NULL, agent TEXT NOT NULL, dangerous INTEGER NOT NULL DEFAULT 0, prompt TEXT, createdutc TEXT NOT NULL);",
                                        "INSERT INTO custom_actions VALUES ('act_1', 'Review', 'codex', 1, 'Review this repo.', '2026-01-01T00:00:00Z');",
                                        "INSERT INTO custom_actions VALUES ('act_2', 'Upgrade', 'claude', 0, NULL, '2026-02-01T00:00:00Z');"
                                    }).ConfigureAwait(false);
                                }

                                using (DatabaseDriverBase db = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings { Filename = file }).ConfigureAwait(false))
                                {
                                    System.Data.DataTable info = await db.ExecuteQueryAsync("SELECT name FROM pragma_table_info('custom_actions');").ConfigureAwait(false);
                                    List<string> columns = new List<string>();
                                    foreach (System.Data.DataRow row in info.Rows) columns.Add(row["name"].ToString());
                                    AssertTrue(!columns.Contains("agent") && !columns.Contains("dangerous"), "legacy columns dropped");

                                    List<CustomAction> actions = await db.CustomActions.EnumerateAsync().ConfigureAwait(false);
                                    AssertTrue(actions.Count == 2, "actions kept (got " + actions.Count + ")");
                                    AssertTrue(actions.Exists(a => a.Id == "act_1" && a.Name == "Review" && a.Prompt == "Review this repo."), "name and prompt kept");

                                    // New actions insert without an agent.
                                    await db.CustomActions.UpsertAsync(new CustomAction { Name = "New", Prompt = "p" }).ConfigureAwait(false);
                                    AssertTrue((await db.CustomActions.EnumerateAsync().ConfigureAwait(false)).Count == 3, "insert after migration");
                                }

                                // Re-initializing an already-migrated database is a no-op.
                                using (DatabaseDriverBase db = await DatabaseDriverFactory.CreateAndInitializeAsync(new DatabaseSettings { Filename = file }).ConfigureAwait(false))
                                {
                                    AssertTrue((await db.CustomActions.EnumerateAsync().ConfigureAwait(false)).Count == 3, "idempotent");
                                }
                            }
                            finally
                            {
                                SqliteConnection.ClearAllPools();
                                Directory.Delete(dir, true);
                            }
                        })
                });
        }


        /// <summary>
        /// Cross-platform launch suite: agent command lines, quoting, and the POSIX launch scripts
        /// used on macOS and Linux (executed for real on non-Windows hosts).
        /// </summary>
        /// <returns>Suite descriptor.</returns>
        public static TestSuiteDescriptor LaunchSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Launch",
                displayName: "Launcher",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor("Launch", "TargetCommands", "Open targets map to agent commands and flags",
                        executeAsync: _ =>
                        {
                            AssertTrue(LaunchHelper.TargetCommand("terminal", true) == null, "terminal runs a plain shell");
                            AssertTrue(LaunchHelper.TargetCommand("claude", false) == "claude", "claude");
                            AssertTrue(LaunchHelper.TargetCommand(" Claude ", true) == "claude --dangerously-skip-permissions", "claude dangerous");
                            AssertTrue(LaunchHelper.TargetCommand("codex", true) == "codex --yolo", "codex dangerous");
                            AssertTrue(LaunchHelper.TargetCommand("mux", true) == "mux --yolo", "mux dangerous");
                            AssertTrue(LaunchHelper.TargetCommand("opencode", true) == "opencode", "opencode has no dangerous flag");
                            AssertThrows<ArgumentException>(() => LaunchHelper.TargetCommand("explorer", false), "explorer is not a terminal target");
                            AssertThrows<ArgumentException>(() => LaunchHelper.TargetCommand("bogus", false), "unknown target rejected");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Launch", "AgentPromptCommand", "Agent commands pass the prompt file as one argument",
                        executeAsync: _ =>
                        {
                            AssertTrue(LaunchHelper.PosixAgentCommand("claude", false, null) == "claude", "no prompt");
                            AssertTrue(LaunchHelper.PosixAgentCommand("claude", true, "/tmp/p.txt")
                                == "claude --dangerously-skip-permissions \"$(cat '/tmp/p.txt')\"", "claude with prompt");
                            AssertTrue(LaunchHelper.PosixAgentCommand("mux", true, "/tmp/p.txt")
                                == "mux --yolo --prompt \"$(cat '/tmp/p.txt')\"", "mux uses --prompt");
                            AssertThrows<ArgumentException>(() => LaunchHelper.PosixAgentCommand("gpt", false, null), "unknown agent rejected");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Launch", "Quoting", "Shell and AppleScript quoting escape special characters",
                        executeAsync: _ =>
                        {
                            AssertTrue(LaunchHelper.ShellQuote("it's") == "'it'\\''s'", "single quote escaped");
                            AssertTrue(LaunchHelper.AppleScriptQuote("a \"b\" \\c") == "\"a \\\"b\\\" \\\\c\"", "applescript escaped");

                            if (!OperatingSystem.IsWindows())
                            {
                                string nasty = "it's \"$HOME\" `id` $(id) \\ ; & | *\nline two";
                                ProcessResult result = RunProcess("/bin/sh", null, "-c", "printf %s " + LaunchHelper.ShellQuote(nasty));
                                AssertTrue(result.Output == nasty, "shell round-trips quoted value, got: " + result.Output);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Launch", "LinuxTerminals", "Linux terminal emulators get the right arguments",
                        executeAsync: _ =>
                        {
                            Dictionary<string, string[]> terminals = new Dictionary<string, string[]>();
                            foreach (KeyValuePair<string, string[]> terminal in LaunchHelper.LinuxTerminals) terminals.Add(terminal.Key, terminal.Value);
                            AssertTrue(String.Join(" ", LaunchHelper.LinuxTerminalArgs(terminals["gnome-terminal"], "/s.sh")) == "-- /s.sh", "gnome-terminal");
                            AssertTrue(String.Join(" ", LaunchHelper.LinuxTerminalArgs(terminals["xterm"], "/s.sh")) == "-e /s.sh", "xterm");
                            AssertTrue(String.Join(" ", LaunchHelper.LinuxTerminalArgs(terminals["kitty"], "/s.sh")) == "/s.sh", "kitty");
                            AssertTrue(String.Join(" ", LaunchHelper.LinuxTerminalArgs(terminals["wezterm"], "/s.sh")) == "start -- /s.sh", "wezterm");
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Launch", "FindOnPath", "Executables are found on a PATH list",
                        executeAsync: _ =>
                        {
                            string dir = Path.Combine(Path.GetTempPath(), "codehub-path-" + Guid.NewGuid().ToString("N"));
                            Directory.CreateDirectory(dir);
                            try
                            {
                                string tool = Path.Combine(dir, "sometool");
                                File.WriteAllText(tool, String.Empty);
                                string pathVar = "/definitely/missing" + Path.PathSeparator + dir;
                                AssertTrue(LaunchHelper.FindOnPath("sometool", pathVar) == tool, "found in second entry");
                                AssertTrue(LaunchHelper.FindOnPath("othertool", pathVar) == null, "missing tool is null");
                            }
                            finally
                            {
                                Directory.Delete(dir, true);
                            }
                            return Task.CompletedTask;
                        }),

                    new TestCaseDescriptor("Launch", "PosixScriptsRun", "POSIX launch scripts run the agent in the repo with the prompt intact",
                        executeAsync: _ =>
                        {
                            if (OperatingSystem.IsWindows()) return Task.CompletedTask; // POSIX-only path

                            string root = Path.Combine(Path.GetTempPath(), "codehub-launch-" + Guid.NewGuid().ToString("N"));
                            string bin = Path.Combine(root, "bin");
                            string output = Path.Combine(root, "out");
                            string repo = Path.Combine(root, "My Repo's \"Dir\"");
                            Directory.CreateDirectory(bin);
                            Directory.CreateDirectory(output);
                            Directory.CreateDirectory(repo);
                            try
                            {
                                // Fake agent: record the working directory and each argument.
                                WriteScript(Path.Combine(bin, "claude"),
                                    "#!/bin/sh\npwd > \"$OUT/pwd\"\necho $# > \"$OUT/argc\"\ni=0\nfor a in \"$@\"; do i=$((i+1)); printf '%s' \"$a\" > \"$OUT/arg$i\"; done\n");

                                // Fake login shell: record each invocation; run "-c" commands with sh.
                                string shell = Path.Combine(bin, "fakeshell");
                                WriteScript(shell,
                                    "#!/bin/sh\necho \"$*\" >> \"$OUT/shell\"\nif [ \"$3\" = \"-c\" ]; then exec /bin/sh -c \"$4\"; fi\n");

                                string prompt = "Review this repo.\n\nIt's \"quoted\", has $HOME, `id`, and $(id)\nlast line";
                                string promptFile = Path.Combine(root, "prompt.txt");
                                File.WriteAllText(promptFile, prompt);

                                string inner = Path.Combine(root, "inner.sh");
                                string launcher = Path.Combine(root, "launch.sh");
                                WriteScript(inner, LaunchHelper.PosixInnerScript(repo, LaunchHelper.PosixAgentCommand("claude", true, promptFile)));
                                WriteScript(launcher, LaunchHelper.PosixLauncherScript(inner));

                                Dictionary<string, string> env = new Dictionary<string, string>
                                {
                                    { "PATH", bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH") },
                                    { "SHELL", shell },
                                    { "OUT", output }
                                };
                                ProcessResult result = RunProcess(launcher, env);
                                AssertTrue(result.ExitCode == 0, "launcher exit code " + result.ExitCode + ": " + result.Error);

                                AssertTrue(File.ReadAllText(Path.Combine(output, "pwd")).Trim() == repo, "agent ran in the repository");
                                AssertTrue(File.ReadAllText(Path.Combine(output, "argc")).Trim() == "2", "agent got two arguments");
                                AssertTrue(File.ReadAllText(Path.Combine(output, "arg1")) == "--dangerously-skip-permissions", "dangerous flag first");
                                AssertTrue(File.ReadAllText(Path.Combine(output, "arg2")) == prompt, "prompt passed verbatim as one argument");

                                string[] shellCalls = File.ReadAllLines(Path.Combine(output, "shell"));
                                AssertTrue(shellCalls.Length == 2, "login shell invoked twice, got " + shellCalls.Length);
                                AssertTrue(shellCalls[0].StartsWith("-l -i -c "), "launcher runs the inner script from an interactive login shell");
                                AssertTrue(shellCalls[1] == "-l", "a login shell is left open afterward");
                            }
                            finally
                            {
                                Directory.Delete(root, true);
                            }
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

        private static void AssertThrows<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Assertion failed (expected " + typeof(T).Name + "): " + message);
        }

        private static void WriteScript(string file, string contents)
        {
            File.WriteAllText(file, contents);
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }

        private static ProcessResult RunProcess(string fileName, Dictionary<string, string> env, params string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            };
            foreach (string arg in args) info.ArgumentList.Add(arg);
            if (env != null)
            {
                foreach (KeyValuePair<string, string> pair in env) info.Environment[pair.Key] = pair.Value;
            }

            using (Process process = Process.Start(info))
            {
                process.StandardInput.Close();
                Task<string> stdout = process.StandardOutput.ReadToEndAsync();
                Task<string> stderr = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(30000))
                {
                    process.Kill(true);
                    throw new Exception(fileName + " timed out.");
                }
                return new ProcessResult { ExitCode = process.ExitCode, Output = stdout.Result, Error = stderr.Result };
            }
        }

        private class ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }

        private static void AssertTrue(bool condition, string message)
        {
            if (!condition) throw new Exception("Assertion failed: " + message);
        }

        #endregion
    }
}
