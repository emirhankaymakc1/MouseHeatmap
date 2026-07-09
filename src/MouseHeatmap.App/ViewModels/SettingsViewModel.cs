using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseHeatmap.App.Services;

namespace MouseHeatmap.App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;

    [ObservableProperty] private int _minDistancePx;
    [ObservableProperty] private int _minIntervalMs;
    [ObservableProperty] private bool _startRecordingOnLaunch;
    [ObservableProperty] private bool _autostart;
    [ObservableProperty] private bool _isDarkTheme;
    [ObservableProperty] private string _databasePath;
    [ObservableProperty] private string _statusText = "";

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        var s = services.Settings;

        _minDistancePx = s.MinDistancePx;
        _minIntervalMs = s.MinIntervalMs;
        _startRecordingOnLaunch = s.StartRecordingOnLaunch;
        _autostart = AutostartManager.IsEnabled();
        _isDarkTheme = !s.Theme.Equals("Light", StringComparison.OrdinalIgnoreCase);
        _databasePath = Core.AppSettings.DatabasePath;
    }

    partial void OnIsDarkThemeChanged(bool value) =>
        App.ApplyTheme(value ? "Dark" : "Light");

    [RelayCommand]
    private void Save()
    {
        var s = _services.Settings;

        s.MinDistancePx = Math.Clamp(MinDistancePx, 1, 100);
        s.MinIntervalMs = Math.Clamp(MinIntervalMs, 10, 2000);
        s.StartRecordingOnLaunch = StartRecordingOnLaunch;
        s.Theme = IsDarkTheme ? "Dark" : "Light";

        _services.Filter.MinDistancePx = s.MinDistancePx;
        _services.Filter.MinIntervalMs = s.MinIntervalMs;

        var autostartOk = AutostartManager.Set(Autostart);
        s.Autostart = Autostart;

        s.Save();

        StatusText = autostartOk || !Autostart
            ? "Ayarlar kaydedildi."
            : "Ayarlar kaydedildi (otomatik başlatma ayarlanamadı).";
    }
}
