using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using OverlayerTool.App.Controls;
using OverlayerTool.App.ViewModels;
using OverlayerTool.Core.Models;
using System.Collections.Specialized;
using System.ComponentModel;

namespace OverlayerTool.App;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel { OwnerWindow = this };
        DataContext = _viewModel;

        HorizontalAlignCombo.ItemsSource = _viewModel.HorizontalAlignOptions;
        VerticalAlignCombo.ItemsSource = _viewModel.VerticalAlignOptions;
        FontWeightCombo.ItemsSource = RegionItemViewModel.FontWeightOptions;

        RegionCanvasControl.RegionDrawn += OnRegionDrawn;
        RegionCanvasControl.RegionMoved += OnRegionMoved;
        RegionCanvasControl.RegionSelected += OnRegionSelected;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.Regions.CollectionChanged += OnRegionsChanged;
        _viewModel.CanvasResetViewRequested += (_, _) => RegionCanvasControl.ResetView();
        UpdateTableColumns();
        RefreshCanvasRegions();
    }

    private void OnRegionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshCanvasRegions();
        if (_viewModel is not null)
            UpdateMatchOnRegionChange();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.TableHeaders))
            UpdateTableColumns();

        if (e.PropertyName is nameof(MainViewModel.SelectedRegion))
        {
            RefreshCanvasRegions();
            HookSelectedRegion();
        }
    }

    private void HookSelectedRegion()
    {
        if (_viewModel?.SelectedRegion is null)
            return;

        _viewModel.SelectedRegion.PropertyChanged -= OnSelectedRegionPropertyChanged;
        _viewModel.SelectedRegion.PropertyChanged += OnSelectedRegionPropertyChanged;
    }

    private void OnSelectedRegionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        _viewModel?.OnRegionPropertyChanged();
        RefreshCanvasRegions();
    }

    private void UpdateMatchOnRegionChange()
    {
        _viewModel?.OnRegionPropertyChanged();
    }

    private void RefreshCanvasRegions()
    {
        if (_viewModel is null)
            return;

        RegionCanvasControl.Regions = _viewModel.Regions.Select(r => r.Region).ToList();
        RegionCanvasControl.SelectedRegionId = _viewModel.SelectedRegion?.Id;
        RegionCanvasControl.InvalidateVisual();
    }

    private void UpdateTableColumns()
    {
        if (_viewModel is null)
            return;

        TableDataGrid.Columns.Clear();
        TableDataGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "#",
            Binding = new Binding("RowIndex"),
            Width = new DataGridLength(50)
        });

        for (var i = 0; i < _viewModel.TableHeaders.Count; i++)
        {
            var index = i;
            var header = _viewModel.TableHeaders[i];
            TableDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding($"Values[{index}]"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        var previewColumn = new DataGridTemplateColumn
        {
            Header = "预览",
            Width = new DataGridLength(72),
            CellTemplate = new FuncDataTemplate<TableRowViewModel>((row, _) =>
            {
                var button = new Button
                {
                    Content = "预览",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(8, 2)
                };
                button.Bind(Button.CommandProperty, new Binding(nameof(MainViewModel.PreviewRowCommand))
                {
                    Source = _viewModel
                });
                button.Bind(Button.CommandParameterProperty, new Binding(nameof(TableRowViewModel.DataRowIndex)));
                return button;
            }, supportsRecycling: true)
        };
        TableDataGrid.Columns.Add(previewColumn);
    }

    private void OnRegionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshCanvasRegions();
        HookSelectedRegion();
    }

    private void OnRegionDrawn(object? sender, RegionBoundsEventArgs e)
    {
        _viewModel?.OnRegionDrawn(e.Bounds);
        RefreshCanvasRegions();
    }

    private void OnRegionMoved(object? sender, RegionBoundsEventArgs e)
    {
        _viewModel?.OnRegionMoved(e.RegionId, e.Bounds);
        RefreshCanvasRegions();
    }

    private void OnRegionSelected(object? sender, Guid regionId)
    {
        _viewModel?.OnRegionSelected(regionId);
    }

    private async void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel is null)
            return;

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
        {
            await _viewModel.SaveProjectCommand.ExecuteAsync(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            _viewModel.DeleteSelectedRegionCommand.Execute(null);
            RefreshCanvasRegions();
            e.Handled = true;
        }
    }
}
