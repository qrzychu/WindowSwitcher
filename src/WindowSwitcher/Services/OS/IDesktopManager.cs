using System;

namespace WindowSwitcher.Services.OS;

public interface IDesktopManager
{
    Guid GetWindowDesktopId(IntPtr hwnd);
}