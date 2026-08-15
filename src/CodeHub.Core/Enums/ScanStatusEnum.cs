namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Lifecycle status of a scan run.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ScanStatusEnum
    {
        /// <summary>
        /// The scan is in progress.
        /// </summary>
        Running,

        /// <summary>
        /// The scan finished successfully.
        /// </summary>
        Completed,

        /// <summary>
        /// The scan ended with an error.
        /// </summary>
        Failed
    }
}
