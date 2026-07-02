using OverlayerTool.Core.Models;

namespace OverlayerTool.App.ViewModels;

public record OutputFormatOption(OutputImageFormat Format, string Label)
{
    public override string ToString() => Label;
}
