namespace A2lEditor.Core.Model;

public sealed record A2lAxisDescr(
    string Attribute,
    string InputQuantity,
    string Conversion,
    ulong MaxNumberOfAxisPoints,
    string LowerLimit,
    string UpperLimit,
    LineRange SourceLines);