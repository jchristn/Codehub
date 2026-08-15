namespace CodeHub.Server.Security
{
    /// <summary>
    /// Minimal authenticated request context for the single-operator model.
    /// </summary>
    public class RequestContext
    {
        /// <summary>
        /// Whether the request is authenticated.
        /// </summary>
        public bool IsAuthenticated { get; set; } = false;
    }
}
