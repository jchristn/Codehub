namespace CodeHub.Core.Responses
{
    /// <summary>
    /// Result of validating a GitHub personal access token.
    /// </summary>
    public class TokenValidationResult
    {
        #region Public-Members

        /// <summary>
        /// Whether the token is valid.
        /// </summary>
        public bool Valid { get; set; }

        /// <summary>
        /// The GitHub login the token authenticates as, when valid.
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// A message describing why the token is invalid, when not valid.
        /// </summary>
        public string Message { get; set; }

        #endregion
    }
}
