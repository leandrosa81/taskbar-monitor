using System;
using System.Collections.Generic;
using System.Diagnostics;
using Com = System.Runtime.InteropServices.ComTypes;

namespace TaskbarMonitorInstaller.BLL
{
    class RestartExplorer : Win32Api
    {
        public event Action<string> ReportProgress;
        public event Action<uint> ReportPercentage;
        public void Execute() => Execute(() => { });
        public void Execute(Action action)
        {
            IntPtr handle;
            string key = Guid.NewGuid().ToString();

            int res = RmStartSession(out handle, 0, key);
            if (res == 0)
            {
                ReportProgress?.Invoke($"Restart Manager session created with ID {key}");

                RM_UNIQUE_PROCESS[] processes = GetProcesses("explorer");
                res = RmRegisterResources(
                    handle,
                    0, null,
                    (uint)processes.Length, processes,
                    0, null
                );
                if (res == 0)
                {
                    ReportProgress?.Invoke("Successfully registered resources.");

                    res = RmShutdown(handle, RM_SHUTDOWN_TYPE.RmForceShutdown, (percent) => ReportPercentage?.Invoke(percent));
                    if (res == 0)
                    {
                        ReportProgress?.Invoke("Applications stopped successfully.\n");
                        action();

                        res = RmRestart(handle, 0, (percent)=>ReportPercentage?.Invoke(percent));
                        if (res == 0)
                            ReportProgress?.Invoke("Applications restarted successfully.");
                    }
                }
                res = RmEndSession(handle);
                if (res == 0)
                    ReportProgress?.Invoke("Restart Manager session ended.");

            }
        }

        private RM_UNIQUE_PROCESS[] GetProcesses(string name)
        {
            List<RM_UNIQUE_PROCESS> lst = new List<RM_UNIQUE_PROCESS>();
            foreach (Process p in Process.GetProcessesByName(name))
            {
                RM_UNIQUE_PROCESS rp = new RM_UNIQUE_PROCESS();
                rp.dwProcessId = p.Id;
                Com.FILETIME creationTime, exitTime, kernelTime, userTime;
                GetProcessTimes(p.Handle, out creationTime, out exitTime, out kernelTime, out userTime);
                rp.ProcessStartTime = creationTime;
                lst.Add(rp);
            }
            return lst.ToArray();
        }
    }
}
