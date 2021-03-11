using System;
using System.Runtime.InteropServices;
using Com = System.Runtime.InteropServices.ComTypes;

namespace TaskbarMonitorInstaller.BLL
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RM_UNIQUE_PROCESS
    {
        public int dwProcessId;
        public Com.FILETIME ProcessStartTime;
    }

    [Flags]
    internal enum RM_SHUTDOWN_TYPE : uint
    {
        RmForceShutdown = 0x1,
        RmShutdownOnlyRegistered = 0x10
    }

    ///
    /// Consts defined in WINBASE.H
    ///
    [Flags]
    internal enum MoveFileFlags
    {
        MOVEFILE_REPLACE_EXISTING = 1,
        MOVEFILE_COPY_ALLOWED = 2,
        MOVEFILE_DELAY_UNTIL_REBOOT = 4, //This value can be used only if the process is in the context of a user who belongs to the administrators group or the LocalSystem account
        MOVEFILE_WRITE_THROUGH = 8
    }

    internal delegate void RM_WRITE_STATUS_CALLBACK(UInt32 nPercentComplete);

    internal class Win32Api
    {
        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        protected static extern int RmStartSession(out IntPtr pSessionHandle, int dwSessionFlags, string strSessionKey);

        [DllImport("rstrtmgr.dll")]
        protected static extern int RmEndSession(IntPtr pSessionHandle);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        protected static extern int RmRegisterResources(IntPtr pSessionHandle, UInt32 nFiles, string[] rgsFilenames, UInt32 nApplications, RM_UNIQUE_PROCESS[] rgApplications, UInt32 nServices, string[] rgsServiceNames);

        [DllImport("rstrtmgr.dll")]
        protected static extern int RmShutdown(IntPtr pSessionHandle, RM_SHUTDOWN_TYPE lActionFlags, RM_WRITE_STATUS_CALLBACK fnStatus);

        [DllImport("rstrtmgr.dll")]
        protected static extern int RmRestart(IntPtr pSessionHandle, int dwRestartFlags, RM_WRITE_STATUS_CALLBACK fnStatus);

        [DllImport("kernel32.dll")]
        protected static extern bool GetProcessTimes(IntPtr hProcess, out Com.FILETIME lpCreationTime, out Com.FILETIME lpExitTime, out Com.FILETIME lpKernelTime, out Com.FILETIME lpUserTime);




        /// <summary>
        /// Marks the file for deletion during next system reboot
        /// </summary>
        /// <param name="lpExistingFileName">The current name of the file or directory on the local computer.</param>
        /// <param name="lpNewFileName">The new name of the file or directory on the local computer.</param>
        /// <param name="dwFlags">MoveFileFlags</param>
        /// <returns>bool</returns>
        /// <remarks>http://msdn.microsoft.com/en-us/library/aa365240(VS.85).aspx</remarks>
        [System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "MoveFileEx")]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName,
        MoveFileFlags dwFlags);

        [System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "DeleteFile")]
        internal static extern bool DeleteFile(
  string lpFileName
);


    }
}
