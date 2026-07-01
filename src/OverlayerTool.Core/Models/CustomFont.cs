namespace OverlayerTool.Core.Models;

public class CustomFont
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
