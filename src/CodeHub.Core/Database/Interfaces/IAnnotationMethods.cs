namespace CodeHub.Core.Database.Interfaces
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;

    /// <summary>
    /// Data access for repository value overrides (annotations).
    /// </summary>
    public interface IAnnotationMethods
    {
        /// <summary>
        /// Enumerate every annotation across all repositories.
        /// </summary>
        Task<List<Annotation>> EnumerateAllAsync(CancellationToken token = default);

        /// <summary>
        /// Enumerate annotations for a repository.
        /// </summary>
        Task<List<Annotation>> EnumerateByRepositoryAsync(string repositoryId, CancellationToken token = default);

        /// <summary>
        /// Insert or update the annotation for a repository + column.
        /// </summary>
        Task<Annotation> UpsertAsync(Annotation annotation, CancellationToken token = default);

        /// <summary>
        /// Delete the annotation for a repository + column.
        /// </summary>
        Task DeleteAsync(string repositoryId, string column, CancellationToken token = default);
    }
}
