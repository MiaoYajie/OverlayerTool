using OverlayerTool.Core.Models;

namespace OverlayerTool.App.ViewModels;

public record FontWeightOption(RegionFontWeight Weight, string Label)
{
    public override string ToString() => Label;
}
