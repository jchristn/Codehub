namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for computed repository signals.
    /// </summary>
    public interface ISignalMethods
    {
        /// <summary>
        /// Replace all signals for a repository with the supplied set.
        /// </summary>
        Task ReplaceForRepositoryAsync(string repositoryId, List<Signal> signals, CancellationToken token = default);

        /// <summary>
        /// Enumerate signals for a repository.
        /// </summary>
        Task<List<Signal>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default);

        /// <summary>
        /// Enumerate all signals across all repositories.
        /// </summary>
        Task<List<Signal>> EnumerateAllAsync(CancellationToken token = default);
    }
}
