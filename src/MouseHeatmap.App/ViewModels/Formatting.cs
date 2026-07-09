namespace MouseHeatmap.App.ViewModels;

public static class Formatting
{
    public static string Duration(double seconds)
    {
        var total = (int)seconds;
        var hours = total / 3600;
        var minutes = total % 3600 / 60;
        if (hours > 0) return $"{hours}s {minutes}d";
        if (minutes > 0) return $"{minutes}d";
        return $"{total}sn";
    }

    public static string Distance(double pixels)
    {
        var meters = pixels * 0.0254 / 96.0;
        return meters >= 1000 ? $"{meters / 1000:F2} km" : $"{meters:F1} m";
    }

    public static string Count(long value) => value.ToString("N0");
}
