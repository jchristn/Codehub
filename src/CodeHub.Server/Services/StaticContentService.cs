namespace CodeHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Serves the built React dashboard from a local directory at /dashboard, with SPA fallback
    /// (unknown sub-paths return index.html so client-side routing works on deep links/refresh).
    /// </summary>
    public class StaticContentService
    {
        #region Public-Members

        /// <summary>
        /// Whether a dashboard build was found and is being served.
        /// </summary>
        public bool IsAvailable
        {
            get { return _Root != null; }
        }

        /// <summary>
        /// The resolved dashboard root directory, or null.
        /// </summary>
        public string Root
        {
            get { return _Root; }
        }

        #endregion

        #region Private-Members

        private readonly string _Root;
        private readonly LoggingModule _Logging;

        private static readonly Dictionary<string, string> _ContentTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".html", "text/html; charset=utf-8" },
            { ".js", "text/javascript; charset=utf-8" },
            { ".mjs", "text/javascript; charset=utf-8" },
            { ".css", "text/css; charset=utf-8" },
            { ".json", "application/json; charset=utf-8" },
            { ".map", "application/json; charset=utf-8" },
            { ".svg", "image/svg+xml" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".ico", "image/x-icon" },
            { ".webp", "image/webp" },
            { ".woff", "font/woff" },
            { ".woff2", "font/woff2" },
            { ".ttf", "font/ttf" },
            { ".txt", "text/plain; charset=utf-8" }
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate, resolving the dashboard directory from the setting or by auto-detection.
        /// </summary>
        /// <param name="configuredDirectory">Configured dashboard directory (may be empty).</param>
        /// <param name="logging">Logging module.</param>
        public StaticContentService(string configuredDirectory, LoggingModule logging)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Root = Resolve(configuredDirectory);
            if (_Root != null) _Logging.Info("[Dashboard] serving from " + _Root + " at /dashboard");
            else _Logging.Warn("[Dashboard] no built dashboard found; /dashboard is disabled (run 'npm run build' in dashboard/)");
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Serve a /dashboard request from the resolved directory.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task ServeAsync(HttpContextBase ctx)
        {
            if (_Root == null)
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.ContentType = "text/plain; charset=utf-8";
                await ctx.Response.Send("Dashboard is not built. Run 'npm run build' in the dashboard directory.", ctx.Token).ConfigureAwait(false);
                return;
            }

            string rawPath = ctx.Request.Url.RawWithoutQuery ?? "/dashboard";
            string relative = rawPath.Length > "/dashboard".Length ? rawPath.Substring("/dashboard".Length) : String.Empty;
            relative = relative.TrimStart('/');

            // Reject path traversal.
            if (relative.Contains("..", StringComparison.Ordinal))
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send("Bad path.", ctx.Token).ConfigureAwait(false);
                return;
            }

            string filePath = String.IsNullOrEmpty(relative) ? null : Path.Combine(_Root, relative.Replace('/', Path.DirectorySeparatorChar));

            if (filePath != null && File.Exists(filePath))
            {
                await SendFileAsync(ctx, filePath).ConfigureAwait(false);
                return;
            }

            // SPA fallback: serve index.html for any non-file path.
            string indexPath = Path.Combine(_Root, "index.html");
            if (File.Exists(indexPath))
            {
                await SendFileAsync(ctx, indexPath).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 404;
            await ctx.Response.Send("Not found.", ctx.Token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private async Task SendFileAsync(HttpContextBase ctx, string filePath)
        {
            byte[] bytes = await File.ReadAllBytesAsync(filePath, ctx.Token).ConfigureAwait(false);
            string ext = Path.GetExtension(filePath);
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = _ContentTypes.TryGetValue(ext, out string type) ? type : "application/octet-stream";

            // Cache fingerprinted build assets aggressively; never cache index.html.
            if (rawIsAsset(ctx))
                ctx.Response.Headers.Add("Cache-Control", "public, max-age=31536000, immutable");
            else
                ctx.Response.Headers.Add("Cache-Control", "no-cache");

            await ctx.Response.Send(bytes, ctx.Token).ConfigureAwait(false);
        }

        private static bool rawIsAsset(HttpContextBase ctx)
        {
            string p = ctx.Request.Url.RawWithoutQuery ?? String.Empty;
            return p.StartsWith("/dashboard/assets/", StringComparison.OrdinalIgnoreCase);
        }

        private string Resolve(string configured)
        {
            if (!String.IsNullOrWhiteSpace(configured) && HasIndex(configured)) return Path.GetFullPath(configured);

            List<string> candidates = new List<string>();
            string baseDir = AppContext.BaseDirectory;
            candidates.Add(Path.Combine(baseDir, "wwwroot"));
            candidates.Add(Path.Combine(baseDir, "dashboard"));

            // Walk up from the app base directory looking for dashboard/dist (covers the dev layout).
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                candidates.Add(Path.Combine(dir.FullName, "dashboard", "dist"));
                dir = dir.Parent;
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, "dashboard", "dist"));
            candidates.Add(Path.Combine(Environment.CurrentDirectory, "..", "dashboard", "dist"));

            foreach (string candidate in candidates)
            {
                if (HasIndex(candidate)) return Path.GetFullPath(candidate);
            }
            return null;
        }

        private static bool HasIndex(string dir)
        {
            try
            {
                return !String.IsNullOrEmpty(dir) && File.Exists(Path.Combine(dir, "index.html"));
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion
    }
}
