namespace CodeHub.Server.Services
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;
    using CodeHub.Core.Serialization;
    using CodeHub.Core.Settings;
    using CodeHub.Core.Responses;
    using CodeHub.Server.Security;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Static-key authentication. Validates a bearer key from the settings JSON.
    /// </summary>
    public class AuthenticationService
    {
        #region Private-Members

        private readonly AuthSettings _Settings;
        private readonly Serializer _Serializer;
        private readonly LoggingModule _Logging;
        private readonly string _Header = "[Auth] ";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Auth settings.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="logging">Logging module.</param>
        public AuthenticationService(AuthSettings settings, Serializer serializer, LoggingModule logging)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Watson AuthenticateRequest hook. Establishes a RequestContext or rejects with 401.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public async Task AuthenticateRequestAsync(HttpContextBase ctx)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));

            // The dashboard bundle is public static content; API calls it makes carry the bearer token.
            string rawPath = ctx.Request.Url.RawWithoutQuery ?? String.Empty;
            if (rawPath.StartsWith("/dashboard", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string presented = ExtractKey(ctx);
            if (!String.IsNullOrEmpty(presented) && FixedTimeEquals(presented, _Settings.ApiKey))
            {
                ctx.Metadata = new RequestContext { IsAuthenticated = true };
                return;
            }

            _Logging.Warn(_Header + "unauthorized request to " + ctx.Request.Url.RawWithoutQuery);
            ctx.Response.StatusCode = 401;
            ctx.Response.ContentType = "application/json";
            string body = _Serializer.SerializeJson(new ErrorResponse("Unauthorized", "Authentication required."), true);
            await ctx.Response.Send(body, ctx.Token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private static string ExtractKey(HttpContextBase ctx)
        {
            string auth = ctx.Request.RetrieveHeaderValue("Authorization");
            if (!String.IsNullOrEmpty(auth))
            {
                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    return auth.Substring(7).Trim();
                return auth.Trim();
            }
            string token = ctx.Request.RetrieveHeaderValue("x-api-key");
            return token;
        }

        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            byte[] ab = Encoding.UTF8.GetBytes(a);
            byte[] bb = Encoding.UTF8.GetBytes(b);
            if (ab.Length != bb.Length) return false;
            return CryptographicOperations.FixedTimeEquals(ab, bb);
        }

        #endregion
    }
}
