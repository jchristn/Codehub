namespace CodeHub.Server.Interop
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Windows-only filesystem operations that go through the shell, so a directory can be sent
    /// to the Recycle Bin (undoable) or deleted permanently — handling read-only files (e.g. the
    /// .git object store) that a plain Directory.Delete chokes on.
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
        /// Recycle Bin; otherwise it is deleted permanently.
        /// </summary>
        /// <param name="path">Directory path.</param>
        /// <param name="recycle">True to send to the Recycle Bin, false to delete permanently.</param>
        public static void DeleteDirectory(string path, bool recycle)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new NotSupportedException(
                    "Deleting a repository from disk is only supported when the CodeHub server runs on Windows.");

            string fullPath = Path.GetFullPath(path);

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

        #endregion
    }
}
