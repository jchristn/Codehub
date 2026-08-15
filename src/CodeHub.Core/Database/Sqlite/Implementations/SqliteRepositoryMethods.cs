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
    /// SQLite implementation of repository data access.
    /// </summary>
    public class SqliteRepositoryMethods : IRepositoryMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteRepositoryMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Repository> UpsertAsync(Repository repository, CancellationToken token = default)
        {
            if (repository == null) throw new ArgumentNullException(nameof(repository));

            Repository existing = await ReadByPathAsync(repository.Path, token).ConfigureAwait(false);
            if (existing != null)
            {
                repository.Id = existing.Id;
                repository.CreatedUtc = existing.CreatedUtc;
            }

            List<string> batch = new List<string>();

            if (existing != null)
            {
                batch.Add(
                    "UPDATE repositories SET " +
                    "name=" + Sanitizer.Quote(repository.Name) + ", " +
                    "visibility=" + Sanitizer.Quote(repository.Visibility.ToString()) + ", " +
                    "primarylanguage=" + Sanitizer.Quote(repository.PrimaryLanguage.ToString()) + ", " +
                    "currentversion=" + Sanitizer.Quote(repository.CurrentVersion) + ", " +
                    "isgitrepository=" + Sanitizer.Bool(repository.IsGitRepository) + ", " +
                    "remoteurl=" + Sanitizer.Quote(repository.RemoteUrl) + ", " +
                    "currentbranch=" + Sanitizer.Quote(repository.CurrentBranch) + ", " +
                    "basebranch=" + Sanitizer.Quote(repository.BaseBranch) + ", " +
                    "commitsahead=" + repository.CommitsAhead + ", " +
                    "commitsbehind=" + repository.CommitsBehind + ", " +
                    "commithash=" + Sanitizer.Quote(repository.LastCommitHash) + ", " +
                    "lastupdateutc=" + Sanitizer.Timestamp(repository.LastUpdateUtc) + ", " +
                    "projectcount=" + repository.ProjectCount + ", " +
                    "branchcount=" + repository.BranchCount + ", " +
                    "overallhealth=" + Sanitizer.Quote(repository.OverallHealth.ToString()) + ", " +
                    "isincluded=" + Sanitizer.Bool(repository.IsIncluded) + ", " +
                    "lastscannedutc=" + Sanitizer.Timestamp(repository.LastScannedUtc) + " " +
                    "WHERE id=" + Sanitizer.Quote(repository.Id) + ";");
            }
            else
            {
                batch.Add(
                    "INSERT INTO repositories " +
                    "(id, path, name, visibility, primarylanguage, currentversion, isgitrepository, remoteurl, currentbranch, basebranch, commitsahead, commitsbehind, commithash, lastupdateutc, projectcount, branchcount, overallhealth, isincluded, lastscannedutc, createdutc) VALUES (" +
                    Sanitizer.Quote(repository.Id) + ", " +
                    Sanitizer.Quote(repository.Path) + ", " +
                    Sanitizer.Quote(repository.Name) + ", " +
                    Sanitizer.Quote(repository.Visibility.ToString()) + ", " +
                    Sanitizer.Quote(repository.PrimaryLanguage.ToString()) + ", " +
                    Sanitizer.Quote(repository.CurrentVersion) + ", " +
                    Sanitizer.Bool(repository.IsGitRepository) + ", " +
                    Sanitizer.Quote(repository.RemoteUrl) + ", " +
                    Sanitizer.Quote(repository.CurrentBranch) + ", " +
                    Sanitizer.Quote(repository.BaseBranch) + ", " +
                    repository.CommitsAhead + ", " +
                    repository.CommitsBehind + ", " +
                    Sanitizer.Quote(repository.LastCommitHash) + ", " +
                    Sanitizer.Timestamp(repository.LastUpdateUtc) + ", " +
                    repository.ProjectCount + ", " +
                    repository.BranchCount + ", " +
                    Sanitizer.Quote(repository.OverallHealth.ToString()) + ", " +
                    Sanitizer.Bool(repository.IsIncluded) + ", " +
                    Sanitizer.Timestamp(repository.LastScannedUtc) + ", " +
                    Sanitizer.Timestamp(repository.CreatedUtc) + ");");
            }

            batch.Add("DELETE FROM repository_languages WHERE repoid=" + Sanitizer.Quote(repository.Id) + ";");
            if (repository.Languages != null)
            {
                foreach (string language in repository.Languages)
                {
                    if (String.IsNullOrEmpty(language)) continue;
                    batch.Add("INSERT INTO repository_languages (repoid, language) VALUES (" +
                        Sanitizer.Quote(repository.Id) + ", " + Sanitizer.Quote(language) + ");");
                }
            }

            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
            return repository;
        }

        /// <inheritdoc />
        public async Task<Repository> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM repositories WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            Repository repo = FromRow(table.Rows[0]);
            await LoadLanguagesAsync(repo, token).ConfigureAwait(false);
            return repo;
        }

        /// <inheritdoc />
        public async Task<Repository> ReadByPathAsync(string path, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM repositories WHERE path=" + Sanitizer.Quote(path) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            Repository repo = FromRow(table.Rows[0]);
            await LoadLanguagesAsync(repo, token).ConfigureAwait(false);
            return repo;
        }

        /// <inheritdoc />
        public async Task<List<Repository>> EnumerateAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM repositories ORDER BY name COLLATE NOCASE ASC;", false, token).ConfigureAwait(false);
            List<Repository> results = new List<Repository>();
            foreach (DataRow row in table.Rows)
            {
                Repository repo = FromRow(row);
                await LoadLanguagesAsync(repo, token).ConfigureAwait(false);
                results.Add(repo);
            }
            return results;
        }

        /// <inheritdoc />
        public async Task SetIncludedAsync(string id, bool included, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            await _Db.ExecuteQueryAsync(
                "UPDATE repositories SET isincluded=" + Sanitizer.Bool(included) + " WHERE id=" + Sanitizer.Quote(id) + ";",
                false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            List<string> batch = new List<string>
            {
                "DELETE FROM repository_languages WHERE repoid=" + Sanitizer.Quote(id) + ";",
                "DELETE FROM projects WHERE repoid=" + Sanitizer.Quote(id) + ";",
                "DELETE FROM dependencies WHERE repoid=" + Sanitizer.Quote(id) + ";",
                "DELETE FROM signals WHERE repoid=" + Sanitizer.Quote(id) + ";",
                "DELETE FROM github_snapshots WHERE repoid=" + Sanitizer.Quote(id) + ";",
                "DELETE FROM repositories WHERE id=" + Sanitizer.Quote(id) + ";"
            };
            await _Db.ExecuteQueriesAsync(batch, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task LoadLanguagesAsync(Repository repo, CancellationToken token)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT language FROM repository_languages WHERE repoid=" + Sanitizer.Quote(repo.Id) + ";",
                false, token).ConfigureAwait(false);
            List<string> languages = new List<string>();
            foreach (DataRow row in table.Rows)
            {
                string lang = row.GetString("language");
                if (!String.IsNullOrEmpty(lang)) languages.Add(lang);
            }
            repo.Languages = languages;
        }

        private static Repository FromRow(DataRow row)
        {
            Repository repo = new Repository
            {
                Id = row.GetString("id"),
                Path = row.GetString("path"),
                Name = row.GetString("name"),
                Visibility = row.GetEnum("visibility", SourceVisibilityEnum.Unknown),
                PrimaryLanguage = row.GetEnum("primarylanguage", ProjectTypeEnum.Unknown),
                CurrentVersion = row.GetString("currentversion"),
                IsGitRepository = row.GetBool("isgitrepository"),
                RemoteUrl = row.GetString("remoteurl"),
                CurrentBranch = row.GetString("currentbranch"),
                BaseBranch = row.GetString("basebranch"),
                CommitsAhead = row.GetInt("commitsahead"),
                CommitsBehind = row.GetInt("commitsbehind"),
                LastCommitHash = row.GetString("commithash"),
                LastUpdateUtc = row.GetDateTime("lastupdateutc"),
                ProjectCount = row.GetInt("projectcount"),
                BranchCount = row.GetInt("branchcount"),
                OverallHealth = row.GetEnum("overallhealth", HealthStatusEnum.Unknown),
                IsIncluded = row.GetBool("isincluded", true),
                LastScannedUtc = row.GetDateTime("lastscannedutc"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
            return repo;
        }

        #endregion
    }
}
