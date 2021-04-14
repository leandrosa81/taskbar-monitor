using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskbarMonitor
{    
    public class Options
    {
        public enum ThemeList
        {
            AUTOMATIC,
            DARK,
            LIGHT,
            CUSTOM            
        }
        public static readonly int LATESTOPTIONSVERSION = 2;
        public int OptionsVersion = LATESTOPTIONSVERSION;
        public Dictionary<string, CounterOptions> CounterOptions { get; set; }
        public int HistorySize { get; set; } = 50;
        public int PollTime { get; set; } = 3;
        public ThemeList ThemeType { get; set; } = ThemeList.DARK;

        public void CopyTo(Options opt)
        {
            opt.HistorySize = this.HistorySize;
            opt.PollTime = this.PollTime;
            opt.ThemeType = this.ThemeType;
            if(opt.CounterOptions == null)
                opt.CounterOptions = new Dictionary<string, CounterOptions>();

            foreach (var item in this.CounterOptions)
            {
                if (!opt.CounterOptions.ContainsKey(item.Key))
                    opt.CounterOptions.Add(item.Key, new TaskbarMonitor.CounterOptions());

                opt.CounterOptions[item.Key].ShowTitle = item.Value.ShowTitle;
                opt.CounterOptions[item.Key].Enabled = item.Value.Enabled;
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
        public static Options DefaultOptions()
        {
            return new Options
            {
                CounterOptions = new Dictionary<string, CounterOptions>
                {
                    { "CPU", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
                    { "MEM", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
                    { "DISK", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
                    { "NET", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } }
                },
                HistorySize = 50,
                PollTime = 3,
                ThemeType = ThemeList.DARK
            };
        }
        public static Options ReadFromDisk()
        {
            Options opt = DefaultOptions();

            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
            var origin = System.IO.Path.Combine(folder, "config.json");
            if (System.IO.File.Exists(origin))
            {
                opt = JsonConvert.DeserializeObject<Options>(System.IO.File.ReadAllText(origin));
                
            }
            return opt;
        }
        public bool Upgrade(GraphTheme graphTheme)
        {
            if (_Upgrade(graphTheme)) // do a inplace upgrade
            {
                return SaveToDisk();
            }
            return false;
        }
        public bool SaveToDisk()
        {
            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
            var origin = System.IO.Path.Combine(folder, "config.json");
            if (!System.IO.Directory.Exists(folder))
                System.IO.Directory.CreateDirectory(folder);

            System.IO.File.WriteAllText(origin, JsonConvert.SerializeObject(this));
            return true;
        }
        private bool _Upgrade(GraphTheme graphTheme)
        {
            if (Options.LATESTOPTIONSVERSION > this.OptionsVersion)
            {
                switch (this.OptionsVersion)
                {
                    case 0:
                        //this.OptionsVersion = LATESTOPTIONSVERSION;
                        //return true;
                    case 1:
                        if (GraphTheme.IsCustom(graphTheme))
                            this.ThemeType = ThemeList.CUSTOM;
                        else
                            this.ThemeType = ThemeList.AUTOMATIC;
                        this.OptionsVersion = LATESTOPTIONSVERSION;
                        return true;
                    default:
                        break;
                }
            }
            return false;
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
        public bool Enabled { get; set; } = true;
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
    }
}
