using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskbarMonitor;
using TaskbarMonitorWindows11.Properties;
using static System.Windows.Forms.Application;

namespace TaskbarMonitorWindows11
{
    static class Program
    {

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static TaskbarManager taskbarManager;
        private static TaskbarMonitorApplicationContext ctx;        

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Environment.OSVersion.Version.Major >= 6)
                SetProcessDPIAware();
            if(!TaskbarMonitor.BLL.WindowsInformation.IsWindows11())
            {
                MessageBox.Show("Please use this application on Windows 11+ devices only.");
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                Options opt = TaskbarMonitor.Options.ReadFromDisk();

                Monitor monitor = new Monitor(opt);

                taskbarManager = new TaskbarManager(monitor);
                taskbarManager.AddControlsWin11();
 
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting taskbar-monitor: {ex.Message}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
              
            ctx = new TaskbarMonitorApplicationContext();         
            Application.Run(ctx);                       
        }
        

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            //if (ctx != null && ctx.is)
                //ctx.Dispose();
            taskbarManager.RemoveControls();
        }

        public class TaskbarMonitorApplicationContext : ApplicationContext
        {
            private NotifyIcon trayIcon;

            public TaskbarMonitorApplicationContext()
            {
                // Initialize Tray Icon
                trayIcon = new NotifyIcon()
                {
                    Icon = Resources.icon,
                    ContextMenu = new ContextMenu(new MenuItem[] {
                        new MenuItem("Settings...", (e, a) => {
                             OpenSettings();
                        }),
                        new MenuItem("Open Task Manager...", (e, a) =>
                        {
                            if(System.IO.File.Exists(Environment.SystemDirectory + @"\taskmgr.exe"))
                                System.Diagnostics.Process.Start(Environment.SystemDirectory + @"\taskmgr.exe");
                            else
                                System.Diagnostics.Process.Start(@"taskmgr.exe");
                        }),
                        new MenuItem("Open Resource Monitor...", (e, a) =>
                        {
                            System.Diagnostics.Process.Start("resmon.exe");
                        }),
                        new MenuItem(String.Format("About taskbar-monitor (v{0})...", taskbarManager.MainControl.Version.ToString(3)), (e, a) => {
                             OpenSettings(2);
                        }),
                        new MenuItem("Hide", (e, a) => {
                             taskbarManager.RemoveControls();
                        }),
                        new MenuItem("Show", (e, a) => {
                             taskbarManager.AddControlsWin11();
                        }),
                        new MenuItem("Exit", Exit),
                    }),
                    Visible = true
                };              
            }
 
            private void OpenSettings(int activeIndex = 0)
            {
                var qtd = Application.OpenForms.OfType<OptionForm>();
                OptionForm optForm = null;
                if (qtd.Count() == 0)
                {
                    optForm = new OptionForm(taskbarManager.MainControl.Options, taskbarManager.MainControl.customTheme, taskbarManager.MainControl.Version, taskbarManager.MainControl);
                    optForm.Show();
                }
                else
                {
                    optForm = qtd.First();
                    optForm.Focus();
                }
                optForm.OpenTab(activeIndex);
            }

            void Exit(object sender, EventArgs e)
            {               
                Application.Exit();
            }

            protected override void Dispose(bool disposing)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;

                base.Dispose(disposing);
            }
        }
    }
}
