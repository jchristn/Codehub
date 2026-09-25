namespace CodeHub.Core.Database.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Sqlite.Implementations;
    using CodeHub.Core.Database.Sqlite.Queries;
    using CodeHub.Core.Enums;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// SQLite database driver with serialized writes.
    /// </summary>
    public class SqliteDatabaseDriver : DatabaseDriverBase
    {
        #region Public-Members

        /// <inheritdoc />
        public override DatabaseTypeEnum DatabaseType
        {
            get { return DatabaseTypeEnum.Sqlite; }
        }

        #endregion

        #region Private-Members

        private readonly SemaphoreSlim _Gate = new SemaphoreSlim(1, 1);
        private readonly string _ConnectionString;
        private readonly string _Filename;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the SQLite driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        public SqliteDatabaseDriver(DatabaseSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            _Filename = settings.Filename;
            _ConnectionString = settings.GetConnectionString();

            Repositories = new SqliteRepositoryMethods(this);
            Projects = new SqliteProjectMethods(this);
            Dependencies = new SqliteDependencyMethods(this);
            Signals = new SqliteSignalMethods(this);
            ScanRuns = new SqliteScanRunMethods(this);
            GitHubSnapshots = new SqliteGitHubSnapshotMethods(this);
            Selections = new SqliteScanSelectionMethods(this);
            RequestHistory = new SqliteRequestHistoryMethods(this);
            CustomActions = new SqliteCustomActionMethods(this);
            Branches = new SqliteBranchMethods(this);
            Annotations = new SqliteAnnotationMethods(this);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override async Task InitializeAsync(CancellationToken token = default)
        {
            if (!String.IsNullOrEmpty(_Filename))
            {
                string dir = Path.GetDirectoryName(Path.GetFullPath(_Filename));
                if (!String.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }

            // Use the default rollback journal (DELETE), never WAL, so the database is a single
            // flat .db file with no -wal/-shm sidecars — clean to mount into a container.
            await ExecuteQueryAsync("PRAGMA journal_mode=DELETE;", false, token).ConfigureAwait(false);

            await ExecuteQueriesAsync(TableQueries.All, token).ConfigureAwait(false);
            await MigrateAsync(token).ConfigureAwait(false);
        }

        /// <summary>
        /// Apply additive column migrations to existing databases. Each statement is idempotent:
        /// a "duplicate column" failure on an already-migrated database is expected and ignored.
        /// </summary>
        private async Task MigrateAsync(CancellationToken token)
        {
            List<string> migrations = new List<string>
            {
                "ALTER TABLE repositories ADD COLUMN currentbranch TEXT;",
                "ALTER TABLE repositories ADD COLUMN basebranch TEXT;",
                "ALTER TABLE repositories ADD COLUMN commitsahead INTEGER NOT NULL DEFAULT 0;",
                "ALTER TABLE repositories ADD COLUMN commitsbehind INTEGER NOT NULL DEFAULT 0;",
                "ALTER TABLE repositories ADD COLUMN commithash TEXT;",
                "ALTER TABLE github_snapshots ADD COLUMN isarchived INTEGER NOT NULL DEFAULT 0;",
                "ALTER TABLE repositories ADD COLUMN branchcount INTEGER NOT NULL DEFAULT 0;",
                "ALTER TABLE scan_runs ADD COLUMN targetrepository TEXT;"
            };

            foreach (string migration in migrations)
            {
                try
                {
                    await ExecuteQueryAsync(migration, false, token).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Column already exists on an already-migrated database.
                }
            }

            // Case-insensitive uniqueness for text keys. Older databases could hold case-variant
            // duplicates (e.g. C:\Code\X and c:\code\X); each rebuild collapses them.
            await RebuildWithNoCaseAsync("scan_selections", "path", TableQueries.ScanSelections,
                new List<string>(), "createdutc ASC, rowid ASC", token).ConfigureAwait(false);

            await RebuildWithNoCaseAsync("repositories", "path", TableQueries.Repositories,
                RepositoryDedupeQueries(), "createdutc ASC, rowid ASC", token).ConfigureAwait(false);

            // For overrides, the most recently created variant is the user's latest intent.
            await RebuildWithNoCaseAsync("annotations", "signalcolumn", TableQueries.Annotations,
                new List<string>(), "createdutc DESC, rowid DESC", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Rebuild a table so the given text column compares case-insensitively (COLLATE NOCASE),
        /// making its UNIQUE constraint and lookups case-insensitive. Runs once per table: skipped
        /// when the column is already NOCASE. Rows are copied in the given order and INSERT OR
        /// IGNORE keeps the first of any case-variant duplicates.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="column">Text column to make case-insensitive.</param>
        /// <param name="createSql">Current CREATE TABLE/INDEX statements for the table.</param>
        /// <param name="dedupeQueries">Statements run first to resolve duplicates (e.g. child rows).</param>
        /// <param name="orderBy">Copy order; the first row of each duplicate group wins.</param>
        /// <param name="token">Cancellation token.</param>
        private async Task RebuildWithNoCaseAsync(
            string table,
            string column,
            string createSql,
            List<string> dedupeQueries,
            string orderBy,
            CancellationToken token)
        {
            DataTable schema = await ExecuteQueryAsync(
                "SELECT sql FROM sqlite_master WHERE type='table' AND name=" + Sanitizer.Quote(table) + ";", false, token).ConfigureAwait(false);
            if (schema.Rows.Count == 0) return;

            string sql = schema.Rows[0]["sql"] as string ?? String.Empty;
            if (Regex.IsMatch(sql, @"(?im)^\s*" + column + @"\s+[^,\r\n]*COLLATE\s+NOCASE")) return;

            List<string> columns = new List<string>();
            DataTable info = await ExecuteQueryAsync(
                "SELECT name FROM pragma_table_info(" + Sanitizer.Quote(table) + ");", false, token).ConfigureAwait(false);
            foreach (DataRow row in info.Rows) columns.Add(row["name"].ToString());
            string columnList = String.Join(", ", columns);

            // Indexes follow a renamed table; drop them so the CREATE statements recreate them.
            DataTable indexes = await ExecuteQueryAsync(
                "SELECT name FROM sqlite_master WHERE type='index' AND sql IS NOT NULL AND tbl_name=" + Sanitizer.Quote(table) + ";", false, token).ConfigureAwait(false);

            string staging = table + "_migrate";
            List<string> queries = new List<string>(dedupeQueries)
            {
                "DROP TABLE IF EXISTS " + staging + ";",
                "ALTER TABLE " + table + " RENAME TO " + staging + ";"
            };
            foreach (DataRow row in indexes.Rows) queries.Add("DROP INDEX IF EXISTS " + row["name"] + ";");
            queries.Add(createSql);
            queries.Add("INSERT OR IGNORE INTO " + table + " (" + columnList + ") SELECT " + columnList + " FROM " + staging + " ORDER BY " + orderBy + ";");
            queries.Add("DROP TABLE " + staging + ";");

            await ExecuteQueriesAsync(queries, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Collapse case-variant duplicate repositories before the NOCASE rebuild. The oldest row
        /// is kept (it carries the user's overrides and history) and takes the newest row's path
        /// and name (the current on-disk casing); the newer rows and their child rows are removed,
        /// moving over any override the kept row does not already have.
        /// </summary>
        private static List<string> RepositoryDedupeQueries()
        {
            List<string> queries = new List<string>
            {
                "DROP TABLE IF EXISTS temp.repo_dupe_keep;",
                "DROP TABLE IF EXISTS temp.repo_dupe_drop;",
                "CREATE TEMP TABLE repo_dupe_keep AS " +
                    "SELECT lower(r.path) AS lp, " +
                    "(SELECT k.id FROM repositories k WHERE lower(k.path)=lower(r.path) ORDER BY k.createdutc ASC, k.rowid ASC LIMIT 1) AS keepid, " +
                    "(SELECT n.path FROM repositories n WHERE lower(n.path)=lower(r.path) ORDER BY n.createdutc DESC, n.rowid DESC LIMIT 1) AS newpath, " +
                    "(SELECT n.name FROM repositories n WHERE lower(n.path)=lower(r.path) ORDER BY n.createdutc DESC, n.rowid DESC LIMIT 1) AS newname " +
                    "FROM repositories r GROUP BY lower(r.path) HAVING COUNT(*) > 1;",
                "CREATE TEMP TABLE repo_dupe_drop AS " +
                    "SELECT r.id AS dropid, k.keepid AS keepid FROM repositories r JOIN repo_dupe_keep k ON lower(r.path)=k.lp WHERE r.id<>k.keepid;",
                "UPDATE OR IGNORE annotations SET repoid=(SELECT d.keepid FROM repo_dupe_drop d WHERE d.dropid=annotations.repoid) " +
                    "WHERE repoid IN (SELECT dropid FROM repo_dupe_drop);"
            };

            foreach (string child in new[] { "annotations", "repository_languages", "projects", "dependencies", "signals", "branches", "github_snapshots" })
                queries.Add("DELETE FROM " + child + " WHERE repoid IN (SELECT dropid FROM repo_dupe_drop);");

            queries.Add("DELETE FROM repositories WHERE id IN (SELECT dropid FROM repo_dupe_drop);");
            queries.Add(
                "UPDATE repositories SET " +
                "path=(SELECT k.newpath FROM repo_dupe_keep k WHERE k.keepid=repositories.id), " +
                "name=(SELECT k.newname FROM repo_dupe_keep k WHERE k.keepid=repositories.id) " +
                "WHERE id IN (SELECT keepid FROM repo_dupe_keep);");
            queries.Add("DROP TABLE temp.repo_dupe_keep;");
            queries.Add("DROP TABLE temp.repo_dupe_drop;");
            return queries;
        }

        /// <inheritdoc />
        public override async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            // Match SQLite's own (BINARY) uniqueness semantics. DataTable enforces the schema's keys
            // case-insensitively by default, so data SQLite accepts (e.g. "C:\X" and "c:\x" in a
            // case-sensitive UNIQUE column) would otherwise make every read of the table throw.
            DataTable result = new DataTable { CaseSensitive = true };

            await _Gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    await conn.OpenAsync(token).ConfigureAwait(false);
                    using (SqliteCommand cmd = new SqliteCommand(query, conn))
                    using (SqliteDataReader reader = (SqliteDataReader)await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                    {
                        result.Load(reader);
                    }
                }
            }
            finally
            {
                _Gate.Release();
            }

            return result;
        }

        /// <inheritdoc />
        public override async Task ExecuteQueriesAsync(IEnumerable<string> queries, CancellationToken token = default)
        {
            if (queries == null) throw new ArgumentNullException(nameof(queries));

            await _Gate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    await conn.OpenAsync(token).ConfigureAwait(false);
                    using (SqliteTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            foreach (string query in queries)
                            {
                                if (String.IsNullOrEmpty(query)) continue;
                                using (SqliteCommand cmd = new SqliteCommand(query, conn, transaction))
                                {
                                    await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                                }
                            }
                            await transaction.CommitAsync(token).ConfigureAwait(false);
                        }
                        catch
                        {
                            await transaction.RollbackAsync(token).ConfigureAwait(false);
                            throw;
                        }
                    }
                }
            }
            finally
            {
                _Gate.Release();
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            _Gate.Dispose();
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
