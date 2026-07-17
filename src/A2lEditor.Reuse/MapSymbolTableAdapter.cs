using A2L_UpdateProj;

namespace A2lEditor.Reuse;

public sealed class MapSymbolTableAdapter : IMapSymbolTableAdapter
{
    public IReadOnlyList<MapSymbol> LoadSymbols(string mapPath)
    {
        if (string.IsNullOrEmpty(mapPath))
            throw new ArgumentException("Map path must not be empty.", nameof(mapPath));

        // Let FileNotFoundException propagate (caller maps to CLI exit 2).
        A2L_UpdateProj.MapSymbolTable table;
        try
        {
            table = MapSymbolTable.FromFile(mapPath);
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (InvalidDataException ex)
        {
            // Legacy throws InvalidDataException for unknown MAP/ELF format.
            throw new InvalidMapException(
                $"Unrecognized MAP/ELF format: {mapPath}", ex);
        }
        catch (Exception ex)
        {
            // ELFSharp or any other legacy parse error.
            throw new InvalidMapException(
                $"Failed to parse MAP/ELF file: {mapPath}", ex);
        }

        var result = new List<MapSymbol>(table.Addr.Count);
        foreach (var kvp in table.Addr)
        {
            result.Add(new MapSymbol(kvp.Key, kvp.Value));
        }
        return result;
    }
}
