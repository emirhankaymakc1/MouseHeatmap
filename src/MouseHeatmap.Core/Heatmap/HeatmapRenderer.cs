using MouseHeatmap.Core.Models;
using SkiaSharp;

namespace MouseHeatmap.Core.Heatmap;

public sealed class HeatmapRenderer
{
    public int Downscale { get; init; } = 4;

    public int BlurRadius { get; init; } = 6;

    private static readonly (float Pos, SKColor Color)[] ColorStops =
    [
        (0.00f, new SKColor(0x0B, 0x1D, 0x78)),
        (0.25f, new SKColor(0x1E, 0x90, 0xFF)),
        (0.45f, new SKColor(0x2E, 0xCC, 0x71)),
        (0.65f, new SKColor(0xF1, 0xC4, 0x0F)),
        (0.82f, new SKColor(0xE6, 0x7E, 0x22)),
        (1.00f, new SKColor(0xE7, 0x4E, 0x3C))
    ];

    public SKBitmap Render(
        IReadOnlyList<MouseEvent> events,
        MonitorInfo bounds,
        bool darkBackground = true)
    {
        var gridW = Math.Max(1, bounds.Width / Downscale);
        var gridH = Math.Max(1, bounds.Height / Downscale);
        var grid = new float[gridW * gridH];

        foreach (var e in events)
        {
            if (!bounds.Contains(e.X, e.Y)) continue;
            var gx = Math.Clamp((e.X - bounds.Left) / Downscale, 0, gridW - 1);
            var gy = Math.Clamp((e.Y - bounds.Top) / Downscale, 0, gridH - 1);
            grid[gy * gridW + gx] += 1f;
        }

        BoxBlur(grid, gridW, gridH, BlurRadius, passes: 3);
        ApplyLogScale(grid);

        using var small = Colorize(grid, gridW, gridH, darkBackground);
        return small.Resize(
            new SKImageInfo(bounds.Width, bounds.Height),
            SKFilterQuality.Medium) ?? small.Copy();
    }

    public SKBitmap RenderLiveGrid(float[] grid, int gridW, int gridH)
    {
        var copy = (float[])grid.Clone();
        BoxBlur(copy, gridW, gridH, radius: 2, passes: 2);
        ApplyLogScale(copy);
        return Colorize(copy, gridW, gridH, darkBackground: true);
    }

    public static string SavePng(SKBitmap bitmap, string directory, string prefix = "heatmap")
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = File.OpenWrite(path);
        data.SaveTo(stream);
        return path;
    }

    internal static void BoxBlur(float[] grid, int width, int height, int radius, int passes)
    {
        if (radius <= 0) return;
        var temp = new float[grid.Length];

        for (var pass = 0; pass < passes; pass++)
        {
            BlurAxis(grid, temp, width, height, radius, horizontal: true);
            BlurAxis(temp, grid, width, height, radius, horizontal: false);
        }
    }

    private static void BlurAxis(
        float[] source, float[] target, int width, int height, int radius, bool horizontal)
    {
        var lineCount = horizontal ? height : width;
        var lineLength = horizontal ? width : height;
        var norm = 1f / (radius * 2 + 1);

        for (var line = 0; line < lineCount; line++)
        {
            float sum = 0;
            for (var i = -radius; i <= radius; i++)
                sum += ReadClamped(source, width, height, line, i, lineLength, horizontal);

            for (var i = 0; i < lineLength; i++)
            {
                WritePixel(target, width, line, i, sum * norm, horizontal);
                sum -= ReadClamped(source, width, height, line, i - radius, lineLength, horizontal);
                sum += ReadClamped(source, width, height, line, i + radius + 1, lineLength, horizontal);
            }
        }
    }

    private static float ReadClamped(
        float[] grid, int width, int height, int line, int index, int lineLength, bool horizontal)
    {
        index = Math.Clamp(index, 0, lineLength - 1);
        return horizontal ? grid[line * width + index] : grid[index * width + line];
    }

    private static void WritePixel(
        float[] grid, int width, int line, int index, float value, bool horizontal)
    {
        if (horizontal) grid[line * width + index] = value;
        else grid[index * width + line] = value;
    }

    private static void ApplyLogScale(float[] grid)
    {
        var max = 0f;
        for (var i = 0; i < grid.Length; i++)
        {
            grid[i] = MathF.Log(1 + grid[i]);
            if (grid[i] > max) max = grid[i];
        }
        if (max <= 0) return;
        for (var i = 0; i < grid.Length; i++)
            grid[i] /= max;
    }

    private static SKBitmap Colorize(float[] grid, int width, int height, bool darkBackground)
    {
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        var background = darkBackground
            ? new SKColor(0x12, 0x12, 0x1A)
            : new SKColor(0xF5, 0xF6, 0xFA);

        var pixels = bitmap.GetPixels();
        unsafe
        {
            var ptr = (uint*)pixels.ToPointer();
            for (var i = 0; i < grid.Length; i++)
            {
                var t = grid[i];
                var heat = MapColor(t);
                var alpha = Math.Clamp(t * 1.6f, 0f, 1f);
                var r = (byte)(heat.Red * alpha + background.Red * (1 - alpha));
                var g = (byte)(heat.Green * alpha + background.Green * (1 - alpha));
                var b = (byte)(heat.Blue * alpha + background.Blue * (1 - alpha));
                ptr[i] = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b;
            }
        }
        return bitmap;
    }

    internal static SKColor MapColor(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        for (var i = 1; i < ColorStops.Length; i++)
        {
            if (t > ColorStops[i].Pos) continue;
            var (p0, c0) = ColorStops[i - 1];
            var (p1, c1) = ColorStops[i];
            var local = (t - p0) / (p1 - p0);
            return new SKColor(
                (byte)(c0.Red + (c1.Red - c0.Red) * local),
                (byte)(c0.Green + (c1.Green - c0.Green) * local),
                (byte)(c0.Blue + (c1.Blue - c0.Blue) * local));
        }
        return ColorStops[^1].Color;
    }
}
