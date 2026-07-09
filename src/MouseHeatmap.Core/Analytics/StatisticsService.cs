using MouseHeatmap.Core.Data;
using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public sealed class StatisticsService
{
    private const double ActivityGapSec = 5.0;

    private static readonly string[] GunAdlari =
        ["Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi", "Pazar"];

    private static readonly string[,] BolgeAdlari =
    {
        { "Sol Üst", "Orta Üst", "Sağ Üst" },
        { "Sol Orta", "Merkez", "Sağ Orta" },
        { "Sol Alt", "Orta Alt", "Sağ Alt" }
    };

    private readonly EventRepository _repository;

    public StatisticsService(EventRepository repository) => _repository = repository;

    public List<MouseEvent> LoadEvents(
        double? startTs = null, double? endTs = null,
        EventType? type = null, int? monitorIndex = null) =>
        _repository.Query(startTs, endTs, type, monitorIndex);

    public UsageSummary ComputeSummary(double? startTs = null, double? endTs = null)
    {
        var events = _repository.Query(startTs, endTs);
        var summary = new UsageSummary { TotalEvents = events.Count };
        if (events.Count == 0) return summary;

        foreach (var e in events)
        {
            switch (e.Type)
            {
                case EventType.Move:
                    summary.TotalMoves++;
                    break;
                case EventType.Click:
                    summary.TotalClicks++;
                    if (e.Button == MouseButton.Left) summary.LeftClicks++;
                    else if (e.Button == MouseButton.Right) summary.RightClicks++;
                    else if (e.Button == MouseButton.Middle) summary.MiddleClicks++;
                    break;
                case EventType.Scroll:
                    summary.TotalScrolls++;
                    break;
            }
        }

        summary.TotalDistancePx = ComputeDistance(events);
        summary.ActiveTimeSec = ComputeActiveTime(events);
        FillTimeBreakdowns(events, summary);
        summary.BusiestRegion = FindBusiestRegion(events);

        return summary;
    }

    internal static double ComputeDistance(IReadOnlyList<MouseEvent> events)
    {
        var total = 0.0;
        MouseEvent? previous = null;
        foreach (var e in events)
        {
            if (e.Type != EventType.Move) continue;
            if (previous is not null)
            {
                double dx = e.X - previous.X, dy = e.Y - previous.Y;
                total += Math.Sqrt(dx * dx + dy * dy);
            }
            previous = e;
        }
        return total;
    }

    internal static double ComputeActiveTime(IReadOnlyList<MouseEvent> events)
    {
        var total = 0.0;
        for (var i = 1; i < events.Count; i++)
        {
            var gap = events[i].Timestamp - events[i - 1].Timestamp;
            if (gap <= ActivityGapSec) total += gap;
        }
        return total;
    }

    private static void FillTimeBreakdowns(List<MouseEvent> events, UsageSummary summary)
    {
        foreach (var e in events)
        {
            var local = TimeUtil.ToLocal(e.Timestamp);

            var hour = local.Hour;
            summary.HourlyCounts[hour] = summary.HourlyCounts.GetValueOrDefault(hour) + 1;

            var date = DateOnly.FromDateTime(local.Date);
            summary.DailyCounts[date] = summary.DailyCounts.GetValueOrDefault(date) + 1;

            var weekday = ((int)local.DayOfWeek + 6) % 7;
            summary.WeekdayCounts[weekday] = summary.WeekdayCounts.GetValueOrDefault(weekday) + 1;
        }

        if (summary.HourlyCounts.Count > 0)
            summary.BusiestHour = summary.HourlyCounts.MaxBy(kv => kv.Value).Key;
        if (summary.WeekdayCounts.Count > 0)
            summary.BusiestDay = GunAdlari[summary.WeekdayCounts.MaxBy(kv => kv.Value).Key];
    }

    private string? FindBusiestRegion(List<MouseEvent> events)
    {
        var monitors = _repository.LoadMonitors();
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors.FirstOrDefault();
        if (primary is null) return null;

        var cells = new int[9];
        foreach (var e in events)
        {
            if (!primary.Contains(e.X, e.Y)) continue;
            var col = Math.Clamp((e.X - primary.Left) * 3 / primary.Width, 0, 2);
            var row = Math.Clamp((e.Y - primary.Top) * 3 / primary.Height, 0, 2);
            cells[row * 3 + col]++;
        }

        if (cells.All(c => c == 0)) return null;
        var busiest = Array.IndexOf(cells, cells.Max());
        return BolgeAdlari[busiest / 3, busiest % 3];
    }

    public static string WeekdayName(int index) => GunAdlari[index];
}
