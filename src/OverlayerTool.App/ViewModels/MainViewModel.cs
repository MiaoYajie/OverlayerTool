using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OverlayerTool.App.Helpers;
using OverlayerTool.Core.Models;
using OverlayerTool.Core.Services;
using System.Collections.ObjectModel;

namespace OverlayerTool.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProjectService _projectService = new();
    private readonly ImageRenderer _imageRenderer = new();
    private CancellationTokenSource? _generateCts;

    [ObservableProperty]
    private Bitmap? _baseImage;

    [ObservableProperty]
    private Bitmap? _referenceImage;

    [ObservableProperty]
    private float _referenceOpacity = 0.5f;

    [ObservableProperty]
    private bool _showReferenceImage = true;

    [ObservableProperty]
    private RegionItemViewModel? _selectedRegion;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _matchStatusText = "未导入表格";

    [ObservableProperty]
    private bool _isMatchValid;

    [ObservableProperty]
    private double _generateProgress;

    [ObservableProperty]
    private bool _isGenerating;

    [ObservableProperty]
    private string? _outputDirectory;

    [ObservableProperty]
    private bool _isPreviewMode;

    [ObservableProperty]
    private Bitmap? _previewImage;

    [ObservableProperty]
    private int? _previewRowIndex;

    [ObservableProperty]
    private string _previewTitle = string.Empty;

    [ObservableProperty]
    private PreviewCompareMode _previewCompareMode = PreviewCompareMode.PreviewOnly;

    [ObservableProperty]
    private double _previewOverlayOpacity = 0.5;

    [ObservableProperty]
    private bool _isPreviewLoading;

    [ObservableProperty]
    private double _canvasZoom = 1.0;

    [ObservableProperty]
    private int _outputQuality = 100;

    [ObservableProperty]
    private OutputFormatOption? _selectedOutputFormatOption;

    public const double CanvasMinZoom = 0.25;
    public const double CanvasMaxZoom = 5.0;

    public IReadOnlyList<OutputFormatOption> OutputFormatOptions { get; } =
    [
        new(OutputImageFormat.Png, "PNG（无损）"),
        new(OutputImageFormat.Jpeg, "JPEG（可压缩）")
    ];

    public IReadOnlyList<PreviewCompareOption> PreviewCompareOptions { get; } =
    [
        new(PreviewCompareMode.PreviewOnly, "仅预览"),
        new(PreviewCompareMode.SideBySideWithBase, "与底板并排"),
        new(PreviewCompareMode.SideBySideWithReference, "与示例图并排"),
        new(PreviewCompareMode.OverlayBase, "叠加底板对比"),
        new(PreviewCompareMode.OverlayReference, "叠加示例图对比")
    ];

    [ObservableProperty]
    private PreviewCompareOption? _selectedPreviewCompareOption;

    public ObservableCollection<RegionItemViewModel> Regions { get; } = [];

    public ObservableCollection<TableRowViewModel> TableRows { get; } = [];

    public ObservableCollection<string> TableHeaders { get; } = [];

    public ObservableCollection<FontOption> FontOptions { get; } = [];

    public ObservableCollection<CustomFontItemViewModel> CustomFonts { get; } = [];

    [ObservableProperty]
    private CustomFontItemViewModel? _selectedCustomFont;

    [ObservableProperty]
    private FontOption? _selectedRegionFontOption;

    private static readonly string[] SystemFonts =
    [
        "Arial",
        "Helvetica",
        "Times New Roman",
        "Courier New",
        "Microsoft YaHei",
        "SimHei",
        "PingFang SC",
        "Segoe UI"
    ];

    public IReadOnlyList<HorizontalTextAlignment> HorizontalAlignOptions { get; } =
        Enum.GetValues<HorizontalTextAlignment>();

    public IReadOnlyList<VerticalTextAlignment> VerticalAlignOptions { get; } =
        Enum.GetValues<VerticalTextAlignment>();

    public ProjectTemplate Template => _projectService.Template;

    public TableData CurrentTable { get; private set; } = TableData.Empty;

    public Window? OwnerWindow { get; set; }

    partial void OnPreviewCompareModeChanged(PreviewCompareMode value)
    {
        OnPropertyChanged(nameof(ShowPreviewOverlayOpacity));
    }

    public bool ShowPreviewOverlayOpacity =>
        PreviewCompareMode is PreviewCompareMode.OverlayBase or PreviewCompareMode.OverlayReference;

    public string OutputQualityHint =>
        SelectedOutputFormatOption?.Format == OutputImageFormat.Jpeg
            ? "数值越高画质越好、文件越大"
            : "PNG 无损；此项为压缩级别";

    partial void OnOutputQualityChanged(int value)
    {
        _projectService.Template.OutputQuality = Math.Clamp(value, 1, 100);
        _projectService.MarkDirty();
    }

    partial void OnSelectedOutputFormatOptionChanged(OutputFormatOption? value)
    {
        if (value is null)
            return;

        _projectService.Template.OutputFormat = value.Format;
        _projectService.MarkDirty();
        OnPropertyChanged(nameof(OutputQualityHint));
    }

    partial void OnSelectedRegionChanged(RegionItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedRegion));
        UpdateSelectedRegionFontOption();
    }

    partial void OnSelectedRegionFontOptionChanged(FontOption? value)
    {
        if (value is null || SelectedRegion is null)
            return;

        if (value.Matches(SelectedRegion.Region))
            return;

        FontService.ApplyFontOption(SelectedRegion.Region, value);
        OnRegionPropertyChanged();
    }

    public bool HasSelectedRegion => SelectedRegion is not null;

    public MainViewModel()
    {
        RebuildFontOptions();
        SelectedPreviewCompareOption = PreviewCompareOptions[0];
        SelectedOutputFormatOption = OutputFormatOptions[0];
    }

    partial void OnSelectedPreviewCompareOptionChanged(PreviewCompareOption? value)
    {
        if (value is not null)
            PreviewCompareMode = value.Mode;
    }

    partial void OnReferenceOpacityChanged(float value)
    {
        _projectService.Template.ReferenceOpacity = value;
        _projectService.MarkDirty();
    }

    partial void OnShowReferenceImageChanged(bool value)
    {
        _projectService.Template.ShowReferenceImage = value;
        _projectService.MarkDirty();
    }

    [RelayCommand]
    private void NewProject()
    {
        _projectService.NewProject();
        Regions.Clear();
        SelectedRegion = null;
        BaseImage = null;
        ReferenceImage = null;
        ExitPreviewMode();
        ClearTable();
        ReloadCustomFonts();
        RebuildFontOptions();
        ReloadOutputSettings();
        StatusMessage = "已新建项目";
    }

    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickProjectFolderAsync(OwnerWindow, "打开项目");
        if (string.IsNullOrEmpty(path))
            return;

        await _projectService.LoadAsync(path);
        ReloadFromTemplate();
        StatusMessage = $"已打开项目: {path}";
    }

    [RelayCommand]
    private async Task SaveProjectAsync()
    {
        if (OwnerWindow is null)
            return;

        var projectPath = await DialogHelper.PickSaveFolderAsync(OwnerWindow, "选择项目保存目录");
        if (string.IsNullOrEmpty(projectPath))
            return;

        Directory.CreateDirectory(projectPath);
        _projectService.SetProjectPath(projectPath);

        SyncRegionsToTemplate();
        await _projectService.SaveAsync(projectPath);
        StatusMessage = $"项目已保存: {projectPath}";
    }

    [RelayCommand]
    private void ZoomInCanvas()
    {
        CanvasZoom = Math.Min(CanvasMaxZoom, CanvasZoom * 1.25);
    }

    [RelayCommand]
    private void ZoomOutCanvas()
    {
        CanvasZoom = Math.Max(CanvasMinZoom, CanvasZoom / 1.25);
    }

    public event EventHandler? CanvasResetViewRequested;

    [RelayCommand]
    private void ResetCanvasView()
    {
        CanvasZoom = 1.0;
        CanvasResetViewRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ImportBaseImageAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickOpenFileAsync(OwnerWindow, "导入底板图片", "*.png", "*.jpg", "*.jpeg", "*.webp");
        if (string.IsNullOrEmpty(path))
            return;

        await _projectService.SetBaseImageAsync(path);
        BaseImage = new Bitmap(path);
        SyncRegionsToTemplate();
        StatusMessage = "底板图片已导入";
    }

    [RelayCommand]
    private async Task ImportFontAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickOpenFileAsync(
            OwnerWindow,
            "上传字体文件",
            "*.ttf",
            "*.otf",
            "*.ttc",
            "*.woff",
            "*.woff2");

        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            var font = await _projectService.AddCustomFontAsync(path);
            CustomFonts.Add(new CustomFontItemViewModel(font));
            RebuildFontOptions();
            StatusMessage = $"已上传字体: {font.DisplayName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"上传字体失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DeleteSelectedFont()
    {
        if (SelectedCustomFont is null)
            return;

        var fontId = SelectedCustomFont.Id;
        _projectService.RemoveCustomFont(fontId);
        CustomFonts.Remove(SelectedCustomFont);
        SelectedCustomFont = CustomFonts.FirstOrDefault();

        foreach (var region in Regions.Where(r => r.Region.CustomFontId == fontId))
        {
            region.Region.CustomFontId = null;
            region.Region.FontFamily = "Arial";
        }

        RebuildFontOptions();
        UpdateSelectedRegionFontOption();
        StatusMessage = "已删除自定义字体";
    }

    [RelayCommand]
    private async Task PreviewRowAsync(int rowIndex)
    {
        if (BaseImage is null || string.IsNullOrEmpty(_projectService.GetBaseImagePath()))
        {
            StatusMessage = "请先导入底板图片";
            return;
        }

        if (rowIndex < 0 || rowIndex >= CurrentTable.Rows.Count)
            return;

        SyncRegionsToTemplate();
        IsPreviewLoading = true;
        StatusMessage = $"正在生成第 {rowIndex + 1} 行预览...";

        try
        {
            using var rendered = await _imageRenderer.RenderRowAsync(
                _projectService.GetBaseImagePath()!,
                _projectService.Template,
                _projectService.CurrentProjectPath,
                CurrentTable,
                rowIndex);

            PreviewImage?.Dispose();
            PreviewImage = BitmapHelper.FromSkBitmap(rendered);
            PreviewRowIndex = rowIndex;
            PreviewTitle = $"预览 - 第 {rowIndex + 1} 行";
            IsPreviewMode = true;
            StatusMessage = $"已加载第 {rowIndex + 1} 行预览";
        }
        catch (Exception ex)
        {
            StatusMessage = $"预览失败: {ex.Message}";
        }
        finally
        {
            IsPreviewLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshPreviewAsync()
    {
        if (PreviewRowIndex is int rowIndex)
            await PreviewRowAsync(rowIndex);
    }

    [RelayCommand]
    private void ExitPreviewMode()
    {
        IsPreviewMode = false;
        PreviewRowIndex = null;
        PreviewTitle = string.Empty;
        PreviewImage?.Dispose();
        PreviewImage = null;
    }

    [RelayCommand]
    private async Task ImportReferenceImageAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickOpenFileAsync(OwnerWindow, "导入示例图片", "*.png", "*.jpg", "*.jpeg", "*.webp");
        if (string.IsNullOrEmpty(path))
            return;

        await _projectService.SetReferenceImageAsync(path);
        ReferenceImage = new Bitmap(path);
        StatusMessage = "示例图片已导入";
    }

    [RelayCommand]
    private void AddRegion()
    {
        var bounds = new NormalizedRect(0.1, 0.1, 0.3, 0.08);
        var region = new TextRegion
        {
            Name = $"区域{Regions.Count + 1}",
            Bounds = bounds,
            FontSize = ComputeDefaultFontSize(bounds)
        };
        AddRegionInternal(region);
        _projectService.MarkDirty();
    }

    [RelayCommand]
    private void DeleteSelectedRegion()
    {
        if (SelectedRegion is null)
            return;

        Regions.Remove(SelectedRegion);
        SelectedRegion = Regions.FirstOrDefault();
        _projectService.MarkDirty();
    }

    [RelayCommand]
    private async Task PasteTableAsync()
    {
        if (OwnerWindow is null)
            return;

        var dialog = new Views.PasteTableWindow();
        await dialog.ShowDialog(OwnerWindow);
        if (!dialog.IsConfirmed || string.IsNullOrWhiteSpace(dialog.TableText))
            return;

        LoadTable(TableParser.ParseFromText(dialog.TableText));
    }

    [RelayCommand]
    private async Task ImportCsvAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickOpenFileAsync(OwnerWindow, "导入 CSV", "*.csv", "*.txt", "*.tsv");
        if (string.IsNullOrEmpty(path))
            return;

        var table = await TableParser.ParseFromCsvFileAsync(path);
        LoadTable(table);
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickOpenFileAsync(OwnerWindow, "导入 Excel", "*.xlsx");
        if (string.IsNullOrEmpty(path))
            return;

        var table = await TableParser.ParseFromExcelFileAsync(path);
        LoadTable(table);
    }

    [RelayCommand]
    private async Task PickOutputDirectoryAsync()
    {
        if (OwnerWindow is null)
            return;

        var path = await DialogHelper.PickSaveFolderAsync(OwnerWindow, "选择输出目录");
        if (!string.IsNullOrEmpty(path))
            OutputDirectory = path;
    }

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (BaseImage is null || string.IsNullOrEmpty(_projectService.GetBaseImagePath()))
        {
            StatusMessage = "请先导入底板图片";
            return;
        }

        if (CurrentTable.Rows.Count == 0)
        {
            StatusMessage = "请先导入表格数据";
            return;
        }

        SyncRegionsToTemplate();
        var validation = HeaderMatcher.Validate(CurrentTable.Headers, _projectService.Template.Regions);
        UpdateMatchStatus(validation);

        if (!validation.IsValid)
        {
            StatusMessage = "表头与区域名称未完全匹配，请修正后再生成";
            return;
        }

        if (string.IsNullOrEmpty(OutputDirectory))
        {
            StatusMessage = "请先选择输出目录";
            return;
        }

        _generateCts = new CancellationTokenSource();
        IsGenerating = true;
        GenerateProgress = 0;
        StatusMessage = "正在生成图片...";

        try
        {
            var progress = new Progress<GenerateProgress>(p =>
            {
                GenerateProgress = p.Total == 0 ? 0 : (double)p.Current / p.Total * 100;
                StatusMessage = $"正在生成 ({p.Current}/{p.Total}): {p.CurrentFileName}";
            });

            var files = await _imageRenderer.GenerateBatchAsync(
                _projectService.Template,
                _projectService.GetBaseImagePath()!,
                _projectService.CurrentProjectPath,
                CurrentTable,
                OutputDirectory,
                progress,
                _generateCts.Token);

            StatusMessage = $"生成完成，共 {files.Count} 张图片";
            PlatformHelper.OpenFolder(OutputDirectory);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "生成已取消";
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
            _generateCts?.Dispose();
            _generateCts = null;
        }
    }

    [RelayCommand]
    private void CancelGenerate()
    {
        _generateCts?.Cancel();
    }

    public void OnRegionDrawn(NormalizedRect bounds)
    {
        var region = new TextRegion
        {
            Name = $"区域{Regions.Count + 1}",
            Bounds = bounds,
            FontSize = ComputeDefaultFontSize(bounds)
        };
        AddRegionInternal(region);
        _projectService.MarkDirty();
    }

    public void OnRegionMoved(Guid regionId, NormalizedRect bounds)
    {
        var region = Regions.FirstOrDefault(r => r.Id == regionId);
        if (region is null)
            return;

        region.Region.Bounds = bounds;
        _projectService.MarkDirty();
    }

    public void OnRegionSelected(Guid regionId)
    {
        SelectedRegion = Regions.FirstOrDefault(r => r.Id == regionId);
    }

    public void OnRegionPropertyChanged()
    {
        _projectService.MarkDirty();
        UpdateMatchStatus(HeaderMatcher.Validate(CurrentTable.Headers, Regions.Select(r => r.Region)));
    }

    private void AddRegionInternal(TextRegion region)
    {
        var vm = new RegionItemViewModel(region);
        vm.PropertyChanged += (_, _) => OnRegionPropertyChanged();
        Regions.Add(vm);
        SelectedRegion = vm;
        SyncRegionsToTemplate();
        UpdateMatchStatus(HeaderMatcher.Validate(CurrentTable.Headers, Regions.Select(r => r.Region)));
    }

    private void LoadTable(TableData table)
    {
        CurrentTable = table;
        TableHeaders.Clear();
        TableRows.Clear();

        foreach (var header in table.Headers)
            TableHeaders.Add(header);

        for (var i = 0; i < table.Rows.Count; i++)
            TableRows.Add(new TableRowViewModel(i + 1, i, table.Rows[i]));

        OnPropertyChanged(nameof(TableHeaders));

        UpdateMatchStatus(HeaderMatcher.Validate(table.Headers, Regions.Select(r => r.Region)));
        StatusMessage = $"已导入 {table.Rows.Count} 行数据";
    }

    private void ClearTable()
    {
        ExitPreviewMode();
        CurrentTable = TableData.Empty;
        TableHeaders.Clear();
        TableRows.Clear();
        MatchStatusText = "未导入表格";
        IsMatchValid = false;
    }

    private void UpdateMatchStatus(MatchValidationResult validation)
    {
        IsMatchValid = validation.IsValid;
        var matched = validation.MatchedHeaders.Count;
        var totalRegions = Regions.Count;
        MatchStatusText = validation.IsValid
            ? $"匹配状态: {matched}/{totalRegions} ✓"
            : $"匹配状态: {matched}/{totalRegions}（未匹配表头: {string.Join(", ", validation.UnmatchedHeaders)}；未匹配区域: {string.Join(", ", validation.UnmatchedRegions)}）";
    }

    private void ReloadFromTemplate()
    {
        Regions.Clear();
        foreach (var region in _projectService.Template.Regions)
        {
            var vm = new RegionItemViewModel(region.Clone());
            vm.PropertyChanged += (_, _) => OnRegionPropertyChanged();
            Regions.Add(vm);
        }

        SelectedRegion = Regions.FirstOrDefault();
        ReferenceOpacity = _projectService.Template.ReferenceOpacity;
        ShowReferenceImage = _projectService.Template.ShowReferenceImage;

        var basePath = _projectService.GetBaseImagePath();
        BaseImage = basePath is not null && File.Exists(basePath) ? new Bitmap(basePath) : null;

        var refPath = _projectService.GetReferenceImagePath();
        ReferenceImage = refPath is not null && File.Exists(refPath) ? new Bitmap(refPath) : null;

        ReloadCustomFonts();
        RebuildFontOptions();
        ReloadOutputSettings();
        UpdateSelectedRegionFontOption();
        UpdateMatchStatus(HeaderMatcher.Validate(CurrentTable.Headers, Regions.Select(r => r.Region)));
    }

    private void ReloadCustomFonts()
    {
        CustomFonts.Clear();
        foreach (var font in _projectService.Template.CustomFonts)
            CustomFonts.Add(new CustomFontItemViewModel(font));
    }

    private void RebuildFontOptions()
    {
        FontOptions.Clear();
        foreach (var option in FontService.BuildFontOptions(SystemFonts, _projectService.Template.CustomFonts))
            FontOptions.Add(option);
    }

    private void UpdateSelectedRegionFontOption()
    {
        SelectedRegionFontOption = SelectedRegion is null
            ? null
            : FontService.FindMatchingOption(SelectedRegion.Region, FontOptions);
    }

    private void ReloadOutputSettings()
    {
        OutputQuality = _projectService.Template.OutputQuality;
        SelectedOutputFormatOption = OutputFormatOptions.FirstOrDefault(o => o.Format == _projectService.Template.OutputFormat)
            ?? OutputFormatOptions[0];
    }

    private void SyncRegionsToTemplate()
    {
        _projectService.Template.Regions = Regions.Select(r => r.Region.Clone()).ToList();
        _projectService.Template.OutputFormat = SelectedOutputFormatOption?.Format ?? OutputImageFormat.Png;
        _projectService.Template.OutputQuality = Math.Clamp(OutputQuality, 1, 100);
    }

    private float ComputeDefaultFontSize(NormalizedRect bounds) =>
        TextRegion.ComputeDefaultFontSize(bounds, BaseImage?.PixelSize.Height ?? 1000);
}
