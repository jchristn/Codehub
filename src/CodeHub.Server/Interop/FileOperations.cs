namespace CodeHub.Server.Interop
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using CodeHub.Core.Helpers;

    /// <summary>
    /// Directory deletion that can send to the Recycle Bin / Trash (undoable) or delete permanently.
    /// On Windows this goes through the shell, handling read-only files (e.g. the .git object store)
    /// that a plain Directory.Delete chokes on. On macOS the Trash is reached through the trash
    /// command (or Finder); on Linux through gio or trash-cli.
    /// </summary>
    internal static class FileOperations
    {
        #region Constants

        private const uint FO_DELETE = 0x0003;
        private const ushort FOF_SILENT = 0x0004;
        private const ushort FOF_NOCONFIRMATION = 0x0010;
        private const ushort FOF_ALLOWUNDO = 0x0040;   // send to Recycle Bin
        private const ushort FOF_NOERRORUI = 0x0400;

        #endregion

        #region Interop

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            public string pFrom;
            public string pTo;
            public ushort fFlags;
            public int fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Delete a directory. When <paramref name="recycle"/> is true the directory is sent to the
        /// Recycle Bin / Trash; otherwise it is deleted permanently.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="recycle">True to send to the Recycle Bin / Trash, false to delete permanently.</param>
        public static void DeleteDirectory(string path, bool recycle)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullPath = Path.GetFullPath(path);
            if (OperatingSystem.IsWindows()) DeleteWindows(fullPath, recycle);
            else if (!recycle) Directory.Delete(fullPath, true);
            else if (OperatingSystem.IsMacOS()) TrashMac(fullPath);
            else TrashLinux(fullPath);
        }

        #endregion

        #region Private-Methods

        private static void DeleteWindows(string fullPath, bool recycle)
        {

            ushort flags = (ushort)(FOF_NOCONFIRMATION | FOF_NOERRORUI | FOF_SILENT);
            if (recycle) flags |= FOF_ALLOWUNDO;

            SHFILEOPSTRUCT op = new SHFILEOPSTRUCT
            {
                wFunc = FO_DELETE,
                pFrom = fullPath + "\0\0", // pFrom must be double-null terminated
                fFlags = flags
            };

            int result = SHFileOperation(ref op);
            if (result != 0)
                throw new IOException("Shell delete failed with code " + result + " for path: " + fullPath);
            if (op.fAnyOperationsAborted != 0)
                throw new IOException("The delete operation was aborted for path: " + fullPath);
        }

        private static void TrashMac(string fullPath)
        {
            // macOS 15+ ships /usr/bin/trash; older releases go through Finder.
            if (File.Exists("/usr/bin/trash")) Run("/usr/bin/trash", fullPath);
            else Run("/usr/bin/osascript", "-e",
                "tell application \"Finder\" to delete (POSIX file " + LaunchHelper.AppleScriptQuote(fullPath) + ")");
        }

        private static void TrashLinux(string fullPath)
        {
            string gio = LaunchHelper.FindOnPath("gio");
            if (gio != null)
            {
                Run(gio, "trash", fullPath);
                return;
            }

            string trashPut = LaunchHelper.FindOnPath("trash-put");
            if (trashPut != null)
            {
                Run(trashPut, fullPath);
                return;
            }

            throw new NotSupportedException(
                "No trash utility (gio or trash-put) was found on the CodeHub server. Install one, or delete permanently.");
        }

        private static void Run(string fileName, params string[] args)
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string arg in args) info.ArgumentList.Add(arg);

            using (Process process = Process.Start(info))
            {
                process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(60000))
                {
                    try { process.Kill(); } catch (Exception) { }
                    throw new IOException(Path.GetFileName(fileName) + " timed out.");
                }
                if (process.ExitCode != 0)
                    throw new IOException(Path.GetFileName(fileName) + " failed (exit " + process.ExitCode + "): " + error.Trim());
            }
        }

        #endregion
    }
}
