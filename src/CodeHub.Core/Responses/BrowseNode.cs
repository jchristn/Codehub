namespace CodeHub.Core.Responses
{
    /// <summary>
    /// A directory node in the file-picker tree.
    /// </summary>
    public class BrowseNode
    {
        #region Public-Members

        /// <summary>
        /// Directory display name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Absolute directory path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether the directory is a git repository.
        /// </summary>
        public bool IsGitRepository { get; set; }

        /// <summary>
        /// Whether the directory has scannable subdirectories (drives lazy expansion).
        /// </summary>
        public bool HasSubdirectories { get; set; }

        /// <summary>
        /// Selection state: None, Selected, Partial, or Excluded.
        /// </summary>
        public string State { get; set; }

        #endregion
    }
}
