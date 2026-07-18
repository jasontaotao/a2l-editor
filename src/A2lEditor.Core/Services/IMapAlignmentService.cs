using A2lEditor.Core.Model;
using A2lEditor.Reuse;

namespace A2lEditor.Core.Services;

public sealed record MapApplyOptions(
    bool DryRun,
    bool Backup,
    string? OutputPath);

public sealed record MapApplyResult(
    int UpdatedCount,
    int SkippedCount,
    IReadOnlyList<string> SkippedReasons,
    A2lDocument? NewDocument);

public interface IMapAlignmentService
{
    IReadOnlyList<MapSymbol> LoadMapSymbols(string mapPath);
    MapCoverageReport ValidateCoverage(IReadOnlyList<MapSymbol> symbols, A2lDocument doc);
    MapApplyResult ApplyAddresses(A2lDocument doc, IReadOnlyList<MapSymbol> symbols, MapApplyOptions options);
}
