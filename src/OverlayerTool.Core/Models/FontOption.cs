namespace OverlayerTool.Core.Models;

public class FontOption
{
    public string DisplayName { get; init; } = string.Empty;
    public bool IsCustom { get; init; }
    public Guid? CustomFontId { get; init; }
    public string SystemFontName { get; init; } = "Arial";

    public override string ToString() => DisplayName;

    public static FontOption FromSystem(string name) => new()
    {
        DisplayName = name,
        IsCustom = false,
        SystemFontName = name
    };

    public static FontOption FromCustom(CustomFont font) => new()
    {
        DisplayName = $"[自定义] {font.DisplayName}",
        IsCustom = true,
        CustomFontId = font.Id,
        SystemFontName = font.DisplayName
    };

    public bool Matches(TextRegion region)
    {
        if (IsCustom)
            return region.CustomFontId == CustomFontId;

        return region.CustomFontId is null
               && string.Equals(region.FontFamily, SystemFontName, StringComparison.OrdinalIgnoreCase);
    }
}
