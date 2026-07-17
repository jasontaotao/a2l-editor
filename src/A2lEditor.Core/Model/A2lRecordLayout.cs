namespace A2lEditor.Core.Model;

public sealed record A2lRecordLayout(
    string Name,
    IReadOnlyList<RecordLayoutEntry> Entries,
    LineRange SourceLines);

public sealed record RecordLayoutEntry(
    string Keyword,
    int Position,
    string DataType,
    string IndexMode,
    string AddressingMode,
    ulong? IndexIncr,
    ulong? IndexDecr);
