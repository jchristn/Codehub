namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;
    using CodeHub.Core.Models;

    /// <summary>
    /// A repository plus its computed signals and GitHub snapshot, as shown in the main table.
    /// </summary>
    public class RepositoryListItem
    {
        #region Public-Members

        /// <summary>
        /// The repository.
        /// </summary>
        public Repository Repository { get; set; }

        /// <summary>
        /// Computed signals for the repository.
        /// </summary>
        public List<Signal> Signals { get; set; } = new List<Signal>();

        /// <summary>
        /// Latest GitHub snapshot, if any.
        /// </summary>
        public GitHubSnapshot GitHub { get; set; } = null;

        #endregion
    }
}
