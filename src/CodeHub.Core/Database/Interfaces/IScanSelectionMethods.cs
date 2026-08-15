namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for scan selections (the include/exclude directory set).
    /// </summary>
    public interface IScanSelectionMethods
    {
        /// <summary>
        /// Insert or update a selection, keyed on path.
        /// </summary>
        Task<ScanSelection> UpsertAsync(ScanSelection selection, CancellationToken token = default);

        /// <summary>
        /// Read a selection by path.
        /// </summary>
        Task<ScanSelection> ReadByPathAsync(string path, CancellationToken token = default);

        /// <summary>
        /// Enumerate all selections.
        /// </summary>
        Task<List<ScanSelection>> EnumerateAsync(CancellationToken token = default);

        /// <summary>
        /// Delete a selection by path.
        /// </summary>
        Task DeleteByPathAsync(string path, CancellationToken token = default);

        /// <summary>
        /// Count all selections.
        /// </summary>
        Task<int> CountAsync(CancellationToken token = default);
    }
}
