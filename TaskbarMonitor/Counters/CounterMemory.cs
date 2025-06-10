using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterMemory: ICounter
    {
        PerformanceCounterReader reader;
        public CounterMemory(Options options)
           : base(options)
        {

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        PerformanceCounter ramCounter;        
        long totalMemory = 0;

        internal override void Initialize(PerformanceCounterReader reader)
        {
            this.reader = reader;
            reader.AddPath(@"\Memory(*)\Available MBytes");
            
            if (!GetPhysicallyInstalledSystemMemory(out totalMemory) || totalMemory == 0)
            {                
                var memStatus = new MEMORYSTATUSEX();
                if (GlobalMemoryStatusEx(memStatus))
                {
                    totalMemory = (long)(memStatus.ullTotalPhys / 1024); // in KB
                }
            }

            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = totalMemory / 1024 };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = totalMemory / 1024 });
            }           
        }

        public override void Update()
        {
            float currentRead = reader.Values.Where(x => x.Key.StartsWith(@"\Memory(*)\Available MBytes")).Sum(x => x.Value);
            float currentValue = (totalMemory / 1024) - currentRead;

            lock (ThreadLock)
            {
                InfoSummary.CurrentValue = currentValue;
                InfoSummary.History.Add(currentValue);
                if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);

                InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue / 1024).ToString("0.0") + "GB";

                {
                    var info = Infos.Where(x => x.Name == "U").Single();
                    info.CurrentValue = currentValue;
                    info.History.Add(currentValue);
                    if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                    info.CurrentStringValue = (info.CurrentValue / 1024).ToString("0.0") + "GB";
                }

            }
            

        }
       

        public override string GetName()
        {
            return "MEM";
        }

        public override CounterType GetCounterType()
        {
            return CounterType.SINGLE;
        }
    }
}
