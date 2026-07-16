namespace A2lEditor.Core.Parsing;

public sealed record ParseResult<T>(
    T? Value,
    IReadOnlyList<ParseError> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public bool HasFatalErrors =>
        Errors.Any(e => e.Severity == ErrorSeverity.Fatal);

    public static ParseResult<T> Success(T value) =>
        new(value, Array.Empty<ParseError>());

    public static ParseResult<T> Partial(T? value, IReadOnlyList<ParseError> errors) =>
        new(value, errors);

    public static ParseResult<T> Failure(IReadOnlyList<ParseError> errors) =>
        new(default, errors);
}