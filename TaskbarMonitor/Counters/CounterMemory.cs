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

        public CounterMemory(Options options)
           : base(options)
        {

        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        PerformanceCounter ramCounter;
        float currentValue = 0;
        long totalMemory = 0;
        Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        public override void Initialize()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            GetPhysicallyInstalledSystemMemory(out totalMemory);
            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = totalMemory / 1024 }
            });
        }
        public override void Update()
        {
            currentValue = (totalMemory / 1024) - ramCounter.NextValue();
            info[GetCounterType()][0].CurrentValue = currentValue;
            info[GetCounterType()][0].History.Add(currentValue);
            if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);
            
            info[GetCounterType()][0].CurrentStringValue = (info[GetCounterType()][0].CurrentValue / 1024).ToString("0.0") + "GB";

        }
        public override List<CounterInfo> GetValues()
        {            
            return info[GetCounterType()];
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
