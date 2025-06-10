using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterDisk: ICounter
    {
        PerformanceCounterReader reader;

        public CounterDisk(Options options)
           : base(options)
        {

        }
        

        internal override void Initialize(PerformanceCounterReader reader)
        {
            this.reader = reader;
            reader.AddPath(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec");
            reader.AddPath(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec");

            lock (ThreadLock)
            {
                InfoSummary = new CounterInfo() { Name = "summary", History = new List<float>(), MaximumValue = 1 };
                Infos = new List<CounterInfo>();
                Infos.Add(new CounterInfo() { Name = "R", History = new List<float>(), MaximumValue = 1 });
                Infos.Add(new CounterInfo() { Name = "W", History = new List<float>(), MaximumValue = 1 });
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

                info.CurrentStringValue = (info.CurrentValue / 1024 / 1024).ToString("0.0") + "MB/s";
            };

            float currentRead = reader.Values.Where(x => x.Key.StartsWith(@"\PhysicalDisk(_Total)\Disk Read Bytes/sec")).Sum(x => x.Value);
            float currentWritten = reader.Values.Where(x => x.Key.StartsWith(@"\PhysicalDisk(_Total)\Disk Write Bytes/sec")).Sum(x => x.Value);


            lock (ThreadLock)
            {
                addValue(InfoSummary, currentRead + currentWritten);
                addValue(Infos.Where(x => x.Name == "R").Single(), currentRead);
                addValue(Infos.Where(x => x.Name == "W").Single(), currentWritten);

                // if locks down same scale for both counters is on
                if (!Options.CounterOptions["DISK"].SeparateScales)
                {
                    var info1 = Infos.Where(x => x.Name == "R").Single();
                    var info2 = Infos.Where(x => x.Name == "W").Single();

                    float max = info1.MaximumValue > info2.MaximumValue ? info1.MaximumValue : info2.MaximumValue;
                    info1.MaximumValue = info2.MaximumValue = max;
                }
            }
        }
         
        public override string GetName()
        {
            return "DISK";
        }

        public override CounterType GetCounterType()
        {
            return Options.CounterOptions["DISK"].GraphType;// ? CounterType.SINGLE : CounterType.MIRRORED;
        }
    }
}
