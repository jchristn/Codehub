namespace CodeHub.Server.Routes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using CodeHub.Core.Helpers;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Routes for user-defined custom actions: CRUD plus running an action against repositories.
    /// Actions are agent-agnostic prompts; the agent is supplied when an action is run.
    /// </summary>
    public class CustomActionRoutes
    {
        #region Private-Members

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
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/api/custom-actions/{id}/run", RunAsync);
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

        private async Task RunAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            CustomAction action = await _Ctx.Db.CustomActions.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (action == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Custom action not found.")).ConfigureAwait(false);
                return;
            }

            string body = ctx.Request.DataAsString;
            RunCustomActionRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<RunCustomActionRequest>(body);
            if (request == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "A request body is required.")).ConfigureAwait(false);
                return;
            }

            string agent = AgentHelper.Normalize(request.Agent);
            if (agent == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", AgentHelper.InvalidMessage())).ConfigureAwait(false);
                return;
            }

            List<string> repositoryIds = new List<string>();
            if (request.RepositoryIds != null)
            {
                foreach (string repositoryId in request.RepositoryIds)
                {
                    if (!String.IsNullOrWhiteSpace(repositoryId) && !repositoryIds.Contains(repositoryId)) repositoryIds.Add(repositoryId);
                }
            }
            if (repositoryIds.Count == 0)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "At least one repository identifier is required.")).ConfigureAwait(false);
                return;
            }

            string prompt = request.Prompt ?? action.Prompt ?? String.Empty;
            RunCustomActionResponse response = new RunCustomActionResponse { ActionId = action.Id, Agent = agent };

            foreach (string repositoryId in repositoryIds)
            {
                RunCustomActionResult result = new RunCustomActionResult { RepositoryId = repositoryId };
                response.Results.Add(result);

                Repository repo = await _Ctx.Db.Repositories.ReadAsync(repositoryId, ctx.Token).ConfigureAwait(false);
                if (repo == null)
                {
                    result.Error = "Repository not found.";
                    response.Failed++;
                    continue;
                }
                result.RepositoryName = repo.Name;

                try
                {
                    _Ctx.Launcher.OpenAgentPrompt(agent, repo.Path, request.Dangerous, prompt);
                    result.Launched = true;
                    response.Launched++;
                }
                catch (NotSupportedException e)
                {
                    // Launching is unavailable on this host, so every repository would fail the same way.
                    await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("NotSupported", e.Message)).ConfigureAwait(false);
                    return;
                }
                catch (Exception e)
                {
                    _Ctx.Logging.Warn("[CustomActionRoutes] run " + action.Id + " failed for " + repositoryId + ": " + e.Message);
                    result.Error = e.Message;
                    response.Failed++;
                }
            }

            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
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
            return null;
        }

        #endregion
    }
}
