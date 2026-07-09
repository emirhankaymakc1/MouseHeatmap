using MouseHeatmap.Core.Models;

namespace MouseHeatmap.Core.Analytics;

public static class MotionMath
{
    public const double MaxSpeedGapSec = 2.0;

    public static double? StepSpeed(MouseEvent previous, MouseEvent current)
    {
        var dt = current.Timestamp - previous.Timestamp;
        if (dt <= 0 || dt > MaxSpeedGapSec) return null;
        double dx = current.X - previous.X, dy = current.Y - previous.Y;
        return Math.Sqrt(dx * dx + dy * dy) / dt;
    }
}
