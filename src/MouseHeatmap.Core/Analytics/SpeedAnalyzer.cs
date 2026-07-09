using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public static class SpeedAnalyzer
{
    private static readonly (double Limit, string Label)[] Buckets =
    [
        (100, "0-100"),
        (300, "100-300"),
        (600, "300-600"),
        (1200, "600-1.2K"),
        (2500, "1.2K-2.5K"),
        (double.MaxValue, "2.5K+")
    ];

    public static SpeedStats Analyze(IReadOnlyList<MouseEvent> events)
    {
        var stats = new SpeedStats();
        var samples = new List<double>();
        var hourlySums = new Dictionary<int, (double Sum, int Count)>();

        MouseEvent? previous = null;
        foreach (var e in events)
        {
            if (e.Type != EventType.Move) continue;
            if (previous is not null && MotionMath.StepSpeed(previous, e) is double speed)
            {
                samples.Add(speed);

                var hour = TimeUtil.ToLocal(e.Timestamp).Hour;
                var (sum, count) = hourlySums.GetValueOrDefault(hour);
                hourlySums[hour] = (sum + speed, count + 1);
            }
            previous = e;
        }

        if (samples.Count == 0) return stats;

        samples.Sort();
        stats.AvgSpeedPxPerSec = samples.Average();
        stats.MaxSpeedPxPerSec = samples[^1];
        stats.MedianSpeedPxPerSec = samples[samples.Count / 2];

        foreach (var (_, label) in Buckets)
            stats.Histogram[label] = 0;
        foreach (var speed in samples)
        {
            foreach (var (limit, label) in Buckets)
            {
                if (speed < limit)
                {
                    stats.Histogram[label]++;
                    break;
                }
            }
        }

        foreach (var (hour, (sum, count)) in hourlySums)
            stats.HourlyAvgSpeed[hour] = sum / count;

        return stats;
    }
}
