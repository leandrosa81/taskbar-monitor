using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterGPU : ICounter
    {
        List<PerformanceCounter> gpuCounters;

        public CounterGPU(Options options)
            : base(options)
        {

        }

        public override void Initialize()
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var counterNames = category.GetInstanceNames();

            gpuCounters = counterNames
                                .Where(counterName => counterName.EndsWith("engtype_3D"))
                                .SelectMany(counterName => category.GetCounters(counterName))
                                .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                .ToList();

            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "GPU Summary", History = new List<float>(), MaximumValue = 100.0f };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "GPUM", History = new List<float>(), MaximumValue = 100.0f });
            }

            gpuCounters.ForEach(x => x.NextValue());
        }
        public override void Update()
        {
            var currentValue = gpuCounters.Sum(x => x.NextValue());

            lock (ThreadLock)
            {
                InfoSummary.CurrentValue = currentValue;
                InfoSummary.History.Add(currentValue);
                if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);

                InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue).ToString("0") + "%";

                {
                    var info = Infos.Where(x => x.Name == "GPUM").Single();
                    info.CurrentValue = currentValue;
                    info.History.Add(currentValue);
                    if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                    info.CurrentStringValue = (info.CurrentValue).ToString("0") + "%";
                }

            }
        }
       
        public override string GetName()
        {
            return "GPU";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["GPU"].GraphType;
        }
    }
}
