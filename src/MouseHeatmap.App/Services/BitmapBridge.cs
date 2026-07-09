using Avalonia.Media.Imaging;
using SkiaSharp;

namespace MouseHeatmap.App.Services;

public static class BitmapBridge
{
    public static Bitmap ToAvalonia(SKBitmap source)
    {
        using var image = SKImage.FromBitmap(source);
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        using var stream = data.AsStream();
        return new Bitmap(stream);
    }
}
