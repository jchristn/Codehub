namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core;
    using CodeHub.Core.Responses;
    using CodeHub.Core.Settings;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Redacted settings route.
    /// </summary>
    public class SettingsRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public SettingsRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/settings", GetAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/settings/editable", GetEditableAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.PUT, "/v1.0/api/settings", UpdateAsync);
        }

        #endregion

        #region Private-Methods

        private async Task GetAsync(HttpContextBase ctx)
        {
            CodeHub.Core.Services.SelectionSets sets = await _Ctx.Selection.GetSetsAsync(ctx.Token).ConfigureAwait(false);

            SettingsResponse response = new SettingsResponse
            {
                RootPaths = _Ctx.Settings.Directories.RootPaths,
                SelectedCount = sets.Included.Count,
                ExcludedCount = sets.Excluded.Count,
                IntervalHours = _Ctx.Settings.Scan.IntervalHours,
                ScanOnStartup = _Ctx.Settings.Scan.ScanOnStartup,
                MaxConcurrency = _Ctx.Settings.Scan.MaxConcurrency,
                DependencyCheck = _Ctx.Settings.Scan.DependencyCheck,
                GitHubConfigured = _Ctx.Settings.GitHub.IsConfigured(),
                GitHubOwner = _Ctx.Settings.GitHub.Owner,
                DatabaseType = _Ctx.Settings.Database.Type.ToString(),
                Hostname = _Ctx.Settings.Webserver.Hostname,
                Port = _Ctx.Settings.Webserver.Port,
                RequestHistoryEnabled = _Ctx.Settings.RequestHistory.Enabled,
                Version = Constants.SoftwareVersion
            };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
        }

        private async Task GetEditableAsync(HttpContextBase ctx)
        {
            // Full settings for the editable form (local single-operator model).
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, _Ctx.Settings).ConfigureAwait(false);
        }

        private async Task UpdateAsync(HttpContextBase ctx)
        {
            string body = ctx.Request.DataAsString;
            if (String.IsNullOrEmpty(body))
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "A settings body is required.")).ConfigureAwait(false);
                return;
            }

            try
            {
                CodeHubSettings incoming = _Ctx.Serializer.DeserializeJson<CodeHubSettings>(body);
                if (incoming == null)
                {
                    await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400, new ErrorResponse("BadRequest", "Invalid settings body.")).ConfigureAwait(false);
                    return;
                }

                _Ctx.SettingsWriter.ApplyAndSave(incoming);
                _Ctx.Logging.Info("[SettingsRoutes] settings updated and saved to " + _Ctx.SettingsWriter.FilePath);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, _Ctx.Settings).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[SettingsRoutes] settings update failed: " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 500, new ErrorResponse("UpdateFailed", e.Message)).ConfigureAwait(false);
            }
        }

        #endregion
    }
}
