namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;
    using CodeHub.Core.Models;

    /// <summary>
    /// Full repository detail for the drill-down modal.
    /// </summary>
    public class RepositoryDetailResponse
    {
        #region Public-Members

        /// <summary>
        /// The repository.
        /// </summary>
        public Repository Repository { get; set; }

        /// <summary>
        /// Discrete projects in the repository.
        /// </summary>
        public List<Project> Projects { get; set; } = new List<Project>();

        /// <summary>
        /// Outdated or vulnerable dependencies in the repository.
        /// </summary>
        public List<Dependency> Dependencies { get; set; } = new List<Dependency>();

        /// <summary>
        /// Computed signals with evidence.
        /// </summary>
        public List<Signal> Signals { get; set; } = new List<Signal>();

        /// <summary>
        /// Latest GitHub snapshot, if any.
        /// </summary>
        public GitHubSnapshot GitHub { get; set; } = null;

        #endregion
    }
}
