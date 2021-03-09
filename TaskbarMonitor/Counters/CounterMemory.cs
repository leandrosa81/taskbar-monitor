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
        long totalMemory = 0;
        //Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        public override void Initialize()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            GetPhysicallyInstalledSystemMemory(out totalMemory);

            InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = totalMemory / 1024 };
            Infos = new List<CounterInfo>();
            Infos.Add(new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = totalMemory / 1024 });
            /*
            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = totalMemory / 1024 }
            });*/
        }
        public override void Update()
        {
            float currentValue = (totalMemory / 1024) - ramCounter.NextValue();

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
