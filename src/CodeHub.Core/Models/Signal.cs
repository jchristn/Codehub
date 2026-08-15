namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A computed health signal for a repository, including the evidence behind its status.
    /// </summary>
    public class Signal
    {
        #region Public-Members

        /// <summary>
        /// Signal identifier (prefix "sig_").
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
        /// Owning repository identifier.
        /// </summary>
        public string RepositoryId { get; set; } = String.Empty;

        /// <summary>
        /// Signal category.
        /// </summary>
        public SignalTypeEnum SignalType { get; set; } = SignalTypeEnum.TestInfra;

        /// <summary>
        /// Traffic-light status.
        /// </summary>
        public HealthStatusEnum Status { get; set; } = HealthStatusEnum.Unknown;

        /// <summary>
        /// Human-readable evidence for the status (shown in the tooltip and detail modal).
        /// </summary>
        public string Detail { get; set; } = String.Empty;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateSignalId();

        #endregion
    }
}
