namespace A2lEditor.Core.Model;

/// <summary>
/// ASAP2 CHARACTERISTIC block.
/// EB tresos v1.71 完整语法：
///   /begin CHARACTERISTIC Name LongIdentifier Type RecordLayout EcuAddress LowerLimit UpperLimit [MaxDiff] [Conversion]
/// Type: VALUE | CURVE | MAP | CUBOID | CUBE_4 | CUBE_5 | VAL_BLK | ASCII
/// </summary>
public sealed record A2lCharacteristic(
    string Name,
    string LongIdentifier,
    string Type,
    string RecordLayout,
    ulong EcuAddress,
    string LowerLimit,
    string UpperLimit,
    string? MaxDiff,
    string? Conversion,
    LineRange SourceLines);
