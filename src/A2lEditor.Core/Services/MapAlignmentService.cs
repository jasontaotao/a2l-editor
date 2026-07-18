using A2lEditor.Core.Model;
using A2lEditor.Reuse;

namespace A2lEditor.Core.Services;

public sealed class MapAlignmentService : IMapAlignmentService
{
    private readonly IMapSymbolTableAdapter _adapter;

    public MapAlignmentService(IMapSymbolTableAdapter adapter)
    {
        _adapter = adapter;
    }

    public IReadOnlyList<MapSymbol> LoadMapSymbols(string mapPath) =>
        _adapter.LoadSymbols(mapPath);

    public MapCoverageReport ValidateCoverage(IReadOnlyList<MapSymbol> symbols, A2lDocument doc)
    {
        var a2lNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in doc.Modules)
            foreach (var meas in m.Measurements)
                a2lNames.Add(meas.Name);

        var mapNames = new HashSet<string>(symbols.Select(s => s.Name), StringComparer.Ordinal);
        int matched = symbols.Count(s => a2lNames.Contains(s.Name));
        int missing = symbols.Count(s => !a2lNames.Contains(s.Name));
        var extra = a2lNames.Where(n => !mapNames.Contains(n)).ToList();

        return new MapCoverageReport(symbols.Count, matched, missing, extra);
    }

    public MapApplyResult ApplyAddresses(A2lDocument doc, IReadOnlyList<MapSymbol> symbols, MapApplyOptions options)
    {
        var symbolByName = symbols.ToDictionary(s => s.Name, StringComparer.Ordinal);
        var skipped = new List<string>();
        var newModules = new List<A2lModule>(doc.Modules.Count);
        int updated = 0;

        foreach (var module in doc.Modules)
        {
            var newMeasurements = new List<A2lMeasurement>(module.Measurements.Count);
            foreach (var meas in module.Measurements)
            {
                if (symbolByName.TryGetValue(meas.Name, out var sym))
                {
                    newMeasurements.Add(meas with { EcuAddress = sym.Address });
                    updated++;
                }
                else
                {
                    skipped.Add($"MEASUREMENT {meas.Name} not in MAP");
                    newMeasurements.Add(meas);
                }
            }
            newModules.Add(module with { Measurements = newMeasurements });
        }

        var newDoc = options.DryRun
            ? null
            // MAP2 fix: DROP RawText when returning the updated document. The writer
            // short-circuits to RawText verbatim when it is non-empty
            // (A2lDocumentWriter L18-22), so preserving RawText via "doc with { ... }"
            // would emit the ORIGINAL bytes and silently discard every ECU_ADDRESS
            // edit — the map-update --no-dry-run path wrote files back unchanged.
            // Forcing RawText="" redirects the writer to the semantic emit path.
            : doc with { Modules = newModules, RawText = "" };

        return new MapApplyResult(updated, skipped.Count, skipped, newDoc);
    }
}
