using OverlayerTool.Core.Models;

namespace OverlayerTool.Core.Services;

public static class HeaderMatcher
{
    public static int FindColumnIndex(IReadOnlyList<string> headers, string regionName)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i].Trim(), regionName.Trim(), StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    public static MatchValidationResult Validate(IReadOnlyList<string> headers, IEnumerable<TextRegion> regions)
    {
        var regionNames = regions.Select(r => r.Name.Trim()).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
        var headerSet = headers.Select(h => h.Trim()).Where(h => !string.IsNullOrWhiteSpace(h)).ToList();

        var matchedHeaders = new List<string>();
        var unmatchedHeaders = new List<string>();

        foreach (var header in headerSet)
        {
            if (regionNames.Any(r => string.Equals(r, header, StringComparison.OrdinalIgnoreCase)))
                matchedHeaders.Add(header);
            else
                unmatchedHeaders.Add(header);
        }

        var unmatchedRegions = regionNames
            .Where(r => !headerSet.Any(h => string.Equals(h, r, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return new MatchValidationResult(matchedHeaders, unmatchedHeaders, unmatchedRegions);
    }
}
