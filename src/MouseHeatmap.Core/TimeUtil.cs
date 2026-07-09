namespace MouseHeatmap.Core;

public static class TimeUtil
{
    public static DateTimeOffset ToLocal(double unixSeconds) =>
        DateTimeOffset.FromUnixTimeMilliseconds((long)(unixSeconds * 1000)).ToLocalTime();
}
