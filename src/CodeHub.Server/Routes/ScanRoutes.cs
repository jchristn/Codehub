namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Scan trigger, status, and history routes.
    /// </summary>
    public class ScanRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public ScanRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.POST, "/v1.0/api/scan", TriggerAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/scan/status", StatusAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/scan/runs", RunsAsync);
        }

        #endregion

        #region Private-Methods

        private async Task TriggerAsync(HttpContextBase ctx)
        {
            if (_Ctx.Scan.IsScanning)
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 409,
                    new ErrorResponse("ScanInProgress", "A scan is already running.")).ConfigureAwait(false);
                return;
            }

            string repoId = null;
            string body = ctx.Request.DataAsString;
            if (!String.IsNullOrEmpty(body))
            {
                ScanRequest request = _Ctx.Serializer.DeserializeJson<ScanRequest>(body);
                repoId = request?.RepositoryId;
            }

            // Fire-and-forget the scan; return the run record immediately.
            string capturedRepoId = repoId;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _Ctx.Scan.RunAsync(ScanTriggerEnum.Manual, capturedRepoId).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Ctx.Logging.Warn("[ScanRoutes] scan failed: " + e.Message);
                }
            });

            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 202,
                new { status = "started", repositoryId = capturedRepoId }).ConfigureAwait(false);
        }

        private async Task StatusAsync(HttpContextBase ctx)
        {
            ScanRun latest = await _Ctx.Db.ScanRuns.ReadLatestAsync(ctx.Token).ConfigureAwait(false);
            int repoCount = (await _Ctx.Db.Repositories.EnumerateAsync(ctx.Token).ConfigureAwait(false)).Count;

            ScanStatusResponse response = new ScanStatusResponse
            {
                IsScanning = _Ctx.Scan.IsScanning,
                Current = _Ctx.Scan.CurrentRun,
                Latest = latest,
                LastScannedUtc = latest?.CompletedUtc,
                NextScanUtc = _Ctx.Scan.NextScheduledScanUtc,
                RepositoryCount = repoCount,
                Repositories = _Ctx.Scan.CurrentRepositories
            };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
        }

        private async Task RunsAsync(HttpContextBase ctx)
        {
            int limit = RouteHelper.QueryInt(ctx, "limit", 50);
            System.Collections.Generic.List<ScanRun> runs = await _Ctx.Db.ScanRuns.EnumerateAsync(limit, ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, runs).ConfigureAwait(false);
        }

        #endregion
    }
}
