using OverlayerTool.Core.Models;
using SkiaSharp;

namespace OverlayerTool.Core.Services;

public static class ImageEncoder
{
    public static string GetExtension(OutputImageFormat format) =>
        format == OutputImageFormat.Jpeg ? ".jpg" : ".png";

    public static void Save(SKBitmap bitmap, string path, OutputImageFormat format, int quality)
    {
        quality = Math.Clamp(quality, 1, 100);

        using var image = SKImage.FromBitmap(bitmap);
        var encodedFormat = format == OutputImageFormat.Jpeg
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;

        using var data = image.Encode(encodedFormat, quality);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }
}
