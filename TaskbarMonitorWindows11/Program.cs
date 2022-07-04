using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaskbarMonitor;
using TaskbarMonitorWindows11.Properties;

namespace TaskbarMonitorWindows11
{
    static class Program
    {

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        private static TaskbarManager st;

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


            st = new TaskbarManager();

            st.AddControlsWin11();
            st.AddControlsExtraMonitors();

            Timer tm = new Timer();
            tm.Interval = 500;
            tm.Tick += Tm_Tick;
            tm.Start();

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            Application.Run(new MyCustomApplicationContext());
        }

        private static void Tm_Tick(object sender, EventArgs e)
        {
            st.UpdatePositions();
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            st.RemoveControls();
        }

        public class MyCustomApplicationContext : ApplicationContext
        {
            private NotifyIcon trayIcon;

            public MyCustomApplicationContext()
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
                    System.Diagnostics.Process.Start("taskmgr.exe");
                }),
                        new MenuItem("Open Resource Monitor...", (e, a) =>
                {
                    System.Diagnostics.Process.Start("resmon.exe");
                }),                        
                        new MenuItem(String.Format("About taskbar-monitor (v{0})...", st.MainControl.Version.ToString(3)), (e, a) => {
                             OpenSettings(2);
                        }),
                        new MenuItem(String.Format("test"), (e, a) => {
                           TaskbarMonitor.BLL.Win32Api.PostMessageA(st.MainControl.Handle, 0x0204, 0x0002, 0); // right
                            //TaskbarMonitor.BLL.Win32Api.PostMessageA(st.OnTopControls.First(x => x != st.MainControl).Handle, 0x0204, 0x0002, 0); // right, second monitor
                           //TaskbarMonitor.BLL.Win32Api.PostMessageA(st.MainControl.Handle, 0x0201, 0x0001, 0); // left
                           // TaskbarMonitor.BLL.Win32Api.PostMessageA(st.MainControl.Handle, 0x0203, 0x0001, 0); // double
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
                    optForm = new OptionForm(st.MainControl.Options, st.MainControl.customTheme, st.MainControl.Version, st.MainControl);
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
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;

                Application.Exit();
            }
        }
    }
}
