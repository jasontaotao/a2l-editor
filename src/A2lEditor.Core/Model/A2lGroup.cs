namespace A2lEditor.Core.Model;

public sealed record A2lGroup(
    string Name,
    string LongIdentifier,
    bool IsRoot,
    IReadOnlyList<string> RefMeasurements,
    IReadOnlyList<string> RefCharacteristics,
    LineRange SourceLines);
