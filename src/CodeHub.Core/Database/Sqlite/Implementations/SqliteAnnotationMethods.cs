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
    /// SQLite implementation of annotation (value override) data access.
    /// </summary>
    public class SqliteAnnotationMethods : IAnnotationMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteAnnotationMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<List<Annotation>> EnumerateAllAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync("SELECT * FROM annotations;", false, token).ConfigureAwait(false);
            List<Annotation> results = new List<Annotation>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        /// <inheritdoc />
        public async Task<List<Annotation>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM annotations WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";", false, token).ConfigureAwait(false);
            List<Annotation> results = new List<Annotation>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        /// <inheritdoc />
        public async Task<Annotation> UpsertAsync(Annotation annotation, CancellationToken token = default)
        {
            if (annotation == null) throw new ArgumentNullException(nameof(annotation));

            DataTable existingTable = await _Db.ExecuteQueryAsync(
                "SELECT id FROM annotations WHERE repoid=" + Sanitizer.Quote(annotation.RepositoryId) +
                " AND signalcolumn=" + Sanitizer.Quote(annotation.Column) + ";", false, token).ConfigureAwait(false);

            string sql;
            if (existingTable.Rows.Count > 0)
            {
                sql =
                    "UPDATE annotations SET " +
                    "status=" + Sanitizer.Quote(annotation.Status) + ", " +
                    "note=" + Sanitizer.Quote(annotation.Note) + " " +
                    "WHERE repoid=" + Sanitizer.Quote(annotation.RepositoryId) +
                    " AND signalcolumn=" + Sanitizer.Quote(annotation.Column) + ";";
            }
            else
            {
                sql =
                    "INSERT INTO annotations (id, repoid, signalcolumn, status, note, createdutc) VALUES (" +
                    Sanitizer.Quote(annotation.Id) + ", " +
                    Sanitizer.Quote(annotation.RepositoryId) + ", " +
                    Sanitizer.Quote(annotation.Column) + ", " +
                    Sanitizer.Quote(annotation.Status) + ", " +
                    Sanitizer.Quote(annotation.Note) + ", " +
                    Sanitizer.Timestamp(annotation.CreatedUtc) + ");";
            }

            await _Db.ExecuteQueryAsync(sql, false, token).ConfigureAwait(false);
            return annotation;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string repositoryId, string column, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            if (String.IsNullOrEmpty(column)) throw new ArgumentNullException(nameof(column));
            await _Db.ExecuteQueryAsync(
                "DELETE FROM annotations WHERE repoid=" + Sanitizer.Quote(repositoryId) +
                " AND signalcolumn=" + Sanitizer.Quote(column) + ";", false, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static Annotation FromRow(DataRow row)
        {
            return new Annotation
            {
                Id = row.GetString("id"),
                RepositoryId = row.GetString("repoid"),
                Column = row.GetString("signalcolumn"),
                Status = row.GetString("status"),
                Note = row.GetString("note"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
