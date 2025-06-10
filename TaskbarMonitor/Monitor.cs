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

        public delegate void OptionsUpdate();
        public event OptionsUpdate OnOptionsUpdated;

        public Options Options { get; private set; }

        public List<Counters.ICounter> Counters;

        private System.Timers.Timer pollingTimer;

        private static Monitor _instance = null;
        private static object instanceLock = new object();
        public static Monitor GetInstance(Options opt)
        {
            lock (instanceLock)
            {
                if (_instance == null) _instance = new Monitor(opt);
            }
            return _instance;
        }

        private static object updateLock = new object();
        private static bool updating = false;
        private Monitor(Options opt)
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

            if(Counters == null)
                Counters = new List<Counters.ICounter>();
            var counterNames = new List<string> { "CPU", "MEM", "DISK", "NET", "GPU 3D", "GPU MEM" };
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
                        case "GPU 3D":
                            ct = new Counters.CounterGPU(opt);
                            break;
                        case "GPU MEM":
                            ct = new Counters.CounterGPUMemory(opt);
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
                else if(q != null && !opt.CounterOptions.ContainsKey(counterName))
                {
                    q.Dispose();
                    Counters.Remove(q);
                }
            }

            if (OnOptionsUpdated != null)
                OnOptionsUpdated();
        }
        
        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (pollingTimer != null && pollingTimer.Interval != Options.PollTime * 1000)
                pollingTimer.Interval = Options.PollTime * 1000;

            lock(updateLock)
            {
                if (updating)
                    return;
                updating = true;
            }
            try
            {
                foreach (var ct in Counters)
                {
                    ct.Update();
                }
                ;

                if (OnMonitorUpdated != null)
                    OnMonitorUpdated();
            }
            finally
            {
                lock (updateLock)
                {
                    updating = false;
                }
            }
        }

        public void Dispose()
        {
            foreach(var ct in Counters)
            {
                ct.Dispose();
            }
            pollingTimer?.Stop();
            pollingTimer?.Dispose();
            pollingTimer.Elapsed -= PollingTimer_Elapsed;
        }
    }
}
