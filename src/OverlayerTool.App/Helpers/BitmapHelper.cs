using Avalonia.Media.Imaging;
using SkiaSharp;

namespace OverlayerTool.App.Helpers;

public static class BitmapHelper
{
    public static Bitmap FromSkBitmap(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new Bitmap(new MemoryStream(data.ToArray()));
    }
}
