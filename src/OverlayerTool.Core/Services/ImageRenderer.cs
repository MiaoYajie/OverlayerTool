using OverlayerTool.Core.Models;
using SkiaSharp;

namespace OverlayerTool.Core.Services;

public class ImageRenderer
{
    public async Task<IReadOnlyList<string>> GenerateBatchAsync(
        ProjectTemplate template,
        string baseImagePath,
        string? projectFolder,
        TableData table,
        string outputDirectory,
        IProgress<GenerateProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(baseImagePath))
            throw new FileNotFoundException("底板图片不存在", baseImagePath);

        Directory.CreateDirectory(outputDirectory);
        var generatedFiles = new List<string>();

        await Task.Run(() =>
        {
            using var baseBitmap = SKBitmap.Decode(baseImagePath)
                ?? throw new InvalidOperationException("无法解码底板图片");

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                using var output = RenderRow(baseBitmap, template, projectFolder, table, rowIndex);
                var fileName = BuildFileName(rowIndex, table.Rows[rowIndex], template.OutputFormat);
                var outputPath = Path.Combine(outputDirectory, fileName);
                ImageEncoder.Save(output, outputPath, template.OutputFormat, template.OutputQuality);
                generatedFiles.Add(outputPath);

                progress?.Report(new GenerateProgress(rowIndex + 1, table.Rows.Count, fileName));
            }
        }, cancellationToken);

        return generatedFiles;
    }

    public Task<SKBitmap> RenderRowAsync(
        string baseImagePath,
        ProjectTemplate template,
        string? projectFolder,
        TableData table,
        int rowIndex,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(baseImagePath))
            throw new FileNotFoundException("底板图片不存在", baseImagePath);

        if (rowIndex < 0 || rowIndex >= table.Rows.Count)
            throw new ArgumentOutOfRangeException(nameof(rowIndex));

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var baseBitmap = SKBitmap.Decode(baseImagePath)
                ?? throw new InvalidOperationException("无法解码底板图片");

            return RenderRow(baseBitmap, template, projectFolder, table, rowIndex);
        }, cancellationToken);
    }

    public SKBitmap RenderRow(
        SKBitmap baseBitmap,
        ProjectTemplate template,
        string? projectFolder,
        TableData table,
        int rowIndex)
    {
        var row = table.Rows[rowIndex];
        var output = baseBitmap.Copy();
        using var canvas = new SKCanvas(output);

        foreach (var region in template.Regions)
        {
            var columnIndex = HeaderMatcher.FindColumnIndex(table.Headers, region.Name);
            if (columnIndex < 0 || columnIndex >= row.Count)
                continue;

            var text = row[columnIndex];
            if (string.IsNullOrEmpty(text))
                continue;

            DrawTextInRegion(canvas, output.Width, output.Height, template, projectFolder, region, text);
        }

        return output;
    }

    private static void DrawTextInRegion(
        SKCanvas canvas,
        int imageWidth,
        int imageHeight,
        ProjectTemplate template,
        string? projectFolder,
        TextRegion region,
        string text)
    {
        var (rx, ry, rw, rh) = region.Bounds.ToPixels(imageWidth, imageHeight);
        if (rw <= 0 || rh <= 0)
            return;

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = ParseColor(region.Color)
        };

        using var typeface = FontService.ResolveTypeface(region, template, projectFolder);
        using var font = new SKFont(typeface, region.FontSize);

        var textBounds = font.MeasureText(text, out _);

        var centerX = (float)(rx + rw / 2);
        var centerY = (float)(ry + rh / 2);

        var drawX = region.HorizontalAlign switch
        {
            HorizontalTextAlignment.Left => (float)rx,
            HorizontalTextAlignment.Right => (float)(rx + rw - textBounds),
            _ => (float)(rx + (rw - textBounds) / 2)
        };

        var fontMetrics = font.Metrics;
        var textHeight = fontMetrics.Descent - fontMetrics.Ascent;
        var drawY = region.VerticalAlign switch
        {
            VerticalTextAlignment.Top => (float)(ry - fontMetrics.Ascent),
            VerticalTextAlignment.Bottom => (float)(ry + rh - fontMetrics.Descent),
            _ => (float)(ry + (rh - textHeight) / 2 - fontMetrics.Ascent)
        };

        canvas.Save();
        canvas.Translate(centerX, centerY);
        canvas.RotateDegrees(region.RotationDegrees);
        canvas.Translate(-centerX, -centerY);
        canvas.DrawText(text, drawX, drawY, SKTextAlign.Left, font, paint);
        canvas.Restore();
    }

    private static SKColor ParseColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return SKColors.Black;

        if (SKColor.TryParse(color, out var parsed))
            return parsed;

        return SKColors.Black;
    }

    private static string BuildFileName(int rowIndex, IReadOnlyList<string> row, OutputImageFormat format)
    {
        var suffix = row.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "output";
        foreach (var invalid in Path.GetInvalidFileNameChars())
            suffix = suffix.Replace(invalid, '_');

        suffix = suffix.Length > 40 ? suffix[..40] : suffix;
        return $"{rowIndex + 1:D4}_{suffix}{ImageEncoder.GetExtension(format)}";
    }
}
