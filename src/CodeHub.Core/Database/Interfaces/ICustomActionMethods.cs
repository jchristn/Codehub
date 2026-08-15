namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for user-defined custom actions.
    /// </summary>
    public interface ICustomActionMethods
    {
        /// <summary>
        /// Enumerate all custom actions.
        /// </summary>
        Task<List<CustomAction>> EnumerateAsync(CancellationToken token = default);

        /// <summary>
        /// Read a custom action by identifier.
        /// </summary>
        Task<CustomAction> ReadAsync(string id, CancellationToken token = default);

        /// <summary>
        /// Insert or update a custom action.
        /// </summary>
        Task<CustomAction> UpsertAsync(CustomAction action, CancellationToken token = default);

        /// <summary>
        /// Delete a custom action by identifier.
        /// </summary>
        Task DeleteAsync(string id, CancellationToken token = default);
    }
}
