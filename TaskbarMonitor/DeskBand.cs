using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarMonitor
{
    [ComVisible(true)]
    [Guid("13790826-15fa-46d0-9814-c2a5c6c11f32")]
    [CSDeskBand.CSDeskBandRegistration(Name = "Taskbar Monitor", ShowDeskBand = true)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        public Deskband()
        {
            Options opt = new Options
            {
                CounterOptions = new Dictionary<string, CounterOptions>
        {
            { "CPU", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "MEM", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "DISK", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } },
            { "NET", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE } }
        }
        ,
                HistorySize = 50
        ,
                PollTime = 3
            };

            var folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "taskbar-monitor");
            var origin = System.IO.Path.Combine(folder, "config.json");
            if (System.IO.File.Exists(origin))
            {
                opt = JsonConvert.DeserializeObject<Options>(System.IO.File.ReadAllText(origin));
            }
            
            var ctl = new SystemWatcherControl(opt);//this
            Options.MinHorizontalSize = new Size((ctl.Options.HistorySize + 10) * ctl.CountersCount, 30);
            ctl.OnChangeSize += Ctl_OnChangeSize;
            _control = ctl;
        }

        private void Ctl_OnChangeSize(Size size)
        {
            Options.MinHorizontalSize = size;
        }

        protected override Control Control => _control;
    }
}
