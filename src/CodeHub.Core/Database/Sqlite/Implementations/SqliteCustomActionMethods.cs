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
    /// SQLite implementation of custom action data access.
    /// </summary>
    public class SqliteCustomActionMethods : ICustomActionMethods
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Owning driver.</param>
        public SqliteCustomActionMethods(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<List<CustomAction>> EnumerateAsync(CancellationToken token = default)
        {
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM custom_actions ORDER BY name COLLATE NOCASE ASC;", false, token).ConfigureAwait(false);
            List<CustomAction> results = new List<CustomAction>();
            foreach (DataRow row in table.Rows) results.Add(FromRow(row));
            return results;
        }

        /// <inheritdoc />
        public async Task<CustomAction> ReadAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            DataTable table = await _Db.ExecuteQueryAsync(
                "SELECT * FROM custom_actions WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
            if (table.Rows.Count == 0) return null;
            return FromRow(table.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<CustomAction> UpsertAsync(CustomAction action, CancellationToken token = default)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            CustomAction existing = await ReadAsync(action.Id, token).ConfigureAwait(false);
            string sql;
            if (existing != null)
            {
                sql =
                    "UPDATE custom_actions SET " +
                    "name=" + Sanitizer.Quote(action.Name) + ", " +
                    "agent=" + Sanitizer.Quote(action.Agent) + ", " +
                    "dangerous=" + Sanitizer.Bool(action.Dangerous) + ", " +
                    "prompt=" + Sanitizer.Quote(action.Prompt) + " " +
                    "WHERE id=" + Sanitizer.Quote(action.Id) + ";";
            }
            else
            {
                sql =
                    "INSERT INTO custom_actions (id, name, agent, dangerous, prompt, createdutc) VALUES (" +
                    Sanitizer.Quote(action.Id) + ", " +
                    Sanitizer.Quote(action.Name) + ", " +
                    Sanitizer.Quote(action.Agent) + ", " +
                    Sanitizer.Bool(action.Dangerous) + ", " +
                    Sanitizer.Quote(action.Prompt) + ", " +
                    Sanitizer.Timestamp(action.CreatedUtc) + ");";
            }

            await _Db.ExecuteQueryAsync(sql, false, token).ConfigureAwait(false);
            return action;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(string id, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            await _Db.ExecuteQueryAsync(
                "DELETE FROM custom_actions WHERE id=" + Sanitizer.Quote(id) + ";", false, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static CustomAction FromRow(DataRow row)
        {
            return new CustomAction
            {
                Id = row.GetString("id"),
                Name = row.GetString("name"),
                Agent = row.GetString("agent"),
                Dangerous = row.GetBool("dangerous"),
                Prompt = row.GetString("prompt"),
                CreatedUtc = row.GetDateTimeRequired("createdutc")
            };
        }

        #endregion
    }
}
