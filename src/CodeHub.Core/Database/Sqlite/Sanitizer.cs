namespace CodeHub.Core.Database.Sqlite
{
    using System;

    /// <summary>
    /// SQL literal sanitization helpers for handwritten SQLite statements.
    /// </summary>
    public static class Sanitizer
    {
        #region Public-Methods

        /// <summary>
        /// Escape a string for use inside a single-quoted SQL literal, or return NULL.
        /// </summary>
        /// <param name="value">Value to escape.</param>
        /// <returns>Quoted, escaped literal or the token NULL.</returns>
        public static string Quote(string value)
        {
            if (value == null) return "NULL";
            return "'" + value.Replace("'", "''") + "'";
        }

        /// <summary>
        /// Render a boolean as a SQLite integer literal.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <returns>"1" or "0".</returns>
        public static string Bool(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>
        /// Render a nullable UTC timestamp as an ISO 8601 SQL literal or NULL.
        /// </summary>
        /// <param name="value">Timestamp.</param>
        /// <returns>Quoted ISO literal or NULL.</returns>
        public static string Timestamp(DateTime? value)
        {
            if (!value.HasValue) return "NULL";
            return Quote(value.Value.ToUniversalTime().ToString("o"));
        }

        /// <summary>
        /// Render a UTC timestamp as an ISO 8601 SQL literal.
        /// </summary>
        /// <param name="value">Timestamp.</param>
        /// <returns>Quoted ISO literal.</returns>
        public static string Timestamp(DateTime value)
        {
            return Quote(value.ToUniversalTime().ToString("o"));
        }

        #endregion
    }
}
