namespace OverlayerTool.Core.Models;

public class TextRegion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "区域";
    public NormalizedRect Bounds { get; set; } = new(0.1, 0.1, 0.3, 0.1);
    public string FontFamily { get; set; } = "Arial";
    public Guid? CustomFontId { get; set; }
    public float FontSize { get; set; } = 24;
    public RegionFontWeight FontWeight { get; set; } = RegionFontWeight.Regular;
    public string Color { get; set; } = "#000000";
    public float RotationDegrees { get; set; }
    public HorizontalTextAlignment HorizontalAlign { get; set; } = HorizontalTextAlignment.Center;
    public VerticalTextAlignment VerticalAlign { get; set; } = VerticalTextAlignment.Center;

    public static float ComputeDefaultFontSize(NormalizedRect bounds, double imageHeight)
    {
        if (imageHeight <= 0)
            return 24;

        var (_, _, _, rh) = bounds.ToPixels(1, imageHeight);
        return Math.Max(6, (float)(rh * 0.9));
    }

    public TextRegion Clone() => new()
    {
        Id = Id,
        Name = Name,
        Bounds = Bounds,
        FontFamily = FontFamily,
        CustomFontId = CustomFontId,
        FontSize = FontSize,
        FontWeight = FontWeight,
        Color = Color,
        RotationDegrees = RotationDegrees,
        HorizontalAlign = HorizontalAlign,
        VerticalAlign = VerticalAlign
    };
}
