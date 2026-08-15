namespace CodeHub.Core.Helpers
{
    using System;
    using CodeHub.Core;

    /// <summary>
    /// Generates K-sortable, prefixed application identifiers using PrettyId.
    /// </summary>
    public static class IdGenerator
    {
        #region Private-Members

        private static readonly PrettyId.IdGenerator _Generator = new PrettyId.IdGenerator();

        #endregion

        #region Public-Methods

        /// <summary>
        /// Generate an identifier with the supplied prefix.
        /// </summary>
        /// <param name="prefix">Entity prefix, for example "repo_".</param>
        /// <returns>Prefixed, K-sortable identifier.</returns>
        public static string Generate(string prefix)
        {
            if (String.IsNullOrEmpty(prefix)) throw new ArgumentNullException(nameof(prefix));
            return _Generator.GenerateKSortable(prefix, Constants.IdLength);
        }

        /// <summary>
        /// Generate a repository identifier.
        /// </summary>
        /// <returns>Repository identifier.</returns>
        public static string GenerateRepositoryId()
        {
            return Generate(Constants.RepositoryPrefix);
        }

        /// <summary>
        /// Generate a project identifier.
        /// </summary>
        /// <returns>Project identifier.</returns>
        public static string GenerateProjectId()
        {
            return Generate(Constants.ProjectPrefix);
        }

        /// <summary>
        /// Generate a dependency identifier.
        /// </summary>
        /// <returns>Dependency identifier.</returns>
        public static string GenerateDependencyId()
        {
            return Generate(Constants.DependencyPrefix);
        }

        /// <summary>
        /// Generate a signal identifier.
        /// </summary>
        /// <returns>Signal identifier.</returns>
        public static string GenerateSignalId()
        {
            return Generate(Constants.SignalPrefix);
        }

        /// <summary>
        /// Generate a scan run identifier.
        /// </summary>
        /// <returns>Scan run identifier.</returns>
        public static string GenerateScanRunId()
        {
            return Generate(Constants.ScanRunPrefix);
        }

        /// <summary>
        /// Generate a GitHub snapshot identifier.
        /// </summary>
        /// <returns>GitHub snapshot identifier.</returns>
        public static string GenerateGitHubSnapshotId()
        {
            return Generate(Constants.GitHubSnapshotPrefix);
        }

        /// <summary>
        /// Generate a scan selection identifier.
        /// </summary>
        /// <returns>Scan selection identifier.</returns>
        public static string GenerateSelectionId()
        {
            return Generate(Constants.SelectionPrefix);
        }

        /// <summary>
        /// Generate a request history identifier.
        /// </summary>
        /// <returns>Request history identifier.</returns>
        public static string GenerateRequestHistoryId()
        {
            return Generate(Constants.RequestHistoryPrefix);
        }

        /// <summary>
        /// Generate a custom action identifier.
        /// </summary>
        /// <returns>Custom action identifier.</returns>
        public static string GenerateCustomActionId()
        {
            return Generate(Constants.CustomActionPrefix);
        }

        /// <summary>
        /// Generate a branch identifier.
        /// </summary>
        /// <returns>Branch identifier.</returns>
        public static string GenerateBranchId()
        {
            return Generate(Constants.BranchPrefix);
        }

        #endregion
    }
}
