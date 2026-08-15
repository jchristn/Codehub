namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// How far a dependency is behind its latest available version.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DriftLevelEnum
    {
        /// <summary>
        /// Up to date.
        /// </summary>
        None,

        /// <summary>
        /// A newer patch version is available.
        /// </summary>
        Patch,

        /// <summary>
        /// A newer minor version is available.
        /// </summary>
        Minor,

        /// <summary>
        /// A newer major version is available.
        /// </summary>
        Major
    }
}
