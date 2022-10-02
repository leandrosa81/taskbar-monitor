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
        public Size Size { get { return Options.MinHorizontalSize; } }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        public Deskband()
        {
            try
            {
                if (Environment.OSVersion.Version.Major >= 6)
                    SetProcessDPIAware();

                Application.EnableVisualStyles();
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
                Options opt = TaskbarMonitor.Options.ReadFromDisk();
                TaskbarInfo.TaskbarSizeChanged += TaskbarInfo_TaskbarSizeChanged;
                TaskbarInfo.TaskbarEdgeChanged += TaskbarInfo_TaskbarEdgeChanged;
                TaskbarInfo.TaskbarOrientationChanged += TaskbarInfo_TaskbarOrientationChanged;
                Monitor monitor = new Monitor(opt);
                var ctl = new SystemWatcherControl(monitor, false, this);
                Options.MinHorizontalSize = new Size((ctl.Options.HistorySize + 10) * ctl.CountersCount, CSDeskBand.CSDeskBandOptions.TaskbarHorizontalHeightSmall);
                
                ctl.OnChangeSize += Ctl_OnChangeSize;
                //Options.HeightCanChange = false;                
                _control = ctl;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error intializing Deskband: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            // remove other monitors
        }

        private void TaskbarInfo_TaskbarOrientationChanged(object sender, CSDeskBand.TaskbarOrientationChangedEventArgs e)
        {
            
        }

        private void TaskbarInfo_TaskbarEdgeChanged(object sender, CSDeskBand.TaskbarEdgeChangedEventArgs e)
        {
            
        }

        private void TaskbarInfo_TaskbarSizeChanged(object sender, CSDeskBand.TaskbarSizeChangedEventArgs e)
        {
            
        }

        private void Ctl_OnChangeSize(Size size)
        {                        
            Options.MinHorizontalSize = new Size(size.Width, CSDeskBand.CSDeskBandOptions.TaskbarHorizontalHeightSmall);            
            // Options.MaxHorizontalHeight = size.Height;
        }

        protected override Control Control => _control;         
    }
}
