namespace CodeHub.Core.Database
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database.Interfaces;
    using CodeHub.Core.Enums;

    /// <summary>
    /// Provider-neutral database driver base. SQLite is the only concrete provider in v1.
    /// </summary>
    public abstract class DatabaseDriverBase : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Database provider type.
        /// </summary>
        public abstract DatabaseTypeEnum DatabaseType { get; }

        /// <summary>
        /// Repository methods.
        /// </summary>
        public IRepositoryMethods Repositories { get; protected set; }

        /// <summary>
        /// Project methods.
        /// </summary>
        public IProjectMethods Projects { get; protected set; }

        /// <summary>
        /// Dependency methods.
        /// </summary>
        public IDependencyMethods Dependencies { get; protected set; }

        /// <summary>
        /// Signal methods.
        /// </summary>
        public ISignalMethods Signals { get; protected set; }

        /// <summary>
        /// Scan run methods.
        /// </summary>
        public IScanRunMethods ScanRuns { get; protected set; }

        /// <summary>
        /// GitHub snapshot methods.
        /// </summary>
        public IGitHubSnapshotMethods GitHubSnapshots { get; protected set; }

        /// <summary>
        /// Scan selection methods.
        /// </summary>
        public IScanSelectionMethods Selections { get; protected set; }

        /// <summary>
        /// Request history methods.
        /// </summary>
        public IRequestHistoryMethods RequestHistory { get; protected set; }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the database, applying schema migrations.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        public abstract Task InitializeAsync(CancellationToken token = default);

        /// <summary>
        /// Execute a single query and return the resulting rows.
        /// </summary>
        /// <param name="query">SQL text.</param>
        /// <param name="isTransaction">Whether to wrap the statement in a transaction.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Result table.</returns>
        public abstract Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default);

        /// <summary>
        /// Execute a batch of queries in a single transaction.
        /// </summary>
        /// <param name="queries">SQL statements.</param>
        /// <param name="token">Cancellation token.</param>
        public abstract Task ExecuteQueriesAsync(IEnumerable<string> queries, CancellationToken token = default);

        /// <summary>
        /// Close the driver.
        /// </summary>
        public abstract void Dispose();

        #endregion
    }
}
