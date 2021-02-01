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

            info.Add(CounterType.STACKED, new List<CounterInfo> {
                new CounterInfo() { Name = "R", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "W", History = new List<float>(), MaximumValue = 1 }
            });
        }
        public override void Update()
        {
            Action<CounterType, int, float> addValue = (counterType, index, value) =>
            {
                info[counterType][index].CurrentValue = value;
                info[counterType][index].History.Add(value);
                if (info[counterType][index].History.Count > Options.HistorySize) info[counterType][index].History.RemoveAt(0);
                info[counterType][index].MaximumValue = Convert.ToInt64(info[counterType][index].History.Max()) + 1;

                info[counterType][index].CurrentStringValue = (info[counterType][index].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            };

            float currentRead = diskReadCounter.NextValue();
            float currentWritten = diskWriteCounter.NextValue();

            addValue(CounterType.SINGLE, 0, currentRead + currentWritten);

            addValue(CounterType.MIRRORED, 0, currentRead);
            addValue(CounterType.MIRRORED, 1, currentWritten);

            addValue(CounterType.STACKED, 0, currentRead);
            addValue(CounterType.STACKED, 1, currentWritten);

            // if locks down same scale for both counters is on
            //float max = info[GetCounterType()][0].MaximumValue > info[GetCounterType()][1].MaximumValue ? info[GetCounterType()][0].MaximumValue : info[GetCounterType()][1].MaximumValue;
            //info[GetCounterType()][0].MaximumValue = info[GetCounterType()][1].MaximumValue = max;

        }

        public override List<CounterInfo> GetValues()
        {                        
            
            return info[GetCounterType()];
        }
         
        public override string GetName()
        {
            return "DISK";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["DISK"].GraphType;// ? CounterType.SINGLE : CounterType.MIRRORED;
        }
    }
}
