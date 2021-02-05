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
            //Options.MinHorizontalSize = new Size(200, 30);
            //TODO: read from file
            Options opt = new Options
            {
                CounterOptions = new Dictionary<string, CounterOptions>
        {
            { "CPU", new CounterOptions {
                GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                AvailableGraphTypes = new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED
                }
            }
            },
            { "MEM", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                AvailableGraphTypes = new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE
                } } },
            { "DISK", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                AvailableGraphTypes = new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                    TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED
                } } },
            { "NET", new CounterOptions { GraphType = TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                AvailableGraphTypes = new List<TaskbarMonitor.Counters.ICounter.CounterType>
                {
                    TaskbarMonitor.Counters.ICounter.CounterType.SINGLE,
                    TaskbarMonitor.Counters.ICounter.CounterType.STACKED,
                    TaskbarMonitor.Counters.ICounter.CounterType.MIRRORED
                } } }
        }
        ,
                HistorySize = 50
        ,
                PollTime = 3
            };
            var ctl = new SystemWatcherControl(this, opt);
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
