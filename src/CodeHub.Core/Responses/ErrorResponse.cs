namespace CodeHub.Core.Responses
{
    /// <summary>
    /// Standard error envelope.
    /// </summary>
    public class ErrorResponse
    {
        #region Public-Members

        /// <summary>
        /// Short machine-readable error code.
        /// </summary>
        public string Code { get; set; } = "Error";

        /// <summary>
        /// Human-readable message.
        /// </summary>
        public string Message { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate an empty error response.
        /// </summary>
        public ErrorResponse()
        {
        }

        /// <summary>
        /// Instantiate an error response.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="message">Error message.</param>
        public ErrorResponse(string code, string message)
        {
            Code = code;
            Message = message;
        }

        #endregion
    }
}
