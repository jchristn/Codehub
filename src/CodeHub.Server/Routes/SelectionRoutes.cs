namespace CodeHub.Server.Routes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CodeHub.Core.Models;
    using CodeHub.Core.Requests;
    using CodeHub.Core.Responses;
    using CodeHub.Core.Services;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// Sandboxed filesystem browse and scan-selection routes for the directory picker.
    /// </summary>
    public class SelectionRoutes
    {
        #region Private-Members

        private readonly ServiceContext _Ctx;
        private static readonly HashSet<string> _SkipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "bin", "obj", "node_modules", "dist", ".git", ".vs", "packages", "TestResults"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">Service context.</param>
        public SelectionRoutes(ServiceContext ctx)
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
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/fs/browse", BrowseAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/api/scan/selection", ListSelectionAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.POST, "/v1.0/api/scan/selection", SetSelectionAsync);
            server.Routes.PostAuthentication.Static.Add(HttpMethod.POST, "/v1.0/api/scan/selection/bulk", AddBulkSelectionAsync);
        }

        #endregion

        #region Private-Methods

        private async Task BrowseAsync(HttpContextBase ctx)
        {
            // The path is base64url-encoded in the query to survive query-string handling
            // (Watson does not URL-decode query values, and raw paths contain '/', '\\', ':').
            string path = DecodePath(RouteHelper.Query(ctx, "path"));
            List<string> rootPaths = _Ctx.Settings.Directories.RootPaths ?? new List<string>();
            SelectionSets sets = await _Ctx.Selection.GetSetsAsync(ctx.Token).ConfigureAwait(false);

            BrowseResponse response = new BrowseResponse();

            List<string> targets;
            if (String.IsNullOrEmpty(path))
            {
                // Top level: the configured base roots.
                response.IsRoot = true;
                targets = rootPaths.Where(Directory.Exists).ToList();
            }
            else
            {
                string normalized = SelectionService.Normalize(path);
                if (!IsWithinRoots(normalized, rootPaths))
                {
                    await RouteHelper.SendJson(ctx, _Ctx.Serializer, 403,
                        new ErrorResponse("Forbidden", "Path is outside the configured root directories.")).ConfigureAwait(false);
                    return;
                }
                if (!Directory.Exists(normalized))
                {
                    await RouteHelper.SendJson(ctx, _Ctx.Serializer, 404,
                        new ErrorResponse("NotFound", "Directory not found.")).ConfigureAwait(false);
                    return;
                }
                response.Path = normalized;
                targets = SafeGetDirectories(normalized)
                    .Where(d => { string n = Path.GetFileName(d); return !_SkipDirs.Contains(n) && !n.StartsWith("."); })
                    .ToList();
            }

            foreach (string dir in targets.OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
            {
                response.Children.Add(BuildNode(dir, sets));
            }

            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, response).ConfigureAwait(false);
        }

        private async Task ListSelectionAsync(HttpContextBase ctx)
        {
            List<ScanSelection> selections = await _Ctx.Db.Selections.EnumerateAsync(ctx.Token).ConfigureAwait(false);
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, selections).ConfigureAwait(false);
        }

        private async Task SetSelectionAsync(HttpContextBase ctx)
        {
            string body = ctx.Request.DataAsString;
            SelectionRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<SelectionRequest>(body);
            if (request == null || String.IsNullOrEmpty(request.Path))
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400,
                    new ErrorResponse("BadRequest", "A path is required.")).ConfigureAwait(false);
                return;
            }

            string normalized = SelectionService.Normalize(request.Path);
            if (!IsWithinRoots(normalized, _Ctx.Settings.Directories.RootPaths ?? new List<string>()))
            {
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 403,
                    new ErrorResponse("Forbidden", "Path is outside the configured root directories.")).ConfigureAwait(false);
                return;
            }

            if (request.Selected) await _Ctx.Selection.SelectAsync(normalized, ctx.Token).ConfigureAwait(false);
            else await _Ctx.Selection.DeselectAsync(normalized, ctx.Token).ConfigureAwait(false);

            SelectionSets sets = await _Ctx.Selection.GetSetsAsync(ctx.Token).ConfigureAwait(false);
            Dictionary<string, object> result = new Dictionary<string, object>
            {
                { "path", normalized },
                { "state", SelectionService.StateFor(normalized, sets).ToString() }
            };
            await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, result).ConfigureAwait(false);
        }

        private async Task AddBulkSelectionAsync(HttpContextBase ctx)
        {
            try
            {
                string body = ctx.Request.DataAsString;
                BulkSelectionRequest request = String.IsNullOrEmpty(body) ? null : _Ctx.Serializer.DeserializeJson<BulkSelectionRequest>(body);
                List<string> rawPaths = request?.Paths ?? new List<string>();
                List<string> rootPaths = _Ctx.Settings.Directories.RootPaths ?? new List<string>();

                HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                List<string> valid = new List<string>();
                int ignored = 0;

                foreach (string raw in rawPaths)
                {
                    if (String.IsNullOrWhiteSpace(raw)) continue; // ignore empty lines

                    string normalized = SelectionService.Normalize(raw);
                    if (String.IsNullOrEmpty(normalized) || !seen.Add(normalized)) // dedupe (case-insensitive)
                    {
                        if (!String.IsNullOrEmpty(normalized)) ignored++;
                        continue;
                    }

                    if (!IsWithinRoots(normalized, rootPaths) || !Directory.Exists(normalized))
                    {
                        ignored++; // ignore bad directories (outside roots or non-existent)
                        continue;
                    }

                    valid.Add(normalized);
                }

                foreach (string path in valid)
                    await _Ctx.Selection.SelectAsync(path, ctx.Token).ConfigureAwait(false);

                Dictionary<string, object> result = new Dictionary<string, object>
                {
                    { "added", valid.Count },
                    { "ignored", ignored }
                };
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 200, result).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Ctx.Logging.Warn("[Selection] bulk add failed: " + e.Message);
                await RouteHelper.SendJson(ctx, _Ctx.Serializer, 400,
                    new ErrorResponse("BadRequest", "Could not process the directory list.")).ConfigureAwait(false);
            }
        }

        private BrowseNode BuildNode(string dir, SelectionSets sets)
        {
            return new BrowseNode
            {
                Name = Path.GetFileName(dir.TrimEnd('\\', '/')),
                Path = SelectionService.Normalize(dir),
                IsGitRepository = Directory.Exists(Path.Combine(dir, ".git")),
                HasSubdirectories = HasScannableSubdir(dir),
                State = SelectionService.StateFor(dir, sets).ToString()
            };
        }

        private bool HasScannableSubdir(string dir)
        {
            foreach (string sub in SafeGetDirectories(dir))
            {
                string name = Path.GetFileName(sub);
                if (!_SkipDirs.Contains(name) && !name.StartsWith(".")) return true;
            }
            return false;
        }

        private static string DecodePath(string value)
        {
            if (String.IsNullOrEmpty(value)) return null;
            try
            {
                string s = value.Replace('-', '+').Replace('_', '/');
                switch (s.Length % 4)
                {
                    case 2: s += "=="; break;
                    case 3: s += "="; break;
                }
                byte[] bytes = Convert.FromBase64String(s);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch (Exception)
            {
                // Not base64 (e.g. a raw path from a manual call) — use as-is.
                return value;
            }
        }

        private static bool IsWithinRoots(string path, List<string> rootPaths)
        {
            foreach (string root in rootPaths)
            {
                if (SelectionService.PathEquals(path, root) || SelectionService.IsStrictlyUnder(path, root)) return true;
            }
            return false;
        }

        private static IEnumerable<string> SafeGetDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        #endregion
    }
}
