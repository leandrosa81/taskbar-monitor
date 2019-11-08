using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SystemWatchBand.Counters
{
    class CounterMemory: ICounter
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetPhysicallyInstalledSystemMemory(out long TotalMemoryInKilobytes);

        PerformanceCounter ramCounter;
        float currentValue = 0;
        long totalMemory = 0;
        List<float> history = new List<float>();

        public override void Initialize()
        {
            ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            
            GetPhysicallyInstalledSystemMemory(out totalMemory);
        }
        public override void Update()
        {
            currentValue = (totalMemory / 1024) - ramCounter.NextValue();
            history.Add(currentValue);
            if (history.Count > 40) history.RemoveAt(0);

        }

        public override List<float> GetValues(out float current, out float max, out string representation)
        {
            max = totalMemory / 1024;
            current = currentValue;
            representation = (current / 1024).ToString("0.0") + "GB";
            return history;
        }

        public override string GetName()
        {
            return "MEM";
        }
    }
}
