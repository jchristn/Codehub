namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Helpers;
    using CodeHub.Core.Models;

    /// <summary>
    /// Parses project manifests into Project models, applying C#-specific health detection.
    /// </summary>
    public static class ManifestAnalyzer
    {
        #region Public-Methods

        /// <summary>
        /// Analyze a .NET project file.
        /// </summary>
        /// <param name="csprojPath">Path to the .csproj.</param>
        /// <param name="repoRoot">Repository root path.</param>
        /// <returns>Project model.</returns>
        public static Project AnalyzeCsproj(string csprojPath, string repoRoot)
        {
            Project project = NewProject(csprojPath, repoRoot, ProjectTypeEnum.CSharp);
            project.Name = Path.GetFileNameWithoutExtension(csprojPath);

            string text;
            try
            {
                text = File.ReadAllText(csprojPath);
            }
            catch (Exception)
            {
                return project;
            }

            List<string> packages = new List<string>();
            try
            {
                XDocument doc = XDocument.Parse(text);
                foreach (XElement element in doc.Descendants().Where(e => e.Name.LocalName == "PackageReference"))
                {
                    string include = (string)element.Attribute("Include");
                    if (String.IsNullOrEmpty(include)) include = (string)element.Attribute("Update");
                    if (!String.IsNullOrEmpty(include)) packages.Add(include);
                }

                project.TargetFramework = FirstNonEmpty(
                    ElementValue(doc, "TargetFramework"),
                    ElementValue(doc, "TargetFrameworks"));

                project.Version = FirstNonEmpty(
                    ElementValue(doc, "Version"),
                    ElementValue(doc, "VersionPrefix"));
            }
            catch (Exception)
            {
                // fall back to regex-free defaults
            }

            bool hasTestSdk = packages.Any(p =>
                p.Equals("Microsoft.NET.Test.Sdk", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("xunit", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("NUnit", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("MSTest", StringComparison.OrdinalIgnoreCase));

            bool hasTouchstone = packages.Any(p => p.StartsWith("Touchstone", StringComparison.OrdinalIgnoreCase));
            bool hasWatson = packages.Any(p =>
                p.Equals("Watson", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("WatsonWebserver", StringComparison.OrdinalIgnoreCase));
            bool hasRadiant = packages.Any(p => p.Equals("Radiant", StringComparison.OrdinalIgnoreCase));
            bool hasAspNet = text.Contains("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase) ||
                             text.Contains("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase);

            bool nameLooksTest = project.Name.StartsWith("Test", StringComparison.OrdinalIgnoreCase) ||
                                 project.Name.Contains(".Test", StringComparison.OrdinalIgnoreCase);

            bool isTest = hasTestSdk || hasTouchstone || nameLooksTest;

            project.HasTouchstone = hasTouchstone;
            project.HasWatson7 = hasWatson;
            project.HasRadiant = hasRadiant;
            project.IsTestProject = isTest;
            // A test harness (e.g. a TCP library's Test.Server console app) is never a web service,
            // so it never counts toward the telemetry signal.
            project.IsWebService = !isTest &&
                                   (hasWatson || hasAspNet ||
                                    project.Name.EndsWith(".Server", StringComparison.OrdinalIgnoreCase));

            // Strengthen Radiant detection with a bounded source scan when the package is not referenced.
            if (!project.HasRadiant && project.IsWebService)
            {
                project.HasRadiant = SourceMentions(Path.GetDirectoryName(csprojPath), "RadiantHost");
            }

            // Telemetry (metrics/tracing) is about instrumentation of observable code paths, so it is
            // evaluated for every project, not just web services.
            bool hasTelemetryPackage = packages.Any(p =>
                p.StartsWith("OpenTelemetry", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("Radiant", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("prometheus-net", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("App.Metrics", StringComparison.OrdinalIgnoreCase) ||
                p.Equals("System.Diagnostics.DiagnosticSource", StringComparison.OrdinalIgnoreCase));

            bool hasTelemetry = hasTelemetryPackage || project.HasRadiant;
            if (!hasTelemetry && !isTest)
            {
                hasTelemetry = SourceMentionsAny(Path.GetDirectoryName(csprojPath),
                    "System.Diagnostics.Metrics", "new Meter(", "ActivitySource", "AddOpenTelemetry", "RadiantHost");
            }
            project.HasTelemetry = hasTelemetry;

            return project;
        }

        /// <summary>
        /// Analyze a Node package.json.
        /// </summary>
        /// <param name="packageJsonPath">Path to package.json.</param>
        /// <param name="repoRoot">Repository root path.</param>
        /// <returns>Project model.</returns>
        public static Project AnalyzeNode(string packageJsonPath, string repoRoot)
        {
            Project project = NewProject(packageJsonPath, repoRoot, ProjectTypeEnum.Node);
            project.Name = Path.GetFileName(Path.GetDirectoryName(packageJsonPath));

            try
            {
                string text = File.ReadAllText(packageJsonPath);
                Match nameMatch = Regex.Match(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
                if (nameMatch.Success) project.Name = nameMatch.Groups[1].Value;
                Match versionMatch = Regex.Match(text, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                if (versionMatch.Success) project.Version = versionMatch.Groups[1].Value;
            }
            catch (Exception)
            {
                // ignore
            }

            return project;
        }

        /// <summary>
        /// Analyze a Python project manifest.
        /// </summary>
        /// <param name="manifestPath">Path to pyproject.toml/setup.py/requirements.txt.</param>
        /// <param name="repoRoot">Repository root path.</param>
        /// <returns>Project model.</returns>
        public static Project AnalyzePython(string manifestPath, string repoRoot)
        {
            Project project = NewProject(manifestPath, repoRoot, ProjectTypeEnum.Python);
            project.Name = Path.GetFileName(Path.GetDirectoryName(manifestPath));

            try
            {
                if (Path.GetFileName(manifestPath).Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase))
                {
                    string text = File.ReadAllText(manifestPath);
                    Match versionMatch = Regex.Match(text, "version\\s*=\\s*\"([^\"]+)\"");
                    if (versionMatch.Success) project.Version = versionMatch.Groups[1].Value;
                    Match nameMatch = Regex.Match(text, "name\\s*=\\s*\"([^\"]+)\"");
                    if (nameMatch.Success) project.Name = nameMatch.Groups[1].Value;
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return project;
        }

        /// <summary>
        /// Analyze a PowerShell module manifest.
        /// </summary>
        /// <param name="psd1Path">Path to the .psd1.</param>
        /// <param name="repoRoot">Repository root path.</param>
        /// <returns>Project model.</returns>
        public static Project AnalyzePowerShell(string psd1Path, string repoRoot)
        {
            Project project = NewProject(psd1Path, repoRoot, ProjectTypeEnum.PowerShell);
            project.Name = Path.GetFileNameWithoutExtension(psd1Path);

            try
            {
                string text = File.ReadAllText(psd1Path);
                Match versionMatch = Regex.Match(text, "ModuleVersion\\s*=\\s*'([^']+)'");
                if (versionMatch.Success) project.Version = versionMatch.Groups[1].Value;
            }
            catch (Exception)
            {
                // ignore
            }

            return project;
        }

        #endregion

        #region Private-Methods

        private static Project NewProject(string manifestPath, string repoRoot, ProjectTypeEnum type)
        {
            Project project = new Project
            {
                Id = IdGenerator.GenerateProjectId(),
                Path = manifestPath,
                Type = type
            };
            if (!String.IsNullOrEmpty(repoRoot) && manifestPath.StartsWith(repoRoot, StringComparison.OrdinalIgnoreCase))
            {
                project.RelativePath = manifestPath.Substring(repoRoot.Length).TrimStart('\\', '/');
            }
            else
            {
                project.RelativePath = Path.GetFileName(manifestPath);
            }
            return project;
        }

        private static string ElementValue(XDocument doc, string localName)
        {
            XElement element = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
            return element?.Value;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!String.IsNullOrWhiteSpace(value)) return value.Trim();
            }
            return null;
        }

        private static bool SourceMentions(string directory, string token)
        {
            return SourceMentionsAny(directory, token);
        }

        private static bool SourceMentionsAny(string directory, params string[] tokens)
        {
            try
            {
                if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return false;
                int scanned = 0;
                foreach (string file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
                {
                    if (scanned++ > 200) break;
                    string content = File.ReadAllText(file);
                    foreach (string token in tokens)
                    {
                        if (content.Contains(token, StringComparison.Ordinal)) return true;
                    }
                }
            }
            catch (Exception)
            {
                // ignore
            }
            return false;
        }

        #endregion
    }
}
