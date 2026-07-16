namespace A2lEditor.Core.Model;

public sealed record A2lModCommon(
    string Comment,
    A2lByteOrder ByteOrder,
    LineRange SourceLines);