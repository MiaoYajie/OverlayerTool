namespace OverlayerTool.Core.Models;

public record MatchValidationResult(
    IReadOnlyList<string> MatchedHeaders,
    IReadOnlyList<string> UnmatchedHeaders,
    IReadOnlyList<string> UnmatchedRegions)
{
    public bool IsValid => UnmatchedHeaders.Count == 0 && UnmatchedRegions.Count == 0;
}
