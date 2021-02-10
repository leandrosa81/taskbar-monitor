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

        public void CopyTo(Options opt)
        {
            opt.HistorySize = this.HistorySize;
            opt.PollTime = this.PollTime;
            if(opt.CounterOptions == null)
                opt.CounterOptions = new Dictionary<string, CounterOptions>();

            foreach (var item in this.CounterOptions)
            {
                if (!opt.CounterOptions.ContainsKey(item.Key))
                    opt.CounterOptions.Add(item.Key, new TaskbarMonitor.CounterOptions());

                opt.CounterOptions[item.Key].ShowTitle = item.Value.ShowTitle;
                opt.CounterOptions[item.Key].TitlePosition = item.Value.TitlePosition;
                opt.CounterOptions[item.Key].ShowTitleShadowOnHover = item.Value.ShowTitleShadowOnHover;
                opt.CounterOptions[item.Key].ShowCurrentValue = item.Value.ShowCurrentValue;
                opt.CounterOptions[item.Key].ShowCurrentValueShadowOnHover = item.Value.ShowCurrentValueShadowOnHover;
                opt.CounterOptions[item.Key].CurrentValueAsSummary = item.Value.CurrentValueAsSummary;
                opt.CounterOptions[item.Key].SummaryPosition = item.Value.SummaryPosition;
                opt.CounterOptions[item.Key].InvertOrder = item.Value.InvertOrder;
                opt.CounterOptions[item.Key].SeparateScales = item.Value.SeparateScales;
                opt.CounterOptions[item.Key].GraphType = item.Value.GraphType;                
            }
        }
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
