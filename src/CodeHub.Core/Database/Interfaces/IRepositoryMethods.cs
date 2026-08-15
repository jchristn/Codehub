namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for repositories.
    /// </summary>
    public interface IRepositoryMethods
    {
        /// <summary>
        /// Insert or update a repository, keyed on its path. Replaces its language rows.
        /// </summary>
        Task<Repository> UpsertAsync(Repository repository, CancellationToken token = default);

        /// <summary>
        /// Read a repository by identifier.
        /// </summary>
        Task<Repository> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read a repository by path.
        /// </summary>
        Task<Repository> ReadByPathAsync(string path, CancellationToken token = default);

        /// <summary>
        /// Enumerate all repositories.
        /// </summary>
        Task<List<Repository>> EnumerateAsync(CancellationToken token = default);

        /// <summary>
        /// Set the included flag on a repository.
        /// </summary>
        Task SetIncludedAsync(string id, bool included, CancellationToken token = default);

        /// <summary>
        /// Delete a repository and its dependent rows.
        /// </summary>
        Task DeleteAsync(string id, CancellationToken token = default);
    }
}
