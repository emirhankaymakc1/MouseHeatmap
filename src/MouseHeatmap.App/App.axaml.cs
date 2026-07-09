using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MouseHeatmap.App.Services;
using MouseHeatmap.App.ViewModels;
using MouseHeatmap.App.Views;

namespace MouseHeatmap.App;

public partial class App : Application
{
    private AppServices? _services;
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private bool _reallyQuitting;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _services = new AppServices();
            _services.Start();

            ApplyTheme(_services.Settings.Theme);

            var mainViewModel = new MainWindowViewModel(_services);
            _mainWindow = new MainWindow { DataContext = mainViewModel };
            _mainWindow.Icon = IconFactory.CreateWindowIcon(_services.Tracker.IsRecording);
            _mainWindow.Closing += OnMainWindowClosing;
            desktop.MainWindow = _mainWindow;

            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            SetupTrayIcon(desktop);

            desktop.ShutdownRequested += (_, _) => _services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var showItem = new NativeMenuItem("Pencereyi Göster");
        showItem.Click += (_, _) => ShowMainWindow();

        var quitItem = new NativeMenuItem("Çıkış");
        quitItem.Click += (_, _) =>
        {
            _reallyQuitting = true;
            _trayIcon?.Dispose();
            desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Add(showItem);
        menu.Add(new NativeMenuItemSeparator());
        menu.Add(quitItem);

        _trayIcon = new TrayIcon
        {
            Icon = IconFactory.CreateWindowIcon(_services!.Tracker.IsRecording),
            ToolTipText = "Mouse Heatmap Recorder",
            Menu = menu
        };
        _trayIcon.Clicked += (_, _) => ShowMainWindow();

        TrayIcon.SetIcons(this, new TrayIcons { _trayIcon });
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_reallyQuitting) return;
        e.Cancel = true;
        _mainWindow?.Hide();
    }

    private void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public static void ApplyTheme(string theme) =>
        Current!.RequestedThemeVariant =
            theme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
}
