namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A record of a single scan sweep across the code tree.
    /// </summary>
    public class ScanRun
    {
        #region Public-Members

        /// <summary>
        /// Scan run identifier (prefix "scan_").
        /// </summary>
        public string Id
        {
            get
            {
                return _Id;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(Id));
                _Id = value;
            }
        }

        /// <summary>
        /// What triggered the scan.
        /// </summary>
        public ScanTriggerEnum Trigger { get; set; } = ScanTriggerEnum.Manual;

        /// <summary>
        /// Scan status.
        /// </summary>
        public ScanStatusEnum Status { get; set; } = ScanStatusEnum.Running;

        /// <summary>
        /// Number of repositories scanned so far.
        /// </summary>
        public int ReposScanned { get; set; } = 0;

        /// <summary>
        /// Total number of repositories targeted by this scan.
        /// </summary>
        public int ReposTotal { get; set; } = 0;

        /// <summary>
        /// The single repository this manual scan targeted, or null for a full scan.
        /// </summary>
        public string TargetRepository { get; set; } = null;

        /// <summary>
        /// Error message when the scan failed.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// UTC start timestamp.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC completion timestamp.
        /// </summary>
        public DateTime? CompletedUtc { get; set; } = null;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateScanRunId();

        #endregion
    }
}
