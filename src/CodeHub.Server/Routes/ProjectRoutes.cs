namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Project detail route.
    /// </summary>
    public class ProjectRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public ProjectRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/api/projects/{id}", DetailAsync);
        }

        #endregion

        #region Private-Methods

        private async Task DetailAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            Project project = await _Ctx.Db.Projects.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (project == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Project not found.")).ConfigureAwait(false);
                return;
            }
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, project).ConfigureAwait(false);
        }

        #endregion
    }
}
