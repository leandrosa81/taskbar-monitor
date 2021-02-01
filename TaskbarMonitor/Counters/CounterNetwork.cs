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
    class CounterNetwork : ICounter
    {
        public CounterNetwork(Options options)
           : base(options)
        {

        }
        List<PerformanceCounter> netCountersSent;
        List<PerformanceCounter> netCountersReceived;
         
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
                new CounterInfo() { Name = "D", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = 1 }

            });
            info.Add(CounterType.STACKED, new List<CounterInfo> {
                new CounterInfo() { Name = "D", History = new List<float>(), MaximumValue = 1 },
                new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = 1 }

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

                if (info[counterType][index].CurrentValue > (1024 * 1024))
                    info[counterType][index].CurrentStringValue = (info[counterType][index].CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
                else
                    info[counterType][index].CurrentStringValue = (info[counterType][index].CurrentValue / 1024).ToString("0.0") + "KB/s";
            };

            float currentSent = 0;
            float currentReceived = 0;
            foreach (var netCounter in netCountersSent)
            {
                currentSent += netCounter.NextValue();                
            }
            foreach (var netCounter in netCountersReceived)
            {
                currentReceived += netCounter.NextValue();
            }
            addValue(CounterType.SINGLE, 0, currentSent + currentReceived);

            addValue(CounterType.MIRRORED, 0, currentReceived);
            addValue(CounterType.MIRRORED, 1, currentSent);

            addValue(CounterType.STACKED, 0, currentReceived);
            addValue(CounterType.STACKED, 1, currentSent);

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
            return "NET";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["NET"].GraphType;//
        }
    }
}
