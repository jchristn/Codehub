namespace CodeHub.Server.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Token validation route.
    /// </summary>
    public class AuthRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public AuthRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/token", ValidateAsync);
        }

        #endregion

        #region Private-Methods

        private async Task ValidateAsync(HttpContextBase ctx)
        {
            Dictionary<string, object> body = new Dictionary<string, object> { { "authenticated", true } };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, body).ConfigureAwait(false);
        }

        #endregion
    }
}
