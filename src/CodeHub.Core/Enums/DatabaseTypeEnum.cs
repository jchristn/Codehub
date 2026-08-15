namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Supported database providers. SQLite is implemented in v1; the others are
    /// reserved so the provider-neutral abstraction can be extended later.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DatabaseTypeEnum
    {
        /// <summary>
        /// SQLite (implemented).
        /// </summary>
        Sqlite,

        /// <summary>
        /// MySQL (reserved).
        /// </summary>
        Mysql,

        /// <summary>
        /// PostgreSQL (reserved).
        /// </summary>
        Postgresql,

        /// <summary>
        /// SQL Server (reserved).
        /// </summary>
        SqlServer
    }
}
