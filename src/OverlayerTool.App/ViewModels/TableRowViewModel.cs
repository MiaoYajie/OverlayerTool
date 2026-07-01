using CommunityToolkit.Mvvm.ComponentModel;

namespace OverlayerTool.App.ViewModels;

public class TableRowViewModel : ObservableObject
{
    public TableRowViewModel(int rowIndex, int dataRowIndex, IReadOnlyList<string> values)
    {
        RowIndex = rowIndex;
        DataRowIndex = dataRowIndex;
        Values = values.ToList();
    }

    public int RowIndex { get; }

    public int DataRowIndex { get; }

    public List<string> Values { get; }
}
