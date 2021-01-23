using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterNetwork: ICounter
    {
        public CounterNetwork(Options options)
           : base(options)
        {

        }
        List<PerformanceCounter> netCounters;        

        float currentValue = 0;
        long totalIO = 1;

        Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        public override void Initialize()
        {
            PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");
            string[] instances = pcg.GetInstanceNames();
            //string instance = pcg.GetInstanceNames()[0];
            //PerformanceCounter pcsent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
            //PerformanceCounter pcreceived = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);

            netCounters = new List<PerformanceCounter>();
            foreach (var instance in instances)
            {
                netCounters.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
                netCounters.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
            }

            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>() }
            });
        }
        public override void Update()
        {
            currentValue = 0;
            foreach (var netCounter in netCounters)
            {
                currentValue += netCounter.NextValue();
            }
            //netReadCounter.NextValue() + netWriteCounter.NextValue();


            //if (currentValue > totalIO)
            //totalIO = Convert.ToInt64(currentValue);

            info[GetCounterType()][0].CurrentValue = currentValue;
            info[GetCounterType()][0].History.Add(currentValue);
            if (info[GetCounterType()][0].History.Count > 40) info[GetCounterType()][0].History.RemoveAt(0);
            totalIO = Convert.ToInt64(info[GetCounterType()][0].History.Max()) + 1;

        }

        public override List<CounterInfo> GetValues()
        {
            info[GetCounterType()][0].MaximumValue = totalIO;
            if (info[GetCounterType()][0].CurrentValue > (1024 * 1024))
                info[GetCounterType()][0].StringValue = (info[GetCounterType()][0].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            else
                info[GetCounterType()][0].StringValue = (info[GetCounterType()][0].CurrentValue / 1024).ToString("0.0") + "KB/s";

            return info[GetCounterType()];
        }

        public override string GetName()
        {
            return "NET";
        }

        public override CounterType GetCounterType()
        {
            return CounterType.SINGLE;
        }
    }
}
