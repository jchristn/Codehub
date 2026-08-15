namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to validate a GitHub personal access token.
    /// </summary>
    public class TokenValidateRequest
    {
        #region Public-Members

        /// <summary>
        /// The token to validate. When empty, the currently-configured token is validated.
        /// </summary>
        public string Token { get; set; }

        #endregion
    }
}
