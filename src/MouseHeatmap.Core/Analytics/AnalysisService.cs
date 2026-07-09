using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public sealed class AnalysisService
{
    private readonly StatisticsService _statistics;

    public AnalysisService(StatisticsService statistics) => _statistics = statistics;

    public AnalysisResult Analyze(double? startTs = null, double? endTs = null)
    {
        var events = _statistics.LoadEvents(startTs, endTs);
        var summary = _statistics.ComputeSummary(startTs, endTs);

        var windows = ModeClassifier.Classify(events);
        var speed = SpeedAnalyzer.Analyze(events);
        var score = ProductivityCalculator.Calculate(events, windows);

        return new AnalysisResult
        {
            Windows = windows,
            WorkMinutes = windows.Count(w => w.Mode == ActivityMode.Work),
            GamingMinutes = windows.Count(w => w.Mode == ActivityMode.Gaming),
            IdleMinutes = windows.Count(w => w.Mode == ActivityMode.Idle),
            Speed = speed,
            Score = score,
            Insights = InsightsEngine.Generate(summary, speed, windows, score)
        };
    }
}
