namespace A2lEditor.Core.Model;

public sealed record A2lUserRights(
    string UserId,
    string ReadAccess,
    string WriteAccess,
    string AccessMethod,
    LineRange SourceLines);