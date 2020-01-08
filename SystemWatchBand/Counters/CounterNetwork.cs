using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SystemWatchBand.Counters
{
    class CounterNetwork: ICounter
    {
        PerformanceCounter netReadCounter;
        PerformanceCounter netWriteCounter;

        float currentValue = 0;
        long totalIO = 1;
        List<float> history = new List<float>();

        public override void Initialize()
        {
            PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");
            string instance = pcg.GetInstanceNames()[0];
            //PerformanceCounter pcsent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            //PerformanceCounter pcreceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

            netReadCounter = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            netWriteCounter = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);            
        }
        public override void Update()
        {
            currentValue = netReadCounter.NextValue() + netWriteCounter.NextValue();
                                                    
            
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
            if(current > (1024 * 1024))
                representation = (current / 1024 / 1024).ToString("0.0") + "MB/s";
            else
                representation = (current / 1024).ToString("0.0") + "KB/s";
            return history;
        }

        public override string GetName()
        {
            return "NET";
        }
    }
}
