namespace CodeHub.Core.Database
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Sqlite;
    using CodeHub.Core.Enums;

    /// <summary>
    /// Composition root for the data layer.
    /// </summary>
    public static class DatabaseDriverFactory
    {
        #region Public-Methods

        /// <summary>
        /// Create a driver for the configured provider.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <returns>Database driver.</returns>
        public static DatabaseDriverBase Create(DatabaseSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            switch (settings.Type)
            {
                case DatabaseTypeEnum.Sqlite:
                    return new SqliteDatabaseDriver(settings);
                default:
                    throw new NotImplementedException(
                        "Database provider " + settings.Type + " is reserved but not implemented in this version. Use Sqlite.");
            }
        }

        /// <summary>
        /// Create and initialize a driver.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Initialized database driver.</returns>
        public static async Task<DatabaseDriverBase> CreateAndInitializeAsync(DatabaseSettings settings, CancellationToken token = default)
        {
            DatabaseDriverBase driver = Create(settings);
            await driver.InitializeAsync(token).ConfigureAwait(false);
            return driver;
        }

        #endregion
    }
}
