namespace MouseHeatmap.Core.Models;

public sealed class UsageSummary
{
    public int TotalEvents { get; set; }
    public int TotalMoves { get; set; }
    public int TotalClicks { get; set; }
    public int LeftClicks { get; set; }
    public int RightClicks { get; set; }
    public int MiddleClicks { get; set; }
    public int TotalScrolls { get; set; }

    public double TotalDistancePx { get; set; }

    public double ActiveTimeSec { get; set; }

    public int? BusiestHour { get; set; }
    public string? BusiestDay { get; set; }
    public string? BusiestRegion { get; set; }

    public Dictionary<int, int> HourlyCounts { get; set; } = new();

    public Dictionary<DateOnly, int> DailyCounts { get; set; } = new();

    public Dictionary<int, int> WeekdayCounts { get; set; } = new();

    public double TotalDistanceMeters => TotalDistancePx * 0.0254 / 96.0;
}

public enum ActivityMode
{
    Idle,

    Work,

    Gaming
}

public sealed record ActivityWindow(
    DateTime Start,
    double AvgSpeedPxPerSec,
    double PeakSpeedPxPerSec,
    double ClicksPerMinute,
    double ScrollsPerMinute,
    double DistancePx,
    ActivityMode Mode);

public sealed class SpeedStats
{
    public double AvgSpeedPxPerSec { get; set; }
    public double MaxSpeedPxPerSec { get; set; }
    public double MedianSpeedPxPerSec { get; set; }

    public Dictionary<string, int> Histogram { get; set; } = new();

    public Dictionary<int, double> HourlyAvgSpeed { get; set; } = new();
}

public sealed class ProductivityScore
{
    public double Total { get; set; }

    public double ActivityRatio { get; set; }

    public double WorkModeRatio { get; set; }

    public double MovementEfficiency { get; set; }

    public double Consistency { get; set; }

    public string Grade =>
        Total >= 90 ? "A+" : Total >= 80 ? "A" : Total >= 65 ? "B" :
        Total >= 50 ? "C" : "D";
}

public sealed record Insight(string Emoji, string Title, string Detail);

public sealed class AnalysisResult
{
    public List<ActivityWindow> Windows { get; set; } = new();
    public double WorkMinutes { get; set; }
    public double GamingMinutes { get; set; }
    public double IdleMinutes { get; set; }
    public SpeedStats Speed { get; set; } = new();
    public ProductivityScore Score { get; set; } = new();
    public List<Insight> Insights { get; set; } = new();
}
