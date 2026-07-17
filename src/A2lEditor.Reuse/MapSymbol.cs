namespace A2lEditor.Reuse;

// Size field deliberately omitted — legacy MapSymbolTable.Addr is
// Dictionary<string, ulong>; no size data is available (spec-audit 2026-07-17).
public sealed record MapSymbol(string Name, ulong Address);
