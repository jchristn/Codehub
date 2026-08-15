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
    /// SQLite implementation of GitHub snapshot data access.
    /// </summary>
    public class SqliteGitHubSnapshotMethods : IGitHubSnapshotMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteGitHubSnapshotMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<GitHubSnapshot> UpsertAsync(GitHubSnapshot snapshot, CancellationToken token = default)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            GitHubSnapshot existing = await ReadByRepositoryAsync(snapshot.RepositoryId, token).ConfigureAwait(false);
            if (existing != null) snapshot.Id = existing.Id;

            string sql;
            if (existing != null)
            {
                sql =
                    "UPDATE github_snapshots SET " +
                    "owner=" + Sanitizer.Quote(snapshot.Owner) + ", " +
                    "repo=" + Sanitizer.Quote(snapshot.Repo) + ", " +
                    "isprivate=" + Sanitizer.Bool(snapshot.IsPrivate) + ", " +
                    "isarchived=" + Sanitizer.Bool(snapshot.IsArchived) + ", " +
                    "openissues=" + snapshot.OpenIssues + ", " +
                    "openpullrequests=" + snapshot.OpenPullRequests + ", " +
                    "dependabotopen=" + snapshot.DependabotOpen + ", " +
                    "dependabothigh=" + snapshot.DependabotHigh + ", " +
                    "dependabotcritical=" + snapshot.DependabotCritical + ", " +
                    "error=" + Sanitizer.Quote(snapshot.Error) + ", " +
                    "fetchedutc=" + Sanitizer.Timestamp(snapshot.FetchedUtc) + " " +
                    "WHERE repoid=" + Sanitizer.Quote(snapshot.RepositoryId) + ";";
            }
            else
            {
                sql =
                    "INSERT INTO github_snapshots (id, repoid, owner, repo, isprivate, isarchived, openissues, openpullrequests, dependabotopen, dependabothigh, dependabotcritical, error, fetchedutc) VALUES (" +
                    Sanitizer.Quote(snapshot.Id) + ", " +
                    Sanitizer.Quote(snapshot.RepositoryId) + ", " +
                    Sanitizer.Quote(snapshot.Owner) + ", " +
                    Sanitizer.Quote(snapshot.Repo) + ", " +
                    Sanitizer.Bool(snapshot.IsPrivate) + ", " +
                    Sanitizer.Bool(snapshot.IsArchived) + ", " +
                    snapshot.OpenIssues + ", " +
                    snapshot.OpenPullRequests + ", " +
                    snapshot.DependabotOpen + ", " +
                    snapshot.DependabotHigh + ", " +
                    snapshot.DependabotCritical + ", " +
                    Sanitizer.Quote(snapshot.Error) + ", " +
                    Sanitizer.Timestamp(snapshot.FetchedUtc) + ");";
            }

            await _Db.ExecuteQueryAsync(sql, false, token).ConfigureAwait(false);
            return snapshot;
        }

        /// <inheritdoc />
        public async Task<GitHubSnapshot> ReadByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM github_snapshots WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<GitHubSnapshot>> EnumerateAllAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM github_snapshots;", false, token).ConfigureAwait(false);
            List<GitHubSnapshot> results = new List<GitHubSnapshot>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        #endregion

        #region Private-Methods

        private static GitHubSnapshot FromRow(DataRow row)
        {
            return new GitHubSnapshot
            {
                Id = row.GetString("id"),
                RepositoryId = row.GetString("repoid"),
                Owner = row.GetString("owner"),
                Repo = row.GetString("repo"),
                IsPrivate = row.GetBool("isprivate"),
                IsArchived = row.GetBool("isarchived"),
                OpenIssues = row.GetInt("openissues"),
                OpenPullRequests = row.GetInt("openpullrequests"),
                DependabotOpen = row.GetInt("dependabotopen"),
                DependabotHigh = row.GetInt("dependabothigh"),
                DependabotCritical = row.GetInt("dependabotcritical"),
                Error = row.GetString("error"),
                FetchedUtc = row.GetDateTimeRequired("fetchedutc")
            };
        }

        #endregion
    }
}
