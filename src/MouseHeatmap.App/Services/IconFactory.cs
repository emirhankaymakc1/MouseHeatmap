using Avalonia.Controls;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace MouseHeatmap.App.Services;

public static class IconFactory
{
    public static WindowIcon CreateWindowIcon(bool recording)
    {
        using var bitmap = Draw(recording, 64);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = data.AsStream();
        return new WindowIcon(new Bitmap(stream));
    }

    private static SKBitmap Draw(bool recording, int size)
    {
        var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint { IsAntialias = true };

        paint.Color = new SKColor(0x2B, 0x6C, 0xB0);
        canvas.DrawRoundRect(4, 4, size - 8, size - 8, 14, 14, paint);

        paint.Color = SKColors.White;
        canvas.DrawOval(new SKRect(size * 0.34f, size * 0.28f,
            size * 0.66f, size * 0.75f), paint);

        if (recording)
        {
            paint.Color = new SKColor(0xE7, 0x4E, 0x3C);
            canvas.DrawCircle(size * 0.78f, size * 0.24f, size * 0.13f, paint);
        }

        return bitmap;
    }
}
