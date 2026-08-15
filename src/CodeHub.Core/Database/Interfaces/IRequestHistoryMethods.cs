namespace CodeHub.Core.Database.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;

    /// <summary>
    /// Data access for captured request history.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Insert a captured request record.
        /// </summary>
        Task CreateAsync(RequestHistoryEntry entry, CancellationToken token = default);

        /// <summary>
        /// Read a single entry by identifier.
        /// </summary>
        Task<RequestHistoryEntry> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Page through entries matching a filter (bodies omitted).
        /// </summary>
        Task<RequestHistoryPage> EnumerateAsync(RequestHistoryFilter filter, CancellationToken token = default);

        /// <summary>
        /// Return time-bucketed counts for chart rendering.
        /// </summary>
        Task<RequestHistorySummary> SummarizeAsync(RequestHistoryFilter filter, CancellationToken token = default);

        /// <summary>
        /// Delete a single entry.
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Prune entries older than the given UTC cutoff.
        /// </summary>
        Task<int> PruneAsync(DateTime olderThanUtc, CancellationToken token = default);
    }
}
