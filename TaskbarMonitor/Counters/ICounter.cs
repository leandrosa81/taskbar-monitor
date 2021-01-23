using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    class CounterInfo
    {
        public string Name { get; set; }
        public float MaximumValue { get; set; }
        public float CurrentValue { get; set; }
        public string StringValue { get; set; }
        public List<float> History { get; set; }
    }
    abstract class ICounter
    {
        public Options Options { get; private set; }

        public ICounter(Options options)
        {
            this.Options = options;
        }
        public enum CounterType
        {
            SINGLE,
            STACKED,
            MIRRORED
        }
        public abstract string GetName();
        public abstract void Initialize();

        public abstract void Update();

        public abstract CounterType GetCounterType();        

        public abstract List<CounterInfo> GetValues();
    }
}
