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

        public Monitor Monitor { get; private set; }                

        List<Taskbar> TaskbarList = new List<Taskbar>();
        Taskbar MainTaskbar = null;        

        List<IntPtr> g_hook = new List<IntPtr>();
        List<GCHandle> gchs = new List<GCHandle>();
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
            pollingTimer.Stop();

            if (g_hook.Count == 0)
            {
               // TaskbarMonitor.BLL.WindowList.SendMessage(0xffff, 0x0210, IntPtr.Zero, IntPtr.Zero);
            }

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
                    if(taskbar.PreviousRect.Width == 0)
                        taskbar.PreviousRect = rectTray;
                    //if (rectTray.Width < taskbar.PreviousRect.Width)
                    {
                        if (taskbar.TaskbarMonitorControl.IsHandleCreated)
                        {
                            taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                            {
                                /*
                                RECT recDiff = new RECT();
                                recDiff.left = rect.Width - taskbar.TaskbarMonitorControl.Width - taskbar.PreviousRect.Width;
                                recDiff.top = 0;
                                recDiff.right = rect.Width - taskbar.TaskbarMonitorControl.Width - rectTray.Width;
                                recDiff.bottom = rect.Bottom;

                                int rawsize = Marshal.SizeOf(recDiff);
                                IntPtr ptr = Marshal.AllocHGlobal(rawsize);

                                Marshal.StructureToPtr(recDiff, ptr, true);
                                */
                                taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width - rectTray.Width;
                                var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
                            });
                            
                        }                        
                    }
                    
                }
                else if (taskbar.ClockWnd != IntPtr.Zero && taskbar.TaskbarMonitorControl != null)
                {
                    var rectTray = BLL.Win32Api.GetWindowSize(taskbar.ClockWnd);
                    if (rectTray.Width != taskbar.PreviousRect.Width)
                    {
                        taskbar.PreviousRect = rectTray;
                        if (taskbar.TaskbarMonitorControl.IsHandleCreated)
                        {
                            taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                            {
                                taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width - rectTray.Width;
                                var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
                            });
                        }
                    }

                }
                else if (taskbar.TaskbarMonitorControl != null)
                {
                    if (taskbar.TaskbarMonitorControl.IsHandleCreated)
                    {
                        taskbar.TaskbarMonitorControl.Invoke((MethodInvoker)delegate
                        {
                            taskbar.TaskbarMonitorControl.Left = rect.Width - taskbar.TaskbarMonitorControl.Width;
                            var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, IntPtr.Zero, true);
                        });
                    }
                }
            }
        }

        public bool AddControlsWin11(bool restart = false)
        {
            Debug.WriteLine("AddControlsWin11");
            List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();

            var taskarea = windowListExtended.Where(w => w.Class == "Shell_TrayWnd").SingleOrDefault();
            
            if (taskarea == null)
            {
                //PollingTimer.Start();
                return false;
            }
           

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

            AddControlsExtraMonitors(restart);

            return true;
        }
        
        public bool AddControlsExtraMonitors(bool restart = false)
        {
            Debug.WriteLine("AddControlsExtraMonitors");


            WindowInformation taskarea = null;
            do
            {
                List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();
                taskarea = windowListExtended.Where(w => w.Class == "Shell_SecondaryTrayWnd").SingleOrDefault();
                
                if(taskarea == null)
                {
                    if (!restart)
                        return false;
                    else
                        Thread.Sleep(2000);
                }                
            } while (taskarea == null && restart);



            Taskbar tb = new Taskbar();
            TaskbarList.Add(tb);

            if (BLL.WindowsInformation.IsWindows11())
            {
                // item.ChildWindows.Where(x => x.Class == "Windows.UI.Composition.DesktopWindowContentBridge").SingleOrDefault();
                if (taskarea != null)
                {
                    tb.TargetWnd = taskarea.Handle;

                    var clock = taskarea.ChildWindows.Where(x => x.Class == "Windows.UI.Composition.DesktopWindowContentBridge").LastOrDefault();
                    var rectTray = Rectangle.Empty;
                    if (clock != null)
                    {
                        tb.ClockWnd = clock.Handle;
                        rectTray = BLL.Win32Api.GetWindowSize(clock.Handle);
                    }

                    var rect = BLL.Win32Api.GetWindowSize(taskarea.Handle);

                    var OnTopControl = new SystemWatcherControl(this.Monitor);
                    OnTopControl.Name = "Secondary";
                    OnTopControl.Left = rect.Width - OnTopControl.Width - rectTray.Width;
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
            if (accEvent == AccessibleEvents.Destroy &&  TaskbarList.Any(x => x.TargetWnd.ToInt32() == windowHandle.ToInt32()))
            {
                Debug.WriteLine("DestroyCallback");
                UnhookEvents();
                RemoveControls();
                while (!AddControlsWin11())
                {
                    Thread.Sleep(2000);
                }
                //ThreadPool.QueueUserWorkItem(new WaitCallback(this.DestroyHelper));                
            } 
        }
    
        private void DestroyHelper(object state)
        {
            Debug.WriteLine("DestroyHelper");
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
