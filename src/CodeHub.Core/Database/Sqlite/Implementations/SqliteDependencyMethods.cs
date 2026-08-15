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
    /// SQLite implementation of dependency data access.
    /// </summary>
    public class SqliteDependencyMethods : IDependencyMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteDependencyMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task ReplaceForRepositoryAsync(string repositoryId, List<Dependency> dependencies, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            List<string> batch = new List<string>
            {
                "DELETE FROM dependencies WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";"
            };

            if (dependencies != null)
            {
                foreach (Dependency d in dependencies)
                {
                    d.RepositoryId = repositoryId;
                    batch.Add(
                        "INSERT INTO dependencies (id, projectid, repoid, ecosystem, packagename, currentversion, latestversion, drift, isvulnerable, severity, advisoryurl, createdutc) VALUES (" +
                        Sanitizer.Quote(d.Id) + ", " +
                        Sanitizer.Quote(d.ProjectId) + ", " +
                        Sanitizer.Quote(d.RepositoryId) + ", " +
                        Sanitizer.Quote(d.Ecosystem) + ", " +
                        Sanitizer.Quote(d.PackageName) + ", " +
                        Sanitizer.Quote(d.CurrentVersion) + ", " +
                        Sanitizer.Quote(d.LatestVersion) + ", " +
                        Sanitizer.Quote(d.Drift.ToString()) + ", " +
                        Sanitizer.Bool(d.IsVulnerable) + ", " +
                        Sanitizer.Quote(d.Severity.ToString()) + ", " +
                        Sanitizer.Quote(d.AdvisoryUrl) + ", " +
                        Sanitizer.Timestamp(d.CreatedUtc) + ");");
                }
            }

            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Dependency>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM dependencies WHERE repoid=" + Sanitizer.Quote(repositoryId) + " ORDER BY packagename COLLATE NOCASE ASC;",
                false, token).ConfigureAwait(false);
            List<Dependency> results = new List<Dependency>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        #endregion

        #region Private-Methods

        private static Dependency FromRow(DataRow row)
        {
            return new Dependency
            {
                Id = row.GetString("id"),
                ProjectId = row.GetString("projectid"),
                RepositoryId = row.GetString("repoid"),
                Ecosystem = row.GetString("ecosystem"),
                PackageName = row.GetString("packagename"),
                CurrentVersion = row.GetString("currentversion"),
                LatestVersion = row.GetString("latestversion"),
                Drift = row.GetEnum("drift", DriftLevelEnum.None),
                IsVulnerable = row.GetBool("isvulnerable"),
                Severity = row.GetEnum("severity", VulnerabilitySeverityEnum.None),
                AdvisoryUrl = row.GetString("advisoryurl"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
