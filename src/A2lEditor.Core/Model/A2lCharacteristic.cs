namespace A2lEditor.Core.Model;

public sealed record A2lCharacteristic(
    string Name,
    string LongIdentifier,
    string RecordLayout,
    ulong EcuAddress,
    string LowerLimit,
    string UpperLimit,
    LineRange SourceLines);
