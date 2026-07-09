using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public static class ProductivityCalculator
{
    public static ProductivityScore Calculate(
        IReadOnlyList<MouseEvent> events,
        IReadOnlyList<ActivityWindow> windows)
    {
        var score = new ProductivityScore();
        if (events.Count < 2 || windows.Count == 0) return score;

        score.ActivityRatio = ComputeActivityRatio(events);
        score.WorkModeRatio = ComputeWorkModeRatio(windows);
        score.MovementEfficiency = ComputeMovementEfficiency(events);
        score.Consistency = ComputeConsistency(windows);

        score.Total =
            score.ActivityRatio * 0.35 +
            score.WorkModeRatio * 0.30 +
            score.MovementEfficiency * 0.20 +
            score.Consistency * 0.15;

        return score;
    }

    private static double ComputeActivityRatio(IReadOnlyList<MouseEvent> events)
    {
        var span = events[^1].Timestamp - events[0].Timestamp;
        if (span <= 0) return 0;
        var active = StatisticsService.ComputeActiveTime(events);
        return Math.Clamp(active / span, 0, 1) * 100;
    }

    private static double ComputeWorkModeRatio(IReadOnlyList<ActivityWindow> windows)
    {
        var activeWindows = windows.Count(w => w.Mode != ActivityMode.Idle);
        if (activeWindows == 0) return 0;
        var workWindows = windows.Count(w => w.Mode == ActivityMode.Work);
        return (double)workWindows / activeWindows * 100;
    }

    private static double ComputeMovementEfficiency(IReadOnlyList<MouseEvent> events)
    {
        double totalPath = 0, totalDisplacement = 0;
        var segment = new List<MouseEvent>();

        foreach (var e in events)
        {
            if (e.Type == EventType.Move)
            {
                segment.Add(e);
                continue;
            }
            AccumulateSegment(segment, ref totalPath, ref totalDisplacement);
            segment.Clear();
        }
        AccumulateSegment(segment, ref totalPath, ref totalDisplacement);

        if (totalPath <= 0) return 0;
        return Math.Clamp(totalDisplacement / totalPath, 0, 1) * 100;
    }

    private static void AccumulateSegment(
        List<MouseEvent> segment, ref double path, ref double displacement)
    {
        if (segment.Count < 2) return;
        path += StatisticsService.ComputeDistance(segment);
        double dx = segment[^1].X - segment[0].X;
        double dy = segment[^1].Y - segment[0].Y;
        displacement += Math.Sqrt(dx * dx + dy * dy);
    }

    private static double ComputeConsistency(IReadOnlyList<ActivityWindow> windows)
    {
        var active = windows.Where(w => w.Mode != ActivityMode.Idle)
            .Select(w => w.ClicksPerMinute + w.ScrollsPerMinute + w.DistancePx / 1000)
            .ToList();
        if (active.Count < 2) return 50;

        var mean = active.Average();
        if (mean <= 0) return 0;
        var stdDev = Math.Sqrt(active.Average(v => (v - mean) * (v - mean)));
        var cv = stdDev / mean;

        return Math.Clamp(1.0 - cv / 2.0, 0, 1) * 100;
    }
}
