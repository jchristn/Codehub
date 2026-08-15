namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for project dependencies.
    /// </summary>
    public interface IDependencyMethods
    {
        /// <summary>
        /// Replace all dependencies for a repository with the supplied set.
        /// </summary>
        Task ReplaceForRepositoryAsync(string repositoryId, List<Dependency> dependencies, CancellationToken token = default);

        /// <summary>
        /// Enumerate dependencies for a repository.
        /// </summary>
        Task<List<Dependency>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default);
    }
}
