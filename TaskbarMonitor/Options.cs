using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor
{
    public class Options
    {
        public Dictionary<string, CounterOptions> CounterOptions { get; set; }
        public int HistorySize { get; set; }
        
        // themes
    }

    public class CounterOptions
    {
        public enum DisplayType
        {
            HIDDEN,
            SHOW,
            HOVER
        }
        public enum DisplayPosition
        {
            TOP,
            BOTTOM,
            MIDDLE
        }
        public DisplayType ShowTitle { get; set; } = DisplayType.HOVER;
        public DisplayType ShowCurrentValue { get; set; } = DisplayType.SHOW;
        public bool CurrentValueAsSummary { get; set; } = true;
        public DisplayPosition SummaryPosition { get; set; } = DisplayPosition.TOP;
        public bool InvertOrder { get; set; } = false;
        public TaskbarMonitor.Counters.ICounter.CounterType GraphType { get; set; }

        public IList<TaskbarMonitor.Counters.ICounter.CounterType> AvailableGraphTypes { get; set; }
    }
}
