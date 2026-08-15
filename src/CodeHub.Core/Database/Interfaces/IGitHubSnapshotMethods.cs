namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for GitHub snapshots.
    /// </summary>
    public interface IGitHubSnapshotMethods
    {
        /// <summary>
        /// Insert or update the snapshot for a repository.
        /// </summary>
        Task<GitHubSnapshot> UpsertAsync(GitHubSnapshot snapshot, CancellationToken token = default);

        /// <summary>
        /// Read the snapshot for a repository.
        /// </summary>
        Task<GitHubSnapshot> ReadByRepositoryAsync(string repositoryId, CancellationToken token = default);

        /// <summary>
        /// Enumerate every GitHub snapshot across all repositories.
        /// </summary>
        Task<List<GitHubSnapshot>> EnumerateAllAsync(CancellationToken token = default);
    }
}
