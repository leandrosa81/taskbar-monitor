using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TaskbarMonitor.BLL;

using System.Diagnostics;
using System.Drawing;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TaskbarMonitor
{
    public class Taskbar
    {
        public IntPtr TargetWnd = IntPtr.Zero;
        public IntPtr TrayWnd = IntPtr.Zero;
        public IntPtr ClockWnd = IntPtr.Zero;
        public SystemWatcherControl TaskbarMonitorControl;        
        public Rectangle PreviousRect = Rectangle.Empty;
    }
    public class TaskbarManager: IDisposable
    {
        const int timeoutTimeRegisterAttemptAfterTaskbarRestart = 10000;

        public Monitor Monitor { get; private set; }                

        List<Taskbar> TaskbarList = new List<Taskbar>();
        Taskbar MainTaskbar = null;        

        List<IntPtr> g_hook = new List<IntPtr>();
        List<GCHandle> gchs = new List<GCHandle>();        

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
        }

        private void UpdatePosition(Taskbar taskbar)
        {
            var handle = taskbar.TargetWnd;
            Rectangle rect = BLL.Win32Api.GetWindowSize(handle);           
            Rectangle offset = Rectangle.Empty;

            if (taskbar.TrayWnd != IntPtr.Zero && taskbar.TaskbarMonitorControl != null)
            {
                offset = BLL.Win32Api.GetWindowSize(taskbar.TrayWnd);
                // offset.Width += 20;
            }
            else if (taskbar.ClockWnd != IntPtr.Zero && taskbar.TaskbarMonitorControl != null)
            {
                offset = BLL.Win32Api.GetWindowSize(taskbar.ClockWnd);                
            }

            if (taskbar.PreviousRect.Width == 0 || (offset.Width != taskbar.PreviousRect.Width && taskbar.TaskbarMonitorControl.IsHandleCreated))
            {
                taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                {
                    
                    RECT recDiff = new RECT();
                    recDiff.left = rect.Width - taskbar.TaskbarMonitorControl.Width - taskbar.PreviousRect.Width;
                    recDiff.top = 0;
                    recDiff.right = rect.Width - taskbar.TaskbarMonitorControl.Width - offset.Width;
                    recDiff.bottom = rect.Bottom;

                    int rawsize = Marshal.SizeOf(recDiff);
                    IntPtr ptr = Marshal.AllocHGlobal(rawsize);

                    Marshal.StructureToPtr(recDiff, ptr, true);
                    
                    taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width - offset.Width;
                    var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, ptr, true);
                    Marshal.DestroyStructure(ptr, typeof(RECT));
                });

            }
            taskbar.PreviousRect = offset;
        }
        private void UpdatePositions()
        {            
            foreach (var taskbar in TaskbarList)
            {
                UpdatePosition(taskbar);
            }
        }

        public bool AddControlsToTaskbars()
        {
            var everythingOK = true;

            List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();

            string taskbarClass = "Shell_TrayWnd";
            string trayClass = "TrayNotifyWnd";

            var taskbarArea = windowListExtended.Where(w => w.Class == taskbarClass).SingleOrDefault();
            if (taskbarArea == null) return false;

            var trayArea = taskbarArea.ChildWindows.Where(x => x.Class == trayClass).SingleOrDefault();
            if (trayArea == null) return false;

            everythingOK &= AddControlToTaskbar(taskbarArea, trayArea, true);

            if (BLL.WindowsInformation.IsWindows11())
            {
                taskbarClass = "Shell_SecondaryTrayWnd";
                trayClass = "Windows.UI.Composition.DesktopWindowContentBridge";

                var taskbarsAreas = windowListExtended.Where(w => w.Class == taskbarClass).ToList();

                foreach (var tbArea in taskbarsAreas)
                {
                    var clockArea = tbArea.ChildWindows.Where(x => x.Class == trayClass).LastOrDefault();                
                    everythingOK &= AddControlToTaskbar(tbArea, clockArea, false);
                }
            }

            HookEvents();
            return everythingOK;
        }

        private bool AddControlToTaskbar(WindowInformation taskbarArea, WindowInformation trayArea, bool isMainTaskbar = true)
        {
            Debug.WriteLine("AddControlToTaskbar");

            if (TaskbarList.Any(x => x.TaskbarMonitorControl.Name == "taskbarMonitorFor" + taskbarArea.Handle))
                return false;
               
            Taskbar tb = new Taskbar();
            TaskbarList.Add(tb);
            
            tb.TargetWnd = taskbarArea.Handle;
            if (isMainTaskbar)
            {
                MainTaskbar = tb;
                tb.TrayWnd = trayArea.Handle;
            }       
            else if(trayArea != null)
            {
                tb.ClockWnd = trayArea.Handle;
            }

            var rect = BLL.Win32Api.GetWindowSize(taskbarArea.Handle);
            var rectTray = trayArea != null ? BLL.Win32Api.GetWindowSize(trayArea.Handle) : Rectangle.Empty;

            var taskbarMonitorControl = new SystemWatcherControl(this.Monitor);
            tb.TaskbarMonitorControl = taskbarMonitorControl;
            taskbarMonitorControl.Name = "taskbarMonitorFor" + taskbarArea.Handle;
            // taskbarMonitorControl.Left = rect.Width - taskbarMonitorControl.Width - rectTray.Width;
            

            BLL.Win32Api.SetParent(taskbarMonitorControl.Handle, taskbarArea.Handle);
            taskbarMonitorControl.Show();

            UpdatePosition(tb);

            return true;
        }
         
        private void HookEvents()
        {
            if (g_hook.Count > 0)
                return;
            
            Debug.WriteLine("HookEvents");
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> events = InitializeWinEventToHandlerMap();

            //Hook window close event - close our HoverContorl on Target window close.
            BLL.Win32Api.WinEventProc eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.LocationChange].Invoke);

            GCHandle gch = GCHandle.Alloc(eventHandler);
            gchs.Add(gch);

            g_hook.Add(BLL.Win32Api.SetWinEventHook(AccessibleEvents.LocationChange,
       AccessibleEvents.LocationChange, IntPtr.Zero, eventHandler
       , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT));

            //Hook window close event - close our HoverContorl on Target window close.
            eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.Destroy].Invoke);

            gch = GCHandle.Alloc(eventHandler);
            gchs.Add(gch);

            g_hook.Add(BLL.Win32Api.SetWinEventHook(AccessibleEvents.Destroy,
                AccessibleEvents.LocationChange, IntPtr.Zero, eventHandler
                , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT));
        }

        private void UnhookEvents()
        {
            Debug.WriteLine("UnhookEvents");
            foreach (var gh in g_hook)
            {
                BLL.Win32Api.UnhookWinEvent(gh);
            }
            g_hook.Clear();
            foreach (var gch in gchs)
            {
                gch.Free();
            }
        }
         

        public void RemoveControls()
        {
            Debug.WriteLine("RemoveControls");
            UnhookEvents();
           
            foreach (var taskbar in TaskbarList)
            {
                if (taskbar.TaskbarMonitorControl.Created && taskbar.TaskbarMonitorControl.IsHandleCreated)
                {
                    taskbar.TaskbarMonitorControl?.Invoke((MethodInvoker)delegate
                    {
                        taskbar.TaskbarMonitorControl?.Hide();
                        taskbar.TaskbarMonitorControl?.Dispose();
                    });
                }
                 
                var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
            }
            TaskbarList.Clear();
        }

        private void RemoveControl(Taskbar taskbar)
        {
            if (taskbar.TaskbarMonitorControl.Created && taskbar.TaskbarMonitorControl.IsHandleCreated)
            {
                taskbar.TaskbarMonitorControl?.Invoke((MethodInvoker)delegate
                {
                    taskbar.TaskbarMonitorControl?.Hide();
                    taskbar.TaskbarMonitorControl?.Dispose();
                });
            }

            var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
            TaskbarList.Remove(taskbar);
        }
         
        private Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> InitializeWinEventToHandlerMap()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> dictionary = new Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc>
            {
                { AccessibleEvents.LocationChange, new BLL.Win32Api.WinEventProc(this.LocationChangedCallback) },
                { AccessibleEvents.Destroy, new BLL.Win32Api.WinEventProc(this.DestroyCallback) }
            };
            return dictionary;
        }

        private void DestroyCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {            
            if (accEvent == AccessibleEvents.Destroy)
            {
                
                var taskbar = TaskbarList.Where(x => x.TargetWnd.ToInt32() == windowHandle.ToInt32()).SingleOrDefault();
                if(taskbar != null)
                {
                    Debug.WriteLine("DestroyCallback");
                    RemoveControl(taskbar);
                    int timeElapsed = 0;                    
                    const int timeStep = 1000;
                    while (!AddControlsToTaskbars() && timeElapsed < timeoutTimeRegisterAttemptAfterTaskbarRestart)
                    {
                        Thread.Sleep(timeStep);
                        timeElapsed += timeStep;
                    }
                }
            } 
        }
     
        private void LocationChangedCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {
            if (accEvent == AccessibleEvents.LocationChange) // && TaskbarList.Any(x => x.TrayWnd != IntPtr.Zero && x.TrayWnd.ToInt32() == windowHandle.ToInt32())
            {
                var taskbar = TaskbarList.Where(x => x.TrayWnd.ToInt32() == windowHandle.ToInt32()).SingleOrDefault();
                if (taskbar != null)
                {
                    Debug.WriteLine("LocationChangedCallback");
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(this.LocationChangedHelper));
                    UpdatePosition(taskbar);
                }
            }
        }

        private void LocationChangedHelper(object state)
        {
            UpdatePositions();
        }
         
        public void Dispose()
        {
            UnhookEvents();
        }
    }
}
