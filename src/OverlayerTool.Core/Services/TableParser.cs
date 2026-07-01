using ClosedXML.Excel;
using OverlayerTool.Core.Models;

namespace OverlayerTool.Core.Services;

public static class TableParser
{
    public static TableData ParseFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return TableData.Empty;

        var lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)
            return TableData.Empty;

        var delimiter = lines[0].Contains('\t') ? '\t' : DetectDelimiter(lines[0]);
        var rows = lines.Select(line => ParseLine(line, delimiter).ToList()).ToList();
        if (rows.Count == 0)
            return TableData.Empty;

        var headers = rows[0];
        var dataRows = rows.Skip(1).Select(r => PadRow(r, headers.Count)).ToList();

        return new TableData
        {
            Headers = headers,
            Rows = dataRows
        };
    }

    public static async Task<TableData> ParseFromCsvFileAsync(string path, CancellationToken cancellationToken = default)
    {
        var text = await File.ReadAllTextAsync(path, cancellationToken);
        return ParseFromText(text);
    }

    public static Task<TableData> ParseFromExcelFileAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var workbook = new XLWorkbook(path);
        var worksheet = workbook.Worksheets.First();
        var usedRange = worksheet.RangeUsed();
        if (usedRange is null)
            return Task.FromResult(TableData.Empty);

        var allRows = new List<List<string>>();
        foreach (var row in usedRange.Rows())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = row.Cells().Select(c => c.GetFormattedString().Trim()).ToList();
            if (cells.Any(c => !string.IsNullOrEmpty(c)))
                allRows.Add(cells);
        }

        if (allRows.Count == 0)
            return Task.FromResult(TableData.Empty);

        var headers = allRows[0];
        var dataRows = allRows.Skip(1).Select(r => PadRow(r, headers.Count)).ToList();

        return Task.FromResult(new TableData
        {
            Headers = headers,
            Rows = dataRows
        });
    }

    private static char DetectDelimiter(string line)
    {
        var commaCount = line.Count(c => c == ',');
        var semicolonCount = line.Count(c => c == ';');
        return semicolonCount > commaCount ? ';' : ',';
    }

    private static IEnumerable<string> ParseLine(string line, char delimiter)
    {
        if (delimiter == '\t')
            return line.Split('\t').Select(c => c.Trim());

        return ParseCsvLine(line, delimiter);
    }

    private static List<string> ParseCsvLine(string line, char delimiter)
    {
        var result = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == delimiter && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        result.Add(current.ToString().Trim());
        return result;
    }

    private static IReadOnlyList<string> PadRow(IReadOnlyList<string> row, int columnCount)
    {
        if (row.Count >= columnCount)
            return row.Take(columnCount).ToList();

        var padded = row.ToList();
        while (padded.Count < columnCount)
            padded.Add(string.Empty);

        return padded;
    }
}
