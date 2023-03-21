using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskbarMonitor.BLL
{
    internal struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uCallbackMessage;
        public int uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    internal struct RECT
    {
        public int left, top, right, bottom;
    }
     
    public class Win32Api
    {
        internal const int ABM_GETTASKBARPOS = 5;

        //Specifies the zero-based offset to the value to be set.
        //Valid values are in the range zero through the number of bytes of extra window memory, 
        //minus the size of an integer.
        public enum GWLParameter
        {
            GWL_EXSTYLE = -20, //Sets a new extended window style
            GWL_HINSTANCE = -6, //Sets a new application instance handle.
            GWL_HWNDPARENT = -8, //Set window handle as parent
            GWL_ID = -12, //Sets a new identifier of the window.
            GWL_STYLE = -16, // Set new window style
            GWL_USERDATA = -21, //Sets the user data associated with the window. 
                                //This data is intended for use by the application 
                                //that created the window. Its value is initially zero.
            GWL_WNDPROC = -4 //Sets a new address for the window procedure.

        }

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(int msg, ref APPBARDATA data);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        private static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);

        // Get a handle to an application window.
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        internal static extern bool DestroyWindow(IntPtr hWnd);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        internal static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        ///The SetWindowLongPtr function changes an attribute of the specified window
        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        internal static extern int SetWindowLong32
            (HandleRef hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        internal static extern int SetWindowLong32
            (IntPtr windowHandle, GWLParameter nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        internal static extern IntPtr SetWindowLongPtr64
            (IntPtr windowHandle, GWLParameter nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        internal static extern IntPtr SetWindowLongPtr64
            (HandleRef hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetParent")]
        internal static extern IntPtr SetParent(IntPtr windowHandle, IntPtr parentHandle);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetWindowLong(IntPtr hWnd, GWLParameter nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint SetWindowLong(IntPtr hWnd, GWLParameter nIndex, uint pos);

        [DllImport("user32.dll")]
        internal static extern bool SetLayeredWindowAttributes(IntPtr hWnd, uint crKey, byte bAlpha, uint dwFlags );


        [DllImport("user32.dll", EntryPoint = "BringWindowToTop")]
        internal static extern bool BringWindowToTop(IntPtr windowHandle);

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        internal static extern bool SetWindowPos(IntPtr hWnd,IntPtr hWndInsertAfter,int X,int Y,int cx,int cy,uint uFlags);

        [DllImport("user32.dll", EntryPoint = "EnableWindow")]
        internal static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool UnhookWinEvent(IntPtr eventHookHandle);
        
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll")]
        internal static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool PostMessageA(IntPtr hWnd, uint Msg, uint wparam, uint lparam);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(
    AccessibleEvents eventMin,  //Specifies the event constant for the 
                                //lowest event value in the range of events that are 
                                //handled by the hook function. This parameter can 
                                //be set to EVENT_MIN to indicate the 
                                //lowest possible event value.
    AccessibleEvents eventMax,  //Specifies the event constant for the highest event 
                                //value in the range of events that are handled 
                                //by the hook function. This parameter can be set 
                                //to EVENT_MAX to indicate the highest possible 
                                //event value.
    IntPtr eventHookAssemblyHandle,     //Handle to the DLL that contains the hook 
                                        //function at lpfnWinEventProc, if the 
                                        //WINEVENT_INCONTEXT flag is specified in the 
                                        //dwFlags parameter. If the hook function is not 
                                        //located in a DLL, or if the WINEVENT_OUTOFCONTEXT 
                                        //flag is specified, this parameter is NULL.
    WinEventProc eventHookHandle,   //Pointer to the event hook function. 
                                    //For more information about this function
    uint processId,         //Specifies the ID of the process from which the 
                            //hook function receives events. Specify zero (0) 
                            //to receive events from all processes on the 
                            //current desktop.
    uint threadId,          //Specifies the ID of the thread from which the 
                            //hook function receives events. 
                            //If this parameter is zero, the hook function is 
                            //associated with all existing threads on the 
                            //current desktop.
    SetWinEventHookParameter parameterFlags //Flag values that specify the location 
                                            //of the hook function and of the events to be 
                                            //skipped. The following flags are valid:
    );
        internal delegate void WinEventProc(IntPtr winEventHookHandle, AccessibleEvents accEvent,
    IntPtr windowHandle, int objectId, int childId, uint eventThreadId,
    uint eventTimeInMilliseconds);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();

        [Flags]
        internal enum SetWinEventHookParameter
        {
            WINEVENT_INCONTEXT = 4,
            WINEVENT_OUTOFCONTEXT = 0,
            WINEVENT_SKIPOWNPROCESS = 2,
            WINEVENT_SKIPOWNTHREAD = 1
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct TITLEBARINFO
        {
            public const int CCHILDREN_TITLEBAR = 5;
            public uint cbSize; //Specifies the size, in bytes, of the structure. 
                                //The caller must set this to sizeof(TITLEBARINFO).

            public RECT rcTitleBar; //Pointer to a RECT structure that receives the 
                                    //coordinates of the title bar. These coordinates include all title-bar elements
                                    //except the window menu.

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]

            //Add reference for System.Windows.Forms
            public AccessibleStates[] rgstate;
            //0    The title bar itself.
            //1    Reserved.
            //2    Minimize button.
            //3    Maximize button.
            //4    Help button.
            //5    Close button.
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
        }


        public static Rectangle GetTaskbarPosition()
        {            
            APPBARDATA data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(data);

            IntPtr retval = SHAppBarMessage(ABM_GETTASKBARPOS, ref data);
            if (retval == IntPtr.Zero)
            {
                throw new Win32Exception("Please re-install Windows");
            }

            return new Rectangle(data.rc.left, data.rc.top, data.rc.right - data.rc.left, data.rc.bottom - data.rc.top);            
        }

        public static Rectangle GetWindowSize(IntPtr handle)
        {
            RECT rct;

            if (!GetWindowRect(handle, out rct))
            {
                return Rectangle.Empty;
            }
            var myRect = new Rectangle();
            myRect.X = rct.left;
            myRect.Y = rct.top;
            myRect.Width = rct.right - rct.left;
            myRect.Height = rct.bottom - rct.top;
            return myRect;
        }

        public static Rectangle GetWorkingAreaSize()
        {
            RECT rct;

            if (!GetWindowRect(GetDesktopWindow(), out rct))
            {
                return Rectangle.Empty;
            }
            var myRect = new Rectangle();
            myRect.X = rct.left;
            myRect.Y = rct.top;
            myRect.Width = rct.right - rct.left;
            myRect.Height = rct.bottom - rct.top;
            return myRect;
        }



        public static Color GetColourAt(Point location)
        {
            using (Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb))
            using (Graphics gdest = Graphics.FromImage(screenPixel))
            {
                using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
                {
                    IntPtr hSrcDC = gsrc.GetHdc();
                    IntPtr hDC = gdest.GetHdc();
                    int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
                    gdest.ReleaseHdc();
                    gsrc.ReleaseHdc();
                }

                return screenPixel.GetPixel(0, 0);
            }
        }

        public static IntPtr Find(string ModuleName, string MainWindowTitle)
        {
            //Search the window using Module and Title
            IntPtr WndToFind = FindWindow(ModuleName, MainWindowTitle);
            if (WndToFind.Equals(IntPtr.Zero))
            {
                if (!string.IsNullOrEmpty(MainWindowTitle))
                {
                    //Search window using TItle only.
                    WndToFind = FindWindowByCaption(WndToFind, MainWindowTitle);
                    if (WndToFind.Equals(IntPtr.Zero))
                        return new IntPtr(0);
                }
            }
            return WndToFind;
        }

        public static int SetWindowLong(IntPtr windowHandle, GWLParameter nIndex, int dwNewLong)
        {
            if (IntPtr.Size == 8) //Check if this window is 64bit
            {
                return (int)SetWindowLongPtr64
                (windowHandle, nIndex, new IntPtr(dwNewLong));
            }
            return SetWindowLong32(windowHandle, nIndex, dwNewLong);
        }

        public static IntPtr SetWindowLongPtr(HandleRef hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) //Check if this window is 64bit
            {
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            }
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        


    }
}
