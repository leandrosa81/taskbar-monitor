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
        float currentValue = 0;
        
        Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();
        public CounterCPU(Options options)
            : base(options)
        {

        }

        public override void Initialize()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>() }
            });            
        }
        public override void Update()
        {
            currentValue = cpuCounter.NextValue();
            info[GetCounterType()][0].CurrentValue = currentValue;
            info[GetCounterType()][0].History.Add(currentValue);
            if (info[GetCounterType()][0].History.Count > Options.HistorySize) info[GetCounterType()][0].History.RemoveAt(0);            

        }
        public override List<CounterInfo> GetValues()
        {
            info[GetCounterType()][0].MaximumValue = 100.0f;
            info[GetCounterType()][0].StringValue = info[GetCounterType()][0].CurrentValue.ToString("0") + "%";

            return info[GetCounterType()];
        }
       

        public override string GetName()
        {
            return "CPU";
        }

        public override CounterType GetCounterType()
        {
            return CounterType.SINGLE;
        }
    }
}
