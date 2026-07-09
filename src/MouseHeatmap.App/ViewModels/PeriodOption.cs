namespace MouseHeatmap.App.ViewModels;

public sealed record PeriodOption(string Label)
{
    public double? StartTimestamp()
    {
        var now = DateTimeOffset.Now;
        return Label switch
        {
            "Bugün" => new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset)
                .ToUnixTimeMilliseconds() / 1000.0,
            "Son 7 Gün" => now.AddDays(-7).ToUnixTimeMilliseconds() / 1000.0,
            "Son 30 Gün" => now.AddDays(-30).ToUnixTimeMilliseconds() / 1000.0,
            _ => null
        };
    }

    public override string ToString() => Label;

    public static IReadOnlyList<PeriodOption> All { get; } =
    [
        new("Bugün"), new("Son 7 Gün"), new("Son 30 Gün"), new("Tümü")
    ];
}
