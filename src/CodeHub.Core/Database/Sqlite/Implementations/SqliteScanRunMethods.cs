namespace CodeHub.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Interfaces;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;

    /// <summary>
    /// SQLite implementation of scan run data access.
    /// </summary>
    public class SqliteScanRunMethods : IScanRunMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteScanRunMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ScanRun> CreateAsync(ScanRun run, CancellationToken token = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            await _Db.ExecuteQueryAsync(
                "INSERT INTO scan_runs (id, trigger, status, reposscanned, repostotal, error, startedutc, completedutc) VALUES (" +
                Sanitizer.Quote(run.Id) + ", " +
                Sanitizer.Quote(run.Trigger.ToString()) + ", " +
                Sanitizer.Quote(run.Status.ToString()) + ", " +
                run.ReposScanned + ", " +
                run.ReposTotal + ", " +
                Sanitizer.Quote(run.Error) + ", " +
                Sanitizer.Timestamp(run.StartedUtc) + ", " +
                Sanitizer.Timestamp(run.CompletedUtc) + ");",
                false, token).ConfigureAwait(false);
            return run;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(ScanRun run, CancellationToken token = default)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            await _Db.ExecuteQueryAsync(
                "UPDATE scan_runs SET " +
                "status=" + Sanitizer.Quote(run.Status.ToString()) + ", " +
                "reposscanned=" + run.ReposScanned + ", " +
                "repostotal=" + run.ReposTotal + ", " +
                "error=" + Sanitizer.Quote(run.Error) + ", " +
                "completedutc=" + Sanitizer.Timestamp(run.CompletedUtc) + " " +
                "WHERE id=" + Sanitizer.Quote(run.Id) + ";",
                false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ScanRun> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM scan_runs WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<ScanRun> ReadLatestAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM scan_runs ORDER BY startedutc DESC LIMIT 1;", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<ScanRun>> EnumerateAsync(int limit, CancellationToken token = default)
        {
            if (limit <= 0) limit = 50;
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM scan_runs ORDER BY startedutc DESC LIMIT " + limit + ";", false, token).ConfigureAwait(false);
            List<ScanRun> results = new List<ScanRun>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        #endregion

        #region Private-Methods

        private static ScanRun FromRow(DataRow row)
        {
            return new ScanRun
            {
                Id = row.GetString("id"),
                Trigger = row.GetEnum("trigger", ScanTriggerEnum.Manual),
                Status = row.GetEnum("status", ScanStatusEnum.Running),
                ReposScanned = row.GetInt("reposscanned"),
                ReposTotal = row.GetInt("repostotal"),
                Error = row.GetString("error"),
                StartedUtc = row.GetDateTimeRequired("startedutc"),
                CompletedUtc = row.GetDateTime("completedutc")
            };
        }

        #endregion
    }
}
