namespace CodeHub.Core.Responses
{
    using System.Collections.Generic;

    /// <summary>
    /// Time-bucketed request activity summary.
    /// </summary>
    public class RequestHistorySummary
    {
        #region Public-Members

        /// <summary>
        /// Total requests in range.
        /// </summary>
        public int TotalCount { get; set; } = 0;

        /// <summary>
        /// Total successful requests (status &lt; 400).
        /// </summary>
        public int TotalSuccess { get; set; } = 0;

        /// <summary>
        /// Total failed requests (status &gt;= 400).
        /// </summary>
        public int TotalFailure { get; set; } = 0;

        /// <summary>
        /// Average duration in milliseconds.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;

        /// <summary>
        /// Buckets across the range, including empty ones.
        /// </summary>
        public List<RequestHistoryBucket> Buckets { get; set; } = new List<RequestHistoryBucket>();

        #endregion
    }
}
