using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            this.Options = opt;

            Counters = new List<Counters.ICounter>();
            if (opt.CounterOptions.ContainsKey("CPU"))
            {
                var ct = new Counters.CounterCPU(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("MEM"))
            {
                var ct = new Counters.CounterMemory(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("DISK"))
            {
                var ct = new Counters.CounterDisk(opt);
                ct.Initialize();
                Counters.Add(ct);
            }
            if (opt.CounterOptions.ContainsKey("NET"))
            {
                var ct = new Counters.CounterNetwork(opt);
                ct.Initialize();
                Counters.Add(ct);
            }

            pollingTimer = new System.Timers.Timer(opt.PollTime * 1000);
            pollingTimer.Enabled = true;
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            pollingTimer.Start();


        }
        
        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (pollingTimer != null && pollingTimer.Interval != Options.PollTime * 1000)
                pollingTimer.Interval = Options.PollTime * 1000;

            foreach (var ct in Counters)
            {
                ct.Update();
            };
            
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
