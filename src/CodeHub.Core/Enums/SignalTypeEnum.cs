namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// The category of a repository health signal.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SignalTypeEnum
    {
        /// <summary>
        /// Automated test infrastructure (Touchstone for C#).
        /// </summary>
        TestInfra,

        /// <summary>
        /// Telemetry/metrics exposure (Radiant + Watson 7 for web services).
        /// </summary>
        Telemetry,

        /// <summary>
        /// Outdated package dependencies.
        /// </summary>
        OutdatedDependencies,

        /// <summary>
        /// Known vulnerabilities and Dependabot alerts.
        /// </summary>
        Vulnerabilities,

        /// <summary>
        /// Open issues and pull requests.
        /// </summary>
        IssuesAndPullRequests
    }
}
