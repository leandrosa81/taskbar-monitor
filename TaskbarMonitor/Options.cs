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
        public static readonly int LATESTOPTIONSVERSION = 5;
        public int OptionsVersion = LATESTOPTIONSVERSION;
        public Dictionary<string, CounterOptions> CounterOptions { get; set; }
        public int HistorySize { get; set; } = 50;
        public int PollTime { get; set; } = 3;
        public ThemeList ThemeType { get; set; } = ThemeList.AUTOMATIC;

        public bool EnableOnAllMonitors { get; set; } = true;
        public Dictionary<string, MonitorOptions> MonitorOptions { get; set; } = new Dictionary<string, MonitorOptions>();

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
                opt.CounterOptions[item.Key].Order = item.Value.Order;
            }

            opt.EnableOnAllMonitors = this.EnableOnAllMonitors;

            if (opt.MonitorOptions == null)
                opt.MonitorOptions = new Dictionary<string, MonitorOptions>();
            foreach (var item in this.MonitorOptions)
            {
                if (!opt.MonitorOptions.ContainsKey(item.Key))
                    opt.MonitorOptions.Add(item.Key, new TaskbarMonitor.MonitorOptions());

                opt.MonitorOptions[item.Key].Enabled = item.Value.Enabled;
                opt.MonitorOptions[item.Key].Position = item.Value.Position;
            }
            for(int i = 0; i < opt.MonitorOptions.Count; i++)
            {
                if (!this.MonitorOptions.ContainsKey(opt.MonitorOptions.Keys.ToList()[i]))
                {
                    opt.MonitorOptions.Remove(opt.MonitorOptions.Keys.ToList()[i]);
                    i--;
                }
            }

        }
        public static Options DefaultOptions()
        {
            var opt = new Options
            {
                CounterOptions = new Dictionary<string, CounterOptions>
                {
                    { "CPU", new CounterOptions { 
                        GraphType = TaskbarMonitor.Counters.ICounter.CounterType.STACKED, 
                        SeparateScales =true, 
                        InvertOrder = false, 
                        SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                        CurrentValueAsSummary = true,
                        ShowCurrentValueShadowOnHover = true,
                        ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                        TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                        ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                        Enabled = true,
                        ShowTitleShadowOnHover = true,
                        Order = 0
                    } },
                    { "MEM", new CounterOptions {
                        GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                        SeparateScales =true,
                        InvertOrder = false,
                        SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                        CurrentValueAsSummary = true,
                        ShowCurrentValueShadowOnHover = true,
                        ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                        TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                        ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                        Enabled = true,
                        ShowTitleShadowOnHover = true,
                        Order = 1
                    } },
                    { "DISK", new CounterOptions { 
                        GraphType = TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                        SeparateScales =true,
                        InvertOrder = false,
                        SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                        CurrentValueAsSummary = true,
                        ShowCurrentValueShadowOnHover = true,
                        ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                        TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                        ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                        Enabled = true,
                        ShowTitleShadowOnHover = true,
                        Order = 2
                    } },
                    { "NET", new CounterOptions {
                        GraphType = TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                        SeparateScales =true,
                        InvertOrder = false,
                        SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                        CurrentValueAsSummary = true,
                        ShowCurrentValueShadowOnHover = true,
                        ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                        TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                        ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                        Enabled = true,
                        ShowTitleShadowOnHover = true,
                        Order = 3
                    } },
                },
                HistorySize = 40,
                PollTime = 1,
                ThemeType = ThemeList.AUTOMATIC,
                EnableOnAllMonitors = true,
                MonitorOptions = new Dictionary<string, MonitorOptions>()
            };
            if (Counters.CounterGPU.IsAvailable())
            {
                opt.CounterOptions.Add("GPU 3D", new CounterOptions {
                    GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    SeparateScales = true,
                    InvertOrder = false,
                    SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                    CurrentValueAsSummary = true,
                    ShowCurrentValueShadowOnHover = true,
                    ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                    TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                    ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                    Enabled = true,
                    ShowTitleShadowOnHover = true,
                    Order = 4
                });
                opt.CounterOptions.Add("GPU MEM", new CounterOptions {
                    GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    SeparateScales = true,
                    InvertOrder = false,
                    SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                    CurrentValueAsSummary = true,
                    ShowCurrentValueShadowOnHover = true,
                    ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                    TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                    ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                    Enabled = true,
                    ShowTitleShadowOnHover = true,
                    Order = 5
                });
            }
            /*
            if (BLL.WindowsInformation.IsWindows11())
            {
                foreach (var item in opt.CounterOptions)
                {
                    if (item.Value.ShowTitle == TaskbarMonitor.CounterOptions.DisplayType.HOVER)
                        item.Value.ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.SHOW;

                    if (item.Value.ShowCurrentValue == TaskbarMonitor.CounterOptions.DisplayType.HOVER)
                        item.Value.ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW;
                }
            }*/
            return opt;
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
            else
            {
                // save to disk with default options
                opt.SaveToDisk();
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

            System.IO.File.WriteAllText(origin, JsonConvert.SerializeObject(this, Formatting.Indented));
            return true;
        }
        private bool _Upgrade(GraphTheme graphTheme)
        {
            var ret = false;
            if(this.OptionsVersion <= 1)
            {
                if (GraphTheme.IsCustom(graphTheme))
                    this.ThemeType = ThemeList.CUSTOM;
                else
                    this.ThemeType = ThemeList.AUTOMATIC;                
                ret = true;
            }
            if (this.OptionsVersion <= 3)
            {
                if (Counters.CounterGPU.IsAvailable())
                {
                    if (!this.CounterOptions.ContainsKey("GPU 3D"))
                        this.CounterOptions.Add("GPU 3D", new CounterOptions
                        {
                            GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                            SeparateScales = true,
                            InvertOrder = false,
                            SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                            CurrentValueAsSummary = true,
                            ShowCurrentValueShadowOnHover = true,
                            ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                            TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                            ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                            Enabled = true,
                            ShowTitleShadowOnHover = true
                        });
                    if (!this.CounterOptions.ContainsKey("GPU MEM"))
                        this.CounterOptions.Add("GPU MEM", new CounterOptions
                        {
                            GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                            SeparateScales = true,
                            InvertOrder = false,
                            SummaryPosition = TaskbarMonitor.CounterOptions.DisplayPosition.TOP,
                            CurrentValueAsSummary = true,
                            ShowCurrentValueShadowOnHover = true,
                            ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW,
                            TitlePosition = TaskbarMonitor.CounterOptions.DisplayPosition.MIDDLE,
                            ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.HOVER,
                            Enabled = true,
                            ShowTitleShadowOnHover = true
                        });
                }

                ret = true;
            }
            if(this.OptionsVersion <= 4)
            {
                int i = 0;
                foreach (var item in this.CounterOptions)
                {
                    item.Value.Order = i++;
                }
                ret = true;
            }
            this.OptionsVersion = LATESTOPTIONSVERSION;
             
            /*
            if (BLL.WindowsInformation.IsWindows11())
            {
                foreach (var item in this.CounterOptions)
                {
                    if(item.Value.ShowTitle == TaskbarMonitor.CounterOptions.DisplayType.HOVER)
                        item.Value.ShowTitle = TaskbarMonitor.CounterOptions.DisplayType.SHOW;

                    if (item.Value.ShowCurrentValue == TaskbarMonitor.CounterOptions.DisplayType.HOVER)
                        item.Value.ShowCurrentValue = TaskbarMonitor.CounterOptions.DisplayType.SHOW;
                }
            }*/
            return ret;
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
        public int? Order { get; set; }
    }

    public class MonitorOptions
    {
        public enum DisplayPosition
        {
            LEFT,
            RIGHT
        }
        public bool Enabled { get; set; } = true;
        public DisplayPosition Position { get; set; } = DisplayPosition.RIGHT; 
    }
}
