using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OverlayerTool.App.Views;

public partial class PasteTableWindow : Window
{
    public PasteTableWindow()
    {
        InitializeComponent();
    }

    public bool IsConfirmed { get; private set; }

    public string? TableText => TableTextBox.Text;

    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = true;
        Close();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
