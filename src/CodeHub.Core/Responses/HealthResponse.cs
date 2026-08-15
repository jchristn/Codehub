namespace CodeHub.Core.Responses
{
    using System;
    using CodeHub.Core;

    /// <summary>
    /// Health check payload.
    /// </summary>
    public class HealthResponse
    {
        #region Public-Members

        /// <summary>
        /// Service status.
        /// </summary>
        public string Status { get; set; } = "healthy";

        /// <summary>
        /// Product name.
        /// </summary>
        public string Product { get; set; } = Constants.ProductName;

        /// <summary>
        /// Software version.
        /// </summary>
        public string Version { get; set; } = Constants.SoftwareVersion;

        /// <summary>
        /// Current server UTC time.
        /// </summary>
        public DateTime TimeUtc { get; set; } = DateTime.UtcNow;

        #endregion
    }
}
