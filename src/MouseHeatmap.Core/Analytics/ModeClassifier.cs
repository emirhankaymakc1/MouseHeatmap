using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public static class ModeClassifier
{
    private const int WindowSeconds = 60;

    private const int MinWindowsForClustering = 12;

    private const double GamingSpeedThreshold = 900;
    private const double GamingClickThreshold = 25;
    private const double IdleEventThreshold = 5;

    public static List<ActivityWindow> Classify(IReadOnlyList<MouseEvent> events)
    {
        var rawWindows = BuildWindows(events);
        if (rawWindows.Count == 0) return [];

        return rawWindows.Count >= MinWindowsForClustering
            ? ClassifyWithKMeans(rawWindows)
            : rawWindows.Select(w => w with { Mode = RuleBasedMode(w) }).ToList();
    }

    private static List<ActivityWindow> BuildWindows(IReadOnlyList<MouseEvent> events)
    {
        var windows = new List<ActivityWindow>();
        if (events.Count == 0) return windows;

        var windowStart = Math.Floor(events[0].Timestamp / WindowSeconds) * WindowSeconds;
        var bucket = new List<MouseEvent>();

        foreach (var e in events)
        {
            if (e.Timestamp >= windowStart + WindowSeconds)
            {
                if (bucket.Count > 0)
                    windows.Add(SummarizeWindow(bucket, windowStart));
                bucket.Clear();
                windowStart = Math.Floor(e.Timestamp / WindowSeconds) * WindowSeconds;
            }
            bucket.Add(e);
        }
        if (bucket.Count > 0)
            windows.Add(SummarizeWindow(bucket, windowStart));

        return windows;
    }

    private static ActivityWindow SummarizeWindow(List<MouseEvent> bucket, double startTs)
    {
        var clicks = bucket.Count(e => e.Type == EventType.Click);
        var scrolls = bucket.Count(e => e.Type == EventType.Scroll);
        var distance = StatisticsService.ComputeDistance(bucket);

        double totalSpeed = 0, peakSpeed = 0;
        var speedSamples = 0;
        MouseEvent? previous = null;
        foreach (var e in bucket)
        {
            if (e.Type != EventType.Move) continue;
            if (previous is not null && MotionMath.StepSpeed(previous, e) is double speed)
            {
                totalSpeed += speed;
                speedSamples++;
                if (speed > peakSpeed) peakSpeed = speed;
            }
            previous = e;
        }

        return new ActivityWindow(
            Start: TimeUtil.ToLocal(startTs).DateTime,
            AvgSpeedPxPerSec: speedSamples > 0 ? totalSpeed / speedSamples : 0,
            PeakSpeedPxPerSec: peakSpeed,
            ClicksPerMinute: clicks,
            ScrollsPerMinute: scrolls,
            DistancePx: distance,
            Mode: ActivityMode.Idle);
    }

    private static ActivityMode RuleBasedMode(ActivityWindow w)
    {
        var eventScore = w.ClicksPerMinute + w.ScrollsPerMinute + (w.DistancePx > 0 ? 10 : 0);
        if (eventScore < IdleEventThreshold)
            return ActivityMode.Idle;
        if (w.AvgSpeedPxPerSec >= GamingSpeedThreshold ||
            w.ClicksPerMinute >= GamingClickThreshold)
            return ActivityMode.Gaming;
        return ActivityMode.Work;
    }

    private static List<ActivityWindow> ClassifyWithKMeans(List<ActivityWindow> windows)
    {
        var features = windows
            .Select(w => new[] { w.AvgSpeedPxPerSec, w.ClicksPerMinute, w.ScrollsPerMinute })
            .ToArray();
        Normalize(features);

        var assignments = KMeans(features, k: 3, iterations: 30);

        var clusterModes = new ActivityMode[3];
        for (var cluster = 0; cluster < 3; cluster++)
        {
            var members = windows.Where((_, i) => assignments[i] == cluster).ToList();
            if (members.Count == 0)
            {
                clusterModes[cluster] = ActivityMode.Idle;
                continue;
            }
            var avgWindow = new ActivityWindow(
                default,
                members.Average(m => m.AvgSpeedPxPerSec),
                members.Average(m => m.PeakSpeedPxPerSec),
                members.Average(m => m.ClicksPerMinute),
                members.Average(m => m.ScrollsPerMinute),
                members.Average(m => m.DistancePx),
                ActivityMode.Idle);
            clusterModes[cluster] = RuleBasedMode(avgWindow);
        }

        return windows
            .Select((w, i) => w with { Mode = clusterModes[assignments[i]] })
            .ToList();
    }

    private static void Normalize(double[][] features)
    {
        var dims = features[0].Length;
        for (var d = 0; d < dims; d++)
        {
            var max = features.Max(f => f[d]);
            if (max <= 0) continue;
            foreach (var f in features)
                f[d] /= max;
        }
    }

    private static int[] KMeans(double[][] points, int k, int iterations)
    {
        var random = new Random(42);
        var centroids = points.OrderBy(_ => random.Next()).Take(k)
            .Select(p => (double[])p.Clone()).ToArray();
        var assignments = new int[points.Length];

        for (var iter = 0; iter < iterations; iter++)
        {
            var changed = false;
            for (var i = 0; i < points.Length; i++)
            {
                var best = 0;
                var bestDist = double.MaxValue;
                for (var c = 0; c < k; c++)
                {
                    var dist = SquaredDistance(points[i], centroids[c]);
                    if (dist < bestDist) { bestDist = dist; best = c; }
                }
                if (assignments[i] != best) { assignments[i] = best; changed = true; }
            }
            if (!changed) break;

            for (var c = 0; c < k; c++)
            {
                var members = Enumerable.Range(0, points.Length)
                    .Where(i => assignments[i] == c).ToList();
                if (members.Count == 0) continue;
                for (var d = 0; d < centroids[c].Length; d++)
                    centroids[c][d] = members.Average(i => points[i][d]);
            }
        }
        return assignments;
    }

    private static double SquaredDistance(double[] a, double[] b)
    {
        var sum = 0.0;
        for (var i = 0; i < a.Length; i++)
        {
            var diff = a[i] - b[i];
            sum += diff * diff;
        }
        return sum;
    }
}
