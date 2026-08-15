namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for projects.
    /// </summary>
    public interface IProjectMethods
    {
        /// <summary>
        /// Replace all projects for a repository with the supplied set.
        /// </summary>
        Task ReplaceForRepositoryAsync(string repositoryId, List<Project> projects, CancellationToken token = default);

        /// <summary>
        /// Enumerate projects for a repository.
        /// </summary>
        Task<List<Project>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default);

        /// <summary>
        /// Read a project by identifier.
        /// </summary>
        Task<Project> ReadAsync(string id, CancellationToken token = default);
    }
}
