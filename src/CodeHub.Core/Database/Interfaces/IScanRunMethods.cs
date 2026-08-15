namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for scan runs.
    /// </summary>
    public interface IScanRunMethods
    {
        /// <summary>
        /// Create a scan run.
        /// </summary>
        Task<ScanRun> CreateAsync(ScanRun run, CancellationToken token = default);

        /// <summary>
        /// Update a scan run.
        /// </summary>
        Task UpdateAsync(ScanRun run, CancellationToken token = default);

        /// <summary>
        /// Read a scan run by identifier.
        /// </summary>
        Task<ScanRun> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Read the most recent scan run.
        /// </summary>
        Task<ScanRun> ReadLatestAsync(CancellationToken token = default);

        /// <summary>
        /// Enumerate recent scan runs, newest first.
        /// </summary>
        Task<List<ScanRun>> EnumerateAsync(int limit, CancellationToken token = default);
    }
}
