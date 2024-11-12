using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;

namespace WindowSwitcher.Services.OS;

public class IconExtractor
{
    [DllImport("WindowsApiWrapper.dll")]
    private static extern int GetWindowIconData(IntPtr windowHandle, byte[] buffer, int bufferSize);

    public static Bitmap? GetWindowIcon(IntPtr windowHandle)
    {
        // Get required buffer size
        int size = GetWindowIconData(windowHandle, null, 0);
        if (size == 0) return null;

        // Allocate buffer and get icon data
        byte[] buffer = new byte[size];
        int bytesRead = GetWindowIconData(windowHandle, buffer, buffer.Length);
        
        if (bytesRead == 0) return null;

        // Convert PNG data to Bitmap
        using var ms = new MemoryStream(buffer);
        return new Bitmap(ms);
    }
}