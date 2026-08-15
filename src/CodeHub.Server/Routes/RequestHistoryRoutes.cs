namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Request-history routes backing the dashboard's Request History view.
    /// </summary>
    public class RequestHistoryRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public RequestHistoryRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/request-history", ListAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/request-history/summary", SummaryAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/api/request-history/{id}", DetailAsync);
            server.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/api/request-history/{id}", DeleteAsync);
        }

        #endregion

        #region Private-Methods

        private async Task ListAsync(HttpContextBase ctx)
        {
            RequestHistoryFilter filter = BuildFilter(ctx);
            RequestHistoryPage page = await _Ctx.Db.RequestHistory.EnumerateAsync(filter, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, page).ConfigureAwait(false);
        }

        private async Task SummaryAsync(HttpContextBase ctx)
        {
            RequestHistoryFilter filter = BuildFilter(ctx);
            filter.BucketMinutes = RouteHelper.QueryInt(ctx, "bucketMinutes", 15);
            RequestHistorySummary summary = await _Ctx.Db.RequestHistory.SummarizeAsync(filter, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, summary).ConfigureAwait(false);
        }

        private async Task DetailAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            RequestHistoryEntry entry = await _Ctx.Db.RequestHistory.ReadAsync(id, ctx.Token).ConfigureAwait(false);
            if (entry == null)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404, new ErrorResponse("NotFound", "Entry not found.")).ConfigureAwait(false);
                return;
            }
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, entry).ConfigureAwait(false);
        }

        private async Task DeleteAsync(HttpContextBase ctx)
        {
            string id = ctx.Request.Url.Parameters["id"];
            await _Ctx.Db.RequestHistory.DeleteAsync(id, ctx.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send(ctx.Token).ConfigureAwait(false);
        }

        private static RequestHistoryFilter BuildFilter(HttpContextBase ctx)
        {
            RequestHistoryFilter filter = new RequestHistoryFilter
            {
                Method = RouteHelper.Query(ctx, "method"),
                PathContains = RouteHelper.Query(ctx, "pathContains"),
                StatusCode = RouteHelper.QueryNullableInt(ctx, "statusCode"),
                FromUtc = RouteHelper.QueryDate(ctx, "fromUtc"),
                ToUtc = RouteHelper.QueryDate(ctx, "toUtc"),
                PageNumber = RouteHelper.QueryInt(ctx, "pageNumber", 1),
                PageSize = RouteHelper.QueryInt(ctx, "pageSize", 25)
            };
            return filter;
        }

        #endregion
    }
}
