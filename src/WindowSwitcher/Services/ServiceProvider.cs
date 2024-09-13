using Jab;
using WindowSwitcher.Services.Keyboard;
using WindowSwitcher.Services.OS;
using WindowSwitcher.Services.WindowManager;

namespace WindowSwitcher.Services;

[ServiceProviderModule]
[Singleton(typeof(IWindowService), typeof(WindowServiceWindows))]
[Transient(typeof(MainWindow))]
[Singleton(typeof(MainViewModel))]
[Singleton(typeof(IKeyboardInterceptor), typeof(HotKeyInterceptor))]
[Singleton(typeof(IDesktopManager), typeof(VirtualDesktopManager))]
public interface IMyServiceContainer
{
}
