namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A package dependency of a project and its freshness/vulnerability state.
    /// </summary>
    public class Dependency
    {
        #region Public-Members

        /// <summary>
        /// Dependency identifier (prefix "dep_").
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
        /// Owning project identifier.
        /// </summary>
        public string ProjectId { get; set; } = String.Empty;

        /// <summary>
        /// Owning repository identifier.
        /// </summary>
        public string RepositoryId { get; set; } = String.Empty;

        /// <summary>
        /// Package ecosystem (nuget, npm, pip).
        /// </summary>
        public string Ecosystem { get; set; } = "nuget";

        /// <summary>
        /// Package name.
        /// </summary>
        public string PackageName { get; set; } = String.Empty;

        /// <summary>
        /// Currently referenced version.
        /// </summary>
        public string CurrentVersion { get; set; } = null;

        /// <summary>
        /// Latest available version.
        /// </summary>
        public string LatestVersion { get; set; } = null;

        /// <summary>
        /// How far behind the current version is.
        /// </summary>
        public DriftLevelEnum Drift { get; set; } = DriftLevelEnum.None;

        /// <summary>
        /// Whether the referenced version is known to be vulnerable.
        /// </summary>
        public bool IsVulnerable { get; set; } = false;

        /// <summary>
        /// Vulnerability severity, when vulnerable.
        /// </summary>
        public VulnerabilitySeverityEnum Severity { get; set; } = VulnerabilitySeverityEnum.None;

        /// <summary>
        /// Advisory URL, when vulnerable.
        /// </summary>
        public string AdvisoryUrl { get; set; } = null;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateDependencyId();

        #endregion
    }
}
