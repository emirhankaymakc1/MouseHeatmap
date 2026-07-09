using System.Diagnostics;

namespace MouseHeatmap.Core.Tracking;

public sealed class SmartFilter
{
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private int _lastX = int.MinValue;
    private int _lastY = int.MinValue;
    private long _lastMs;

    public volatile int MinDistancePx;

    public volatile int MinIntervalMs;

    public SmartFilter(int minDistancePx, int minIntervalMs)
    {
        MinDistancePx = minDistancePx;
        MinIntervalMs = minIntervalMs;
    }

    public bool ShouldRecordMove(int x, int y)
    {
        var nowMs = _clock.ElapsedMilliseconds;

        if (_lastX == int.MinValue)
        {
            Accept(x, y, nowMs);
            return true;
        }

        var dx = (double)(x - _lastX);
        var dy = (double)(y - _lastY);
        var distance = Math.Sqrt(dx * dx + dy * dy);
        var elapsed = nowMs - _lastMs;

        if (distance >= MinDistancePx || elapsed >= MinIntervalMs)
        {
            Accept(x, y, nowMs);
            return true;
        }
        return false;
    }

    private void Accept(int x, int y, long nowMs)
    {
        _lastX = x;
        _lastY = y;
        _lastMs = nowMs;
    }
}
