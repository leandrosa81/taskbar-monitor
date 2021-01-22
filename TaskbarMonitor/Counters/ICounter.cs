using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor.Counters
{
    abstract class ICounter
    {
        public abstract string GetName();
        public abstract void Initialize();

        public abstract void Update();

        public abstract List<float> GetValues(out float current, out float max, out string representation);
    }
}
