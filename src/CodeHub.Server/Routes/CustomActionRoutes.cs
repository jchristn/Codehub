namespace CodeHub.Server.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// CRUD routes for user-defined custom actions.
    /// </summary>
    public class CustomActionRoutes
    {
        #region Private-Members

        private static readonly HashSet<string> _Agents = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "claude", "codex", "mux", "opencode"
        };

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public CustomActionRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/custom-actions", ListAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.POST, "/v1.0/api/custom-actions", CreateAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/api/custom-actions/{id}", UpdateAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/api/custom-actions/{id}", DeleteAsync);
        }

        #endregion

        #region Private-Methods

        private async Task ListAsync(HttpContextBase ctx)
        {
            List<CustomAction> actions = await _Ctx.Db.CustomActions.EnumerateAsync(ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, actions).ConfigureAwait(false);
        }

        private async Task CreateAsync(HttpContextBase ctx)
        {
            CustomActionRequest request = Parse(ctx);
            string error = Validate(request);
            if (error != null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", error)).ConfigureAwait(false);
                return;
            }

            CustomAction action = new CustomAction
            {
                Name = request.Name.Trim(),
                Agent = request.Agent.Trim().ToLowerInvariant(),
                Dangerous = request.Dangerous,
                Prompt = request.Prompt ?? String.Empty
            };
            await _Ctx.Db.CustomActions.UpsertAsync(action, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 201, action).ConfigureAwait(false);
        }

        private async Task UpdateAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            CustomAction existing = await _Ctx.Db.CustomActions.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (existing == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Custom action not found.")).ConfigureAwait(false);
                return;
            }

            CustomActionRequest request = Parse(ctx);
            string error = Validate(request);
            if (error != null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", error)).ConfigureAwait(false);
                return;
            }

            existing.Name = request.Name.Trim();
            existing.Agent = request.Agent.Trim().ToLowerInvariant();
            existing.Dangerous = request.Dangerous;
            existing.Prompt = request.Prompt ?? String.Empty;
            await _Ctx.Db.CustomActions.UpsertAsync(existing, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, existing).ConfigureAwait(false);
        }

        private async Task DeleteAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            await _Ctx.Db.CustomActions.DeleteAsync(id, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, new Dictionary<string, object> { { "deleted", true } }).ConfigureAwait(false);
        }

        private CustomActionRequest Parse(HttpContextBase ctx)
        {
            string body = ctx.Request.DataAsString;
            return String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<CustomActionRequest>(body);
        }

        private string Validate(CustomActionRequest request)
        {
            if (request == null) return "A request body is required.";
            if (String.IsNullOrWhiteSpace(request.Name)) return "A name is required.";
            if (String.IsNullOrWhiteSpace(request.Agent) || !_Agents.Contains(request.Agent.Trim()))
                return "Agent must be one of: claude, codex, mux, opencode.";
            return null;
        }

        #endregion
    }
}
