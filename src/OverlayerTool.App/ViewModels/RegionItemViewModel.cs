using CommunityToolkit.Mvvm.ComponentModel;
using OverlayerTool.Core.Models;

namespace OverlayerTool.App.ViewModels;

public partial class RegionItemViewModel : ObservableObject
{
    public RegionItemViewModel(TextRegion region)
    {
        Region = region;
    }

    public TextRegion Region { get; }

    public Guid Id => Region.Id;

    public string Name
    {
        get => Region.Name;
        set
        {
            if (Region.Name != value)
            {
                Region.Name = value;
                OnPropertyChanged();
            }
        }
    }

    public string FontFamily
    {
        get => Region.FontFamily;
        set
        {
            if (Region.FontFamily != value)
            {
                Region.FontFamily = value;
                OnPropertyChanged();
            }
        }
    }

    public float FontSize
    {
        get => Region.FontSize;
        set
        {
            if (Math.Abs(Region.FontSize - value) > 0.01f)
            {
                Region.FontSize = value;
                OnPropertyChanged();
            }
        }
    }

    public RegionFontWeight FontWeight
    {
        get => Region.FontWeight;
        set
        {
            if (Region.FontWeight != value)
            {
                Region.FontWeight = value;
                OnPropertyChanged();
            }
        }
    }

    public string Color
    {
        get => Region.Color;
        set
        {
            if (Region.Color != value)
            {
                Region.Color = value;
                OnPropertyChanged();
            }
        }
    }

    public float RotationDegrees
    {
        get => Region.RotationDegrees;
        set
        {
            if (Math.Abs(Region.RotationDegrees - value) > 0.01f)
            {
                Region.RotationDegrees = value;
                OnPropertyChanged();
            }
        }
    }

    public HorizontalTextAlignment HorizontalAlign
    {
        get => Region.HorizontalAlign;
        set
        {
            if (Region.HorizontalAlign != value)
            {
                Region.HorizontalAlign = value;
                OnPropertyChanged();
            }
        }
    }

    public VerticalTextAlignment VerticalAlign
    {
        get => Region.VerticalAlign;
        set
        {
            if (Region.VerticalAlign != value)
            {
                Region.VerticalAlign = value;
                OnPropertyChanged();
            }
        }
    }

    public static IReadOnlyList<FontWeightOption> FontWeightOptions { get; } =
    [
        new(RegionFontWeight.Regular, "常规"),
        new(RegionFontWeight.Bold, "加粗")
    ];

    public FontWeightOption SelectedFontWeightOption
    {
        get => FontWeightOptions.First(o => o.Weight == Region.FontWeight);
        set
        {
            if (value is null || Region.FontWeight == value.Weight)
                return;

            Region.FontWeight = value.Weight;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFontWeightOption));
        }
    }
}
