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
        public int HistorySize { get; set; } = 50;
        public int PollTime { get; set; } = 3;

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
        public DisplayPosition TitlePosition { get; set; } = DisplayPosition.MIDDLE;
        public bool ShowTitleShadowOnHover { get; set; } = true;
        public DisplayType ShowCurrentValue { get; set; } = DisplayType.SHOW;
        public bool ShowCurrentValueShadowOnHover { get; set; } = true;
        public bool CurrentValueAsSummary { get; set; } = true;
        public DisplayPosition SummaryPosition { get; set; } = DisplayPosition.TOP;
        public bool InvertOrder { get; set; } = false;
        public bool SeparateScales { get; set; } = true;
        public TaskbarMonitor.Counters.ICounter.CounterType GraphType { get; set; }

        public IList<TaskbarMonitor.Counters.ICounter.CounterType> AvailableGraphTypes { get; set; }
    }
}
