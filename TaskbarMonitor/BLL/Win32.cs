using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TaskbarMonitor.BLL
{

    public static class WindowsInformation
    {
        public static bool IsWindows11()
        {
            return System.Environment.OSVersion.Version.Major >= 10 && System.Environment.OSVersion.Version.Build >= 21996;
        }
    }
    /// <summary>
    /// Static class that lists all windows
    /// </summary>
    public static class WindowList
    {

        #region Call these to return window lists

        

        /// <summary>
        /// Gets all windows with basic information: caption, class and handle.
        /// The list starts with the desktop.
        /// </summary>
        /// <returns>WindowInformation list</returns>
        public static List<WindowInformation> GetAllWindows()
        {
            IntPtr desktopWindow = GetDesktopWindow();
            List<WindowInformation> winInfo = new List<WindowInformation>();
            winInfo.Add(winInfoGet(desktopWindow));
            List<IntPtr> handles = getChildWindows(desktopWindow);
            foreach (IntPtr handle in handles)
            {
                try
                {
                    winInfo.Add(winInfoGet(handle));
                }
                catch (Exception ex) { }
            }
            return winInfo;
        }

        /// <summary>
        /// Gets all windows with extended information: caption, class, handle
        /// parent, children, and siblings. The list starts with the desktop.
        /// </summary>
        /// <returns>WindowInformationList</returns>
        public static List<WindowInformation> GetAllWindowsExtendedInfo()
        {
            return winInfoExtendedInfoProcess(GetAllWindowsTree());
        }

        /// <summary>
        /// Gets all windows in nested objects usefule for adding to a TreeView.
        /// Includes the extended information: caption, class, handle, parent,
        /// children, and siblings. The list starts with the desktop.
        /// </summary>
        /// <returns>Desktop WindowInformation object with nested children</returns>
        public static WindowInformation GetAllWindowsTree()
        {
            WindowInformation desktopWindow = winInfoGet(GetDesktopWindow());
            desktopWindow.ChildWindows = getChildWindowsInfo(desktopWindow);
            return desktopWindow;
        }

        #endregion

        #region Unmanaged Code References

        /// <summary>
        /// Enumerates the child windows that belong to the specified parent 
        /// window by passing the handle to each child window, in turn, to an 
        /// application-defined callback function. EnumChildWindows continues 
        /// until the last child window is enumerated or the callback function 
        /// returns FALSE.
        /// </summary>
        /// <param name="window">A handle to the parent window whose child windows are to be enumerated. If this parameter is NULL, this function is equivalent to EnumWindows.</param>
        /// <param name="callback">A pointer to an application-defined callback function.</param>
        /// <param name="i">An application-defined value to be passed to the callback function.</param>
        /// <returns>Return FALSE</returns>
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

        /// <summary>
        /// Retrieves a handle to a window whose class name and window name match the specified strings. 
        /// The function searches child windows, beginning with the one following the specified child window. 
        /// This function does not perform a case-sensitive search.
        /// </summary>
        /// <param name="hwndParent">A handle to the parent window whose child windows are to be searched. If hwndParent is NULL, the function uses the desktop window as the parent window. The function searches among windows that are child windows of the desktop. If hwndParent is HWND_MESSAGE, the function searches all message-only windows.</param>
        /// <param name="hwndChildAfter">A handle to a child window. The search begins with the next child window in the Z order. The child window must be a direct child window of hwndParent, not just a descendant window. If hwndChildAfter is NULL, the search begins with the first child window of hwndParent. Note that if both hwndParent and hwndChildAfter are NULL, the function searches all top-level and message-only windows.</param>
        /// <param name="lpszClass">Optional: The class name or a class atom created by a previous call to the RegisterClass or RegisterClassEx function. The atom must be placed in the low-order word of lpszClass; the high-order word must be zero. If lpszClass is a string, it specifies the window class name. The class name can be any name registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names, or it can be MAKEINTATOM(0x8000). In this latter case, 0x8000 is the atom for a menu class. For more information, see the Remarks section of this topic.</param>
        /// <param name="lpszWindow">Optional: The window name (the window's title). If this parameter is NULL, all window names match.</param>
        /// <returns></returns>
        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        /// <summary>
        /// Retrieves the name of the class to which the specified window belongs.
        /// </summary>
        /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
        /// <param name="lpClassName">The class name string.</param>
        /// <param name="nMaxCount">The length of the lpClassName buffer, in characters. The buffer must be large enough to include the terminating null character; otherwise, the class name string is truncated to nMaxCount-1 characters.</param>
        /// <returns>If the function succeeds, the return value is the number of characters copied to the buffer, not including the terminating null character. If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        /// <summary>
        /// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The 
        /// desktop window is the area on top of which other windows are painted.
        /// </summary>
        /// <returns>The return value is a handle to the desktop window</returns>
        [DllImport("user32.dll", SetLastError = false)]
        private static extern IntPtr GetDesktopWindow();

        /// <summary>
        /// Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to 
        /// the specified window.
        /// </summary>
        /// <param name="hWnd">A handle to a window. The window handle retrieved is relative to this window, based on the value of the uCmd parameter.</param>
        /// <param name="uCmd">The relationship between the specified window and the window whose handle is to be retrieved. This parameter can be one of the following values.</param>
        /// <returns>If the function succeeds, the return value is a window handle. If no window exists with the specified relationship to the specified window, the return value is NULL. To get extended error information, call GetLastError.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        /// <summary>
        /// Copies the text of the specified window's title bar (if it has one) into a buffer. If the 
        /// specified window is a control, the text of the control is copied. However, GetWindowText 
        /// cannot retrieve the text of a control in another application.
        /// </summary>
        /// <param name="hWnd">A handle to the window or control containing the text.</param>
        /// <param name="text">The buffer that will receive the text. If the string is as long or longer than the buffer, the string is truncated and terminated with a null character.</param>
        /// <param name="count">The maximum number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated.</param>
        /// <returns>If the function succeeds, the return value is the length, in characters, of the copied string, not including the terminating null character. If the window has no title bar or text, if the title bar is empty, or if the window or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.</returns>       
        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        /// <summary>
        /// Sends the specified message to a window or windows. The SendMessage function calls the 
        /// window procedure for the specified window and does not return until the window procedure 
        /// has processed the message.
        /// </summary>
        /// <param name="hwnd">A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.</param>
        /// <param name="wmConstant">Messages defined in wmConstants enum. See pinvoke.net for more details.</param>
        /// <param name="wParam">StringBuilder capacity.</param>
        /// <param name="sb">StringBuilder object.</param>
        /// <returns></returns>
        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hwnd, WMConstants wmConstant, int wParam, StringBuilder sb);

        /// <summary>
        /// Sends the specified message to a window or windows. The SendMessage function calls the 
        /// window procedure for the specified window and does not return until the window procedure 
        /// has processed the message.
        /// </summary>
        /// <param name="hwnd">A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.</param>
        /// <param name="wmConstant">Messages defined in wmConstants enum. See pinvoke.net for more details.</param>
        /// <param name="wParam">IntPtr.Zero</param>
        /// <param name="lParam">IntPtr.Zero</param>
        /// <returns></returns>
        [DllImport("User32.dll")]
        private static extern int SendMessage(IntPtr hwnd, WMConstants wmConstant, IntPtr wParam, IntPtr lParam);

        [DllImport("User32.dll")]
        internal static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect,  bool bErase);

        #endregion

        #region Defnitions

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        private enum GetWindow_Cmd : uint
        {
            GW_HWNDFIRST = 0,
            GW_HWNDLAST = 1,
            GW_HWNDNEXT = 2,
            GW_HWNDPREV = 3,
            GW_OWNER = 4,
            GW_CHILD = 5,
            GW_ENABLEDPOPUP = 6
        }

        private enum WMConstants
        {
            WM_GETTEXT = 0x000D,
            WM_GETTEXTLENGTH = 0x000E
        }

        #endregion

        #region Static Methods

        /// <summary>
        /// The delegate functions that is called by EnumChildWindows
        /// </summary>
        /// <param name="handle">Window handle</param>
        /// <param name="pointer">Pointer to the IntPtr list.</param>
        /// <returns>Boolean</returns>
        private static bool enumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }

        /// <summary>
        /// Called by GetAllWindows.
        /// </summary>
        /// <param name="parent">The handle of the desktop window.</param>
        /// <returns>List of window handles.</returns>
        private static List<IntPtr> getChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumWindowProc childProc = new EnumWindowProc(enumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        /// <summary>
        /// Called by GeAlltWindowsTree and builds the nested WindowInformation
        /// object with extended window information. Recursive function.
        /// </summary>
        /// <param name="parent">The parent window.</param>
        /// <returns>List of WindowInformation objects.</returns>
        private static List<WindowInformation> getChildWindowsInfo(WindowInformation parent)
        {
            List<WindowInformation> result = new List<WindowInformation>();
            IntPtr childHwnd = GetWindow(parent.Handle, GetWindow_Cmd.GW_CHILD);
            while (childHwnd != IntPtr.Zero)
            {
                WindowInformation child = winInfoGet(childHwnd);
                child.Parent = parent;
                child.ChildWindows = getChildWindowsInfo(child);
                result.Add(child);
                childHwnd = FindWindowEx(parent.Handle, childHwnd, null, null);
            }
            foreach (WindowInformation child in result)
            {
                child.SiblingWindows.AddRange(result);
                child.SiblingWindows.Remove(child);
            }
            return result;
        }

        /// <summary>
        /// Called by GetAllWindowsExtededInfo. Flattens the nested WindowInformation
        /// object built by GetAllWindowsTree.
        /// </summary>
        /// <param name="winInfo">The nested WindowInformation object created by GetAllWindowsTree.</param>
        /// <returns>Flattened list of WindowInformation objects with extended information.</returns>
        private static List<WindowInformation> winInfoExtendedInfoProcess(WindowInformation winInfo)
        {
            List<WindowInformation> winInfoList = new List<WindowInformation>();
            winInfoList.Add(winInfo);
            foreach (WindowInformation child in winInfo.ChildWindows)
            {
                winInfoList.AddRange(winInfoExtendedInfoProcess(child));
            }
            return winInfoList;
        }

        /// <summary>
        /// Gets the basic window information from a handle.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <returns>WindowInformation object with basic information.</returns>
        private static WindowInformation winInfoGet(IntPtr hWnd)
        {
            StringBuilder caption = new StringBuilder(1024);
            StringBuilder className = new StringBuilder(1024);
            GetWindowText(hWnd, caption, caption.Capacity);
            GetClassName(hWnd, className, className.Capacity);
            WindowInformation wi = new WindowInformation();
            wi.Handle = hWnd;
            wi.Class = className.ToString();
            if (caption.ToString() != string.Empty)
            {
                wi.Caption = caption.ToString();
            }
            else
            {
                // caption = new StringBuilder(Convert.ToInt32(SendMessage(wi.Handle, WMConstants.WM_GETTEXTLENGTH, IntPtr.Zero, IntPtr.Zero)) + 1);
                //SendMessage(wi.Handle, WMConstants.WM_GETTEXT, caption.Capacity, caption);
                //wi.Caption = caption.ToString();
                wi.Caption = hWnd.ToString();
            }
            return wi;
        }

        #endregion

    }

    /// <summary>
    /// Object that holds window specific information.
    /// </summary>
    public class WindowInformation
    {

        #region Constructor

        /// <summary>
        /// Initialize the class.
        /// </summary>
        public WindowInformation() { }

        #endregion

        #region Properties

        /// <summary>
        /// The window caption.
        /// </summary>
        public string Caption = string.Empty;

        /// <summary>
        /// The window class.
        /// </summary>
        public string Class = string.Empty;

        /// <summary>
        /// Children of the window.
        /// </summary>
        public List<WindowInformation> ChildWindows = new List<WindowInformation>();

        /// <summary>
        /// Unmanaged code to get the process and thres IDs of the window.
        /// </summary>
        /// <param name="hWnd"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        /// <summary>
        /// The string representation of the window.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "Window " + this.Handle.ToString() + " \"" + this.Caption + "\" " + this.Class;
        }

        /// <summary>
        /// The handles of the child windows.
        /// </summary>
        public List<IntPtr> ChildWindowHandles
        {
            get
            {
                try
                {
                    var handles = from c in this.ChildWindows.AsEnumerable()
                                  select c.Handle;
                    return handles.ToList<IntPtr>();
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The window handle.
        /// </summary>
        public IntPtr Handle;

        /// <summary>
        /// The parent window.
        /// </summary>
        public WindowInformation Parent { get; set; }

        /// <summary>
        /// The handle of the parent of the window.
        /// </summary>
        public IntPtr ParentHandle
        {
            get
            {
                if (this.Parent != null) return this.Parent.Handle;
                else return IntPtr.Zero;
            }
        }

        /// <summary>
        /// The corresponding process.
        /// </summary>
        public Process Process
        {
            get
            {
                try
                {
                    int processID = 0;
                    GetWindowThreadProcessId(this.Handle, out processID);
                    return Process.GetProcessById(processID);
                }
                catch (Exception ex) { return null; }
            }
        }

        /// <summary>
        /// Sibling window information.
        /// </summary>
        public List<WindowInformation> SiblingWindows = new List<WindowInformation>();

        /// <summary>
        /// The handles of the sibling windows.
        /// </summary>
        public List<IntPtr> SiblingWindowHandles
        {
            get
            {
                try
                {
                    var handles = from s in this.SiblingWindows.AsEnumerable()
                                  select s.Handle;
                    return handles.ToList<IntPtr>();
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The thread ID of the window. Returns -1 on exception.
        /// </summary>
        public int ThreadID
        {
            get
            {
                try
                {
                    int dummy = 0;
                    return GetWindowThreadProcessId(this.Handle, out dummy);
                }
                catch (Exception ex) { return -1; }
            }
        }

        #endregion
    }
}
