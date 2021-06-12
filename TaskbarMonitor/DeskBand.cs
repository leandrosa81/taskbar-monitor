using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TaskbarMonitor
{
    [ComVisible(true)]
    [Guid("13790826-15fa-46d0-9814-c2a5c6c11f32")]
    [CSDeskBand.CSDeskBandRegistration(Name = "taskbar-monitor", ShowDeskBand = true)]
    public class Deskband : CSDeskBand.CSDeskBandWin
    {
        private static Control _control;

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public Deskband()
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error intializing Deskband: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Ctl_OnChangeSize(Size size)
        {
            Options.MinHorizontalSize = new Size(size.Width, 30);
        }

        protected override Control Control => _control;
    }
}
