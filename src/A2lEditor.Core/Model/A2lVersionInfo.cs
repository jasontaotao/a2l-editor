namespace A2lEditor.Core.Model;

public sealed record A2lVersionInfo(
    string VersionNo,
    DateTime Date,
    string Vendor,
    string Description,
    LineRange SourceLines);