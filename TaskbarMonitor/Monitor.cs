using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskbarMonitor.Counters;

namespace TaskbarMonitor
{
    public class Monitor: IDisposable
    {
        public delegate void NotifyUpdate();
        public event NotifyUpdate OnMonitorUpdated;

        public Options Options { get; private set; }

        public List<Counters.ICounter> Counters;

        private System.Timers.Timer pollingTimer;

        public Monitor(Options opt)
        {
            UpdateOptions(opt);            

            pollingTimer = new System.Timers.Timer(opt.PollTime * 1000);
            pollingTimer.Enabled = true;
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            pollingTimer.Start();


        }
        public void UpdateOptions(Options opt)
        {
            this.Options = opt;

            Counters = new List<Counters.ICounter>();
            var counterNames = new List<string> { "CPU", "MEM", "DISK", "NET" };
            foreach(var counterName in counterNames)
            {
                var q = Counters.Where(x => x.GetName() == counterName).SingleOrDefault();
                if (opt.CounterOptions.ContainsKey(counterName) && q == null)
                {
                    ICounter ct = null;
                    switch(counterName)
                    {
                        case "CPU":
                            ct = new Counters.CounterCPU(opt);
                            break;
                        case "MEM":
                            ct = new Counters.CounterMemory(opt);
                            break;
                        case "DISK":
                            ct = new Counters.CounterDisk(opt);
                            break;
                        case "NET":
                            ct = new Counters.CounterNetwork(opt);
                            break;
                    }
                    
                    ct.Initialize();
                    Counters.Add(ct);
                }
                else if(q != null)
                {
                    Counters.Remove(q);
                }
            }            
        }
        
        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (pollingTimer != null && pollingTimer.Interval != Options.PollTime * 1000)
                pollingTimer.Interval = Options.PollTime * 1000;

            foreach (var ct in Counters)
            {
                ct.Update();
            };
            
            if(OnMonitorUpdated != null)
                OnMonitorUpdated();

        }

        public void Dispose()
        {
            pollingTimer?.Stop();
            pollingTimer?.Dispose();
            pollingTimer.Elapsed -= PollingTimer_Elapsed;
        }
    }
}
