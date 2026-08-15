namespace CodeHub.Core.Database
{
    using System;
    using CodeHub.Core.Enums;

    /// <summary>
    /// Database provider configuration.
    /// </summary>
    public class DatabaseSettings
    {
        #region Public-Members

        /// <summary>
        /// Database provider type. Only Sqlite is implemented in v1.
        /// </summary>
        public DatabaseTypeEnum Type { get; set; } = DatabaseTypeEnum.Sqlite;

        /// <summary>
        /// SQLite database file path.
        /// </summary>
        public string Filename { get; set; } = "data/codehub.db";

        /// <summary>
        /// Database server host (non-SQLite providers).
        /// </summary>
        public string Server { get; set; } = null;

        /// <summary>
        /// Database server port (non-SQLite providers).
        /// </summary>
        public int Port { get; set; } = 0;

        /// <summary>
        /// Database name (non-SQLite providers).
        /// </summary>
        public string DatabaseName { get; set; } = null;

        /// <summary>
        /// Database username (non-SQLite providers).
        /// </summary>
        public string Username { get; set; } = null;

        /// <summary>
        /// Database password (non-SQLite providers).
        /// </summary>
        public string Password { get; set; } = null;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build the provider connection string.
        /// </summary>
        /// <returns>Connection string.</returns>
        public string GetConnectionString()
        {
            switch (Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return "Data Source=" + Filename;
                default:
                    throw new NotImplementedException("Database provider " + Type + " is not implemented in this version.");
            }
        }

        #endregion
    }
}
