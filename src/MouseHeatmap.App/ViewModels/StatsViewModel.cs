using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseHeatmap.App.Services;
using MouseHeatmap.Core.Analytics;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.App.ViewModels;

public sealed record ChartBar(string Label, double Value, double HeightRatio);

public sealed partial class StatsViewModel : ObservableObject
{
    private readonly AppServices _services;

    [ObservableProperty] private ObservableCollection<PeriodOption> _periods = new(PeriodOption.All);
    [ObservableProperty] private PeriodOption _selectedPeriod = PeriodOption.All[1];

    [ObservableProperty] private string _activeTime = "-";
    [ObservableProperty] private string _distance = "-";
    [ObservableProperty] private string _clicks = "-";
    [ObservableProperty] private string _scrolls = "-";
    [ObservableProperty] private string _busiestHour = "-";
    [ObservableProperty] private string _busiestDay = "-";
    [ObservableProperty] private string _busiestRegion = "-";
    [ObservableProperty] private string _totalEvents = "-";
    [ObservableProperty] private string _statusText = "";

    public ObservableCollection<ChartBar> HourlyBars { get; } = new();
    public ObservableCollection<ChartBar> WeekdayBars { get; } = new();

    public StatsViewModel(AppServices services) => _services = services;

    [RelayCommand]
    private async Task Refresh()
    {
        StatusText = "Hesaplanıyor...";
        var start = SelectedPeriod.StartTimestamp();

        var summary = await Task.Run(() => _services.Statistics.ComputeSummary(start));
        ApplySummary(summary);
        StatusText = "";
    }

    private void ApplySummary(UsageSummary s)
    {
        ActiveTime = Formatting.Duration(s.ActiveTimeSec);
        Distance = Formatting.Distance(s.TotalDistancePx);
        Clicks = $"{s.TotalClicks:N0} (Sol {s.LeftClicks:N0} / Sağ {s.RightClicks:N0})";
        Scrolls = Formatting.Count(s.TotalScrolls);
        BusiestHour = s.BusiestHour is int h ? $"{h}:00" : "-";
        BusiestDay = s.BusiestDay ?? "-";
        BusiestRegion = s.BusiestRegion ?? "-";
        TotalEvents = Formatting.Count(s.TotalEvents);

        BuildHourlyBars(s);
        BuildWeekdayBars(s);
    }

    private void BuildHourlyBars(UsageSummary s)
    {
        HourlyBars.Clear();
        var max = Math.Max(1, s.HourlyCounts.Count > 0 ? s.HourlyCounts.Values.Max() : 1);
        for (var hour = 0; hour < 24; hour++)
        {
            var value = s.HourlyCounts.GetValueOrDefault(hour);
            HourlyBars.Add(new ChartBar($"{hour}", value, (double)value / max));
        }
    }

    private void BuildWeekdayBars(UsageSummary s)
    {
        WeekdayBars.Clear();
        var max = Math.Max(1, s.WeekdayCounts.Count > 0 ? s.WeekdayCounts.Values.Max() : 1);
        for (var day = 0; day < 7; day++)
        {
            var value = s.WeekdayCounts.GetValueOrDefault(day);
            WeekdayBars.Add(new ChartBar(
                StatisticsService.WeekdayName(day)[..3], value, (double)value / max));
        }
    }

    [RelayCommand]
    private Task ExportCsv() => ExportAsync(_services.Exporter.ExportCsv, "CSV");

    [RelayCommand]
    private Task ExportJson() => ExportAsync(_services.Exporter.ExportJson, "JSON");

    [RelayCommand]
    private Task ExportPdf() => ExportAsync(_services.Exporter.ExportPdf, "PDF");

    private async Task ExportAsync(Func<double?, double?, string> exporter, string label)
    {
        StatusText = $"{label} raporu oluşturuluyor...";
        var start = SelectedPeriod.StartTimestamp();
        try
        {
            var path = await Task.Run(() => exporter(start, null));
            StatusText = $"{label} raporu hazır: {path}";
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
    }
}
