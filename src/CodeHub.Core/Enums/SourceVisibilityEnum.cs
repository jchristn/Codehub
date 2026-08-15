namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Whether a repository is open or closed source.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SourceVisibilityEnum
    {
        /// <summary>
        /// Publicly visible open-source repository.
        /// </summary>
        Open,

        /// <summary>
        /// Private or closed-source repository.
        /// </summary>
        Closed,

        /// <summary>
        /// Visibility could not be determined.
        /// </summary>
        Unknown
    }
}
