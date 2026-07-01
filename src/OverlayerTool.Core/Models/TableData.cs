namespace OverlayerTool.Core.Models;

public class TableData
{
    public IReadOnlyList<string> Headers { get; init; } = [];
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = [];

    public static TableData Empty { get; } = new();
}
