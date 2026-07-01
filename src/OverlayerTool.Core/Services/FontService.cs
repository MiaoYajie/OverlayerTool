using OverlayerTool.Core.Models;
using SkiaSharp;

namespace OverlayerTool.Core.Services;

public static class FontService
{
    public const string FontsDirectory = "fonts";

    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".ttf", ".otf", ".ttc", ".woff", ".woff2" };

    public static bool IsSupportedExtension(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path));

    public static async Task<CustomFont> ImportFontAsync(
        string sourcePath,
        string projectFolder,
        CancellationToken cancellationToken = default)
    {
        if (!IsSupportedExtension(sourcePath))
            throw new NotSupportedException("仅支持 .ttf、.otf、.ttc、.woff、.woff2 字体文件");

        var fontsDir = Path.Combine(projectFolder, FontsDirectory);
        Directory.CreateDirectory(fontsDir);

        var id = Guid.NewGuid();
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var fileName = $"{id:N}{ext}";
        var targetPath = Path.Combine(fontsDir, fileName);

        await using (var sourceStream = File.OpenRead(sourcePath))
        await using (var targetStream = File.Create(targetPath))
            await sourceStream.CopyToAsync(targetStream, cancellationToken);

        using var typeface = SKTypeface.FromFile(targetPath);
        var displayName = typeface?.FamilyName;
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = Path.GetFileNameWithoutExtension(sourcePath);

        return new CustomFont
        {
            Id = id,
            DisplayName = displayName,
            FileName = fileName
        };
    }

    public static string? GetFontFilePath(string? projectFolder, CustomFont font)
    {
        if (string.IsNullOrWhiteSpace(projectFolder))
            return null;

        var path = Path.Combine(projectFolder, FontsDirectory, font.FileName);
        return File.Exists(path) ? path : null;
    }

    public static SKTypeface ResolveTypeface(TextRegion region, ProjectTemplate template, string? projectFolder)
    {
        if (region.CustomFontId is Guid fontId)
        {
            var customFont = template.CustomFonts.FirstOrDefault(f => f.Id == fontId);
            if (customFont is not null)
            {
                var path = GetFontFilePath(projectFolder, customFont);
                if (path is not null)
                {
                    var typeface = SKTypeface.FromFile(path);
                    if (typeface is not null)
                        return typeface;
                }
            }
        }

        return SKTypeface.FromFamilyName(region.FontFamily) ?? SKTypeface.Default;
    }

    public static void ApplyFontOption(TextRegion region, FontOption option)
    {
        if (option.IsCustom)
        {
            region.CustomFontId = option.CustomFontId;
            region.FontFamily = option.SystemFontName;
        }
        else
        {
            region.CustomFontId = null;
            region.FontFamily = option.SystemFontName;
        }
    }

    public static FontOption? FindMatchingOption(TextRegion region, IEnumerable<FontOption> options) =>
        options.FirstOrDefault(o => o.Matches(region));

    public static IReadOnlyList<FontOption> BuildFontOptions(
        IEnumerable<string> systemFonts,
        IEnumerable<CustomFont> customFonts)
    {
        var options = new List<FontOption>();
        options.AddRange(systemFonts.Select(FontOption.FromSystem));
        options.AddRange(customFonts.Select(FontOption.FromCustom));
        return options;
    }

    public static void DeleteFontFile(string? projectFolder, CustomFont font)
    {
        var path = GetFontFilePath(projectFolder, font);
        if (path is not null && File.Exists(path))
            File.Delete(path);
    }
}
