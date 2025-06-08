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
        string[] instances = null;
        //Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        public override void Initialize()
        {

            ReadCounters();
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 1 };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "D", History = new List<float>(), MaximumValue = 1 });
                Infos.Add(new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = 1 });
            }
        }

        private void ReadCounters()
        {
            PerformanceCounterCategory pcg = new PerformanceCounterCategory("Network Interface");
            string[] newinstances = pcg.GetInstanceNames();
            bool changed = instances == null
                || newinstances.Length != instances.Length
                || !newinstances.SequenceEqual(instances);
            if (changed)
            {
                instances = newinstances;
                netCountersSent = new List<PerformanceCounter>();
                netCountersReceived = new List<PerformanceCounter>();
                foreach (var instance in instances)
                {
                    netCountersSent.Add(new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance));
                    netCountersReceived.Add(new PerformanceCounter("Network Interface", "Bytes Received/sec", instance));
                }
                netCountersSent.ForEach(x => x.NextValue());
                netCountersReceived.ForEach(x => x.NextValue());
            }
        }
       

        public override void Update()
        {
            Action<CounterInfo, float> addValue = (info, value) =>
            {
                info.CurrentValue = value;
                info.History.Add(value);
                if (info.History.Count > Options.HistorySize) info.History.RemoveAt(0);
                info.MaximumValue = Convert.ToInt64(info.History.Max()) + 1;

                if (info.CurrentValue > (1024 * 1024))
                    info.CurrentStringValue = (info.CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
                else
                    info.CurrentStringValue = (info.CurrentValue / 1024).ToString("0.0") + "KB/s";
            };
            bool success = false;
            int maxRetries = 1;
            int retries = 0;
            while (!success && retries <= maxRetries)
            {
                try
                {
                    float currentSent = 0;
                    float currentReceived = 0;
                    ReadCounters();
                    currentSent = netCountersSent.Sum(x => x.NextValue());
                    currentReceived = netCountersReceived.Sum(x => x.NextValue());
                    /*foreach (var netCounter in netCountersSent)
                    {
                        currentSent += netCounter.NextValue();
                    }
                    foreach (var netCounter in netCountersReceived)
                    {
                        currentReceived += netCounter.NextValue();
                    }*/

                    lock (ThreadLock)
                    {
                        addValue(InfoSummary, currentSent + currentReceived);
                        addValue(Infos.Where(x => x.Name == "D").Single(), currentReceived);
                        addValue(Infos.Where(x => x.Name == "U").Single(), currentSent);

                        // if locks down same scale for both counters is on
                        if (!Options.CounterOptions["NET"].SeparateScales)
                        {
                            var info1 = Infos.Where(x => x.Name == "D").Single();
                            var info2 = Infos.Where(x => x.Name == "U").Single();

                            float max = info1.MaximumValue > info2.MaximumValue ? info1.MaximumValue : info2.MaximumValue;
                            info1.MaximumValue = info2.MaximumValue = max;
                        }
                        success = true;
                    }                    
                }
                catch (InvalidOperationException e)
                {
                    // in this case we have to reevaluate the counters
                    ReadCounters();
                    retries++;
                }
            }


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
