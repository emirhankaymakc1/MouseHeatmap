using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MouseHeatmap.App.Services;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.App.ViewModels;

public sealed record InsightCard(string Emoji, string Title, string Detail);

public sealed record ModeBar(string Label, double Minutes, double Ratio, string Color);

public sealed partial class AnalysisViewModel : ObservableObject
{
    private readonly AppServices _services;

    [ObservableProperty] private ObservableCollection<PeriodOption> _periods = new(PeriodOption.All);
    [ObservableProperty] private PeriodOption _selectedPeriod = PeriodOption.All[1];
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusText = "Analiz için dönem seçip 'Analiz Et'e basın.";

    [ObservableProperty] private double _scoreTotal;
    [ObservableProperty] private string _scoreGrade = "-";
    [ObservableProperty] private string _activityRatio = "-";
    [ObservableProperty] private string _workRatio = "-";
    [ObservableProperty] private string _movementEfficiency = "-";
    [ObservableProperty] private string _consistency = "-";

    [ObservableProperty] private string _avgSpeed = "-";
    [ObservableProperty] private string _maxSpeed = "-";
    [ObservableProperty] private string _medianSpeed = "-";

    [ObservableProperty] private string _workMinutes = "-";
    [ObservableProperty] private string _gamingMinutes = "-";
    [ObservableProperty] private string _idleMinutes = "-";

    public ObservableCollection<InsightCard> Insights { get; } = new();
    public ObservableCollection<ModeBar> ModeBars { get; } = new();
    public ObservableCollection<ChartBar> SpeedHistogram { get; } = new();

    public AnalysisViewModel(AppServices services) => _services = services;

    [RelayCommand]
    private async Task Analyze()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Yapay zeka analizi çalışıyor...";

        var start = SelectedPeriod.StartTimestamp();
        try
        {
            var result = await Task.Run(() => _services.Analysis.Analyze(start));
            ApplyResult(result);
            StatusText = Insights.Count > 0
                ? $"{Insights.Count} içgörü üretildi."
                : "Analiz tamamlandı.";
        }
        catch (Exception ex)
        {
            StatusText = $"Hata: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyResult(AnalysisResult r)
    {
        ScoreTotal = Math.Round(r.Score.Total, 1);
        ScoreGrade = r.Score.Grade;
        ActivityRatio = $"%{r.Score.ActivityRatio:F0}";
        WorkRatio = $"%{r.Score.WorkModeRatio:F0}";
        MovementEfficiency = $"%{r.Score.MovementEfficiency:F0}";
        Consistency = $"%{r.Score.Consistency:F0}";

        AvgSpeed = $"{r.Speed.AvgSpeedPxPerSec:F0} px/sn";
        MaxSpeed = $"{r.Speed.MaxSpeedPxPerSec:F0} px/sn";
        MedianSpeed = $"{r.Speed.MedianSpeedPxPerSec:F0} px/sn";

        WorkMinutes = $"{r.WorkMinutes:F0} dk";
        GamingMinutes = $"{r.GamingMinutes:F0} dk";
        IdleMinutes = $"{r.IdleMinutes:F0} dk";
        BuildModeBars(r);
        BuildSpeedHistogram(r);

        Insights.Clear();
        foreach (var insight in r.Insights)
            Insights.Add(new InsightCard(insight.Emoji, insight.Title, insight.Detail));
    }

    private void BuildModeBars(AnalysisResult r)
    {
        ModeBars.Clear();
        var total = Math.Max(1, r.WorkMinutes + r.GamingMinutes + r.IdleMinutes);
        ModeBars.Add(new ModeBar("💼 Çalışma", r.WorkMinutes, r.WorkMinutes / total, "#2ECC71"));
        ModeBars.Add(new ModeBar("🎮 Oyun", r.GamingMinutes, r.GamingMinutes / total, "#E74C3C"));
        ModeBars.Add(new ModeBar("💤 Boşta", r.IdleMinutes, r.IdleMinutes / total, "#718096"));
    }

    private void BuildSpeedHistogram(AnalysisResult r)
    {
        SpeedHistogram.Clear();
        var max = Math.Max(1, r.Speed.Histogram.Count > 0
            ? r.Speed.Histogram.Values.Max() : 1);
        foreach (var (label, count) in r.Speed.Histogram)
            SpeedHistogram.Add(new ChartBar(label, count, (double)count / max));
    }
}
