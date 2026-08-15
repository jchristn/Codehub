namespace CodeHub.Core.Services
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Result of running an external process.
    /// </summary>
    public class ProcessResult
    {
        /// <summary>
        /// Process exit code (-1 on timeout or launch failure).
        /// </summary>
        public int ExitCode { get; set; } = -1;

        /// <summary>
        /// Captured standard output.
        /// </summary>
        public string StandardOutput { get; set; } = String.Empty;

        /// <summary>
        /// Captured standard error.
        /// </summary>
        public string StandardError { get; set; } = String.Empty;

        /// <summary>
        /// Whether the process completed before the timeout.
        /// </summary>
        public bool TimedOut { get; set; } = false;

        /// <summary>
        /// Whether the process launched and exited with code 0.
        /// </summary>
        public bool Success
        {
            get { return !TimedOut && ExitCode == 0; }
        }
    }

    /// <summary>
    /// Runs external command-line tools (git, dotnet) with a timeout and captured output.
    /// </summary>
    public static class ProcessRunner
    {
        #region Public-Methods

        /// <summary>
        /// Run a command, capturing output.
        /// </summary>
        /// <param name="fileName">Executable name.</param>
        /// <param name="arguments">Arguments.</param>
        /// <param name="workingDirectory">Working directory.</param>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Process result.</returns>
        public static async Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string workingDirectory,
            int timeoutMs,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException(nameof(fileName));

            ProcessResult result = new ProcessResult();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = String.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();

            using (Process process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

                try
                {
                    if (!process.Start())
                    {
                        result.StandardError = "Failed to start process.";
                        return result;
                    }
                }
                catch (Exception e)
                {
                    result.StandardError = e.Message;
                    return result;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    cts.CancelAfter(timeoutMs);
                    try
                    {
                        await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                        result.ExitCode = process.ExitCode;
                    }
                    catch (OperationCanceledException)
                    {
                        result.TimedOut = true;
                        try
                        {
                            if (!process.HasExited) process.Kill(true);
                        }
                        catch (Exception)
                        {
                            // best effort
                        }
                    }
                }
            }

            result.StandardOutput = stdout.ToString();
            result.StandardError = stderr.ToString();
            return result;
        }

        #endregion
    }
}
