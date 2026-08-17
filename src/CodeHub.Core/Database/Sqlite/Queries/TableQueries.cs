namespace CodeHub.Core.Database.Sqlite.Queries
{
    using System.Collections.Generic;

    /// <summary>
    /// SQLite schema creation statements.
    /// </summary>
    public static class TableQueries
    {
        #region Public-Members

        /// <summary>
        /// All table creation statements, in dependency order.
        /// </summary>
        public static List<string> All
        {
            get
            {
                return new List<string>
                {
                    Repositories,
                    RepositoryLanguages,
                    Projects,
                    Dependencies,
                    Signals,
                    ScanRuns,
                    GitHubSnapshots,
                    ScanSelections,
                    RequestHistory,
                    CustomActions,
                    Branches,
                    Annotations
                };
            }
        }

        /// <summary>
        /// Repositories table.
        /// </summary>
        public static readonly string Repositories = @"
CREATE TABLE IF NOT EXISTS repositories (
    id TEXT PRIMARY KEY,
    path TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    visibility TEXT NOT NULL DEFAULT 'Unknown',
    primarylanguage TEXT NOT NULL DEFAULT 'Unknown',
    currentversion TEXT,
    isgitrepository INTEGER NOT NULL DEFAULT 0,
    remoteurl TEXT,
    currentbranch TEXT,
    basebranch TEXT,
    commitsahead INTEGER NOT NULL DEFAULT 0,
    commitsbehind INTEGER NOT NULL DEFAULT 0,
    commithash TEXT,
    lastupdateutc TEXT,
    projectcount INTEGER NOT NULL DEFAULT 0,
    branchcount INTEGER NOT NULL DEFAULT 0,
    overallhealth TEXT NOT NULL DEFAULT 'Unknown',
    isincluded INTEGER NOT NULL DEFAULT 1,
    lastscannedutc TEXT,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_repositories_name ON repositories(name);
CREATE INDEX IF NOT EXISTS idx_repositories_overallhealth ON repositories(overallhealth);
";

        /// <summary>
        /// Repository languages child table.
        /// </summary>
        public static readonly string RepositoryLanguages = @"
CREATE TABLE IF NOT EXISTS repository_languages (
    repoid TEXT NOT NULL,
    language TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_repository_languages_repoid ON repository_languages(repoid);
";

        /// <summary>
        /// Projects table.
        /// </summary>
        public static readonly string Projects = @"
CREATE TABLE IF NOT EXISTS projects (
    id TEXT PRIMARY KEY,
    repoid TEXT NOT NULL,
    path TEXT NOT NULL,
    relativepath TEXT,
    name TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'Unknown',
    version TEXT,
    targetframework TEXT,
    iswebservice INTEGER NOT NULL DEFAULT 0,
    istestproject INTEGER NOT NULL DEFAULT 0,
    hastouchstone INTEGER NOT NULL DEFAULT 0,
    haswatson7 INTEGER NOT NULL DEFAULT 0,
    hasradiant INTEGER NOT NULL DEFAULT 0,
    outdatedcount INTEGER NOT NULL DEFAULT 0,
    vulnerablecount INTEGER NOT NULL DEFAULT 0,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_projects_repoid ON projects(repoid);
";

        /// <summary>
        /// Dependencies table.
        /// </summary>
        public static readonly string Dependencies = @"
CREATE TABLE IF NOT EXISTS dependencies (
    id TEXT PRIMARY KEY,
    projectid TEXT NOT NULL,
    repoid TEXT NOT NULL,
    ecosystem TEXT NOT NULL DEFAULT 'nuget',
    packagename TEXT NOT NULL,
    currentversion TEXT,
    latestversion TEXT,
    drift TEXT NOT NULL DEFAULT 'None',
    isvulnerable INTEGER NOT NULL DEFAULT 0,
    severity TEXT NOT NULL DEFAULT 'None',
    advisoryurl TEXT,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_dependencies_repoid ON dependencies(repoid);
CREATE INDEX IF NOT EXISTS idx_dependencies_projectid ON dependencies(projectid);
";

        /// <summary>
        /// Signals table.
        /// </summary>
        public static readonly string Signals = @"
CREATE TABLE IF NOT EXISTS signals (
    id TEXT PRIMARY KEY,
    repoid TEXT NOT NULL,
    signaltype TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Unknown',
    detail TEXT,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_signals_repoid ON signals(repoid);
";

        /// <summary>
        /// Scan runs table.
        /// </summary>
        public static readonly string ScanRuns = @"
CREATE TABLE IF NOT EXISTS scan_runs (
    id TEXT PRIMARY KEY,
    trigger TEXT NOT NULL DEFAULT 'Manual',
    status TEXT NOT NULL DEFAULT 'Running',
    reposscanned INTEGER NOT NULL DEFAULT 0,
    repostotal INTEGER NOT NULL DEFAULT 0,
    targetrepository TEXT,
    error TEXT,
    startedutc TEXT NOT NULL,
    completedutc TEXT
);
CREATE INDEX IF NOT EXISTS idx_scan_runs_startedutc ON scan_runs(startedutc);
";

        /// <summary>
        /// GitHub snapshots table.
        /// </summary>
        public static readonly string GitHubSnapshots = @"
CREATE TABLE IF NOT EXISTS github_snapshots (
    id TEXT PRIMARY KEY,
    repoid TEXT NOT NULL UNIQUE,
    owner TEXT,
    repo TEXT,
    isprivate INTEGER NOT NULL DEFAULT 0,
    isarchived INTEGER NOT NULL DEFAULT 0,
    openissues INTEGER NOT NULL DEFAULT 0,
    openpullrequests INTEGER NOT NULL DEFAULT 0,
    dependabotopen INTEGER NOT NULL DEFAULT 0,
    dependabothigh INTEGER NOT NULL DEFAULT 0,
    dependabotcritical INTEGER NOT NULL DEFAULT 0,
    error TEXT,
    fetchedutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_github_snapshots_repoid ON github_snapshots(repoid);
";

        /// <summary>
        /// Scan selections table (the include/exclude directory set, source of truth for scans).
        /// </summary>
        public static readonly string ScanSelections = @"
CREATE TABLE IF NOT EXISTS scan_selections (
    id TEXT PRIMARY KEY,
    path TEXT NOT NULL UNIQUE,
    included INTEGER NOT NULL DEFAULT 1,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_scan_selections_included ON scan_selections(included);
";

        /// <summary>
        /// Custom actions table (user-defined agent launchers shown in the actions menu).
        /// </summary>
        public static readonly string CustomActions = @"
CREATE TABLE IF NOT EXISTS custom_actions (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    agent TEXT NOT NULL,
    dangerous INTEGER NOT NULL DEFAULT 0,
    prompt TEXT,
    createdutc TEXT NOT NULL
);
";

        /// <summary>
        /// Branches table (local branches and their divergence from the base branch, per scan).
        /// </summary>
        public static readonly string Branches = @"
CREATE TABLE IF NOT EXISTS branches (
    id TEXT PRIMARY KEY,
    repoid TEXT NOT NULL,
    name TEXT NOT NULL,
    iscurrent INTEGER NOT NULL DEFAULT 0,
    ahead INTEGER NOT NULL DEFAULT 0,
    behind INTEGER NOT NULL DEFAULT 0,
    createdutc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_branches_repoid ON branches(repoid);
";

        /// <summary>
        /// Annotations table (manual per-repository value overrides with a note).
        /// </summary>
        public static readonly string Annotations = @"
CREATE TABLE IF NOT EXISTS annotations (
    id TEXT PRIMARY KEY,
    repoid TEXT NOT NULL,
    signalcolumn TEXT NOT NULL,
    status TEXT NOT NULL,
    note TEXT,
    createdutc TEXT NOT NULL,
    UNIQUE(repoid, signalcolumn)
);
CREATE INDEX IF NOT EXISTS idx_annotations_repoid ON annotations(repoid);
";

        /// <summary>
        /// Request history table.
        /// </summary>
        public static readonly string RequestHistory = @"
CREATE TABLE IF NOT EXISTS request_history (
    id TEXT PRIMARY KEY,
    method TEXT NOT NULL,
    path TEXT NOT NULL,
    url TEXT NOT NULL,
    statuscode INTEGER NOT NULL DEFAULT 0,
    durationms REAL NOT NULL DEFAULT 0,
    sourceip TEXT,
    requestheaders TEXT,
    requestbody TEXT,
    requestbodybytes INTEGER NOT NULL DEFAULT 0,
    requestbodytruncated INTEGER NOT NULL DEFAULT 0,
    responseheaders TEXT,
    responsebody TEXT,
    responsebodybytes INTEGER NOT NULL DEFAULT 0,
    responsebodytruncated INTEGER NOT NULL DEFAULT 0,
    createdutc TEXT NOT NULL,
    completedutc TEXT
);
CREATE INDEX IF NOT EXISTS idx_request_history_createdutc ON request_history(createdutc);
";

        #endregion
    }
}
