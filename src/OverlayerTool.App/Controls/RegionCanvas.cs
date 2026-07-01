using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using OverlayerTool.Core.Models;

namespace OverlayerTool.App.Controls;

public partial class RegionCanvas : Control
{
    private enum DragMode
    {
        None,
        Draw,
        Move,
        Resize,
        Pan
    }

    private enum ResizeHandle
    {
        None,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public const double MinZoom = 0.25;
    public const double MaxZoom = 5.0;

    private static readonly IBrush[] RegionBorderBrushes =
    [
        Brushes.DodgerBlue,
        Brushes.OrangeRed,
        Brushes.MediumSeaGreen,
        Brushes.MediumPurple,
        Brushes.Goldenrod,
        Brushes.DeepPink
    ];

    public static readonly StyledProperty<Bitmap?> BaseImageProperty =
        AvaloniaProperty.Register<RegionCanvas, Bitmap?>(nameof(BaseImage));

    public static readonly StyledProperty<Bitmap?> ReferenceImageProperty =
        AvaloniaProperty.Register<RegionCanvas, Bitmap?>(nameof(ReferenceImage));

    public static readonly StyledProperty<double> ReferenceOpacityProperty =
        AvaloniaProperty.Register<RegionCanvas, double>(nameof(ReferenceOpacity), 0.5);

    public static readonly StyledProperty<bool> ShowReferenceImageProperty =
        AvaloniaProperty.Register<RegionCanvas, bool>(nameof(ShowReferenceImage), true);

    public static readonly StyledProperty<IReadOnlyList<TextRegion>?> RegionsProperty =
        AvaloniaProperty.Register<RegionCanvas, IReadOnlyList<TextRegion>?>(nameof(Regions));

    public static readonly StyledProperty<Guid?> SelectedRegionIdProperty =
        AvaloniaProperty.Register<RegionCanvas, Guid?>(nameof(SelectedRegionId));

    public static readonly StyledProperty<double> ZoomProperty =
        AvaloniaProperty.Register<RegionCanvas, double>(nameof(Zoom), 1.0, coerce: CoerceZoom);

    private Rect _imageDisplayRect;
    private Point _dragStart;
    private Point _panStart;
    private NormalizedRect? _dragStartBounds;
    private DragMode _dragMode = DragMode.None;
    private ResizeHandle _resizeHandle = ResizeHandle.None;
    private NormalizedRect? _previewBounds;
    private Vector _panOffset;

    static RegionCanvas()
    {
        AffectsRender<RegionCanvas>(
            BaseImageProperty,
            ReferenceImageProperty,
            ReferenceOpacityProperty,
            ShowReferenceImageProperty,
            RegionsProperty,
            SelectedRegionIdProperty,
            ZoomProperty);
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

    public double ReferenceOpacity
    {
        get => GetValue(ReferenceOpacityProperty);
        set => SetValue(ReferenceOpacityProperty, value);
    }

    public bool ShowReferenceImage
    {
        get => GetValue(ShowReferenceImageProperty);
        set => SetValue(ShowReferenceImageProperty, value);
    }

    public IReadOnlyList<TextRegion>? Regions
    {
        get => GetValue(RegionsProperty);
        set => SetValue(RegionsProperty, value);
    }

    public Guid? SelectedRegionId
    {
        get => GetValue(SelectedRegionIdProperty);
        set => SetValue(SelectedRegionIdProperty, value);
    }

    public double Zoom
    {
        get => GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    public event EventHandler<RegionBoundsEventArgs>? RegionDrawn;
    public event EventHandler<RegionBoundsEventArgs>? RegionMoved;
    public event EventHandler<Guid>? RegionSelected;

    public RegionCanvas()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public void ResetView()
    {
        Zoom = 1.0;
        _panOffset = default;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        UpdateImageDisplayRect();

        if (BaseImage is not null && _imageDisplayRect.Width > 0 && _imageDisplayRect.Height > 0)
        {
            context.DrawImage(BaseImage, new Rect(0, 0, BaseImage.PixelSize.Width, BaseImage.PixelSize.Height), _imageDisplayRect);

            if (ShowReferenceImage && ReferenceImage is not null)
            {
                using (context.PushOpacity(ReferenceOpacity))
                {
                    context.DrawImage(ReferenceImage,
                        new Rect(0, 0, ReferenceImage.PixelSize.Width, ReferenceImage.PixelSize.Height),
                        _imageDisplayRect);
                }
            }
        }
        else
        {
            var text = new FormattedText(
                "请导入底板图片",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                16,
                Brushes.Gray);
            context.DrawText(text, new Point(20, 20));
        }

        if (Regions is null)
            return;

        for (var i = 0; i < Regions.Count; i++)
        {
            var region = Regions[i];
            var brush = RegionBorderBrushes[i % RegionBorderBrushes.Length];
            var rect = ToDisplayRect(region.Bounds);
            var isSelected = SelectedRegionId == region.Id;
            var pen = new Pen(brush, isSelected ? 2.5 : 1.5);

            context.DrawRectangle(null, pen, rect);

            var label = new FormattedText(
                region.Name,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                12,
                brush);
            context.DrawText(label, new Point(rect.X + 2, Math.Max(0, rect.Y - 16)));
        }

        if (_previewBounds is not null)
        {
            var previewRect = ToDisplayRect(_previewBounds);
            context.DrawRectangle(null, new Pen(Brushes.White, 1, dashStyle: DashStyle.Dash), previewRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (BaseImage is null)
            return;

        var props = e.GetCurrentPoint(this).Properties;
        var point = e.GetPosition(this);
        UpdateImageDisplayRect();

        if (props.IsRightButtonPressed || props.IsMiddleButtonPressed)
        {
            Focus();
            _dragMode = DragMode.Pan;
            _dragStart = point;
            _panStart = new Point(_panOffset.X, _panOffset.Y);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (!props.IsLeftButtonPressed)
            return;

        Focus();

        if (!_imageDisplayRect.Contains(point))
            return;

        var normalized = ToNormalizedPoint(point);

        if (SelectedRegionId is Guid selectedId)
        {
            var selected = Regions?.FirstOrDefault(r => r.Id == selectedId);
            if (selected is not null)
            {
                var displayRect = ToDisplayRect(selected.Bounds);
                _resizeHandle = HitTestHandle(displayRect, point);
                if (_resizeHandle != ResizeHandle.None)
                {
                    _dragMode = DragMode.Resize;
                    _dragStart = point;
                    _dragStartBounds = selected.Bounds;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                    return;
                }

                if (displayRect.Contains(point))
                {
                    _dragMode = DragMode.Move;
                    _dragStart = point;
                    _dragStartBounds = selected.Bounds;
                    e.Pointer.Capture(this);
                    e.Handled = true;
                    return;
                }
            }
        }

        foreach (var region in Regions ?? [])
        {
            if (ToDisplayRect(region.Bounds).Contains(point))
            {
                SelectedRegionId = region.Id;
                RegionSelected?.Invoke(this, region.Id);
                _dragMode = DragMode.Move;
                _dragStart = point;
                _dragStartBounds = region.Bounds;
                e.Pointer.Capture(this);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        _dragMode = DragMode.Draw;
        _dragStart = point;
        _previewBounds = NormalizedRect.FromPixels(
            normalized.X * BaseImage.PixelSize.Width,
            normalized.Y * BaseImage.PixelSize.Height,
            0,
            0,
            BaseImage.PixelSize.Width,
            BaseImage.PixelSize.Height);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (BaseImage is null || _dragMode == DragMode.None)
            return;

        var point = e.GetPosition(this);

        if (_dragMode == DragMode.Pan)
        {
            _panOffset = new Vector(
                _panStart.X + (point.X - _dragStart.X),
                _panStart.Y + (point.Y - _dragStart.Y));
            InvalidateVisual();
            return;
        }

        var normalized = ToNormalizedPoint(point);

        switch (_dragMode)
        {
            case DragMode.Draw:
            {
                var startNorm = ToNormalizedPoint(_dragStart);
                var x = Math.Min(startNorm.X, normalized.X);
                var y = Math.Min(startNorm.Y, normalized.Y);
                var w = Math.Abs(normalized.X - startNorm.X);
                var h = Math.Abs(normalized.Y - startNorm.Y);
                _previewBounds = new NormalizedRect(x, y, w, h).Clamp();
                InvalidateVisual();
                break;
            }
            case DragMode.Move when _dragStartBounds is not null && SelectedRegionId is Guid moveId:
            {
                var startNorm = ToNormalizedPoint(_dragStart);
                var dx = normalized.X - startNorm.X;
                var dy = normalized.Y - startNorm.Y;
                var moved = new NormalizedRect(
                    _dragStartBounds.X + dx,
                    _dragStartBounds.Y + dy,
                    _dragStartBounds.Width,
                    _dragStartBounds.Height).Clamp();
                RegionMoved?.Invoke(this, new RegionBoundsEventArgs(moveId, moved));
                break;
            }
            case DragMode.Resize when _dragStartBounds is not null && SelectedRegionId is Guid resizeId:
            {
                var resized = ResizeBounds(_dragStartBounds, _resizeHandle, normalized);
                RegionMoved?.Invoke(this, new RegionBoundsEventArgs(resizeId, resized.Clamp()));
                break;
            }
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_dragMode == DragMode.Draw && _previewBounds is not null)
        {
            if (_previewBounds.Width > 0.01 && _previewBounds.Height > 0.01)
                RegionDrawn?.Invoke(this, new RegionBoundsEventArgs(Guid.Empty, _previewBounds));
        }

        _dragMode = DragMode.None;
        _resizeHandle = ResizeHandle.None;
        _dragStartBounds = null;
        _previewBounds = null;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (BaseImage is null)
            return;

        UpdateImageDisplayRect();
        var cursorPoint = e.GetPosition(this);
        var normalizedBefore = TryToNormalizedPoint(cursorPoint);

        var factor = e.Delta.Y > 0 ? 1.15 : 1 / 1.15;
        Zoom = Math.Clamp(Zoom * factor, MinZoom, MaxZoom);

        if (normalizedBefore is Point norm)
        {
            UpdateImageDisplayRect();
            var displayAfter = ToDisplayPoint(norm);
            _panOffset += new Vector(cursorPoint.X - displayAfter.X, cursorPoint.Y - displayAfter.Y);
        }

        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        InvalidateVisual();
    }

    private static double CoerceZoom(AvaloniaObject sender, double value) =>
        Math.Clamp(value, MinZoom, MaxZoom);

    private void UpdateImageDisplayRect()
    {
        if (BaseImage is null)
        {
            _imageDisplayRect = default;
            return;
        }

        var imageSize = BaseImage.PixelSize;
        var fitScale = Math.Min(Bounds.Width / imageSize.Width, Bounds.Height / imageSize.Height);
        if (double.IsInfinity(fitScale) || fitScale <= 0)
            fitScale = 1;

        var scale = fitScale * Zoom;
        var displayWidth = imageSize.Width * scale;
        var displayHeight = imageSize.Height * scale;
        var offsetX = (Bounds.Width - displayWidth) / 2 + _panOffset.X;
        var offsetY = (Bounds.Height - displayHeight) / 2 + _panOffset.Y;
        _imageDisplayRect = new Rect(offsetX, offsetY, displayWidth, displayHeight);
    }

    private Point? TryToNormalizedPoint(Point point)
    {
        if (_imageDisplayRect.Width <= 0 || _imageDisplayRect.Height <= 0)
            return null;

        if (!_imageDisplayRect.Contains(point))
            return null;

        return ToNormalizedPoint(point);
    }

    private Point ToNormalizedPoint(Point point)
    {
        var x = (point.X - _imageDisplayRect.X) / _imageDisplayRect.Width;
        var y = (point.Y - _imageDisplayRect.Y) / _imageDisplayRect.Height;
        return new Point(Math.Clamp(x, 0, 1), Math.Clamp(y, 0, 1));
    }

    private Point ToDisplayPoint(Point normalized)
    {
        return new Point(
            _imageDisplayRect.X + normalized.X * _imageDisplayRect.Width,
            _imageDisplayRect.Y + normalized.Y * _imageDisplayRect.Height);
    }

    private Rect ToDisplayRect(NormalizedRect bounds)
    {
        return new Rect(
            _imageDisplayRect.X + bounds.X * _imageDisplayRect.Width,
            _imageDisplayRect.Y + bounds.Y * _imageDisplayRect.Height,
            bounds.Width * _imageDisplayRect.Width,
            bounds.Height * _imageDisplayRect.Height);
    }

    private static NormalizedRect ResizeBounds(NormalizedRect start, ResizeHandle handle, Point normalized)
    {
        var x = start.X;
        var y = start.Y;
        var w = start.Width;
        var h = start.Height;
        var right = x + w;
        var bottom = y + h;

        switch (handle)
        {
            case ResizeHandle.TopLeft:
                x = normalized.X;
                y = normalized.Y;
                w = right - x;
                h = bottom - y;
                break;
            case ResizeHandle.TopRight:
                y = normalized.Y;
                w = normalized.X - x;
                h = bottom - y;
                break;
            case ResizeHandle.BottomLeft:
                x = normalized.X;
                w = right - x;
                h = normalized.Y - y;
                break;
            case ResizeHandle.BottomRight:
                w = normalized.X - x;
                h = normalized.Y - y;
                break;
        }

        if (w < 0)
        {
            x += w;
            w = Math.Abs(w);
        }

        if (h < 0)
        {
            y += h;
            h = Math.Abs(h);
        }

        return new NormalizedRect(x, y, w, h);
    }

    private static ResizeHandle HitTestHandle(Rect rect, Point point)
    {
        const double size = 8;
        var handles = new Dictionary<ResizeHandle, Rect>
        {
            [ResizeHandle.TopLeft] = new(rect.X - size / 2, rect.Y - size / 2, size, size),
            [ResizeHandle.TopRight] = new(rect.Right - size / 2, rect.Y - size / 2, size, size),
            [ResizeHandle.BottomLeft] = new(rect.X - size / 2, rect.Bottom - size / 2, size, size),
            [ResizeHandle.BottomRight] = new(rect.Right - size / 2, rect.Bottom - size / 2, size, size)
        };

        foreach (var (handle, handleRect) in handles)
        {
            if (handleRect.Contains(point))
                return handle;
        }

        return ResizeHandle.None;
    }
}

public sealed class RegionBoundsEventArgs : EventArgs
{
    public RegionBoundsEventArgs(Guid regionId, NormalizedRect bounds)
    {
        RegionId = regionId;
        Bounds = bounds;
    }

    public Guid RegionId { get; }
    public NormalizedRect Bounds { get; }
}
