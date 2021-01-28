using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterCPU : ICounter
    {
        PerformanceCounter cpuCounter;
        List<PerformanceCounter> cpuCounterCores;
        float currentValue = 0;
        
        Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();
        public CounterCPU(Options options)
            : base(options)
        {

        }

        public override void Initialize()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Processor");
            var instances = cat.GetInstanceNames();
            
            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = 100.0f }
            });

            info.Add(CounterType.STACKED, new List<CounterInfo> {
                
            });
            cpuCounterCores = new List<PerformanceCounter>();
            foreach (var item in instances.OrderBy(x => x))
            {
                if (item.ToLower().Contains("_total")) continue;

                info[CounterType.STACKED].Add(new CounterInfo() { Name = item, History = new List<float>(), MaximumValue = 100.0f });
                cpuCounterCores.Add(new PerformanceCounter("Processor", "% Processor Time", item));
            }
             
        }
        public override void Update()
        {
            currentValue = cpuCounter.NextValue();
            info[CounterType.SINGLE][0].CurrentValue = currentValue;
            info[CounterType.SINGLE][0].History.Add(currentValue);
            if (info[CounterType.SINGLE][0].History.Count > Options.HistorySize) info[CounterType.SINGLE][0].History.RemoveAt(0);            
            info[CounterType.SINGLE][0].StringValue = info[CounterType.SINGLE][0].CurrentValue.ToString("0") + "%";

            foreach (var item in cpuCounterCores)
            {
                var ct = info[CounterType.STACKED].Find(x => x.Name == item.InstanceName);
                ct.CurrentValue = item.NextValue();                
                ct.History.Add(ct.CurrentValue);
                if (ct.History.Count > Options.HistorySize) ct.History.RemoveAt(0);

                ct.StringValue = info[CounterType.SINGLE][0].StringValue;// same string value from SINGLE
            }

        }
        public override List<CounterInfo> GetValues()
        {
             
            return info[GetCounterType()];
        }
       

        public override string GetName()
        {
            return "CPU";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["CPU"].GraphType;//
        }
    }
}
