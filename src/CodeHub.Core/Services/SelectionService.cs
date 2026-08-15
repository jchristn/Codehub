namespace CodeHub.Core.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using CodeHub.Core.Database;
    using CodeHub.Core.Models;

    /// <summary>
    /// Node selection state used by the directory picker.
    /// </summary>
    public enum SelectionStateEnum
    {
        /// <summary>Neither selected nor excluded.</summary>
        None,
        /// <summary>Selected (self or via a selected ancestor).</summary>
        Selected,
        /// <summary>Some descendant is selected, but this node is not fully covered.</summary>
        Partial,
        /// <summary>Excluded (self or via an excluded ancestor).</summary>
        Excluded
    }

    /// <summary>
    /// Manages the scan-selection set stored in the database. Selecting a directory adds an
    /// include entry (dropping redundant descendants); unchecking a directory covered by a
    /// selected ancestor adds an exclude entry — unifying the picker with the row-level exclude.
    /// </summary>
    public class SelectionService
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Database driver.</param>
        public SelectionService(DatabaseDriverBase db)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Select a directory for scanning.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task SelectAsync(string path, CancellationToken token = default)
        {
            path = Normalize(path);
            if (String.IsNullOrEmpty(path)) return;

            List<ScanSelection> all = await _Db.Selections.EnumerateAsync(token).ConfigureAwait(false);
            foreach (ScanSelection selection in all)
            {
                if (IsStrictlyUnder(selection.Path, path))
                    await _Db.Selections.DeleteByPathAsync(selection.Path, token).ConfigureAwait(false);
            }

            await _Db.Selections.UpsertAsync(new ScanSelection { Path = path, Included = true }, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Deselect a directory. If it was an explicit selection, remove it; if it is covered
        /// by a selected ancestor, add an exclude entry to prune it.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task DeselectAsync(string path, CancellationToken token = default)
        {
            path = Normalize(path);
            if (String.IsNullOrEmpty(path)) return;

            List<ScanSelection> all = await _Db.Selections.EnumerateAsync(token).ConfigureAwait(false);

            // Drop any entries strictly under this path (they become redundant).
            foreach (ScanSelection selection in all)
            {
                if (IsStrictlyUnder(selection.Path, path))
                    await _Db.Selections.DeleteByPathAsync(selection.Path, token).ConfigureAwait(false);
            }

            ScanSelection atPath = all.FirstOrDefault(s => PathEquals(s.Path, path));
            if (atPath != null && atPath.Included)
            {
                await _Db.Selections.DeleteByPathAsync(path, token).ConfigureAwait(false);
                return;
            }

            bool coveredByAncestor = all.Any(s => s.Included && IsStrictlyUnder(path, s.Path));
            if (coveredByAncestor)
            {
                await _Db.Selections.UpsertAsync(new ScanSelection { Path = path, Included = false }, token).ConfigureAwait(false);
            }
            else if (atPath != null && !atPath.Included)
            {
                // Un-excluding a path that is not under any selection: remove the stray exclude.
                await _Db.Selections.DeleteByPathAsync(path, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Return the effective included and excluded path sets.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Included and excluded path lists.</returns>
        public async Task<SelectionSets> GetSetsAsync(CancellationToken token = default)
        {
            List<ScanSelection> all = await _Db.Selections.EnumerateAsync(token).ConfigureAwait(false);
            SelectionSets sets = new SelectionSets();
            foreach (ScanSelection selection in all)
            {
                if (selection.Included) sets.Included.Add(selection.Path);
                else sets.Excluded.Add(selection.Path);
            }
            return sets;
        }

        /// <summary>
        /// Compute the picker state for a path given the current sets.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="sets">Selection sets.</param>
        /// <returns>Selection state.</returns>
        public static SelectionStateEnum StateFor(string path, SelectionSets sets)
        {
            path = Normalize(path);
            if (sets.Excluded.Any(e => PathEquals(path, e) || IsStrictlyUnder(path, e))) return SelectionStateEnum.Excluded;
            if (sets.Included.Any(i => PathEquals(path, i) || IsStrictlyUnder(path, i))) return SelectionStateEnum.Selected;
            if (sets.Included.Any(i => IsStrictlyUnder(i, path))) return SelectionStateEnum.Partial;
            return SelectionStateEnum.None;
        }

        /// <summary>
        /// Seed the selection table once from legacy settings lists if it is empty.
        /// </summary>
        /// <param name="selectedPaths">Legacy selected paths.</param>
        /// <param name="excludedPaths">Legacy excluded paths.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task SeedIfEmptyAsync(List<string> selectedPaths, List<string> excludedPaths, CancellationToken token = default)
        {
            int count = await _Db.Selections.CountAsync(token).ConfigureAwait(false);
            if (count > 0) return;

            if (selectedPaths != null)
            {
                foreach (string path in selectedPaths)
                {
                    string normalized = Normalize(path);
                    if (!String.IsNullOrEmpty(normalized))
                        await _Db.Selections.UpsertAsync(new ScanSelection { Path = normalized, Included = true }, token).ConfigureAwait(false);
                }
            }
            if (excludedPaths != null)
            {
                foreach (string path in excludedPaths)
                {
                    string normalized = Normalize(path);
                    if (!String.IsNullOrEmpty(normalized))
                        await _Db.Selections.UpsertAsync(new ScanSelection { Path = normalized, Included = false }, token).ConfigureAwait(false);
                }
            }
        }

        #endregion

        #region Public-Static-Path-Helpers

        /// <summary>
        /// Normalize a path for comparison (full path, trimmed trailing separators).
        /// </summary>
        /// <param name="path">Path.</param>
        /// <returns>Normalized path.</returns>
        public static string Normalize(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return path;
            string trimmed = path.Trim();
            try
            {
                trimmed = System.IO.Path.GetFullPath(trimmed);
            }
            catch (Exception)
            {
                // keep as-is
            }
            return trimmed.TrimEnd('\\', '/');
        }

        /// <summary>
        /// Whether two paths are equal (case-insensitive).
        /// </summary>
        public static bool PathEquals(string a, string b)
        {
            return String.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Whether child is strictly under parent.
        /// </summary>
        public static bool IsStrictlyUnder(string child, string parent)
        {
            string c = Normalize(child);
            string p = Normalize(parent);
            if (String.IsNullOrEmpty(c) || String.IsNullOrEmpty(p)) return false;
            if (String.Equals(c, p, StringComparison.OrdinalIgnoreCase)) return false;
            return c.StartsWith(p + "\\", StringComparison.OrdinalIgnoreCase) ||
                   c.StartsWith(p + "/", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }

    /// <summary>
    /// The included and excluded path sets.
    /// </summary>
    public class SelectionSets
    {
        /// <summary>Included directory paths.</summary>
        public List<string> Included { get; set; } = new List<string>();

        /// <summary>Excluded directory paths.</summary>
        public List<string> Excluded { get; set; } = new List<string>();
    }
}
