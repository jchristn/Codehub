namespace CodeHub.Core.Database.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
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
                "ALTER TABLE repositories ADD COLUMN branchcount INTEGER NOT NULL DEFAULT 0;"
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
        }

        /// <inheritdoc />
        public override async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(nameof(query));

            DataTable result = new DataTable();

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
