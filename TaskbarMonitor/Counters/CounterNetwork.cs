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
        List<PerformanceCounter> netCountersSent;
        List<PerformanceCounter> netCountersReceived;

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

            netCountersSent = new List<PerformanceCounter>();
            netCountersReceived = new List<PerformanceCounter>();
            foreach (var instance in instances)
            {
                netCountersSent.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
                netCountersReceived.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
            }

            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = 1 }
            });

            info.Add(CounterType.MIRRORED, new List<CounterInfo> {
                new CounterInfo() { Name = "RX", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "TX", History = new List<float>(), MaximumValue = 1 }
                
            });
        }
        public override void Update()
        {
            if (Options.NetworkSingleView)
            {
                currentValue = 0;
                foreach (var netCounter in netCountersSent)
                {
                    currentValue += netCounter.NextValue();
                }
                foreach (var netCounter in netCountersReceived)
                {
                    currentValue += netCounter.NextValue();
                }
                info[GetCounterType()][0].CurrentValue = currentValue;
                info[GetCounterType()][0].History.Add(currentValue);
                if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);
                info[GetCounterType()][0].MaximumValue = Convert.ToInt64(info[GetCounterType()][0].History.Max()) + 1;
            }
            else
            {
                currentValue = 0;
                foreach (var netCounter in netCountersReceived)
                {
                    currentValue += netCounter.NextValue();
                }                 
                info[GetCounterType()][0].CurrentValue = currentValue;
                info[GetCounterType()][0].History.Add(currentValue);
                if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);
                info[GetCounterType()][0].MaximumValue = Convert.ToInt64(info[GetCounterType()][0].History.Max()) + 1;

                currentValue = 0;
                foreach (var netCounter in netCountersSent)
                {
                    currentValue += netCounter.NextValue();
                }
                info[GetCounterType()][1].CurrentValue = currentValue;
                info[GetCounterType()][1].History.Add(currentValue);
                if (info[GetCounterType()][1].History.Count > Options.HistorySize) info[GetCounterType()][1].History.RemoveAt(0);
                info[GetCounterType()][1].MaximumValue = Convert.ToInt64(info[GetCounterType()][1].History.Max()) + 1;

                // if locks down same scale for both counters is on
                float max = info[GetCounterType()][0].MaximumValue > info[GetCounterType()][1].MaximumValue ? info[GetCounterType()][0].MaximumValue : info[GetCounterType()][1].MaximumValue;
                info[GetCounterType()][0].MaximumValue = info[GetCounterType()][1].MaximumValue = max;
            }

            

        }

        public override List<CounterInfo> GetValues()
        {
            if (info[GetCounterType()][0].CurrentValue > (1024 * 1024))
                info[GetCounterType()][0].StringValue = (info[GetCounterType()][0].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            else
                info[GetCounterType()][0].StringValue = (info[GetCounterType()][0].CurrentValue / 1024).ToString("0.0") + "KB/s";

            if (!Options.NetworkSingleView)
            {
                if (info[GetCounterType()][1].CurrentValue > (1024 * 1024))
                    info[GetCounterType()][1].StringValue = (info[GetCounterType()][1].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
                else
                    info[GetCounterType()][1].StringValue = (info[GetCounterType()][1].CurrentValue / 1024).ToString("0.0") + "KB/s";
            }

            

            return info[GetCounterType()];
        }

        public override string GetName()
        {
            return "NET";
        }

        public override CounterType GetCounterType()
        {
            return Options.NetworkSingleView ? CounterType.SINGLE : CounterType.MIRRORED;
        }
    }
}
