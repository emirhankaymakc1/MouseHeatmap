using System.Globalization;
using Avalonia.Data.Converters;

namespace MouseHeatmap.App.Converters;

public sealed class RatioToHeightConverter : IValueConverter
{
    public static readonly RatioToHeightConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var ratio = value is double d ? d : 0.0;
        var max = parameter is string s && double.TryParse(s, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var m) ? m : 160.0;
        return Math.Max(2.0, ratio * max);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
