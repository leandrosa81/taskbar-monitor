using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemWatchBand.Counters
{
    class CounterCPU: ICounter
    {
        PerformanceCounter cpuCounter;
        float currentValue = 0;
        List<float> history = new List<float>();

        public override void Initialize()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }
        public override void Update()
        {
            currentValue = cpuCounter.NextValue();
            history.Add(currentValue);
            if (history.Count > 30) history.RemoveAt(0);

        }

        public override List<float> GetValues(out float current, out float max, out string representation)
        {
            max = 100.0f;
            current = currentValue;
            representation = current.ToString("0") + "%";
            return history;
        }
    }
}
