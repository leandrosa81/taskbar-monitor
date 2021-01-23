using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterDisk: ICounter
    {
        public CounterDisk(Options options)
           : base(options)
        {

        }
        PerformanceCounter diskReadCounter;
        PerformanceCounter diskWriteCounter;

        float currentValue = 0;        
        Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        public override void Initialize()
        {
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");

            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = 1 }
            });

            info.Add(CounterType.MIRRORED, new List<CounterInfo> {
                new CounterInfo() { Name = "R", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "W", History = new List<float>(), MaximumValue = 1 }
            });             
        }
        public override void Update()
        {            
            if (Options.DiskSingleView)
            {
                currentValue = diskReadCounter.NextValue() + diskWriteCounter.NextValue();
                info[GetCounterType()][0].CurrentValue = currentValue;
                info[GetCounterType()][0].History.Add(currentValue);
                if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);
                info[GetCounterType()][0].MaximumValue = Convert.ToInt64(info[GetCounterType()][0].History.Max()) + 1;                
            }
            else
            {
                info[GetCounterType()][0].CurrentValue = diskReadCounter.NextValue();
                info[GetCounterType()][0].History.Add(info[GetCounterType()][0].CurrentValue);
                if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);
                info[GetCounterType()][0].MaximumValue = Convert.ToInt64(info[GetCounterType()][0].History.Max()) + 1;

                info[GetCounterType()][1].CurrentValue = diskWriteCounter.NextValue();
                info[GetCounterType()][1].History.Add(info[GetCounterType()][1].CurrentValue);
                if (info[GetCounterType()][1].History.Count > Options.HistorySize) info[GetCounterType()][1].History.RemoveAt(0);
                info[GetCounterType()][1].MaximumValue = Convert.ToInt64(info[GetCounterType()][1].History.Max()) + 1;

                // if locks down same scale for both counters is on
                float max = info[GetCounterType()][0].MaximumValue > info[GetCounterType()][1].MaximumValue ? info[GetCounterType()][0].MaximumValue : info[GetCounterType()][1].MaximumValue;
                info[GetCounterType()][0].MaximumValue = info[GetCounterType()][1].MaximumValue = max;
            }
            
            

        }

        public override List<CounterInfo> GetValues()
        {                        
            info[GetCounterType()][0].StringValue = (info[GetCounterType()][0].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            if (!Options.DiskSingleView)
            {
                info[GetCounterType()][1].StringValue = (info[GetCounterType()][1].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            }

            return info[GetCounterType()];
        }
         
        public override string GetName()
        {
            return "DISK";
        }

        public override CounterType GetCounterType()
        {            
            return Options.DiskSingleView ? CounterType.SINGLE : CounterType.MIRRORED;
        }
    }
}
