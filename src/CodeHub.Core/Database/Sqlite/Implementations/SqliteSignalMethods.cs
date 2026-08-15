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
    /// SQLite implementation of signal data access.
    /// </summary>
    public class SqliteSignalMethods : ISignalMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteSignalMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task ReplaceForRepositoryAsync(string repositoryId, List<Signal> signals, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            List<string> batch = new List<string>
            {
                "DELETE FROM signals WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";"
            };

            if (signals != null)
            {
                foreach (Signal s in signals)
                {
                    s.RepositoryId = repositoryId;
                    batch.Add(
                        "INSERT INTO signals (id, repoid, signaltype, status, detail, createdutc) VALUES (" +
                        Sanitizer.Quote(s.Id) + ", " +
                        Sanitizer.Quote(s.RepositoryId) + ", " +
                        Sanitizer.Quote(s.SignalType.ToString()) + ", " +
                        Sanitizer.Quote(s.Status.ToString()) + ", " +
                        Sanitizer.Quote(s.Detail) + ", " +
                        Sanitizer.Timestamp(s.CreatedUtc) + ");");
                }
            }

            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Signal>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM signals WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";", false, token).ConfigureAwait(false);
            return ToList(table);
        }

        /// <inheritdoc />
        public async Task<List<Signal>> EnumerateAllAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync("SELECT * FROM signals;", false, token).ConfigureAwait(false);
            return ToList(table);
        }

        #endregion

        #region Private-Methods

        private static List<Signal> ToList(DataTable table)
        {
            List<Signal> results = new List<Signal>();
            foreach (DataRow row in table.Rows)
            {
                results.Add(new Signal
                {
                    Id = row.GetString("id"),
                    RepositoryId = row.GetString("repoid"),
                    SignalType = row.GetEnum("signaltype", SignalTypeEnum.TestInfra),
                    Status = row.GetEnum("status", HealthStatusEnum.Unknown),
                    Detail = row.GetString("detail"),
                    CreatedUtc = row.GetDateTimeRequired("createdutc")
                });
            }
            return results;
        }

        #endregion
    }
}
