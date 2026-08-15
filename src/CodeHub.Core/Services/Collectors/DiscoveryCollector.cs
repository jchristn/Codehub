namespace CodeHub.Core.Services.Collectors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using CodeHub.Core.Enums;
    using CodeHub.Core.Models;
    using CodeHub.Core.Settings;

    /// <summary>
    /// Resolves repository boundaries under the scan root and enumerates the projects in each.
    /// </summary>
    public class DiscoveryCollector
    {
        #region Private-Members

        private readonly DirectorySettings _Settings;
        private readonly HashSet<string> _Excludes;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Directory settings.</param>
        public DiscoveryCollector(DirectorySettings settings)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Excludes = new HashSet<string>(_Settings.ExcludeDirectories ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Find the repository root directories under the scan root.
        /// </summary>
        /// <returns>Absolute repository root paths.</returns>
        public List<string> FindRepositoryRoots()
        {
            List<string> roots = new List<string>();
            List<string> rootPaths = _Settings.RootPaths ?? new List<string>();

            foreach (string rootPath in rootPaths)
            {
                if (String.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath)) continue;

                foreach (string topDir in SafeGetDirectories(rootPath))
                {
                    string name = Path.GetFileName(topDir);
                    if (_Excludes.Contains(name) || name.StartsWith(".")) continue;

                    List<string> gitRoots = new List<string>();
                    CollectGitRoots(topDir, gitRoots, 0);

                    if (gitRoots.Count == 0) roots.Add(topDir);
                    else roots.AddRange(gitRoots);
                }
            }

            return roots.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Resolve repository roots from an explicit selection of directories. A selected path
        /// that is itself a configured base root is expanded to its children (scan-everything);
        /// any other selected path is treated as a scan target (its own repos, or itself).
        /// </summary>
        /// <param name="selectedPaths">Selected directory paths.</param>
        /// <returns>Absolute repository root paths.</returns>
        public List<string> FindRepositoryRootsFromSelection(IEnumerable<string> selectedPaths)
        {
            List<string> roots = new List<string>();
            if (selectedPaths == null) return roots;

            HashSet<string> baseRoots = new HashSet<string>(
                (_Settings.RootPaths ?? new List<string>()).Select(NormalizePath), StringComparer.OrdinalIgnoreCase);

            foreach (string selected in selectedPaths)
            {
                if (String.IsNullOrEmpty(selected) || !Directory.Exists(selected)) continue;
                string normalized = NormalizePath(selected);

                if (baseRoots.Contains(normalized))
                {
                    // Selecting a base root scans each of its children.
                    foreach (string child in SafeGetDirectories(selected))
                    {
                        string childName = Path.GetFileName(child);
                        if (_Excludes.Contains(childName) || childName.StartsWith(".")) continue;
                        roots.AddRange(ResolveTarget(child));
                    }
                }
                else
                {
                    roots.AddRange(ResolveTarget(selected));
                }
            }

            return roots.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>
        /// Discover a repository and its projects at the given root.
        /// </summary>
        /// <param name="repoRoot">Repository root path.</param>
        /// <returns>Discovered repository.</returns>
        public DiscoveredRepository Discover(string repoRoot)
        {
            if (String.IsNullOrEmpty(repoRoot)) throw new ArgumentNullException(nameof(repoRoot));

            Repository repository = new Repository
            {
                Path = repoRoot,
                Name = Path.GetFileName(repoRoot.TrimEnd('\\', '/')),
                IsGitRepository = Directory.Exists(Path.Combine(repoRoot, ".git"))
            };

            List<Project> projects = DiscoverProjects(repoRoot);
            repository.ProjectCount = projects.Count;
            repository.Languages = projects.Select(p => p.Type.ToString()).Distinct().ToList();
            repository.PrimaryLanguage = ResolvePrimaryLanguage(projects);
            repository.CurrentVersion = ResolveVersion(projects, repository.Name);

            DiscoveredRepository result = new DiscoveredRepository
            {
                Repository = repository,
                Projects = projects
            };
            return result;
        }

        #endregion

        #region Private-Methods

        private List<string> ResolveTarget(string dir)
        {
            List<string> gitRoots = new List<string>();
            CollectGitRoots(dir, gitRoots, 0);
            if (gitRoots.Count > 0) return gitRoots;
            return new List<string> { dir };
        }

        private static string NormalizePath(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) return path;
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\', '/');
            }
            catch (Exception)
            {
                return path.TrimEnd('\\', '/');
            }
        }

        private void CollectGitRoots(string dir, List<string> results, int depth)
        {
            if (depth > 8) return;
            string name = Path.GetFileName(dir);
            if (_Excludes.Contains(name)) return;

            if (Directory.Exists(Path.Combine(dir, ".git")))
            {
                results.Add(dir);
                return; // do not descend into a git repo looking for more roots
            }

            foreach (string sub in SafeGetDirectories(dir))
            {
                string subName = Path.GetFileName(sub);
                if (_Excludes.Contains(subName) || subName.StartsWith(".")) continue;
                CollectGitRoots(sub, results, depth + 1);
            }
        }

        private List<Project> DiscoverProjects(string repoRoot)
        {
            List<Project> projects = new List<Project>();
            HashSet<string> pythonDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string file in EnumerateManifestFiles(repoRoot))
            {
                string fileName = Path.GetFileName(file);
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (ext == ".csproj")
                {
                    projects.Add(ManifestAnalyzer.AnalyzeCsproj(file, repoRoot));
                }
                else if (fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase))
                {
                    projects.Add(ManifestAnalyzer.AnalyzeNode(file, repoRoot));
                }
                else if (fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
                         fileName.Equals("setup.py", StringComparison.OrdinalIgnoreCase))
                {
                    string dir = Path.GetDirectoryName(file);
                    if (pythonDirs.Add(dir)) projects.Add(ManifestAnalyzer.AnalyzePython(file, repoRoot));
                }
                else if (ext == ".psd1")
                {
                    projects.Add(ManifestAnalyzer.AnalyzePowerShell(file, repoRoot));
                }
            }

            return projects;
        }

        private IEnumerable<string> EnumerateManifestFiles(string root)
        {
            Stack<string> stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                string current = stack.Pop();

                string[] files;
                try
                {
                    files = Directory.GetFiles(current);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (string file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    string fileName = Path.GetFileName(file);
                    if (ext == ".csproj" || ext == ".psd1" ||
                        fileName.Equals("package.json", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
                        fileName.Equals("setup.py", StringComparison.OrdinalIgnoreCase))
                    {
                        yield return file;
                    }
                }

                foreach (string sub in SafeGetDirectories(current))
                {
                    string subName = Path.GetFileName(sub);
                    if (_Excludes.Contains(subName)) continue;
                    stack.Push(sub);
                }
            }
        }

        private static ProjectTypeEnum ResolvePrimaryLanguage(List<Project> projects)
        {
            if (projects.Count == 0) return ProjectTypeEnum.Unknown;
            if (projects.Any(p => p.Type == ProjectTypeEnum.CSharp)) return ProjectTypeEnum.CSharp;
            return projects
                .GroupBy(p => p.Type)
                .OrderByDescending(g => g.Count())
                .First().Key;
        }

        private static string ResolveVersion(List<Project> projects, string repoName)
        {
            Project named = projects.FirstOrDefault(p =>
                !String.IsNullOrEmpty(p.Version) &&
                p.Name != null &&
                p.Name.Equals(repoName, StringComparison.OrdinalIgnoreCase));
            if (named != null) return named.Version;

            Project anyVersioned = projects.FirstOrDefault(p => !String.IsNullOrEmpty(p.Version) && !p.IsTestProject);
            if (anyVersioned != null) return anyVersioned.Version;

            Project anyAtAll = projects.FirstOrDefault(p => !String.IsNullOrEmpty(p.Version));
            return anyAtAll?.Version;
        }

        private static IEnumerable<string> SafeGetDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception)
            {
                return Array.Empty<string>();
            }
        }

        #endregion
    }
}
