﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterGPUMemory : ICounter
    {
        PerformanceCounterReader reader;
        
        public CounterGPUMemory(Options options)
            : base(options)
        {
        }

        internal override void Initialize(PerformanceCounterReader reader)
        {
            float max = 100.0f;
            
            this.reader = reader;
            reader.AddPath(@"\GPU Adapter Memory(*)\Dedicated Usage");

            var category = new PerformanceCounterCategory("GPU Local Adapter Memory");
            var counterNames = category.GetInstanceNames();

            List<PerformanceCounter> gpuCounters = counterNames
                                    .Where(counterName => counterName.EndsWith("part_0"))
                                    .SelectMany(counterName => category.GetCounters(counterName))
                                    .Where(counter => counter.CounterName.Equals("Local Usage"))
                                    .ToList();

            gpuCounters.ForEach(x => x.NextValue());
            max = gpuCounters.Sum(x => x.NextValue()) / 1024;            
             
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
                currentValue = reader.Values.Where(x => x.Key.StartsWith(@"\GPU Adapter Memory(*)\Dedicated Usage")).Sum(x => x.Value) / 1024 / 1024 / 1024;                
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
       
        public override string GetName()
        {
            return "GPU MEM";
        }         

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions[GetName()].GraphType;
        }

        public new static bool IsAvailable()
        {
            try
            {                                                               
                var category = new PerformanceCounterCategory("GPU Adapter Memory");
                var counterNames = category.GetInstanceNames();

                List<PerformanceCounter> gpuCounters = counterNames
                                        //.Where(counterName => counterName.EndsWith("engtype_3D"))
                                        .SelectMany(counterName => category.GetCounters(counterName))
                                        .Where(counter => counter.CounterName.Equals("Dedicated Usage"))
                                        .ToList();

                gpuCounters.ForEach(x => x.NextValue());                   
                
                return true;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
        }
        
    }
}
