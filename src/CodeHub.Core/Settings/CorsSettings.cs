namespace CodeHub.Core.Settings
{
    /// <summary>
    /// CORS settings (JSON section "cors"). Applied to every response.
    /// </summary>
    public class CorsSettings
    {
        #region Public-Members

        /// <summary>
        /// Whether CORS headers are emitted.
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// Access-Control-Allow-Origin value.
        /// </summary>
        public string AllowOrigin { get; set; } = "*";

        /// <summary>
        /// Access-Control-Allow-Methods value.
        /// </summary>
        public string AllowMethods { get; set; } = "GET, POST, PUT, DELETE, OPTIONS, HEAD";

        /// <summary>
        /// Access-Control-Allow-Headers value.
        /// </summary>
        public string AllowHeaders { get; set; } = "Content-Type, Authorization, X-Api-Key";

        #endregion
    }
}
