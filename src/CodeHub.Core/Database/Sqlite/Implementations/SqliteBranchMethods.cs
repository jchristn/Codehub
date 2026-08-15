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
    /// SQLite implementation of branch data access.
    /// </summary>
    public class SqliteBranchMethods : IBranchMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteBranchMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task ReplaceForRepositoryAsync(string repositoryId, List<Branch> branches, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            List<string> batch = new List<string>
            {
                "DELETE FROM branches WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";"
            };

            if (branches != null)
            {
                foreach (Branch b in branches)
                {
                    b.RepositoryId = repositoryId;
                    batch.Add(
                        "INSERT INTO branches (id, repoid, name, iscurrent, ahead, behind, createdutc) VALUES (" +
                        Sanitizer.Quote(b.Id) + ", " +
                        Sanitizer.Quote(b.RepositoryId) + ", " +
                        Sanitizer.Quote(b.Name) + ", " +
                        Sanitizer.Bool(b.IsCurrent) + ", " +
                        b.Ahead + ", " +
                        b.Behind + ", " +
                        Sanitizer.Timestamp(b.CreatedUtc) + ");");
                }
            }

            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Branch>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM branches WHERE repoid=" + Sanitizer.Quote(repositoryId) + " ORDER BY iscurrent DESC, name COLLATE NOCASE ASC;",
                false, token).ConfigureAwait(false);
            List<Branch> results = new List<Branch>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        #endregion

        #region Private-Methods

        private static Branch FromRow(DataRow row)
        {
            return new Branch
            {
                Id = row.GetString("id"),
                RepositoryId = row.GetString("repoid"),
                Name = row.GetString("name"),
                IsCurrent = row.GetBool("iscurrent"),
                Ahead = row.GetInt("ahead"),
                Behind = row.GetInt("behind"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
