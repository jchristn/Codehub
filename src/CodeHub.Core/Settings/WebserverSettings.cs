namespace CodeHub.Core.Settings
{
    using System;

    /// <summary>
    /// HTTP hosting settings (JSON section "webserver").
    /// </summary>
    public class WebserverSettings
    {
        #region Public-Members

        /// <summary>
        /// Bind hostname. Defaults to loopback IPv4 to avoid the Windows IPv6 stall.
        /// </summary>
        public string Hostname { get; set; } = "127.0.0.1";

        /// <summary>
        /// Bind port.
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
            set
            {
                _Port = Math.Clamp(value, 1, 65535);
            }
        }

        /// <summary>
        /// Whether SSL is enabled.
        /// </summary>
        public bool Ssl { get; set; } = false;

        /// <summary>
        /// Whether the OpenAPI document is served.
        /// </summary>
        public bool EnableOpenApi { get; set; } = true;

        /// <summary>
        /// Path to the built React dashboard (the Vite "dist" folder) served at /dashboard.
        /// When empty, the server auto-detects it; when unresolved, /dashboard is disabled.
        /// </summary>
        public string DashboardDirectory { get; set; } = "";

        #endregion

        #region Private-Members

        private int _Port = 8090;

        #endregion
    }
}
