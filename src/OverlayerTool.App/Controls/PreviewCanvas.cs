using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OverlayerTool.Core.Models;

namespace OverlayerTool.App.Controls;

public class PreviewCanvas : Control
{
    public static readonly StyledProperty<Bitmap?> PreviewImageProperty =
        AvaloniaProperty.Register<PreviewCanvas, Bitmap?>(nameof(PreviewImage));

    public static readonly StyledProperty<Bitmap?> BaseImageProperty =
        AvaloniaProperty.Register<PreviewCanvas, Bitmap?>(nameof(BaseImage));

    public static readonly StyledProperty<Bitmap?> ReferenceImageProperty =
        AvaloniaProperty.Register<PreviewCanvas, Bitmap?>(nameof(ReferenceImage));

    public static readonly StyledProperty<PreviewCompareMode> CompareModeProperty =
        AvaloniaProperty.Register<PreviewCanvas, PreviewCompareMode>(nameof(CompareMode));

    public static readonly StyledProperty<double> OverlayOpacityProperty =
        AvaloniaProperty.Register<PreviewCanvas, double>(nameof(OverlayOpacity), 0.5);

    static PreviewCanvas()
    {
        AffectsRender<PreviewCanvas>(
            PreviewImageProperty,
            BaseImageProperty,
            ReferenceImageProperty,
            CompareModeProperty,
            OverlayOpacityProperty);
    }

    public Bitmap? PreviewImage
    {
        get => GetValue(PreviewImageProperty);
        set => SetValue(PreviewImageProperty, value);
    }

    public Bitmap? BaseImage
    {
        get => GetValue(BaseImageProperty);
        set => SetValue(BaseImageProperty, value);
    }

    public Bitmap? ReferenceImage
    {
        get => GetValue(ReferenceImageProperty);
        set => SetValue(ReferenceImageProperty, value);
    }

    public PreviewCompareMode CompareMode
    {
        get => GetValue(CompareModeProperty);
        set => SetValue(CompareModeProperty, value);
    }

    public double OverlayOpacity
    {
        get => GetValue(OverlayOpacityProperty);
        set => SetValue(OverlayOpacityProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        if (PreviewImage is null)
        {
            DrawPlaceholder(context, "暂无预览，请在表格中点击「预览」");
            return;
        }

        switch (CompareMode)
        {
            case PreviewCompareMode.SideBySideWithBase when BaseImage is not null:
                DrawSideBySide(context, BaseImage, PreviewImage, "底板", "预览效果");
                break;
            case PreviewCompareMode.SideBySideWithReference when ReferenceImage is not null:
                DrawSideBySide(context, ReferenceImage, PreviewImage, "示例图", "预览效果");
                break;
            case PreviewCompareMode.OverlayBase when BaseImage is not null:
                DrawOverlay(context, PreviewImage, BaseImage, OverlayOpacity, "叠加底板");
                break;
            case PreviewCompareMode.OverlayReference when ReferenceImage is not null:
                DrawOverlay(context, PreviewImage, ReferenceImage, OverlayOpacity, "叠加示例图");
                break;
            default:
                DrawSingle(context, PreviewImage, "预览效果");
                break;
        }
    }

    private static void DrawPlaceholder(DrawingContext context, string message)
    {
        var text = new FormattedText(
            message,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            16,
            Brushes.Gray);
        context.DrawText(text, new Point(20, 20));
    }

    private void DrawSingle(DrawingContext context, Bitmap image, string label)
    {
        var rect = FitRect(image, new Rect(0, 0, Bounds.Width, Bounds.Height));
        context.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), rect);
        DrawLabel(context, label, rect.X + 8, rect.Y + 8);
    }

    private void DrawSideBySide(DrawingContext context, Bitmap left, Bitmap right, string leftLabel, string rightLabel)
    {
        var gap = 8.0;
        var halfWidth = (Bounds.Width - gap) / 2;
        var leftRect = FitRect(left, new Rect(0, 0, halfWidth, Bounds.Height));
        var rightRect = FitRect(right, new Rect(halfWidth + gap, 0, halfWidth, Bounds.Height));

        context.DrawImage(left, new Rect(0, 0, left.PixelSize.Width, left.PixelSize.Height), leftRect);
        context.DrawImage(right, new Rect(0, 0, right.PixelSize.Width, right.PixelSize.Height), rightRect);

        DrawLabel(context, leftLabel, leftRect.X + 8, leftRect.Y + 8);
        DrawLabel(context, rightLabel, rightRect.X + 8, rightRect.Y + 8);

        var dividerX = halfWidth + gap / 2;
        context.DrawLine(new Pen(Brushes.Gray, 1), new Point(dividerX, 0), new Point(dividerX, Bounds.Height));
    }

    private void DrawOverlay(DrawingContext context, Bitmap bottom, Bitmap top, double topOpacity, string label)
    {
        var rect = FitRect(bottom, new Rect(0, 0, Bounds.Width, Bounds.Height));
        context.DrawImage(bottom, new Rect(0, 0, bottom.PixelSize.Width, bottom.PixelSize.Height), rect);

        using (context.PushOpacity(topOpacity))
        {
            context.DrawImage(top, new Rect(0, 0, top.PixelSize.Width, top.PixelSize.Height), rect);
        }

        DrawLabel(context, label, rect.X + 8, rect.Y + 8);
    }

    private static void DrawLabel(DrawingContext context, string label, double x, double y)
    {
        var background = new Rect(x - 4, y - 2, label.Length * 8 + 12, 22);
        context.DrawRectangle(new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), null, background);

        var text = new FormattedText(
            label,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Arial"),
            12,
            Brushes.White);
        context.DrawText(text, new Point(x + 2, y + 2));
    }

    private static Rect FitRect(Bitmap image, Rect available)
    {
        if (available.Width <= 0 || available.Height <= 0)
            return default;

        var imageSize = image.PixelSize;
        var scale = Math.Min(available.Width / imageSize.Width, available.Height / imageSize.Height);
        if (double.IsInfinity(scale) || scale <= 0)
            scale = 1;

        var width = imageSize.Width * scale;
        var height = imageSize.Height * scale;
        var x = available.X + (available.Width - width) / 2;
        var y = available.Y + (available.Height - height) / 2;
        return new Rect(x, y, width, height);
    }
}
