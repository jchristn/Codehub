namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Globalization;
    using CodeHub.Core.Enums;

    /// <summary>
    /// Computes semantic-version drift between a current and latest version.
    /// </summary>
    public static class DriftCalculator
    {
        #region Public-Methods

        /// <summary>
        /// Compute the drift level from the current version to the latest.
        /// </summary>
        /// <param name="current">Current version string.</param>
        /// <param name="latest">Latest version string.</param>
        /// <returns>Drift level.</returns>
        public static DriftLevelEnum Compute(string current, string latest)
        {
            if (String.IsNullOrWhiteSpace(current) || String.IsNullOrWhiteSpace(latest)) return DriftLevelEnum.None;
            if (current.Equals(latest, StringComparison.OrdinalIgnoreCase)) return DriftLevelEnum.None;

            int[] c = Parse(current);
            int[] l = Parse(latest);

            if (l[0] > c[0]) return DriftLevelEnum.Major;
            if (l[0] < c[0]) return DriftLevelEnum.None;
            if (l[1] > c[1]) return DriftLevelEnum.Minor;
            if (l[1] < c[1]) return DriftLevelEnum.None;
            if (l[2] > c[2]) return DriftLevelEnum.Patch;
            return DriftLevelEnum.None;
        }

        #endregion

        #region Private-Methods

        private static int[] Parse(string version)
        {
            int[] parts = new int[] { 0, 0, 0 };
            if (String.IsNullOrEmpty(version)) return parts;

            // Strip prerelease/build metadata.
            int dash = version.IndexOf('-');
            if (dash >= 0) version = version.Substring(0, dash);
            int plus = version.IndexOf('+');
            if (plus >= 0) version = version.Substring(0, plus);

            string[] segments = version.Split('.');
            for (int i = 0; i < 3 && i < segments.Length; i++)
            {
                if (Int32.TryParse(segments[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    parts[i] = value;
            }
            return parts;
        }

        #endregion
    }
}
