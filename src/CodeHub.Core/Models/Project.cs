namespace CodeHub.Core.Models
{
    using System;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// A discrete project discovered within a repository (a .csproj, package.json, etc.).
    /// </summary>
    public class Project
    {
        #region Public-Members

        /// <summary>
        /// Project identifier (prefix "prj_").
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
        /// Absolute path to the project manifest.
        /// </summary>
        public string Path { get; set; } = String.Empty;

        /// <summary>
        /// Path relative to the repository root.
        /// </summary>
        public string RelativePath { get; set; } = String.Empty;

        /// <summary>
        /// Project name.
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Project type.
        /// </summary>
        public ProjectTypeEnum Type { get; set; } = ProjectTypeEnum.Unknown;

        /// <summary>
        /// Declared version, if any.
        /// </summary>
        public string Version { get; set; } = null;

        /// <summary>
        /// Target framework (for .NET projects).
        /// </summary>
        public string TargetFramework { get; set; } = null;

        /// <summary>
        /// Whether the project is a web service (Watson 7 host detected).
        /// </summary>
        public bool IsWebService { get; set; } = false;

        /// <summary>
        /// Whether the project is a test project.
        /// </summary>
        public bool IsTestProject { get; set; } = false;

        /// <summary>
        /// Whether the project uses Touchstone.
        /// </summary>
        public bool HasTouchstone { get; set; } = false;

        /// <summary>
        /// Whether the project references Watson 7.
        /// </summary>
        public bool HasWatson7 { get; set; } = false;

        /// <summary>
        /// Whether the project wires Radiant telemetry.
        /// </summary>
        public bool HasRadiant { get; set; } = false;

        /// <summary>
        /// Count of outdated dependencies in this project.
        /// </summary>
        public int OutdatedCount { get; set; } = 0;

        /// <summary>
        /// Count of vulnerable dependencies in this project.
        /// </summary>
        public int VulnerableCount { get; set; } = 0;

        /// <summary>
        /// UTC creation timestamp.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Id = IdGenerator.GenerateProjectId();

        #endregion
    }
}
