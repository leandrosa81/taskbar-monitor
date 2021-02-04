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
            var ctl = new SystemWatcherControl(this);
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
