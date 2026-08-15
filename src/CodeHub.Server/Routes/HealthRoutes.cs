namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Health and root routes (anonymous).
    /// </summary>
    public class HealthRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public HealthRoutes(ServiceContext ctx)
        {
            _Ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Register routes.
        /// </summary>
        /// <param name="server">Webserver.</param>
        public void Register(Webserver server)
        {
            server.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", HealthAsync);
            server.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", HealthAsync);
            server.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/health", HealthAsync);
        }

        #endregion

        #region Private-Methods

        private async Task HealthAsync(HttpContextBase ctx)
        {
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, new HealthResponse()).ConfigureAwait(false);
        }

        #endregion
    }
}
