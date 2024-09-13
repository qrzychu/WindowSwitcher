using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs\\log.txt", rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: LogEventLevel.Information)
            .CreateLogger();

        Locator.CurrentMutable.UseSerilogFullLogger();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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
        catch(Exception e)
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
            Icon = new WindowIcon("tray_icon.png"),
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


    [ServiceProvider]
    [Import(typeof(IMyServiceContainer))]
    public partial class MyServiceProvider : IMyServiceContainer
    {
    }
}