namespace A2lEditor.Core.Model;

public sealed record A2lAxisPts(
    string Name,
    string LongIdentifier,
    string RecordLayout,
    ulong EcuAddress,
    string InputQuantity,
    string CompuMethod,
    int NumberOfAxisPts,
    string LowerLimit,
    string UpperLimit,
    LineRange SourceLines);
