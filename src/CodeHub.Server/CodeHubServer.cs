namespace CodeHub.Server
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Server.Routes;
    using CodeHub.Server.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;

    /// <summary>
    /// Watson 7 server host for CodeHub.
    /// </summary>
    public class CodeHubServer
    {
        #region Public-Members

        /// <summary>
        /// The underlying Watson webserver.
        /// </summary>
        public Webserver Server { get; }

        #endregion

        #region Private-Members

        private readonly ServiceContext _Ctx;
        private readonly AuthenticationService _Auth;
        private readonly RequestHistoryCaptureService _Capture;
        private readonly LoggingModule _Logging;
        private readonly string _Header = "[CodeHubServer] ";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        /// <param name="auth">Authentication service.</param>
        /// <param name="capture">Request history capture service.</param>
        public CodeHubServer(ServiceContext ctx, AuthenticationService auth, RequestHistoryCaptureService capture)
        {
            _Ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _Auth = auth ?? throw new ArgumentNullException(nameof(auth));
            _Capture = capture ?? throw new ArgumentNullException(nameof(capture));
            _Logging = ctx.Logging;

            WebserverSettings settings = new WebserverSettings(_Ctx.Settings.Webserver.Hostname, _Ctx.Settings.Webserver.Port);
            settings.Ssl.Enable = _Ctx.Settings.Webserver.Ssl;

            if (_Ctx.Settings.Cors.Enable)
            {
                settings.Headers.DefaultHeaders["Access-Control-Allow-Origin"] = _Ctx.Settings.Cors.AllowOrigin;
                settings.Headers.DefaultHeaders["Access-Control-Allow-Methods"] = _Ctx.Settings.Cors.AllowMethods;
                settings.Headers.DefaultHeaders["Access-Control-Allow-Headers"] = _Ctx.Settings.Cors.AllowHeaders;
            }

            Server = new Webserver(settings, DefaultRouteAsync);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Start the server.
        /// </summary>
        public void Start()
        {
            ConfigureServer();
            ConfigureRoutes();
            Server.Start();
            _Logging.Info(_Header + "listening on " + _Ctx.Settings.Webserver.Hostname + ":" + _Ctx.Settings.Webserver.Port);
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            Server.Stop();
        }

        #endregion

        #region Private-Methods

        private void ConfigureServer()
        {
            Server.Routes.AuthenticateRequest = _Auth.AuthenticateRequestAsync;
            Server.Routes.Preflight = PreflightAsync;
            Server.Routes.PostRouting = PostRoutingAsync;

            if (_Ctx.Settings.Webserver.EnableOpenApi)
            {
                try
                {
                    Server.UseOpenApi(openApi =>
                    {
                        openApi.Info.Title = "CodeHub API";
                        openApi.Info.Version = Core.Constants.SoftwareVersion;
                        openApi.Info.Description = "Local code-tree health inventory API.";
                        openApi.DocumentPath = "/openapi.json";
                        openApi.EnableSwaggerUi = false;
                    });
                }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "failed to enable OpenAPI: " + e.Message);
                }
            }
        }

        private void ConfigureRoutes()
        {
            new HealthRoutes(_Ctx).Register(Server);
            new AuthRoutes(_Ctx).Register(Server);
            new RepositoryRoutes(_Ctx).Register(Server);
            new ProjectRoutes(_Ctx).Register(Server);
            new ScanRoutes(_Ctx).Register(Server);
            new SelectionRoutes(_Ctx).Register(Server);
            new SettingsRoutes(_Ctx).Register(Server);
            new RequestHistoryRoutes(_Ctx).Register(Server);
        }

        private static async Task PreflightAsync(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send(ctx.Token).ConfigureAwait(false);
        }

        private async Task PostRoutingAsync(HttpContextBase ctx)
        {
            ctx.Timestamp.End = DateTime.UtcNow;

            _Logging.Debug(_Header +
                ctx.Request.Method.ToString() + " " +
                ctx.Request.Url.RawWithoutQuery + " " +
                ctx.Response.StatusCode + " (" +
                (ctx.Timestamp.TotalMs.HasValue ? ctx.Timestamp.TotalMs.Value.ToString("F2") : "?") + "ms)");

            if (_Ctx.Settings.RequestHistory.Enabled)
            {
                _Capture.Capture(ctx);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

        private async Task DefaultRouteAsync(HttpContextBase ctx)
        {
            string rawPath = ctx.Request.Url.RawWithoutQuery ?? String.Empty;
            if (rawPath.Equals("/dashboard", StringComparison.OrdinalIgnoreCase) ||
                rawPath.StartsWith("/dashboard/", StringComparison.OrdinalIgnoreCase))
            {
                await _Ctx.StaticContent.ServeAsync(ctx).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 404;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.Send("{\"code\":\"NotFound\",\"message\":\"No matching route.\"}", ctx.Token).ConfigureAwait(false);
        }

        #endregion
    }
}
