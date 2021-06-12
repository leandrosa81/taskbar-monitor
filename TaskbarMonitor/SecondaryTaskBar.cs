using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using TaskbarMonitor.BLL;

namespace TaskbarMonitor
{
    class SecondaryTaskBar
    {
        private delegate void Execute();
        private List<IntPtr> TargetWnd = new List<IntPtr>();//new IntPtr(0);        
        IntPtr g_hook;            
        private SystemWatcherControl OnTopControl;

        public bool AddControl()
        {
            List<WindowInformation> windowListExtended = WindowList.GetAllWindowsExtendedInfo();

            var list = windowListExtended.Where(
                           w => w.Class == "Shell_SecondaryTrayWnd"
                       ).ToList();
            if(list.Count == 0)
            {
                return false;
            }
            /*var TargetWnd = BLL.Win32Api.Find("Shell_SecondaryTrayWnd", "");
            if (TargetWnd == null)
            {
                return false;
            }*/
            foreach (var item in list)
            {
                var worker = item.ChildWindows.Where(x => x.Class == "WorkerW").SingleOrDefault();
                if(worker != null)
                {
                    var taskarea = worker.ChildWindows.Where(x => x.Class == "MSTaskListWClass").SingleOrDefault();
                    if(taskarea != null)
                    {
                        TargetWnd.Add(taskarea.Handle);

                        var rect = BLL.Win32Api.GetTaskBarSize(taskarea.Handle);

                        OnTopControl = new SystemWatcherControl(false);
                        OnTopControl.Name = "Secondary";
                        OnTopControl.Left = rect.Width - OnTopControl.Width;
                        OnTopControl.Show();

                        BLL.Win32Api.SetWindowLong(OnTopControl.Handle, BLL.Win32Api.GWLParameter.GWL_HWNDPARENT, taskarea.Handle.ToInt32());
                    }
                }                
            }
           


            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> events = InitializeWinEventToHandlerMap();
           
            //Hook window close event - close our HoverContorl on Target window close.
            BLL.Win32Api.WinEventProc eventHandler = eventHandler = new BLL.Win32Api.WinEventProc(events[AccessibleEvents.Destroy].Invoke);

            GCHandle gch = GCHandle.Alloc(eventHandler);

            g_hook = BLL.Win32Api.SetWinEventHook(AccessibleEvents.Destroy,
                AccessibleEvents.LocationChange, IntPtr.Zero, eventHandler
                , 0, 0, BLL.Win32Api.SetWinEventHookParameter.WINEVENT_OUTOFCONTEXT);
             
            return true;

        }

        private Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> InitializeWinEventToHandlerMap()
        {
            Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc> dictionary = new Dictionary<AccessibleEvents, BLL.Win32Api.WinEventProc>();
            //You can add more events like ValueChanged - for more info please read - 
            //http://msdn.microsoft.com/en-us/library/system.windows.forms.accessibleevents.aspx
            //dictionary.Add(AccessibleEvents.ValueChange, new BLL.Win32Api.WinEventProc(this.ValueChangedCallback));
            //dictionary.Add(AccessibleEvents.LocationChange, new BLL.Win32Api.WinEventProc(this.LocationChangedCallback));
            dictionary.Add(AccessibleEvents.Destroy, new BLL.Win32Api.WinEventProc(this.DestroyCallback));

            return dictionary;
        }

        private void DestroyCallback(IntPtr winEventHookHandle, AccessibleEvents accEvent, IntPtr windowHandle, int objectId, int childId, uint eventThreadId, uint eventTimeInMilliseconds)
        {/*
            //Make sure AccessibleEvents equals to LocationChange and the current window is the Target Window.
            if (accEvent == AccessibleEvents.Destroy && windowHandle.ToInt32() == TargetWnd.ToInt32())
            {
                //Queues a method for execution. The method executes when a thread pool thread becomes available.
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.DestroyHelper));
            }*/
        }
         

        private void DestroyHelper(object state)
        {
            /*
            Execute ex = delegate ()
            {
                //Removes an event hook function created by a previous call to 
                BLL.Win32Api.UnhookWinEvent(g_hook);
                //Close HoverControl window.
                OnTopControl.Dispose();
            };
            */
        }         
    }
}
