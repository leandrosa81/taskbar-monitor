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
    [CSDeskBand.CSDeskBandRegistration(Name = "taskbar-monitor", ShowDeskBand = true)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public Deskband()
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Options opt = TaskbarMonitor.Options.ReadFromDisk();
            

            var ctl = new SystemWatcherControl(opt);
            Options.MinHorizontalSize = new Size((ctl.Options.HistorySize + 10) * ctl.CountersCount, 30);
            ctl.OnChangeSize += Ctl_OnChangeSize;
            _control = ctl;
        }

        private void Ctl_OnChangeSize(Size size)
        {
            Options.MinHorizontalSize = new Size(size.Width, 30);
        }

        protected override Control Control => _control;
    }
}
