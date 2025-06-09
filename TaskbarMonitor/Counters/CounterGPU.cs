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
        public enum GPUType
        {
            GPU3D,
            MEMORY
        }

        public GPUType GPUCounterType { get; private set; }
        PerformanceCounterCategory categoryGPU3D = null;
        PerformanceCounterCategory categoryGPUMEM = null;

        private List<PerformanceCounter> gpuCounters3D = new List<PerformanceCounter>();
        private string[] lastCounterNames = new string[0];
        private DateTime lastRefresh = DateTime.MinValue;
        private readonly TimeSpan refreshInterval = TimeSpan.FromSeconds(30);


        public CounterGPU(Options options, GPUType t = GPUType.GPU3D)
            : base(options)
        {
            this.GPUCounterType = t;
        }

        public override void Initialize()
        {
            float max = 100.0f;
            if (GPUCounterType == GPUType.GPU3D)
            {
                categoryGPU3D = new PerformanceCounterCategory("GPU Engine");
               
            }
            if (GPUCounterType == GPUType.MEMORY) 
            {
                var category = new PerformanceCounterCategory("GPU Local Adapter Memory");
                var counterNames = category.GetInstanceNames();

                List<PerformanceCounter> gpuCounters = counterNames
                                        .Where(counterName => counterName.EndsWith("part_0"))
                                        .SelectMany(counterName => category.GetCounters(counterName))
                                        .Where(counter => counter.CounterName.Equals("Local Usage"))
                                        .ToList();

                gpuCounters.ForEach(x => x.NextValue());
                max = gpuCounters.Sum(x => x.NextValue()) / 1024;

                categoryGPUMEM = new PerformanceCounterCategory("GPU Adapter Memory");
            }


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
                if (GPUCounterType == GPUType.GPU3D)
                {
                    // Refresh counters only if enough time has passed or if instance names have changed
                    var counterNames = categoryGPU3D.GetInstanceNames();
                    bool needRefresh = (DateTime.Now - lastRefresh) > refreshInterval ||
                                       !counterNames.SequenceEqual(lastCounterNames);

                    if (needRefresh)
                    {
                        gpuCounters3D = counterNames
                            .Where(counterName => counterName.EndsWith("engtype_3D"))
                            .SelectMany(counterName => categoryGPU3D.GetCounters(counterName))
                            .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                            .ToList();
                        lastCounterNames = counterNames;
                        lastRefresh = DateTime.Now;
                        // Prime counters
                        gpuCounters3D.ForEach(x => x.NextValue());
                    }

                    // Only call NextValue once per counter
                    currentValue = gpuCounters3D.Sum(x => x.NextValue());
                }
                else if(GPUCounterType == GPUType.MEMORY)
                {
                    
                    var counterNames = categoryGPUMEM.GetInstanceNames();

                    List<PerformanceCounter> gpuCountersMem = counterNames                                            
                                            .SelectMany(counterName => categoryGPUMEM.GetCounters(counterName))
                                            .Where(counter => counter.CounterName.Equals("Dedicated Usage"))
                                            .ToList();

                    //gpuCountersMem.ForEach(x => x.NextValue());
                    currentValue = gpuCountersMem.Sum(x => x.NextValue()) / 1024 / 1024 / 1024;
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

                if (GPUCounterType == GPUType.GPU3D)
                {
                    InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue).ToString("0") + "%";

                    {
                        var info = Infos.Where(x => x.Name == "GPU").Single();
                        info.CurrentValue = currentValue;
                        info.History.Add(currentValue);
                        if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                        info.CurrentStringValue = (info.CurrentValue).ToString("0") + "%";
                    }
                }
                else if (GPUCounterType == GPUType.MEMORY)
                {
                    InfoSummary.CurrentStringValue = (InfoSummary.CurrentValue).ToString("0.0") + "GB";

                    {
                        var info = Infos.Where(x => x.Name == "GPU").Single();
                        info.CurrentValue = currentValue;
                        info.History.Add(currentValue);
                        if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);

                        info.CurrentStringValue = (info.CurrentValue).ToString("0.0") + "GB";
                    }
                }

            }
        }
       
        public override string GetName()
        {
            return GPUCounterType == GPUType.GPU3D ? "GPU 3D" : "GPU MEM";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions[GetName()].GraphType;
        }

        public new static bool IsAvailable()
        {
            try
            {                
                {
                    var category = new PerformanceCounterCategory("GPU Engine");
                    var counterNames = category.GetInstanceNames();

                    List<PerformanceCounter> gpuCounters = counterNames
                                            .Where(counterName => counterName.EndsWith("engtype_3D"))
                                            .SelectMany(counterName => category.GetCounters(counterName))
                                            .Where(counter => counter.CounterName.Equals("Utilization Percentage"))
                                            .ToList();

                    gpuCounters.ForEach(x => x.NextValue());                    
                }
                
                {
                    var category = new PerformanceCounterCategory("GPU Adapter Memory");
                    var counterNames = category.GetInstanceNames();

                    List<PerformanceCounter> gpuCounters = counterNames
                                            //.Where(counterName => counterName.EndsWith("engtype_3D"))
                                            .SelectMany(counterName => category.GetCounters(counterName))
                                            .Where(counter => counter.CounterName.Equals("Dedicated Usage"))
                                            .ToList();

                    gpuCounters.ForEach(x => x.NextValue());                   
                }
                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }
    }
}
