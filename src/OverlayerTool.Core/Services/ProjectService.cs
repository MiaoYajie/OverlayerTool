using System.Text.Json;
using System.Text.Json.Serialization;
using OverlayerTool.Core.Models;

namespace OverlayerTool.Core.Services;

public class ProjectService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public const string ProjectFileName = "project.json";
    public const string BaseImageName = "base.png";
    public const string ReferenceImageName = "reference.png";

    public string? CurrentProjectPath { get; private set; }

    public ProjectTemplate Template { get; private set; } = new();

    public bool IsDirty { get; private set; }

    public void MarkDirty() => IsDirty = true;

    public void MarkClean() => IsDirty = false;

    public void NewProject()
    {
        Template = new ProjectTemplate();
        CurrentProjectPath = null;
        IsDirty = false;
    }

    public async Task LoadAsync(string projectFolder, CancellationToken cancellationToken = default)
    {
        var projectFile = Path.Combine(projectFolder, ProjectFileName);
        if (!File.Exists(projectFile))
            throw new FileNotFoundException("未找到 project.json", projectFile);

        await using var stream = File.OpenRead(projectFile);
        var template = await JsonSerializer.DeserializeAsync<ProjectTemplate>(stream, JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("项目文件无效");

        Template = template;
        CurrentProjectPath = projectFolder;
        IsDirty = false;
    }

    public void SetProjectPath(string projectFolder)
    {
        CurrentProjectPath = projectFolder;
    }

    public async Task SaveAsync(string? projectFolder = null, CancellationToken cancellationToken = default)
    {
        projectFolder ??= CurrentProjectPath
            ?? throw new InvalidOperationException("请先指定项目保存路径");

        Directory.CreateDirectory(projectFolder);

        if (Template.BaseImageFileName is not null)
        {
            var source = ResolveAssetPath(Template.BaseImageFileName);
            if (source is not null)
            {
                var destination = Path.Combine(projectFolder, Template.BaseImageFileName);
                if (!PathsEqual(source, destination))
                    await CopyFileAsync(source, destination, cancellationToken);
            }
        }

        if (Template.ReferenceImageFileName is not null)
        {
            var source = ResolveAssetPath(Template.ReferenceImageFileName);
            if (source is not null)
            {
                var destination = Path.Combine(projectFolder, Template.ReferenceImageFileName);
                if (!PathsEqual(source, destination))
                    await CopyFileAsync(source, destination, cancellationToken);
            }
        }

        foreach (var font in Template.CustomFonts)
        {
            var source = FontService.GetFontFilePath(CurrentProjectPath, font);
            if (source is null)
                continue;

            var fontsDir = Path.Combine(projectFolder, FontService.FontsDirectory);
            Directory.CreateDirectory(fontsDir);
            var destination = Path.Combine(fontsDir, font.FileName);
            if (!PathsEqual(source, destination))
                await CopyFileAsync(source, destination, cancellationToken);
        }

        Template.UpdatedAt = DateTime.UtcNow;

        var projectFile = Path.Combine(projectFolder, ProjectFileName);
        await using var stream = File.Create(projectFile);
        await JsonSerializer.SerializeAsync(stream, Template, JsonOptions, cancellationToken);

        CurrentProjectPath = projectFolder;
        IsDirty = false;
    }

    public async Task SetBaseImageAsync(string sourcePath, string? projectFolder = null, CancellationToken cancellationToken = default)
    {
        projectFolder ??= EnsureProjectFolder();
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var targetName = ext switch
        {
            ".jpg" or ".jpeg" => "base.jpg",
            ".webp" => "base.webp",
            _ => BaseImageName
        };

        var targetPath = Path.Combine(projectFolder, targetName);
        await CopyFileAsync(sourcePath, targetPath, cancellationToken);

        Template.BaseImageFileName = targetName;
        CurrentProjectPath = projectFolder;
        MarkDirty();
    }

    public async Task SetReferenceImageAsync(string sourcePath, string? projectFolder = null, CancellationToken cancellationToken = default)
    {
        projectFolder ??= EnsureProjectFolder();
        var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
        var targetName = ext switch
        {
            ".jpg" or ".jpeg" => "reference.jpg",
            ".webp" => "reference.webp",
            _ => ReferenceImageName
        };

        var targetPath = Path.Combine(projectFolder, targetName);
        await CopyFileAsync(sourcePath, targetPath, cancellationToken);

        Template.ReferenceImageFileName = targetName;
        CurrentProjectPath = projectFolder;
        MarkDirty();
    }

    public async Task<CustomFont> AddCustomFontAsync(string sourcePath, CancellationToken cancellationToken = default)
    {
        var projectFolder = EnsureProjectFolder();
        var font = await FontService.ImportFontAsync(sourcePath, projectFolder, cancellationToken);
        Template.CustomFonts.Add(font);
        MarkDirty();
        return font;
    }

    public void RemoveCustomFont(Guid fontId)
    {
        var font = Template.CustomFonts.FirstOrDefault(f => f.Id == fontId);
        if (font is null)
            return;

        FontService.DeleteFontFile(CurrentProjectPath, font);
        Template.CustomFonts.Remove(font);

        foreach (var region in Template.Regions.Where(r => r.CustomFontId == fontId))
        {
            region.CustomFontId = null;
            region.FontFamily = "Arial";
        }

        MarkDirty();
    }

    public string? GetBaseImagePath() => ResolveAssetPath(Template.BaseImageFileName);

    public string? GetReferenceImagePath() => ResolveAssetPath(Template.ReferenceImageFileName);

    public string EnsureProjectFolder()
    {
        if (!string.IsNullOrEmpty(CurrentProjectPath))
            return CurrentProjectPath;

        var temp = Path.Combine(Path.GetTempPath(), "OverlayerTool", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        CurrentProjectPath = temp;
        return temp;
    }

    private string? ResolveAssetPath(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(CurrentProjectPath))
            return null;

        var path = Path.Combine(CurrentProjectPath, fileName);
        return File.Exists(path) ? path : null;
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);

    private static async Task CopyFileAsync(string source, string target, CancellationToken cancellationToken)
    {
        await using var sourceStream = File.OpenRead(source);
        await using var targetStream = File.Create(target);
        await sourceStream.CopyToAsync(targetStream, cancellationToken);
    }
}
