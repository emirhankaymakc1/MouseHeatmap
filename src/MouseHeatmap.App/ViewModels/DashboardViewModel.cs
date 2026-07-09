using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseHeatmap.App.Services;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.App.ViewModels;

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly SystemUsage _usage = new();
    private readonly DispatcherTimer _timer;
    private int _tick;
    private bool _summaryBusy;

    [ObservableProperty] private string _todayUsage = "-";
    [ObservableProperty] private string _weekUsage = "-";
    [ObservableProperty] private string _todayClicks = "-";
    [ObservableProperty] private string _todayDistance = "-";
    [ObservableProperty] private string _cpuUsage = "-";
    [ObservableProperty] private string _ramUsage = "-";
    [ObservableProperty] private string _sessionEvents = "0";
    [ObservableProperty] private string _pendingWrites = "0";
    [ObservableProperty] private string _monitorCount = "-";
    [ObservableProperty] private string _recordingStatus = "";
    [ObservableProperty] private string _toggleButtonText = "";
    [ObservableProperty] private bool _isRecording;

    public DashboardViewModel(AppServices services)
    {
        _services = services;
        UpdateRecordingState();
        MonitorCount = $"{_services.Tracker.Monitors.Count} monitör";

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => OnTick();
        _timer.Start();
        OnTick();
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (_services.Tracker.IsRecording)
            _services.Tracker.Stop();
        else
            _services.Tracker.Start();
        UpdateRecordingState();
    }

    private void OnTick()
    {
        var (cpu, ram) = _usage.Sample();
        CpuUsage = $"%{cpu:F1}";
        RamUsage = $"{ram:F0} MB";
        SessionEvents = Formatting.Count(_services.Tracker.SessionEventCount);
        PendingWrites = _services.Writer.PendingCount.ToString();

        if (_tick % 5 == 0)
            _ = RefreshSummariesAsync();
        _tick++;
    }

    private async Task RefreshSummariesAsync()
    {
        if (_summaryBusy) return;
        _summaryBusy = true;
        try
        {
            var todayStart = PeriodOption.All[0].StartTimestamp();
            var weekStart = PeriodOption.All[1].StartTimestamp();

            var (today, week) = await Task.Run(() =>
                (_services.Statistics.ComputeSummary(todayStart),
                 _services.Statistics.ComputeSummary(weekStart)));

            TodayUsage = Formatting.Duration(today.ActiveTimeSec);
            WeekUsage = Formatting.Duration(week.ActiveTimeSec);
            TodayClicks = Formatting.Count(today.TotalClicks);
            TodayDistance = Formatting.Distance(today.TotalDistancePx);
        }
        finally
        {
            _summaryBusy = false;
        }
    }

    private void UpdateRecordingState()
    {
        IsRecording = _services.Tracker.IsRecording;
        RecordingStatus = IsRecording ? "🔴 Kayıt sürüyor" : "⚪ Kayıt durduruldu";
        ToggleButtonText = IsRecording ? "Kaydı Durdur" : "Kaydı Başlat";
    }
}
