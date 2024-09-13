using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WindowSwitcher.Services.Models;

namespace WindowSwitcher.Services.WindowManager;

public interface IWindowService
{
    List<WindowInfo> GetWindows();
    void FocusWindow(WindowInfo handle);
    Guid GetCurrentDesktop();
    Task<bool> CloseWindow(WindowInfo handle);
    bool IsWindowOpen(WindowInfo handle);
}