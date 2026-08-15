namespace CodeHub.Core.Database.Sqlite
{
    using System;
    using System.Data;
    using System.Globalization;

    /// <summary>
    /// Typed accessors for reading DataRow columns into model fields.
    /// </summary>
    public static class DataExtensions
    {
        #region Public-Methods

        /// <summary>
        /// Read a string column, or null.
        /// </summary>
        public static string GetString(this DataRow row, string column)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value) return null;
            return Convert.ToString(row[column], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read an integer column, or the supplied default.
        /// </summary>
        public static int GetInt(this DataRow row, string column, int defaultValue = 0)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value) return defaultValue;
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read a double column, or the supplied default.
        /// </summary>
        public static double GetDouble(this DataRow row, string column, double defaultValue = 0)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value) return defaultValue;
            return Convert.ToDouble(row[column], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read a long column, or the supplied default.
        /// </summary>
        public static long GetLong(this DataRow row, string column, long defaultValue = 0)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value) return defaultValue;
            return Convert.ToInt64(row[column], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Read a boolean column stored as an integer.
        /// </summary>
        public static bool GetBool(this DataRow row, string column, bool defaultValue = false)
        {
            if (!row.Table.Columns.Contains(column) || row[column] == null || row[column] == DBNull.Value) return defaultValue;
            return Convert.ToInt32(row[column], CultureInfo.InvariantCulture) != 0;
        }

        /// <summary>
        /// Read an ISO 8601 timestamp column, or null.
        /// </summary>
        public static DateTime? GetDateTime(this DataRow row, string column)
        {
            string value = row.GetString(column);
            if (String.IsNullOrEmpty(value)) return null;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
                return parsed;
            return null;
        }

        /// <summary>
        /// Read a required ISO 8601 timestamp column, defaulting to UTC now.
        /// </summary>
        public static DateTime GetDateTimeRequired(this DataRow row, string column)
        {
            DateTime? value = row.GetDateTime(column);
            return value ?? DateTime.UtcNow;
        }

        /// <summary>
        /// Read and parse an enum column, or the supplied default.
        /// </summary>
        public static T GetEnum<T>(this DataRow row, string column, T defaultValue) where T : struct
        {
            string value = row.GetString(column);
            if (String.IsNullOrEmpty(value)) return defaultValue;
            if (Enum.TryParse<T>(value, true, out T parsed)) return parsed;
            return defaultValue;
        }

        #endregion
    }
}
