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

        List<IntPtr> g_hook = new List<IntPtr>();
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

            pollingTimer = new System.Timers.Timer(2000);            
            pollingTimer.Elapsed += PollingTimer_Elapsed;
            
        }

        private void PollingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if(g_hook.Count == 0)
                AddControlsWin11();
        }

        private void UpdatePositions()
        {            
            foreach (var taskbar in TaskbarList)
            {
                var handle = taskbar.TargetWnd;
                var rect = BLL.Win32Api.GetWindowSize(handle);
                if (taskbar.TrayWnd != IntPtr.Zero && taskbar.TaskbarMonitorControl != null)
                {
                    var rectTray = BLL.Win32Api.GetWindowSize(taskbar.TrayWnd);                    
                    taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                    {
                        taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width - rectTray.Width;
                    });
                    
                }
                else if (taskbar.TaskbarMonitorControl != null)
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
            if(pollingTimer.Enabled)
                pollingTimer.Stop();

            var tray = taskarea.ChildWindows.Where(x => x.Class == "TrayNotifyWnd").SingleOrDefault();
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
            OnTopControl.Left = rect.Width - OnTopControl.Width - rectTray.Width;
            
            BLL.Win32Api.SetParent(OnTopControl.Handle, target.Handle);          

            OnTopControl.Show();

            if (g_hook.Count == 0)
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
            

            if (g_hook.Count == 0)
                HookEvents();


            return true;

        }

        private void HookEvents()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> events = InitializeWinEventToHandlerMap();

            //Hook window close event - close our HoverContorl on Target window close.
            BLL.Win32Api.WinEventProc eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.LocationChange].Invoke);

            GCHandle gch = GCHandle.Alloc(eventHandler);

            g_hook.Add(BLL.Win32Api.SetWinEventHook(AccessibleEvents.LocationChange,
       AccessibleEvents.LocationChange, IntPtr.Zero, eventHandler
       , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT));

            //Hook window close event - close our HoverContorl on Target window close.
            eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.Destroy].Invoke);

            gch = GCHandle.Alloc(eventHandler);

            g_hook.Add(BLL.Win32Api.SetWinEventHook(AccessibleEvents.Destroy,
                AccessibleEvents.LocationChange, IntPtr.Zero, eventHandler
                , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT));
        }
         

        public void RemoveControls()
        {
            //Removes an event hook function created by a previous call to 
            foreach (var gh in g_hook)
            {
                BLL.Win32Api.UnhookWinEvent(gh);
            }
            g_hook.Clear();

            //Close HoverControl window.
            foreach (var taskbar in TaskbarList)
            {
                taskbar.TaskbarMonitorControl.Hide();
                taskbar.TaskbarMonitorControl.Dispose();

                var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
            }
            TaskbarList.Clear();
        }
         
        private Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> InitializeWinEventToHandlerMap()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> dictionary = new Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc>();

            dictionary.Add(AccessibleEvents.LocationChange, new BLL.Win32Api.WinEventProc(this.LocationChangedCallback));
            dictionary.Add(AccessibleEvents.Destroy, new BLL.Win32Api.WinEventProc(this.DestroyCallback));          
            return dictionary;
        }

        private void DestroyCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {             
            if (accEvent == AccessibleEvents.Destroy &&  TaskbarList.Any(x => x.TargetWnd.ToInt32() == windowHandle.ToInt32()))
            {                
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.DestroyHelper));
            } 
        }
    
        private void DestroyHelper(object state)
        {
            RemoveControls();

            pollingTimer.Start();            
        }

        private void LocationChangedCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {
            if (accEvent == AccessibleEvents.LocationChange && TaskbarList.Any(x => x.TrayWnd != IntPtr.Zero && x.TrayWnd.ToInt32() == windowHandle.ToInt32()))
            {                
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.LocationChangedHelper));
            }
        }

        private void LocationChangedHelper(object state)
        {
            UpdatePositions();
        }


        public void Dispose()
        {
            
        }
    }
}
