using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterCPU : ICounter
    {
        PerformanceCounterReader reader;
         
        public CounterCPU(Options options)
            : base(options)
        {

        }

        internal override void Initialize(PerformanceCounterReader reader)
        {
            this.reader = reader;
            //reader.AddPath(@"\Processor Information(_Total)\% Processor Utility");
            reader.AddPath(@"\Processor Information(*)\% Processor Utility");
            
            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 100.0f };
                Infos = new List<CounterInfo>();                
            }
            
        }
        public override void Update()
        {
            

            lock (ThreadLock)
            {
                float currentValue = reader.Values.Where(x => x.Key == @"\Processor Information(*)\% Processor Utility:_Total").Sum(x => x.Value);
                // if (currentValue > 100.0f) currentValue = 100.0f;
                InfoSummary.CurrentValue = currentValue;
                InfoSummary.History.Add(currentValue);
                if (InfoSummary.History.Count > Options.HistorySize) InfoSummary.History.RemoveAt(0);
                if(InfoSummary.CurrentValue <= InfoSummary.MaximumValue)
                    InfoSummary.CurrentStringValue = InfoSummary.CurrentValue.ToString("0") + "%";
                else
                    InfoSummary.CurrentStringValue = InfoSummary.MaximumValue.ToString("0") + "% +" + (InfoSummary.MaximumValue - InfoSummary.CurrentValue).ToString("0");

                var cores = reader.Values.Where(x => x.Key.StartsWith(@"\Processor Information(*)\% Processor Utility:") && Regex.IsMatch(x.Key, @"\d+,\d+$")).ToList();

                foreach (var item in cores)
                {
                    var ct = Infos.Where(x => x.Name == item.Key).SingleOrDefault();
                    if(ct == null)
                    {
                        ct = new CounterInfo() { Name = item.Key, History = new List<float>(), MaximumValue = 100.0f };
                        Infos.Add(ct);
                    }
                    ct.CurrentValue = item.Value;   
                    //ct.CurrentValue = item.NextValue();                    
                    ct.History.Add(ct.CurrentValue);
                    if (ct.History.Count > Options.HistorySize) ct.History.RemoveAt(0);
                    ct.CurrentStringValue = InfoSummary.CurrentStringValue;// same string value from summary
                }
            }

        }
       
        public override string GetName()
        {
            return "CPU";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["CPU"].GraphType;//
        }
    }
}
