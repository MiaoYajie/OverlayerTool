namespace OverlayerTool.Core.Models;

public record NormalizedRect(double X, double Y, double Width, double Height)
{
    public static NormalizedRect FromPixels(double x, double y, double width, double height, double imageWidth, double imageHeight)
    {
        if (imageWidth <= 0 || imageHeight <= 0)
            return new NormalizedRect(0, 0, 0, 0);

        return new NormalizedRect(
            x / imageWidth,
            y / imageHeight,
            width / imageWidth,
            height / imageHeight);
    }

    public (double X, double Y, double Width, double Height) ToPixels(double imageWidth, double imageHeight) =>
        (X * imageWidth, Y * imageHeight, Width * imageWidth, Height * imageHeight);

    public NormalizedRect Clamp()
    {
        var x = Math.Clamp(X, 0, 1);
        var y = Math.Clamp(Y, 0, 1);
        var w = Math.Clamp(Width, 0, 1 - x);
        var h = Math.Clamp(Height, 0, 1 - y);
        return this with { X = x, Y = y, Width = w, Height = h };
    }
}
