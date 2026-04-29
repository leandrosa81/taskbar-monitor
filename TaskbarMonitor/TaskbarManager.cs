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
using System.Timers;

namespace TaskbarMonitor
{
    public class Taskbar
    {
        public Taskbar(bool ismain = false)
        {
            this.IsMainTaskbar = ismain;
        }
        public bool IsMainTaskbar { get; protected set; }
        public IntPtr TargetWnd = IntPtr.Zero;
        public IntPtr TrayWnd = IntPtr.Zero;
        public IntPtr ClockWnd = IntPtr.Zero;
        public SystemWatcherControl TaskbarMonitorControl;        
        public Rectangle PreviousRect = Rectangle.Empty;
    }
    public class TaskbarManager: IDisposable
    {
        const int timeoutToRegisterAttemptAfterTaskbarRestart = 10000;
        private const int intervalToMonitorTaskbars = 4000;

        public Monitor Monitor { get; private set; }                

        List<Taskbar> TaskbarList = new List<Taskbar>();
        Taskbar MainTaskbar = null;        

        List<IntPtr> g_hook = new List<IntPtr>();
        List<GCHandle> gchs = new List<GCHandle>();

        System.Timers.Timer timer;

        public SystemWatcherControl MainControl
        {
            get
            {
                return MainTaskbar.TaskbarMonitorControl;
            }
        }

        private TaskbarManager()
        {            
            timer = new System.Timers.Timer(intervalToMonitorTaskbars);
            timer.AutoReset = true;
            timer.Elapsed += Timer_Elapsed;
            //timer.Start(); // we start only after first taskbars are create
            Options opt = TaskbarMonitor.Options.ReadFromDisk();
            this.Monitor = Monitor.GetInstance(opt);
            this.Monitor.OnOptionsUpdated += Monitor_OnOptionsUpdated;
            Microsoft.Win32.SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
        }

        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            if (e.Mode == Microsoft.Win32.PowerModes.Resume)
            {
                Debug.WriteLine("System Resume detected. Re-registering taskbars in 5 seconds...");
                // Delay re-registration to let Windows finish setting up displays
                System.Timers.Timer delayTimer = new System.Timers.Timer(5000);
                delayTimer.AutoReset = false;
                delayTimer.Elapsed += (s, ev) => {
                    if (TaskbarList.Count > 0)
                        TaskbarList[0].TaskbarMonitorControl?.Invoke(new Action(() => AddControlsToTaskbars()));
                    delayTimer.Dispose();
                };
                delayTimer.Start();
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (TaskbarList.Count > 0)
            {                
                TaskbarList[0].TaskbarMonitorControl?.Invoke(new Func<bool>(() => { return AddControlsToTaskbars(); }));
            }
        }

        private void Monitor_OnOptionsUpdated()
        {
            for(int i = 0; i < TaskbarList.Count; i++)
            {
                var item = TaskbarList[i];
                UpdatePosition(item, true);                
                
            }
        }

        public void UpdateAllPositions()
        {
            foreach (var item in TaskbarList)
            {
                UpdatePosition(item, true);
            }            
        }

        private void UpdatePosition(Taskbar taskbar, bool force = false)
        {
            

            var handle = taskbar.TargetWnd;
            Rectangle rect = BLL.Win32Api.GetWindowSize(handle);           
            Rectangle offset = Rectangle.Empty;
            if (taskbar.TaskbarMonitorControl != null)
            {
                if (taskbar.IsMainTaskbar)
                {
                    if (taskbar.TrayWnd != IntPtr.Zero)
                    {
                        offset = BLL.Win32Api.GetWindowSize(taskbar.TrayWnd);
                        // offset.Width += 20;
                    }
                }
                else
                {
                    if (taskbar.ClockWnd != IntPtr.Zero)
                    {
                        offset = BLL.Win32Api.GetWindowSize(taskbar.ClockWnd);
                    }
                    else if (WindowsInformation.IsWindows11_22621())
                    {
                        float scale = taskbar.TaskbarMonitorControl?.DpiScale ?? 1.0f;
                        offset = new Rectangle(0, 0, (int)(100 * scale), 0);
                    }
                }
            }
              
            if (force || taskbar.PreviousRect.Width == 0 || (offset.Width != taskbar.PreviousRect.Width && taskbar.TaskbarMonitorControl.IsHandleCreated))
            {
                Debug.WriteLine("UpdatePosition");
                taskbar.TaskbarMonitorControl?.Invoke((MethodInvoker)delegate
                {
                    var mopt = GetOptionsForTaskbar(taskbar);                    
                    taskbar.TaskbarMonitorControl.Visible = this.Monitor.Options.EnableOnAllMonitors || mopt == null || mopt.Enabled;
                    taskbar.TaskbarMonitorControl.Left =
                       (mopt == null || mopt.Position == MonitorOptions.DisplayPosition.RIGHT)
                       ? (rect.Width - taskbar.TaskbarMonitorControl.Width - offset.Width)
                       : 0;

                    RECT recDiff = new RECT();
                    recDiff.left = rect.Width - taskbar.TaskbarMonitorControl.Width - taskbar.PreviousRect.Width;
                    recDiff.top = 0;
                    recDiff.right = rect.Width - taskbar.TaskbarMonitorControl.Width - offset.Width;
                    recDiff.bottom = rect.Bottom;

                    int rawsize = Marshal.SizeOf(recDiff);
                    IntPtr ptr = Marshal.AllocHGlobal(rawsize);

                    Marshal.StructureToPtr(recDiff, ptr, true);
                     
                    var ret = BLL.WindowList.InvalidateRect(taskbar.TargetWnd, ptr, true);
                    Marshal.DestroyStructure(ptr, typeof(RECT));
                });

            }
            
            taskbar.PreviousRect = offset;
        }
        
        public bool AddControlsToTaskbars()
        {
            var everythingOK = true;

            string taskbarClass = "Shell_TrayWnd";
            string trayClass = "TrayNotifyWnd";

            // Find primary taskbar
            IntPtr primaryTaskbarHwnd = BLL.Win32Api.FindWindow(taskbarClass, null);
            if (primaryTaskbarHwnd != IntPtr.Zero)
            {
                IntPtr trayHwnd = BLL.Win32Api.FindWindowEx(primaryTaskbarHwnd, IntPtr.Zero, trayClass, null);
                if (trayHwnd != IntPtr.Zero)
                {
                    everythingOK &= AddControlToTaskbar(primaryTaskbarHwnd, trayHwnd, true);
                }
            }

            if (BLL.WindowsInformation.IsWindows11())
            {
                taskbarClass = "Shell_SecondaryTrayWnd";
                trayClass = "Windows.UI.Composition.DesktopWindowContentBridge";

                // Find secondary taskbars
                IntPtr secondaryTaskbarHwnd = BLL.Win32Api.FindWindowEx(IntPtr.Zero, IntPtr.Zero, taskbarClass, null);
                while (secondaryTaskbarHwnd != IntPtr.Zero)
                {
                    IntPtr clockArea = !WindowsInformation.IsWindows11_22621() ? BLL.Win32Api.FindWindowEx(secondaryTaskbarHwnd, IntPtr.Zero, trayClass, null) : IntPtr.Zero;
                    everythingOK &= AddControlToTaskbar(secondaryTaskbarHwnd, clockArea, false);

                    // Move to the next one
                    secondaryTaskbarHwnd = BLL.Win32Api.FindWindowEx(IntPtr.Zero, secondaryTaskbarHwnd, taskbarClass, null);
                }
            }

            if (everythingOK)
            {
               HookEvents();
                if (!timer.Enabled)
                    timer.Start();
            }
            return everythingOK;
        }

        private MonitorOptions GetOptionsForTaskbar(Taskbar tb)
        {
            MonitorOptions mopt = null;           
            
            // if there is no device that match options and the system returns single setting or default settings if none exists
            if(!this.Monitor.Options.MonitorOptions.Any(x => Screen.AllScreens.Select(y => y.DeviceName).ToList().Any(y=> y == x.Key)))
            {
                if(this.Monitor.Options.MonitorOptions.Count == 1)
                    return this.Monitor.Options.MonitorOptions.Values.FirstOrDefault();
                else
                    return new MonitorOptions();
            }
            var pos = BLL.Win32Api.GetWindowSize(tb.TargetWnd);
            foreach (var item in this.Monitor.Options.MonitorOptions)
            {
                var monitor = Screen.AllScreens.Where(x => x.DeviceName == item.Key).SingleOrDefault();
                if (monitor == null)
                    continue;
                if (pos.IntersectsWith(monitor.Bounds))                
                {                    
                    mopt = item.Value;
                }
            }
            if(mopt == null)
            {                
                return new MonitorOptions();
            }
            return mopt;            
        }

        private bool AddControlToTaskbar(IntPtr taskbarHandle, IntPtr trayHandle, bool isMainTaskbar)
        {
            Debug.WriteLine("AddControlToTaskbar");

            if (TaskbarList.Any(x => x.TargetWnd == taskbarHandle))
                return true;

            Taskbar tb = TaskbarList.Where(x => x.TargetWnd == taskbarHandle).SingleOrDefault();
            if(tb == null)
            {
                tb = new Taskbar(isMainTaskbar);
                TaskbarList.Add(tb);
                tb.TargetWnd = taskbarHandle;
            }
            
            if (isMainTaskbar)
            {
                MainTaskbar = tb;
                tb.TrayWnd = trayHandle;
            }       
            else if(trayHandle != IntPtr.Zero)
            {
                tb.ClockWnd = trayHandle;
            }

            var taskbarMonitorControl = new SystemWatcherControl(this.Monitor);
            tb.TaskbarMonitorControl = taskbarMonitorControl;
            taskbarMonitorControl.Name = "taskbarMonitorFor" + taskbarHandle;


            BLL.Win32Api.SetParent(taskbarMonitorControl.Handle, taskbarHandle);

            if (WindowsInformation.IsWindows11_22621())
            {
                Win32Api.SetWindowLong(taskbarMonitorControl.Handle, Win32Api.GWLParameter.GWL_EXSTYLE, (uint)(0x00000000L | 0x00010000L | 0x00080000 | 0x02000000L | 0x00000020L));
                Win32Api.SetLayeredWindowAttributes(taskbarMonitorControl.Handle, 0, 255, 0x00000001 | 0x00000002);
            }
            taskbarMonitorControl.Show();

            UpdatePosition(tb);

            return true;
        }

        public bool ApplyOptions(Options options)
        {
            foreach (var taskbar in this.TaskbarList)
            {
                taskbar.TaskbarMonitorControl.ApplyOptions(options);
            }
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
            Debug.WriteLine("RemoveControls");
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
                    bool isMain = taskbar.IsMainTaskbar;
                    RemoveControl(taskbar);
                    
                    if(isMain)
                    {
                        // Don't block the thread with Sleep. 
                        // Instead, let the existing 4-second timer (Timer_Elapsed) handle the re-registration.
                        Debug.WriteLine("Main taskbar destroyed. Re-registration will be handled by timer.");
                    }
                }
            } 
        }
     
        private void LocationChangedCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {
            if (accEvent == AccessibleEvents.LocationChange)
            {
                var taskbar = TaskbarList.Where(x => x.TrayWnd.ToInt32() == windowHandle.ToInt32()).SingleOrDefault();
                if (taskbar != null)
                {
                    UpdatePosition(taskbar);
                }
            }
        }
         
        public void Dispose()
        {
            Microsoft.Win32.SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
            UnhookEvents();
            timer.Stop();
            timer.Dispose();    
        }
        private static TaskbarManager _instance = null;
        public static TaskbarManager GetInstance()
        {
            if (_instance == null) _instance = new TaskbarManager();
            return _instance;
        }
    }
}
