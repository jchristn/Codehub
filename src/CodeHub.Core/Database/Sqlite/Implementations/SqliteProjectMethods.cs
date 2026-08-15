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
    /// SQLite implementation of project data access.
    /// </summary>
    public class SqliteProjectMethods : IProjectMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteProjectMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task ReplaceForRepositoryAsync(string repositoryId, List<Project> projects, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));

            List<string> batch = new List<string>
            {
                "DELETE FROM projects WHERE repoid=" + Sanitizer.Quote(repositoryId) + ";"
            };

            if (projects != null)
            {
                foreach (Project p in projects)
                {
                    p.RepositoryId = repositoryId;
                    batch.Add(
                        "INSERT INTO projects (id, repoid, path, relativepath, name, type, version, targetframework, iswebservice, istestproject, hastouchstone, haswatson7, hasradiant, outdatedcount, vulnerablecount, createdutc) VALUES (" +
                        Sanitizer.Quote(p.Id) + ", " +
                        Sanitizer.Quote(p.RepositoryId) + ", " +
                        Sanitizer.Quote(p.Path) + ", " +
                        Sanitizer.Quote(p.RelativePath) + ", " +
                        Sanitizer.Quote(p.Name) + ", " +
                        Sanitizer.Quote(p.Type.ToString()) + ", " +
                        Sanitizer.Quote(p.Version) + ", " +
                        Sanitizer.Quote(p.TargetFramework) + ", " +
                        Sanitizer.Bool(p.IsWebService) + ", " +
                        Sanitizer.Bool(p.IsTestProject) + ", " +
                        Sanitizer.Bool(p.HasTouchstone) + ", " +
                        Sanitizer.Bool(p.HasWatson7) + ", " +
                        Sanitizer.Bool(p.HasRadiant) + ", " +
                        p.OutdatedCount + ", " +
                        p.VulnerableCount + ", " +
                        Sanitizer.Timestamp(p.CreatedUtc) + ");");
                }
            }

            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Project>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(repositoryId)) throw new ArgumentNullException(nameof(repositoryId));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM projects WHERE repoid=" + Sanitizer.Quote(repositoryId) + " ORDER BY relativepath COLLATE NOCASE ASC;",
                false, token).ConfigureAwait(false);
            List<Project> results = new List<Project>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        /// <inheritdoc />
        public async Task<Project> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM projects WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        #endregion

        #region Private-Methods

        private static Project FromRow(DataRow row)
        {
            return new Project
            {
                Id = row.GetString("id"),
                RepositoryId = row.GetString("repoid"),
                Path = row.GetString("path"),
                RelativePath = row.GetString("relativepath"),
                Name = row.GetString("name"),
                Type = row.GetEnum("type", ProjectTypeEnum.Unknown),
                Version = row.GetString("version"),
                TargetFramework = row.GetString("targetframework"),
                IsWebService = row.GetBool("iswebservice"),
                IsTestProject = row.GetBool("istestproject"),
                HasTouchstone = row.GetBool("hastouchstone"),
                HasWatson7 = row.GetBool("haswatson7"),
                HasRadiant = row.GetBool("hasradiant"),
                OutdatedCount = row.GetInt("outdatedcount"),
                VulnerableCount = row.GetInt("vulnerablecount"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
