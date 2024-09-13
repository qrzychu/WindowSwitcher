using Avalonia;

namespace WindowSwitcher.Extensions;

public static class ApplicationExtensions
{
    public static T GetService<T>(this Application app)
    {
        return ((App)app).Services.GetService<T>();
    }
}