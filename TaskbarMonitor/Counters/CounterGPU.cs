using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterGPU : ICounter
    {
        public CounterGPU(Options options)
            : base(options)
        {}

        public override void Initialize()
        {
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "GPU Summary", History = new List<float>(), MaximumValue = 100.0f };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "GPUM", History = new List<float>(), MaximumValue = 100.0f });
            }
        }
        public override void Update()
        {
            float currentValue = 0;

            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();

                List<PerformanceCounter> gpuCounters =gpuCounters = counterNames
                                        .Where(counterName => counterName.EndsWith("engtype_3D"))
                                        .SelectMany(counterName => category.GetCounters(counterName))
                                        .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                        .ToList();

                gpuCounters.ForEach(x => x.NextValue());
                currentValue = gpuCounters.Sum(x => x.NextValue());
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
            }

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
