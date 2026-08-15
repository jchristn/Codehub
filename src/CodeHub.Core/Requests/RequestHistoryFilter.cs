namespace CodeHub.Core.Requests
{
    using System;

    /// <summary>
    /// Filter for querying request history.
    /// </summary>
    public class RequestHistoryFilter
    {
        #region Public-Members

        /// <summary>
        /// HTTP method filter.
        /// </summary>
        public string Method { get; set; } = null;

        /// <summary>
        /// Exact status code filter.
        /// </summary>
        public int? StatusCode { get; set; } = null;

        /// <summary>
        /// Substring path filter.
        /// </summary>
        public string PathContains { get; set; } = null;

        /// <summary>
        /// Lower time bound (inclusive).
        /// </summary>
        public DateTime? FromUtc { get; set; } = null;

        /// <summary>
        /// Upper time bound (inclusive).
        /// </summary>
        public DateTime? ToUtc { get; set; } = null;

        /// <summary>
        /// Page number (1-based).
        /// </summary>
        public int PageNumber
        {
            get
            {
                return _PageNumber;
            }
            set
            {
                _PageNumber = Math.Max(1, value);
            }
        }

        /// <summary>
        /// Page size.
        /// </summary>
        public int PageSize
        {
            get
            {
                return _PageSize;
            }
            set
            {
                _PageSize = Math.Clamp(value, 1, 1000);
            }
        }

        /// <summary>
        /// Bucket size in minutes for the summary call.
        /// </summary>
        public int BucketMinutes
        {
            get
            {
                return _BucketMinutes;
            }
            set
            {
                _BucketMinutes = Math.Clamp(value, 1, 1440);
            }
        }

        #endregion

        #region Private-Members

        private int _PageNumber = 1;
        private int _PageSize = 25;
        private int _BucketMinutes = 15;

        #endregion
    }
}
