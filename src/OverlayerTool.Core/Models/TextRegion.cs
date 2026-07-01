namespace OverlayerTool.Core.Models;

public class TextRegion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "区域";
    public NormalizedRect Bounds { get; set; } = new(0.1, 0.1, 0.3, 0.1);
    public string FontFamily { get; set; } = "Arial";
    public Guid? CustomFontId { get; set; }
    public float FontSize { get; set; } = 24;
    public string Color { get; set; } = "#000000";
    public float RotationDegrees { get; set; }
    public HorizontalTextAlignment HorizontalAlign { get; set; } = HorizontalTextAlignment.Center;
    public VerticalTextAlignment VerticalAlign { get; set; } = VerticalTextAlignment.Center;

    public TextRegion Clone() => new()
    {
        Id = Id,
        Name = Name,
        Bounds = Bounds,
        FontFamily = FontFamily,
        CustomFontId = CustomFontId,
        FontSize = FontSize,
        Color = Color,
        RotationDegrees = RotationDegrees,
        HorizontalAlign = HorizontalAlign,
        VerticalAlign = VerticalAlign
    };
}
