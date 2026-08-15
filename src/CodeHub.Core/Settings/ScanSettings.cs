namespace CodeHub.Core.Settings
{
    using System;

    /// <summary>
    /// Scan scheduling settings (JSON section "scan"). Directory selection lives in the
    /// scan_selections database table, and root directories live under "directories".
    /// </summary>
    public class ScanSettings
    {
        #region Public-Members

        /// <summary>
        /// Interval in hours between automatic scans. Zero disables the timer.
        /// </summary>
        public int IntervalHours
        {
            get
            {
                return _IntervalHours;
            }
            set
            {
                _IntervalHours = Math.Clamp(value, 0, 720);
            }
        }

        /// <summary>
        /// Whether to run a scan automatically at startup.
        /// </summary>
        public bool ScanOnStartup { get; set; } = true;

        /// <summary>
        /// Maximum concurrent per-repository collection tasks.
        /// </summary>
        public int MaxConcurrency
        {
            get
            {
                return _MaxConcurrency;
            }
            set
            {
                _MaxConcurrency = Math.Clamp(value, 1, 64);
            }
        }

        /// <summary>
        /// Whether to run the (slower) dependency freshness/vulnerability checks.
        /// </summary>
        public bool DependencyCheck { get; set; } = true;

        #endregion

        #region Private-Members

        private int _IntervalHours = 6;
        private int _MaxConcurrency = 8;

        #endregion
    }
}
