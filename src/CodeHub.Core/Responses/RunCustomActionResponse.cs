namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// Result of running a custom action against one or more repositories.
    /// </summary>
    public class RunCustomActionResponse
    {
        #region Public-Members

        /// <summary>
        /// Custom action identifier.
        /// </summary>
        public string ActionId { get; set; }

        /// <summary>
        /// Agent the action ran with.
        /// </summary>
        public string Agent { get; set; }

        /// <summary>
        /// Number of repositories a terminal was launched in.
        /// </summary>
        public int Launched { get; set; }

        /// <summary>
        /// Number of repositories that failed to launch.
        /// </summary>
        public int Failed { get; set; }

        /// <summary>
        /// Per-repository outcomes, in request order.
        /// </summary>
        public List<RunCustomActionResult> Results { get; set; } = new List<RunCustomActionResult>();

        #endregion
    }
}
