using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterGPU : ICounter
    {
        PerformanceCounterReader reader;

        
        private readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(30);

        string maxValue = "3D";        
        Dictionary<string, string> labels = new Dictionary<string, string> {
            { "VideoEncode" , "VENC" },
            { "3D", "3D" },
            { "LegacyOverlay", "OVER" },
            { "Copy", "COPY" },
            { "VideoDecode", "VDEC" },
            { "Compute_1", "CPT1" },
            { "Graphics_1", "GFX" },
            { "Security", "SEC" },
            { "Compute_0", "CPT0" },
            { "VR", "VR" },
            { "Cuda", "CUDA" }
        };


        public CounterGPU(Options options)
            : base(options)
        {
        }

        public override void Initialize()
        {
            float max = 100.0f;
            reader = new PerformanceCounterReader(@"\GPU Engine(*)\Utilization Percentage", refreshInterval);            

            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "GPU Summary", History = new List<float>(), MaximumValue = max };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "GPU", History = new List<float>(), MaximumValue = max });
            }
        }
        public override void Update()
        {
            float currentValue = 0;
            
            try
            {
                var counters = reader.ReadCounters();                
                foreach (var item in labels.Keys)
                {
                    var itemValue = counters.Where(x => x.Key.EndsWith("engtype_" + item)).Sum(x => x.Value);
                    /*float itemValue = 0;
                    for (int i = 0; i < gpuCounters[item].Count; i++)
                    {
                        //itemValue += gpuCounters[item][i].NextValue();
                        itemValue += gpuCounters[item][i].NextValue();
                    }*/

                    if (itemValue > currentValue)
                    {
                        maxValue = item;
                        currentValue = itemValue;
                    }
                }

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
                    var info = Infos.Where(x => x.Name == "GPU").Single();
                    info.CurrentValue = currentValue;
                    info.History.Add(currentValue);
                    if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                    info.CurrentStringValue = (info.CurrentValue).ToString("0") + "%";
                }


            }
        }

        public override string GetName()
        {
            return "GPU 3D";
        }
        public override string GetLabel()
        {
            return "GPU " + labels[maxValue];
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions[GetName()].GraphType;
        }

        public new static bool IsAvailable()
        {
            try
            {

                var category = new PerformanceCounterCategory("GPU Engine");
                var counterNames = category.GetInstanceNames();

                List<PerformanceCounter> gpuCounters = counterNames
                                        .Where(counterName => counterName.EndsWith("engtype_3D"))
                                        .SelectMany(counterName => category.GetCounters(counterName))
                                        .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                        .ToList();

                gpuCounters.ForEach(x => x.NextValue());

                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }

        public override void Dispose()
        {
            reader.Dispose();
        }

    }
}
