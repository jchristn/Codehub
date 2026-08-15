namespace CodeHub.Core.Database.Interfaces
{
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
    }
}
