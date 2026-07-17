namespace A2lEditor.Core.Model;

public sealed record A2lModCommon(
    string Comment,
    A2lByteOrder ByteOrder,
    ulong? DataSize,
    A2lByteOrder? AlignmentByteOrder,
    LineRange SourceLines);