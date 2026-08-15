namespace CodeHub.Server.Routes
{
    using System;
    using System.Threading.Tasks;
    using CodeHub.Core.Serialization;
    using WatsonWebserver.Core;

    /// <summary>
    /// Shared helpers for route handlers.
    /// </summary>
    public static class RouteHelper
    {
        #region Public-Methods

        /// <summary>
        /// Serialize and send a JSON response with a status code.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="statusCode">HTTP status code.</param>
        /// <param name="obj">Payload.</param>
        public static async Task SendJson(HttpContextBase ctx, Serializer serializer, int statusCode, object obj)
        {
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            string body = serializer.SerializeJson(obj, true);
            await ctx.Response.Send(body, ctx.Token).ConfigureAwait(false);
        }

        /// <summary>
        /// Read a query string value, or null.
        /// </summary>
        public static string Query(HttpContextBase ctx, string name)
        {
            try
            {
                return ctx.Request.Query?.Elements?[name];
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Read an integer query value, or the default.
        /// </summary>
        public static int QueryInt(HttpContextBase ctx, string name, int defaultValue)
        {
            string value = Query(ctx, name);
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out int parsed)) return parsed;
            return defaultValue;
        }

        /// <summary>
        /// Read a nullable integer query value.
        /// </summary>
        public static int? QueryNullableInt(HttpContextBase ctx, string name)
        {
            string value = Query(ctx, name);
            if (!String.IsNullOrEmpty(value) && Int32.TryParse(value, out int parsed)) return parsed;
            return null;
        }

        /// <summary>
        /// Read a UTC datetime query value.
        /// </summary>
        public static DateTime? QueryDate(HttpContextBase ctx, string name)
        {
            string value = Query(ctx, name);
            if (!String.IsNullOrEmpty(value) && DateTime.TryParse(value, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal,
                out DateTime parsed))
            {
                return parsed;
            }
            return null;
        }

        #endregion
    }
}
