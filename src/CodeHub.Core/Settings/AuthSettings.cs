namespace CodeHub.Core.Settings
{
    using System;

    /// <summary>
    /// Static-key authentication settings for the local single-operator model.
    /// </summary>
    public class AuthSettings
    {
        #region Public-Members

        /// <summary>
        /// The static API key. Presented by the client as "Authorization: Bearer &lt;key&gt;".
        /// Override with the CODEHUB_AUTH_API_KEY environment variable.
        /// </summary>
        public string ApiKey { get; set; } = "codehub-dev-key";

        #endregion
    }
}
