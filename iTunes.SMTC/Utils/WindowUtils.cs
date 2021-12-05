using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PInvoke;
using System.Runtime.InteropServices;

namespace iTunes.SMTC.Utils
{
    public static class WindowUtils
    {
        public static IntPtr GetHWND(this Window window)
        {
            return WinRT.Interop.WindowNative.GetWindowHandle(window);
        }

        public static void SetWindowSize(this Window window, int width, int height)
        {
            // Win32 uses pixels and WinUI 3 uses effective pixels, so you should apply the DPI scale factor
            var hwnd = window.GetHWND();
            int dpi = User32.GetDpiForWindow(hwnd);
            float scalingFactor = (float)dpi / 96;
            width = (int)(width * scalingFactor);
            height = (int)(height * scalingFactor);

            User32.SetWindowPos(hwnd, default, 0, 0, width, height, User32.SetWindowPosFlags.SWP_NOMOVE | User32.SetWindowPosFlags.SWP_NOZORDER);
        }

        public static void PlacementCenterWindowInMonitorWin32(this Window window)
        {
            RECT rc;
            var hwnd = window.GetHWND();
            User32.GetWindowRect(hwnd, out rc);
            ClipOrCenterRectToMonitorWin32(ref rc);
            User32.SetWindowPos(hwnd, default, rc.left, rc.top, 0, 0,
                         User32.SetWindowPosFlags.SWP_NOSIZE |
                         User32.SetWindowPosFlags.SWP_NOZORDER |
                         User32.SetWindowPosFlags.SWP_NOACTIVATE);
        }

        private static void ClipOrCenterRectToMonitorWin32(ref RECT prc)
        {
            IntPtr hMonitor;
            RECT rc;
            int w = prc.right - prc.left;
            int h = prc.bottom - prc.top;

            hMonitor = User32.MonitorFromRect(ref prc, User32.MonitorOptions.MONITOR_DEFAULTTONEAREST);
            User32.MONITORINFO mi = new User32.MONITORINFO
            {
                cbSize = Marshal.SizeOf<User32.MONITORINFO>()
            };

            User32.GetMonitorInfo(hMonitor, ref mi);

            rc = mi.rcWork;
            prc.left = rc.left + (rc.right - rc.left - w) / 2;
            prc.top = rc.top + (rc.bottom - rc.top - h) / 2;
            prc.right = prc.left + w;
            prc.bottom = prc.top + h;
        }

        public static void BringToForeground(this Window window)
        {
            // Bring the window to the foreground... first get the window handle...
            var hwnd = window.GetHWND();

            // Restore window if minimized...
            User32.ShowWindow(hwnd, User32.WindowShowStyle.SW_RESTORE);

            // And call SetForegroundWindow...
            User32.SetForegroundWindow(hwnd);
        }

        public static void LoadIcon(this Window window, string iconName)
        {
            const int ICON_SMALL = 0;
            const int ICON_BIG = 1;

            var hwnd = window.GetHWND();

            var smIconHndl = User32.LoadImage(default,
                iconName,
                User32.ImageType.IMAGE_ICON,
                User32.GetSystemMetrics(User32.SystemMetric.SM_CXSMICON),
                User32.GetSystemMetrics(User32.SystemMetric.SM_CYSMICON),
                User32.LoadImageFlags.LR_LOADFROMFILE | User32.LoadImageFlags.LR_SHARED);

            User32.SendMessage(hwnd, User32.WindowMessage.WM_SETICON, (System.IntPtr)ICON_SMALL, smIconHndl);

            var bigIconHndl = User32.LoadImage(default,
                iconName,
                User32.ImageType.IMAGE_ICON,
                User32.GetSystemMetrics(User32.SystemMetric.SM_CXSMICON),
                User32.GetSystemMetrics(User32.SystemMetric.SM_CYSMICON),
                User32.LoadImageFlags.LR_LOADFROMFILE | User32.LoadImageFlags.LR_SHARED);

            User32.SendMessage(hwnd, User32.WindowMessage.WM_SETICON, (System.IntPtr)ICON_BIG, smIconHndl);
        }
    }
}
