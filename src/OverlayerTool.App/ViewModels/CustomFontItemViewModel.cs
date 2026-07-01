using CommunityToolkit.Mvvm.ComponentModel;
using OverlayerTool.Core.Models;

namespace OverlayerTool.App.ViewModels;

public partial class CustomFontItemViewModel : ObservableObject
{
    public CustomFontItemViewModel(CustomFont font)
    {
        Font = font;
    }

    public CustomFont Font { get; }

    public Guid Id => Font.Id;

    public string DisplayName => Font.DisplayName;
}
