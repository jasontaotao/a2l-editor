namespace A2lEditor.Core.Model;

public sealed record A2lMeasurement(
    string Name,
    string LongIdentifier,
    A2lDataType DataType,
    string CompuMethod,
    string Resolution,
    string Accuracy,
    string LowerLimit,
    string UpperLimit,
    ulong EcuAddress,
    LineRange SourceLines);
