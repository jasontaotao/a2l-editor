namespace A2lEditor.Core.Parsing;

public enum ErrorSeverity
{
    Warning,
    Error,
    Fatal
}

public sealed record ParseError(
    int Line,
    int Column,
    string Message,
    ErrorSeverity Severity);