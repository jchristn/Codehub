namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Traffic-light health status for a repository signal.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HealthStatusEnum
    {
        /// <summary>
        /// Signal is healthy.
        /// </summary>
        Green,

        /// <summary>
        /// Signal needs attention but is not critical.
        /// </summary>
        Yellow,

        /// <summary>
        /// Signal is unhealthy and needs attention.
        /// </summary>
        Red,

        /// <summary>
        /// Signal does not apply to this repository.
        /// </summary>
        NotApplicable,

        /// <summary>
        /// Signal could not be evaluated.
        /// </summary>
        Unknown
    }
}
