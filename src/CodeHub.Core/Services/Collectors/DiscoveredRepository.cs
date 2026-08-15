namespace CodeHub.Core.Services.Collectors
{
    using System.Collections.Generic;
    using CodeHub.Core.Models;

    /// <summary>
    /// A repository and the discrete projects discovered within it.
    /// </summary>
    public class DiscoveredRepository
    {
        /// <summary>
        /// The repository.
        /// </summary>
        public Repository Repository { get; set; }

        /// <summary>
        /// The projects discovered inside it.
        /// </summary>
        public List<Project> Projects { get; set; } = new List<Project>();
    }
}
