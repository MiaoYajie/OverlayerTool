using OverlayerTool.Core.Models;

namespace OverlayerTool.App.ViewModels;

public record PreviewCompareOption(PreviewCompareMode Mode, string Label)
{
    public override string ToString() => Label;
}
