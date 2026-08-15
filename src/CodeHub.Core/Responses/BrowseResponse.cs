namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// The children of a browsed directory (or the base roots when no path is given).
    /// </summary>
    public class BrowseResponse
    {
        #region Public-Members

        /// <summary>
        /// The browsed path, or null for the top level.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Whether this response lists the configured base roots (top level).
        /// </summary>
        public bool IsRoot { get; set; }

        /// <summary>
        /// Child directory nodes.
        /// </summary>
        public List<BrowseNode> Children { get; set; } = new List<BrowseNode>();

        #endregion
    }
}
