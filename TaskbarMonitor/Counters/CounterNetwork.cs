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
        PerformanceCounterReader reader;
        public CounterNetwork(Options options)
           : base(options)
        {

        }
        List<PerformanceCounter> netCountersSent;
        List<PerformanceCounter> netCountersReceived;
        string[] instances = null;
        //Dictionary<CounterType, List<CounterInfo>> info = new Dictionary<CounterType, List<CounterInfo>>();

        internal override void Initialize(PerformanceCounterReader reader)
        {
            this.reader = reader;
            reader.AddPath(@"\Network Interface(*)\Bytes Sent/sec");
            reader.AddPath(@"\Network Interface(*)\Bytes Received/sec");

            //ReadCounters();
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 1 };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "D", History = new List<float>(), MaximumValue = 1 });
                Infos.Add(new CounterInfo() { Name = "U", History = new List<float>(), MaximumValue = 1 });
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
            
            float currentSent = reader.Values.Where(x => x.Key.StartsWith(@"\Network Interface(*)\Bytes Sent/sec")).Sum(x => x.Value);
            float currentReceived = reader.Values.Where(x => x.Key.StartsWith(@"\Network Interface(*)\Bytes Received/sec")).Sum(x => x.Value);                    

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
