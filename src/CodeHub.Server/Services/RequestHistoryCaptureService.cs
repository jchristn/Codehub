namespace CodeHub.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading.Tasks;
    using CodeHub.Core.Database;
    using CodeHub.Core.Models;
    using CodeHub.Core.Settings;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Captures completed requests into the request-history store on a fire-and-forget task.
    /// </summary>
    public class RequestHistoryCaptureService
    {
        #region Private-Members

        private readonly DatabaseDriverBase _Db;
        private readonly RequestHistorySettings _Settings;
        private readonly LoggingModule _Logging;

        private static readonly HashSet<string> _RedactHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization", "Proxy-Authorization", "Cookie", "Set-Cookie", "x-api-key"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="db">Database driver.</param>
        /// <param name="settings">Request history settings.</param>
        /// <param name="logging">Logging module.</param>
        public RequestHistoryCaptureService(DatabaseDriverBase db, RequestHistorySettings settings, LoggingModule logging)
        {
            _Db = db ?? throw new ArgumentNullException(nameof(db));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build a history entry synchronously and dispatch the insert without blocking the response.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public void Capture(HttpContextBase ctx)
        {
            if (ctx == null) return;

            RequestHistoryEntry entry;
            try
            {
                entry = Build(ctx);
            }
            catch (Exception)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await _Db.RequestHistory.CreateAsync(entry).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _Logging.Debug("[RequestCapture] failed to persist entry: " + e.Message);
                }
            });
        }

        #endregion

        #region Private-Methods

        private RequestHistoryEntry Build(HttpContextBase ctx)
        {
            RequestHistoryEntry entry = new RequestHistoryEntry
            {
                Method = ctx.Request.Method.ToString(),
                Path = ctx.Request.Url.RawWithoutQuery,
                Url = ctx.Request.Url.RawWithQuery,
                StatusCode = ctx.Response.StatusCode,
                CreatedUtc = ctx.Timestamp.Start,
                CompletedUtc = ctx.Timestamp.End,
                DurationMs = ctx.Timestamp.TotalMs ?? 0
            };

            try { entry.SourceIp = ctx.Request.Source.IpAddress; } catch (Exception) { /* ignore */ }

            entry.RequestHeaders = CopyHeaders(ctx.Request.Headers);
            entry.ResponseHeaders = CopyHeaders(ctx.Response.Headers);

            string requestBody = null;
            try { requestBody = ctx.Request.DataAsString; } catch (Exception) { /* ignore */ }
            if (!String.IsNullOrEmpty(requestBody))
            {
                entry.RequestBodyBytes = requestBody.Length;
                if (requestBody.Length > _Settings.MaxRequestBodyBytes)
                {
                    entry.RequestBody = requestBody.Substring(0, _Settings.MaxRequestBodyBytes);
                    entry.RequestBodyTruncated = true;
                }
                else
                {
                    entry.RequestBody = requestBody;
                }
            }

            return entry;
        }

        private static Dictionary<string, string> CopyHeaders(NameValueCollection headers)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return result;
            try
            {
                foreach (string key in headers.AllKeys)
                {
                    if (String.IsNullOrEmpty(key)) continue;
                    string value = _RedactHeaders.Contains(key) ? "***redacted***" : headers[key];
                    result[key] = value;
                }
            }
            catch (Exception)
            {
                // ignore
            }
            return result;
        }

        #endregion
    }
}
