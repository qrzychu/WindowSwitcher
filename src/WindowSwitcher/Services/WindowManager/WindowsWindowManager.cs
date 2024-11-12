// WindowServiceWindows.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Serilog;
using WindowSwitcher.Services.Models;
using WindowSwitcher.Services.OS;
// ReSharper disable InconsistentNaming

namespace WindowSwitcher.Services.WindowManager;

public class WindowServiceWindows(IDesktopManager virtualDesktopManager) : IWindowService
{
    private readonly ILogger _logger = Log.Logger.ForContext<WindowServiceWindows>();
    
    private static readonly Bitmap FallbackIcon = new Bitmap("Assets/tray_icon.png");
    
    private static readonly HashSet<string> ExcludedClassNames =
    [
        "Shell_TrayWnd", // Taskbar
        "DV2ControlHost", // Windows Desktop Gadgets
        "WorkerW", // Desktop
        "Progman", // Program Manager
        "ButtonNotification", // Volume, network, etc. popup buttons
        "NotifyIconOverflowWindow", // Notification area overflow
        "NotifyIconWindow", // Notification area icons
        "Microsoft.UI.Content.PopupWindowSiteBridge"
    ];

    private static readonly HashSet<string> ExcludedProcessNames =
    [
        "textinputhost",
        "startmenuexperiencehost",
        "searchui",
        "shellexperiencehost",
        "systemsettings",
        "lockapp",
        "runtimebroker",
        "backgroundtaskhost",
        "cortana",
        "sihost",
        "taskhostw",
        "logioverlay"
    ];

    public List<WindowInfo> GetWindows()
    {
        List<WindowInfo> windows = [];

        var start = Stopwatch.GetTimestamp();
        WindowsApi.EnumWindows((hWnd, _) =>
        {
            if (WindowsApi.IsWindowVisible(hWnd))
            {
                if (TryParseWindow(hWnd, windows)) return true;
            }

            return true;
        }, IntPtr.Zero);
        
        _logger.Information("Loaded {WindowsCount} windows in {TotalMilliseconds} ms", windows.Count, Stopwatch.GetElapsedTime(start).TotalMilliseconds);

        return windows;
    }

    private bool TryParseWindow(IntPtr hWnd, List<WindowInfo> windows)
    {
        StringBuilder sbClassName = new StringBuilder(256);
        WindowsApi.GetClassName(hWnd, sbClassName, sbClassName.Capacity);
        string className = sbClassName.ToString();

        if (ExcludedClassNames.Contains(className))
            return true;

        int length = WindowsApi.GetWindowTextLength(hWnd);
        if (length > 0)
        {
            StringBuilder sb = new StringBuilder(length + 1);
            WindowsApi.GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();

            if (string.IsNullOrEmpty(title))
                return true;

            var processName = GetProcessNameFromWindowHandle(hWnd).ToLower();
            if (processName.Contains("windowswitcher") || ExcludedProcessNames.Contains(processName))
                return true;

            var windowVirtualDesktopId = GetWindowVirtualDesktopId(hWnd);

            // tray icon windows
            if (windowVirtualDesktopId == Guid.Empty)
            {
                return true;
            }
                    
            windows.Add(new WindowInfo(
                hWnd,
                title,
                (GetWindowIcon(hWnd) ?? FallbackIcon),
                processName,
                windowVirtualDesktopId
            ));
        }

        return false;
    }

    public void FocusWindow(WindowInfo handle)
    {
        if (handle.Handle is var hWnd)
        {
            const int SW_RESTORE = 9;
            const int SW_SHOW = 5;
            const int SW_MAXIMIZE = 3;

            WindowsApi.WINDOWPLACEMENT placement = new WindowsApi.WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            WindowsApi.GetWindowPlacement(hWnd, ref placement);

            if (WindowsApi.IsIconic(hWnd))
            {
                // Window is minimized
                WindowsApi.ShowWindow(hWnd, SW_RESTORE);
            }
            else if (placement.showCmd == SW_MAXIMIZE)
            {
                // Window is maximized
                WindowsApi.ShowWindow(hWnd, SW_MAXIMIZE);
            }
            else
            {
                // Window is in normal state
                WindowsApi.ShowWindow(hWnd, SW_SHOW);
            }

            WindowsApi.SetForegroundWindow(hWnd);
        }
    }

    public Guid GetCurrentDesktop()
    {
        try
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            IntPtr foregroundWindow = WindowsApi.GetForegroundWindow();
            return virtualDesktopManager.GetWindowDesktopId(foregroundWindow);
        }
        catch (COMException)
        {
            // Handle the case where the window doesn't exist or other COM errors
            return Guid.Empty;
        }
    }

    public async Task<bool> CloseWindow(WindowInfo handle)
    {
        try
        {
            // Send a close message to the window
            bool result = await Task.Run(() => WindowsApi.PostMessage(handle.Handle, WindowsApi.WmClose, IntPtr.Zero, IntPtr.Zero));

            // Check if the message was processed successfully
            // Note: SendMessage returns 0 if processed successfully for most messages
            if (result)
            {
                _logger.Debug("Close message sent successfully");
                return true;
            }

            _logger.Debug("Failed to send close message to window {Window}", handle);
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occurred while trying to close the window");
            return false;
        }
    }

    public bool IsWindowOpen(WindowInfo handle)
    {
        return WindowsApi.IsWindow(handle.Handle);
    }

    private static string GetProcessNameFromWindowHandle(IntPtr hWnd)
    {
        const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        WindowsApi.GetWindowThreadProcessId(hWnd, out uint processId);

        IntPtr hProcess = WindowsApi.OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
        if (hProcess == IntPtr.Zero)
            return string.Empty;

        try
        {
            StringBuilder sb = new StringBuilder(1024);
            uint size = (uint)sb.Capacity;
            if (WindowsApi.QueryFullProcessImageName(hProcess, 0, sb, ref size))
            {
                string fullPath = sb.ToString(0, (int)size);
                return Path.GetFileNameWithoutExtension(fullPath).ToLower();
            }
        }
        finally
        {
            WindowsApi.CloseHandle(hProcess);
        }

        return string.Empty;
    }

    private Bitmap? GetWindowIcon(object handle)
    {
        if (!(handle is IntPtr hWnd))
            return null;

        IntPtr hIcon = WindowsApi.SendMessage(hWnd, WindowsApi.WM_GETICON, WindowsApi.ICON_SMALL, IntPtr.Zero);

        if (hIcon == IntPtr.Zero)
            hIcon = WindowsApi.SendMessage(hWnd, WindowsApi.WM_GETICON, WindowsApi.ICON_BIG, IntPtr.Zero);

        if (hIcon == IntPtr.Zero)
            hIcon = WindowsApi.GetClassLongPtr(hWnd, WindowsApi.GCL_HICON);

        if (hIcon == IntPtr.Zero)
            return null;

        return CreateBitmapFromHIcon(hIcon);
    }

    private Bitmap CreateBitmapFromHIcon(IntPtr hIcon)
    {
        // HICON to System.Drawing.Icon
        System.Drawing.Icon icon = System.Drawing.Icon.FromHandle(hIcon);

        // Convert to Bitmap
        using (var bitmap = icon.ToBitmap())
        using (var memory = new MemoryStream())
        {
            bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;
            return new Bitmap(memory);
        }
    }

    private Guid GetWindowVirtualDesktopId(IntPtr hWnd)
    {
        try
        {
            return virtualDesktopManager.GetWindowDesktopId(hWnd);
        }
        catch (COMException e)
        {
            // mostly tray icons
            _logger.Debug(e, "Failed to get virtual desktop ID for window");
            return Guid.Empty;
        }
    }
}