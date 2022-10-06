using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TaskbarMonitor.BLL;

using System.Diagnostics;
using System.Drawing;

namespace TaskbarMonitor
{
    public class Taskbar
    {
        public IntPtr TargetWnd = IntPtr.Zero;
        public IntPtr TrayWnd = IntPtr.Zero;
        public SystemWatcherControl TaskbarMonitorControl;
    }
    public class TaskbarManager: IDisposable
    {

        public Monitor Monitor { get; private set; }        
        private delegate void Execute();

        List<Taskbar> TaskbarList = new List<Taskbar>();
        Taskbar MainTaskbar = null;

        IntPtr g_hook = IntPtr.Zero;
        private System.Timers.Timer pollingTimer;

        public SystemWatcherControl MainControl
        {
            get
            {
                return MainTaskbar.TaskbarMonitorControl;
            }
        }

        public TaskbarManager(Monitor monitor)
        {
            this.Monitor = monitor;

            pollingTimer = new System.Timers.Timer(500);
            pollingTimer.Enabled = true;
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            pollingTimer.Start();
        }

        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdatePositions();
        }

        private void UpdatePositions()
        {            
            foreach (var taskbar in TaskbarList)
            {
                var handle = taskbar.TargetWnd;
                var rect = BLL.Win32Api.GetWindowSize(handle);
                if (taskbar.TrayWnd != IntPtr.Zero)
                {
                    var rectTray = BLL.Win32Api.GetWindowSize(taskbar.TrayWnd);

                    taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                    {
                        taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width - rectTray.Width;
                    });
                    
                }
                else
                {                    
                    taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                    {
                        taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width;
                    });
                }
            }
        }

        public bool AddControlsWin11()
        { 
            List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();

            var taskarea = windowListExtended.Where(w => w.Class == "Shell_TrayWnd").SingleOrDefault();
            
            if (taskarea == null)
            {
                return false;
            }

            //var rebar = taskarea.ChildWindows.Where(x => x.Class == "ReBarWindow32").SingleOrDefault();            
            //var apps = rebar.ChildWindows.Where(x => x.Class == "MSTaskSwWClass").SingleOrDefault();
            var tray = taskarea.ChildWindows.Where(x => x.Class == "TrayNotifyWnd").SingleOrDefault();            
            var janelao = taskarea.ChildWindows.Where(x => x.Caption == "DesktopWindowXamlSource" && x.Class == "Windows.UI.Composition.DesktopWindowContentBridge").SingleOrDefault();
            var janelao2 = tray.ChildWindows.Where(x => x.Caption == "DesktopWindowXamlSource" && x.Class == "Windows.UI.Composition.DesktopWindowContentBridge").SingleOrDefault();
            var target = taskarea;

            Taskbar tb = new Taskbar();            
            TaskbarList.Add(tb);
            MainTaskbar = tb;

            tb.TargetWnd = target.Handle;
            tb.TrayWnd = tray.Handle;


            var rect = BLL.Win32Api.GetWindowSize(target.Handle);

            var rectTray = BLL.Win32Api.GetWindowSize(tray.Handle);
            
            var OnTopControl = new SystemWatcherControl(this.Monitor);
            tb.TaskbarMonitorControl = OnTopControl;
                        
            OnTopControl.Name = "TaskbarWin11";
            OnTopControl.Left = rect.Width - OnTopControl.Width - rectTray.Width;// ;
                         
            

            //BLL.Win32Api.SetWindowLong(OnTopControl.Handle, BLL.Win32Api.GWLParameter.GWL_HWNDPARENT, taskarea.Handle.ToInt32());                
            BLL.Win32Api.SetParent(OnTopControl.Handle, target.Handle);

            const UInt32 WS_POPUP = 0x80000000;
            const UInt32 WS_CHILD = 0x40000000;

            uint style = BLL.Win32Api.GetWindowLong(OnTopControl.Handle, Win32Api.GWLParameter.GWL_STYLE);
            style = (style | WS_CHILD) & (~WS_POPUP);
            BLL.Win32Api.SetWindowLong(OnTopControl.Handle, Win32Api.GWLParameter.GWL_STYLE, (int)style);

            OnTopControl.Show();
            //_mainControl = OnTopControl;

            //uint activeThread = Win32Api.GetWindowThreadProcessId(OnTopControl.Handle, out uint activeProcess);
            //uint windowThread = Win32Api.GetWindowThreadProcessId(target.Handle, out uint windowProcess);
            //var ret =BLL.Win32Api.AttachThreadInput(activeThread, windowThread, true);


            BLL.Win32Api.EnableWindow(OnTopControl.Handle, true);
            BLL.Win32Api.BringWindowToTop(OnTopControl.Handle);
            BLL.Win32Api.SetWindowPos(OnTopControl.Handle, new IntPtr(0), OnTopControl.Left, OnTopControl.Top, OnTopControl.Width, OnTopControl.Height, 0);

            var PQ = OnTopControl.Parent;
            //OnTopControls.Add(OnTopControl);
             
            if (g_hook == IntPtr.Zero)
                HookEvents();
              
            return true;
        }
        
        public bool AddControlsExtraMonitors()
        {
            List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();

            var taskarea = windowListExtended.Where(w => w.Class == "Shell_SecondaryTrayWnd").SingleOrDefault();
            if(taskarea == null)
            {
                return false;
            }

            Taskbar tb = new Taskbar();
            TaskbarList.Add(tb);

            if (BLL.WindowsInformation.IsWindows11())
            {
                // item.ChildWindows.Where(x => x.Class == "Windows.UI.Composition.DesktopWindowContentBridge").SingleOrDefault();
                if (taskarea != null)
                {
                    tb.TargetWnd = taskarea.Handle;                    

                    var rect = BLL.Win32Api.GetWindowSize(taskarea.Handle);

                    var OnTopControl = new SystemWatcherControl(this.Monitor);
                    OnTopControl.Name = "Secondary";
                    OnTopControl.Left = rect.Width - OnTopControl.Width;
                    OnTopControl.Show();
                    

                    //BLL.Win32Api.SetWindowLong(OnTopControl.Handle, BLL.Win32Api.GWLParameter.GWL_HWNDPARENT, taskarea.Handle.ToInt32());
                    // BLL.Win32Api.SetWindowLong(taskarea.Handle, BLL.Win32Api.GWLParameter.GWL_EXSTYLE, 0x00010000);
                    BLL.Win32Api.SetParent(OnTopControl.Handle, taskarea.Handle);
                    tb.TaskbarMonitorControl = OnTopControl;
                }
                
            }
            else
            {
                var worker = taskarea.ChildWindows.Where(x => x.Class == "WorkerW").SingleOrDefault();
                if (worker != null)
                {
                    var tasklist = worker.ChildWindows.Where(x => x.Class == "MSTaskListWClass").SingleOrDefault();
                    if (tasklist != null)
                    {
                        tb.TargetWnd = tasklist.Handle;

                        var rect = BLL.Win32Api.GetWindowSize(tasklist.Handle);

                        var OnTopControl = new SystemWatcherControl(this.Monitor);
                        OnTopControl.Name = "Secondary";
                        OnTopControl.Left = rect.Width - OnTopControl.Width;
                        OnTopControl.Show();

                        BLL.Win32Api.SetWindowLong(OnTopControl.Handle, BLL.Win32Api.GWLParameter.GWL_HWNDPARENT, tasklist.Handle.ToInt32());
                        tb.TaskbarMonitorControl = OnTopControl;
                    }
                }
            }
            

            if (g_hook == IntPtr.Zero)
                HookEvents();


            return true;

        }

        private void HookEvents()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> events = InitializeWinEventToHandlerMap();

            //Hook window close event - close our HoverContorl on Target window close.
            BLL.Win32Api.WinEventProc eventHandler = eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.Destroy].Invoke);

            GCHandle gch = GCHandle.Alloc(eventHandler);

            g_hook = BLL.Win32Api.SetWinEventHook(AccessibleEvents.SystemCaptureStart,
                (AccessibleEvents)0x8013, IntPtr.Zero, eventHandler
                , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT);
        }
         

        public void RemoveControls()
        {
            //Removes an event hook function created by a previous call to 
            BLL.Win32Api.UnhookWinEvent(g_hook);
            
            //Close HoverControl window.
            foreach (var taskbar in TaskbarList)
            {
                //if(OnTopControl.Handle != IntPtr.Zero)
                //BLL.Win32Api.DestroyWindow(OnTopControl.Handle);                
                taskbar.TaskbarMonitorControl.Hide();
                taskbar.TaskbarMonitorControl.Dispose();

                var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
            }            
            
        }
         
        private Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> InitializeWinEventToHandlerMap()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> dictionary = new Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc>();
            //You can add more events like ValueChanged - for more info please read - 
            //http://msdn.microsoft.com/en-us/library/system.windows.forms.accessibleevents.aspx
            // dictionary.Add(AccessibleEvents.ValueChange, new BLL.Win32Api.WinEventProc(this.ValueChangedCallback));
            // dictionary.Add(AccessibleEvents.LocationChange, new BLL.Win32Api.WinEventProc(this.LocationChangedCallback));
            
            dictionary.Add(AccessibleEvents.Destroy, new BLL.Win32Api.WinEventProc(this.DestroyCallback));          
            return dictionary;
        }

        private void DestroyCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        { 
            //Make sure AccessibleEvents equals to LocationChange and the current window is the Target Window.
            if (accEvent == AccessibleEvents.Destroy &&  TaskbarList.Any(x => x.TargetWnd.ToInt32() == windowHandle.ToInt32()))
            {
                //Queues a method for execution. The method executes when a thread pool thread becomes available.
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.DestroyHelper));
            } 
        }
    
        private void DestroyHelper(object state)
        {
             
            Execute ex = delegate ()
            {
                //Removes an event hook function created by a previous call to 
                BLL.Win32Api.UnhookWinEvent(g_hook);
                //Close HoverControl window.
                foreach (var taskbar in TaskbarList)
                {
                    taskbar.TaskbarMonitorControl.Dispose();
                }
                
            };            
        }

        public void Dispose()
        {
            pollingTimer?.Stop();
            pollingTimer?.Dispose();
            pollingTimer.Elapsed -= PollingTimer_Elapsed;
        }
    }
}
