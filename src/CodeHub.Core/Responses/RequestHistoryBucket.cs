namespace CodeHub.Core.Responses
{
    using System;

    /// <summary>
    /// A single time bucket in a request activity summary.
    /// </summary>
    public class RequestHistoryBucket
    {
        #region Public-Members

        /// <summary>
        /// Bucket start (inclusive).
        /// </summary>
        public DateTime BucketStartUtc { get; set; }

        /// <summary>
        /// Bucket end (exclusive).
        /// </summary>
        public DateTime BucketEndUtc { get; set; }

        /// <summary>
        /// Successful request count.
        /// </summary>
        public int SuccessCount { get; set; } = 0;

        /// <summary>
        /// Failed request count.
        /// </summary>
        public int FailureCount { get; set; } = 0;

        /// <summary>
        /// Average duration in milliseconds for the bucket.
        /// </summary>
        public double AverageDurationMs { get; set; } = 0;

        #endregion
    }
}
