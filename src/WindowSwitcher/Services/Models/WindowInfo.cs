using System;
using Avalonia.Media.Imaging;

namespace WindowSwitcher.Services.Models;

public record WindowInfo(IntPtr Handle, string Title, Bitmap Icon, string ProcessName, Guid DesktopId);