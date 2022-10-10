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
        
        //Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();
        public CounterCPU(Options options)
            : base(options)
        {

        }

        public override void Initialize()
        {
            cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total", true);
            PerformanceCounterCategory cat = new PerformanceCounterCategory("Processor Information");
            var instances = cat.GetInstanceNames();
            /*
            info.Add(CounterType.SINGLE, new List<CounterInfo> {
                new CounterInfo() { Name = "default", History = new List<float>(), MaximumValue = 100.0f }
            });

            info.Add(CounterType.STACKED, new List<CounterInfo> {
                
            });
            
             */
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 100.0f };
                Infos = new List<CounterInfo>();
                cpuCounterCores = new List<PerformanceCounter>();
                foreach (var item in instances.OrderBy(x => x))
                {
                    if (item.ToLower().Contains("_total")) continue;

                    // info[CounterType.STACKED].Add(new CounterInfo() { Name = item, History = new List<float>(), MaximumValue = 100.0f });
                    Infos.Add(new CounterInfo() { Name = item, History = new List<float>(), MaximumValue = 100.0f });
                    cpuCounterCores.Add(new PerformanceCounter("Processor Information", "% Processor Utility", item));
                }
            }
            
        }
        public override void Update()
        {
            

            lock (ThreadLock)
            {
                currentValue = cpuCounter.NextValue();
                InfoSummary.CurrentValue = currentValue;
                InfoSummary.History.Add(currentValue);
                if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);
                InfoSummary.CurrentStringValue = InfoSummary.CurrentValue.ToString("0") + "%";

                foreach (var item in cpuCounterCores)
                {
                    var ct = Infos.Where(x => x.Name == item.InstanceName).Single();

                    //var ct = info[CounterType.STACKED].Find(x => x.Name == item.InstanceName);
                    ct.CurrentValue = item.NextValue();
                    ct.History.Add(ct.CurrentValue);
                    if (ct.History.Count > Options.HistorySize) ct.History.RemoveAt(0);

                    ct.CurrentStringValue = InfoSummary.CurrentStringValue;// same string value from summary
                }                
            }

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
