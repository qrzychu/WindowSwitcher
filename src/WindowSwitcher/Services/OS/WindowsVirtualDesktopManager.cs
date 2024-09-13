using System;
using System.Runtime.InteropServices;

namespace WindowSwitcher.Services.OS;

public class VirtualDesktopManager : IDesktopManager
{
    [DllImport("VirtualDesktopManager.dll")]
    private static extern int GetWindowDesktopId(IntPtr hwnd, out Guid desktopId);

    public Guid GetWindowDesktopId(IntPtr hwnd)
    {
        Guid desktopId;
        int hr = GetWindowDesktopId(hwnd, out desktopId);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
        return desktopId;
    }
}