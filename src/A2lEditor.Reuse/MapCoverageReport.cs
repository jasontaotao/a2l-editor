namespace A2lEditor.Reuse;

public sealed record MapCoverageReport(
    int TotalMapSymbols,
    int MatchedInA2l,
    int MissingFromA2l,
    IReadOnlyList<string> ExtraInA2l);
