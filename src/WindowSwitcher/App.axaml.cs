using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Jab;
using ReactiveUI;
using Serilog;
using Serilog.Events;
using Splat;
using Splat.Serilog;
using WindowSwitcher.Extensions;
using WindowSwitcher.Services;
using WindowSwitcher.Services.Keyboard;

namespace WindowSwitcher;

public partial class App : Application
{
    private MainWindow _mainWindow = null!;
    private TrayIcon? _trayIcon;
    private IKeyboardInterceptor? _keyboardShortcutInterceptor;

    public MyServiceProvider Services { get; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        var logPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowSwitcher", "log.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Warning)
            .CreateLogger();

        Locator.CurrentMutable.UseSerilogFullLogger();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime)
            {
                SetupTrayIcon();
                SetupKeyboardActivation();

                _mainWindow = Services.GetService<MainWindow>();
                _mainWindow.Closing += (_, args) =>
                {
                    args.Cancel = true;
                    _mainWindow.Hide();
                };

                _mainWindow.ViewModel!.LoadWindows.Execute().ToTask().GetAwaiter().GetResult();
            }

            base.OnFrameworkInitializationCompleted();
        }
        catch (Exception e)
        {
            Log.Logger.Error(e, "Error during initialization");
            throw;
        }
    }

    private void SetupKeyboardActivation()
    {
        _keyboardShortcutInterceptor = this.GetService<IKeyboardInterceptor>();
        _keyboardShortcutInterceptor.Signal.Subscribe(_ => Dispatcher.UIThread.Invoke(ShowMainWindow));
    }


    private async Task ShowMainWindow()
    {
        if (_mainWindow.IsVisible)
        {
            return;
        }

        _mainWindow.ViewModel!.FilterText = "";
        await _mainWindow.ViewModel!.LoadWindows.Execute();
        _mainWindow.Show();
    }

    private static void ExitApplication()
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private void SetupTrayIcon()
    {
        _trayIcon ??= new TrayIcon
        {
            Icon = new WindowIcon(LoadIconResource()),
            ToolTipText = "Window Switcher",
            Command = ReactiveCommand.Create(ShowMainWindow),
            Menu =
            [
                new NativeMenuItem
                {
                    Header = "Close",
                    Command = ReactiveCommand.Create(() => Dispatcher.UIThread.Invoke(ExitApplication))
                }
            ]
        };
    }


    private Stream LoadIconResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream("WindowSwitcher.Assets.tray_icon.png")!;
    }


    [ServiceProvider]
    [Import(typeof(IMyServiceContainer))]
    public partial class MyServiceProvider : IMyServiceContainer
    {
    }
}