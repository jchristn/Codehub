namespace CodeHub.Core.Settings
{
    using System.Collections.Generic;

    /// <summary>
    /// Directory settings (JSON section "directories"). The base roots the directory picker
    /// browses (also the security sandbox), plus directory names skipped during discovery.
    /// </summary>
    public class DirectorySettings
    {
        #region Public-Members

        /// <summary>
        /// Base root directories the picker can browse.
        /// </summary>
        public List<string> RootPaths { get; set; } = new List<string> { "C:\\Code" };

        /// <summary>
        /// Directory names to skip during discovery.
        /// </summary>
        public List<string> ExcludeDirectories { get; set; } = new List<string>
        {
            "bin", "obj", "node_modules", "dist", ".git", ".vs", "packages", "TestResults"
        };

        #endregion
    }
}
