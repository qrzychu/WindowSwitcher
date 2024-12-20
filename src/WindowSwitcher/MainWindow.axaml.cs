using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ReactiveUI;
using Serilog;
using Splat;
using WindowSwitcher.Services.Models;
using ILogger = Serilog.ILogger;

namespace WindowSwitcher;

public partial class MainWindow : ReactiveWindow<MainViewModel>, IDisposable, IEnableLogger
{
    private readonly ILogger _logger = Log.Logger.ForContext<MainWindow>();

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        HotKeyManager.SetHotKey(HelpButton, new KeyGesture(Key.OemQuestion));

        ViewModel = viewModel;

        this.WhenActivated(d =>
        {
            FilterBox.KeyDown += FilterBox_KeyDown;

            ViewModel.SwitchToAppCommand
                .Merge(ViewModel.CloseCommand)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(_ => { Hide(); })
                .Subscribe()
                .DisposeWith(d);

            Disposable.Create(() => FilterBox,
                (filterBox) => filterBox().KeyDown -= FilterBox_KeyDown).DisposeWith(d);
        });
    }

    private void FilterBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (FilterBox.Text!.Length == 0 && e.Key == Key.OemQuestion)
        {
            ViewModel!.ToggleHelpCommand.Execute().Subscribe();

            e.Handled = true;
        }
    }

    private void CenterWindowOnMainScreen()
    {
        InvalidateMeasure();

        var screen = Screens.Primary;
        var workingArea = screen!.WorkingArea;

        if (Height > workingArea.Height)
        {
            Height = workingArea.Height * 0.9;
            InvalidateMeasure();
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Position = new PixelPoint(
            (int)(workingArea.X + (workingArea.Width - Width) / 2),
            (int)(workingArea.Y + (workingArea.Height - Height) / 2)
        );

        _logger.Debug(
            "Window size: {Width}x{Height} at {Position} with {Count} windows, and {ItemCount} displayed items",
            Width, Height, Position, ViewModel!.WindowsList.Count, ViewModel!.WindowsList.Count);
    }

    public double ListMaxHeight => Screens.Primary!.WorkingArea.Height - 100;

    public override void Show()
    {
        MinWidth = Screens.Primary!.WorkingArea.Width * 0.33;
        base.Show();
        CenterWindowOnMainScreen();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        FilterBox.Focus(NavigationMethod.Pointer);
    }

    public override void Hide()
    {
        base.Hide();
        ViewModel!.FilterText = "";
    }

    public void Dispose()
    {
    }

    private void WindowBase_OnDeactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private void WindowsList_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Control control)
        {
            var listBoxItem = control.GetVisualAncestors().OfType<ListBoxItem>().FirstOrDefault();

            if (listBoxItem is null)
            {
                _logger.Error("ListBoxItem is null, control: {Control}", control.GetType());
                return;
            }

            ViewModel!.SwitchToAppCommand.Execute(listBoxItem.DataContext as WindowInfo).Subscribe();
        }
    }
}