namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// What initiated a scan run.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScanTriggerEnum
    {
        /// <summary>
        /// Triggered manually from the dashboard or API.
        /// </summary>
        Manual,

        /// <summary>
        /// Triggered by the periodic timer.
        /// </summary>
        Scheduled,

        /// <summary>
        /// Triggered automatically at server startup.
        /// </summary>
        Startup
    }
}
