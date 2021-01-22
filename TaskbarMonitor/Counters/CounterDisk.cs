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
        PerformanceCounter diskReadCounter;
        PerformanceCounter diskWriteCounter;

        float currentValue = 0;
        long totalIO = 1;
        List<float> history = new List<float>();

        public override void Initialize()
        {
            diskReadCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
            diskWriteCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");            
        }
        public override void Update()
        {
            currentValue = diskReadCounter.NextValue() + diskWriteCounter.NextValue();
            
            //if (currentValue > totalIO)
                //totalIO = Convert.ToInt64(currentValue);

            history.Add(currentValue);
            if (history.Count > 40) history.RemoveAt(0);
            totalIO = Convert.ToInt64(history.Max()) + 1;

        }

        public override List<float> GetValues(out float current, out float max, out string representation)
        {
            max = totalIO;
            current = currentValue;
            representation = (current / 1024 / 1024).ToString("0.0") + "MB/s";
            return history;
        }

        public override string GetName()
        {
            return "DISK";
        }
    }
}
