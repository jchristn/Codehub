namespace CodeHub.Core.Database.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Interfaces;
    using CodeHub.Core.Models;

    /// <summary>
    /// SQLite implementation of scan-selection data access.
    /// </summary>
    public class SqliteScanSelectionMethods : IScanSelectionMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteScanSelectionMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ScanSelection> UpsertAsync(ScanSelection selection, CancellationToken token = default)
        {
            if (selection == null) throw new ArgumentNullException(nameof(selection));

            ScanSelection existing = await ReadByPathAsync(selection.Path, token).ConfigureAwait(false);
            if (existing != null) selection.Id = existing.Id;

            string sql;
            if (existing != null)
            {
                sql =
                    "UPDATE scan_selections SET included=" + Sanitizer.Bool(selection.Included) +
                    " WHERE path=" + Sanitizer.Quote(selection.Path) + ";";
            }
            else
            {
                sql =
                    "INSERT INTO scan_selections (id, path, included, createdutc) VALUES (" +
                    Sanitizer.Quote(selection.Id) + ", " +
                    Sanitizer.Quote(selection.Path) + ", " +
                    Sanitizer.Bool(selection.Included) + ", " +
                    Sanitizer.Timestamp(selection.CreatedUtc) + ");";
            }

            await _Db.ExecuteQueryAsync(sql, false, token).ConfigureAwait(false);
            return selection;
        }

        /// <inheritdoc />
        public async Task<ScanSelection> ReadByPathAsync(string path, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM scan_selections WHERE path=" + Sanitizer.Quote(path) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<ScanSelection>> EnumerateAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM scan_selections ORDER BY path COLLATE NOCASE ASC;", false, token).ConfigureAwait(false);
            List<ScanSelection> results = new List<ScanSelection>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        /// <inheritdoc />
        public async Task DeleteByPathAsync(string path, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            await _Db.ExecuteQueryAsync(
                "DELETE FROM scan_selections WHERE path=" + Sanitizer.Quote(path) + ";", false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> CountAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT COUNT(*) AS cnt FROM scan_selections;", false, token).ConfigureAwait(false);
            return table.Rows.Count > 0 ? table.Rows[0].GetInt("cnt") : 0;
        }

        #endregion

        #region Private-Methods

        private static ScanSelection FromRow(DataRow row)
        {
            return new ScanSelection
            {
                Id = row.GetString("id"),
                Path = row.GetString("path"),
                Included = row.GetBool("included", true),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
