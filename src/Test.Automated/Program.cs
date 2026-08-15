namespace Test.Automated
{
    using System;
    using System.Threading.Tasks;
    using Test.Shared;
    using Touchstone.Cli;

    /// <summary>
    /// Console runner for the CodeHub Touchstone suites.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Arguments; supports "--results &lt;path&gt;".</param>
        /// <returns>Exit code (0 = all passed).</returns>
        public static async Task<int> Main(string[] args)
        {
            string resultsPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--results" && i + 1 < args.Length)
                {
                    resultsPath = args[i + 1];
                    break;
                }
            }

            return await ConsoleRunner.RunAsync(CodeHubSuites.All, resultsPath: resultsPath).ConfigureAwait(false);
        }
    }
}
