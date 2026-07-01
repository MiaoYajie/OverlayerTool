namespace OverlayerTool.Core.Models;

public class ProjectTemplate
{
    public string Version { get; set; } = "1.0";
    public string? BaseImageFileName { get; set; }
    public string? ReferenceImageFileName { get; set; }
    public float ReferenceOpacity { get; set; } = 0.5f;
    public bool ShowReferenceImage { get; set; } = true;
    public List<TextRegion> Regions { get; set; } = [];
    public List<CustomFont> CustomFonts { get; set; } = [];
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
