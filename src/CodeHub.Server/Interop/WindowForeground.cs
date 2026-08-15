namespace CodeHub.Server.Interop
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Windows-only helper that pulls a newly-launched window to the foreground.
    ///
    /// When the CodeHub server (a background process) launches Explorer or a terminal,
    /// Windows' focus-stealing prevention opens the window behind the foreground app and
    /// just flashes the taskbar. This uses the AttachThreadInput bypass together with a
    /// zeroed foreground-lock timeout to force the new window forward — which works even
    /// though the caller does not own the foreground.
    ///
    /// All members are safe to reference on non-Windows targets; they must only be
    /// invoked after an OSPlatform.Windows check (the P/Invokes resolve at call time).
    /// </summary>
    internal static class WindowForeground
    {
        #region Constants

        private const int SW_RESTORE = 9;
        private const uint SPI_SETFOREGROUNDLOCKTIMEOUT = 0x2001;
        private const uint SPIF_SENDCHANGE = 0x02;

        #endregion

        #region Interop

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool BringWindowToTop(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] private static extern bool SystemParametersInfo(uint action, uint param, IntPtr pvParam, uint winIni);

        #endregion

        #region Public-Methods

        /// <summary>
        /// Snapshot the set of top-level, visible, titled window handles that currently exist.
        /// Take this immediately before launching so a new window can be identified afterward.
        /// </summary>
        /// <returns>Set of window handles.</returns>
        public static HashSet<IntPtr> Snapshot()
        {
            HashSet<IntPtr> set = new HashSet<IntPtr>();
            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0) set.Add(hWnd);
                return true;
            }, IntPtr.Zero);
            return set;
        }

        /// <summary>
        /// Poll (on a background task) for a top-level window that appeared after <paramref name="before"/>
        /// and pull it to the foreground. Fire-and-forget so it never blocks the caller.
        /// </summary>
        /// <param name="before">Window snapshot taken before the launch.</param>
        /// <param name="timeoutMs">How long to wait for the new window to appear.</param>
        public static void BringNewWindowToForegroundAsync(HashSet<IntPtr> before, int timeoutMs)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    int waited = 0;
                    while (waited < timeoutMs)
                    {
                        IntPtr found = IntPtr.Zero;
                        EnumWindows((hWnd, lParam) =>
                        {
                            if (!before.Contains(hWnd) && IsWindowVisible(hWnd) && GetWindowTextLength(hWnd) > 0)
                            {
                                found = hWnd;
                                return false; // stop enumerating
                            }
                            return true;
                        }, IntPtr.Zero);

                        if (found != IntPtr.Zero)
                        {
                            ForceForeground(found);
                            return;
                        }

                        Thread.Sleep(100);
                        waited += 100;
                    }
                }
                catch
                {
                    // Best-effort: never let a focus attempt crash anything.
                }
            });
        }

        #endregion

        #region Private-Methods

        private static void ForceForeground(IntPtr hWnd)
        {
            try
            {
                // Disable the foreground lock so SetForegroundWindow is honored.
                SystemParametersInfo(SPI_SETFOREGROUNDLOCKTIMEOUT, 0, IntPtr.Zero, SPIF_SENDCHANGE);

                uint foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
                uint currentThread = GetCurrentThreadId();

                bool attached = foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);
                ShowWindow(hWnd, SW_RESTORE);
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);
                if (attached) AttachThreadInput(currentThread, foregroundThread, false);
            }
            catch
            {
                // Best-effort only.
            }
        }

        #endregion
    }
}
