namespace A2lEditor.Reuse;

public interface IMapSymbolTableAdapter
{
    IReadOnlyList<MapSymbol> LoadSymbols(string mapPath);
}
