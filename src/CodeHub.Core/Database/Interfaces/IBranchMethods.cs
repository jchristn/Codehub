namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for repository branches.
    /// </summary>
    public interface IBranchMethods
    {
        /// <summary>
        /// Replace all branches for a repository with the supplied set.
        /// </summary>
        Task ReplaceForRepositoryAsync(string repositoryId, List<Branch> branches, CancellationToken token = default);

        /// <summary>
        /// Enumerate branches for a repository.
        /// </summary>
        Task<List<Branch>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default);
    }
}
