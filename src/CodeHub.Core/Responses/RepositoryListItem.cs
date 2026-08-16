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

        /// <summary>
        /// Distinct target frameworks across the repository's projects (e.g. "net8.0", "net10.0").
        /// Multi-target project values (e.g. "net8.0;net10.0") are split into individual entries.
        /// </summary>
        public List<string> TargetFrameworks { get; set; } = new List<string>();

        /// <summary>
        /// Manual value overrides applied to this repository's columns, if any.
        /// </summary>
        public List<Annotation> Annotations { get; set; } = new List<Annotation>();

        #endregion
    }
}
