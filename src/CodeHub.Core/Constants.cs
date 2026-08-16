namespace CodeHub.Core
{
    /// <summary>
    /// Application-wide constants, including entity identifier prefixes.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Product name.
        /// </summary>
        public const string ProductName = "CodeHub";

        /// <summary>
        /// Software version.
        /// </summary>
        public const string SoftwareVersion = "0.1.0";

        /// <summary>
        /// ASCII-art startup banner.
        /// </summary>
        public const string Logo =
@"                          $$\           $$\                 $$\
                          $$ |          $$ |                $$ |
 $$$$$$$\  $$$$$$\   $$$$$$$ | $$$$$$\  $$$$$$$\  $$\   $$\ $$$$$$$\
$$  _____|$$  __$$\ $$  __$$ |$$  __$$\ $$  __$$\ $$ |  $$ |$$  __$$\
$$ /      $$ /  $$ |$$ /  $$ |$$$$$$$$ |$$ |  $$ |$$ |  $$ |$$ |  $$ |
$$ |      $$ |  $$ |$$ |  $$ |$$   ____|$$ |  $$ |$$ |  $$ |$$ |  $$ |
\$$$$$$$\ \$$$$$$  |\$$$$$$$ |\$$$$$$$\ $$ |  $$ |\$$$$$$  |$$$$$$$  |
 \_______| \______/  \_______| \_______|\__|  \__| \______/ \_______/ ";

        /// <summary>
        /// Total identifier length including the prefix.
        /// </summary>
        public const int IdLength = 32;

        /// <summary>
        /// Repository identifier prefix.
        /// </summary>
        public const string RepositoryPrefix = "repo_";

        /// <summary>
        /// Project identifier prefix.
        /// </summary>
        public const string ProjectPrefix = "prj_";

        /// <summary>
        /// Dependency identifier prefix.
        /// </summary>
        public const string DependencyPrefix = "dep_";

        /// <summary>
        /// Signal identifier prefix.
        /// </summary>
        public const string SignalPrefix = "sig_";

        /// <summary>
        /// Scan run identifier prefix.
        /// </summary>
        public const string ScanRunPrefix = "scan_";

        /// <summary>
        /// GitHub snapshot identifier prefix.
        /// </summary>
        public const string GitHubSnapshotPrefix = "gh_";

        /// <summary>
        /// Scan selection identifier prefix.
        /// </summary>
        public const string SelectionPrefix = "sel_";

        /// <summary>
        /// Request history identifier prefix.
        /// </summary>
        public const string RequestHistoryPrefix = "req_";

        /// <summary>
        /// Custom action identifier prefix.
        /// </summary>
        public const string CustomActionPrefix = "act_";

        /// <summary>
        /// Branch identifier prefix.
        /// </summary>
        public const string BranchPrefix = "br_";

        /// <summary>
        /// Annotation (value override) identifier prefix.
        /// </summary>
        public const string AnnotationPrefix = "ann_";
    }
}
