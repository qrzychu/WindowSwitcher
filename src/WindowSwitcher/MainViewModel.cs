using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using WindowSwitcher.Services.Models;
using WindowSwitcher.Services.WindowManager;

namespace WindowSwitcher;

public class MainViewModel : ReactiveObject, IActivatableViewModel
{
    private readonly ILogger Logger = Log.Logger.ForContext<MainViewModel>();
    
    private readonly SourceCache<WindowInfo, IntPtr> _windows = new(x => x.Handle);
    private readonly ReadOnlyObservableCollection<WindowInfo> _windowsList;
    private readonly ReadOnlyObservableCollection<VirtualDesktopInfo> _desktops;
    public ViewModelActivator Activator { get; } = new();
    
    private WindowInfo? _selectedWindow;
    private string _filterText = "";
    private int _selectedIndex;
    private VirtualDesktopInfo? _selectedDesktop;
    private int? _selectedDesktopIndex;
    private readonly ObservableAsPropertyHelper<bool> _showDesktops;

    public ReadOnlyObservableCollection<WindowInfo> WindowsList => _windowsList;
    public ReadOnlyObservableCollection<VirtualDesktopInfo> DesktopList => _desktops;

    public MainViewModel(IWindowService windowService)
    {
        SwitchToAppCommand = ReactiveCommand.Create((WindowInfo? w) =>
        {
            if (w is not null)
            {
                windowService.FocusWindow(w);
            }
        });

        LoadWindows = ReactiveCommand.Create(() =>
        {
            var windows = windowService.GetWindows();

            CurrentDesktop = windowService.GetCurrentDesktop();
            SelectedDesktop = null;

            _windows.Edit(u =>
            {
                u.Remove(_windows.Keys.Except(windows.Select(w => w.Handle)));
                u.AddOrUpdate(windows);
            });

            SelectedWindow = WindowsList.FirstOrDefault();
        });

        var filterPredicate = this.WhenAnyValue(x => x.FilterText)
            .Select(text => new Func<WindowInfo, bool>(w =>
                string.IsNullOrWhiteSpace(text) || w.Title.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                w.ProcessName.Contains(text, StringComparison.OrdinalIgnoreCase)));

        var selectedDesktopPredicate = this.WhenAnyValue(x => x.SelectedDesktop).Select(selectedDesktop =>
        {
            return new Func<WindowInfo, bool>(item =>
            {
                if (selectedDesktop is null)
                    return true;
                return item.DesktopId == selectedDesktop.Id;
            });
        });

        _windows.Connect()
            .Filter(filterPredicate)
            .Filter(selectedDesktopPredicate)
            .SortBy(x => x.Title)
            .Bind(out _windowsList)
            .Subscribe();

        _windows.Connect()
            .Group(x => x.DesktopId)
            .SortBy(x => x.Cache.Count, SortDirection.Descending)
            .Transform(x => new VirtualDesktopInfo(x.Key, "Desktop ", x.Cache.Count, x.Key == CurrentDesktop))
            .Sort(SortExpressionComparer<VirtualDesktopInfo>.Descending(x => x.IsCurrentDesktop)
                .ThenByDescending(x => x.WindowCount))
            .Bind(out _desktops)
            .Subscribe();

        WindowsList.ToObservableChangeSet()
            .Count()
            .CombineLatest(this.WhenAnyValue(x => x.SelectedIndex))
            .Do(tuple =>
            {
                var (count, index) = tuple;

                if (index < 0 && count > 0)
                {
                    SelectedIndex = 0;
                }
            })
            .Subscribe();

        MoveDownCommand = ReactiveCommand.Create(() => 1);
        MoveUpCommand = ReactiveCommand.Create(() => -1);

        MoveToNextDesktopCommand = ReactiveCommand.Create(SelectNextDesktop);
        MoveSelectedIndex = ReactiveCommand.Create<int>(change =>
        {
            var newIndex = SelectedIndex + change;
            if (newIndex < 0)
            {
                newIndex = 0;
            }
            else if (newIndex >= WindowsList.Count)
            {
                newIndex = WindowsList.Count - 1;
            }

            SelectedIndex = newIndex;
        });

        MoveUpCommand.Merge(MoveDownCommand)
            .InvokeCommand(MoveSelectedIndex);


        MoveToPreviousDesktopCommand = ReactiveCommand.Create(() =>
        {
            var newIndex = SelectedDesktopIndex - 1;
            if (DesktopList.Any())
            {
                SelectedDesktopIndex = (newIndex is null or < 0) ? DesktopList.Count - 1 : newIndex;
            }
        });

        ToggleDesktopSelection = ReactiveCommand.Create(() =>
        {
            SelectedDesktop =
                SelectedDesktop is null
                    ? DesktopList.Single(x => x.IsCurrentDesktop)
                    : null;
        });

        CloseWindowCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            if (SelectedWindow is not null)
            {
                if (await windowService.CloseWindow(SelectedWindow))
                {
                    return SelectedWindow;
                }
            }

            return null;
        }, this.WhenAnyValue(x => x.SelectedWindow).Select(x => x is not null));

        WatchWindowCommand = ReactiveCommand.CreateFromTask(async (WindowInfo w) =>
        {
            while (windowService.IsWindowOpen(w))
            {
                await Task.Delay(10);
            }

            _windows.Remove(w);
            Logger.Information("Window {Title} closed", w.Title);
        });
        
        CloseWindowCommand.Where(x => x is not null)
            .Log(this, "Watching window")
            .InvokeCommand(WatchWindowCommand!);

        if (Design.IsDesignMode)
        {
            LoadWindows.Execute().Subscribe();
        }

        _showDesktops = _desktops.ToObservableChangeSet()
            .Count()
            .Select(x => x > 1)
            .ToProperty(this, x => x.ShowDesktops);
    }

    public bool ShowDesktops => _showDesktops.Value;

    private ReactiveCommand<WindowInfo, Unit> WatchWindowCommand { get; set; }

    public ReactiveCommand<Unit, WindowInfo?> CloseWindowCommand { get; set; }

    private Guid CurrentDesktop { get; set; }

    public VirtualDesktopInfo? SelectedDesktop
    {
        get => _selectedDesktop;
        set => this.RaiseAndSetIfChanged(ref _selectedDesktop, value);
    }

    public int? SelectedDesktopIndex
    {
        get => _selectedDesktopIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedDesktopIndex, value);
    }

    public ReactiveCommand<Unit, Unit> LoadWindows { get; set; }

    public ReactiveCommand<WindowInfo?, Unit> SwitchToAppCommand { get; }

    public ReactiveCommand<Unit, Unit> CloseCommand { get; } = ReactiveCommand.Create(() => { });

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => this.RaiseAndSetIfChanged(ref _selectedIndex, value);
    }

    public WindowInfo? SelectedWindow
    {
        get => _selectedWindow;
        set => this.RaiseAndSetIfChanged(ref _selectedWindow, value);
    }

    public string FilterText
    {
        get => _filterText;
        set => this.RaiseAndSetIfChanged(ref _filterText, value);
    }

    private ReactiveCommand<int, Unit> MoveSelectedIndex { get; }
    public ReactiveCommand<Unit, int> MoveDownCommand { get; }
    public ReactiveCommand<Unit, int> MoveUpCommand { get; }

    #region DesktopCommands

    public ReactiveCommand<Unit, Unit> MoveToNextDesktopCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveToPreviousDesktopCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleDesktopSelection { get; }

    private void SelectNextDesktop()
    {
        var newIndex = (SelectedDesktopIndex ?? 0) + 1;
        if (DesktopList.Any())
        {
            SelectedDesktopIndex = newIndex % DesktopList.Count;
        }
    }

    #endregion
}