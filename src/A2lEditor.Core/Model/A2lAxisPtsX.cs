namespace A2lEditor.Core.Model;

public sealed record A2lAxisPtsX(
    string Name,
    string LongIdentifier,
    string RecordLayout,
    ulong EcuAddress,
    string InputQuantity,
    string CompuMethod,
    int MaxAxisPoints,
    string LowerLimit,
    string UpperLimit,
    LineRange SourceLines);
