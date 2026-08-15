namespace CodeHub.Core.Enums
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// The type of a discrete project discovered within a repository.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ProjectTypeEnum
    {
        /// <summary>
        /// A .NET C# project (.csproj).
        /// </summary>
        CSharp,

        /// <summary>
        /// A Node.js project (package.json).
        /// </summary>
        Node,

        /// <summary>
        /// A Python project (pyproject.toml, requirements.txt, setup.py).
        /// </summary>
        Python,

        /// <summary>
        /// A PowerShell module (.psd1, .psm1).
        /// </summary>
        PowerShell,

        /// <summary>
        /// An unrecognized project type.
        /// </summary>
        Unknown
    }
}
