namespace A2lEditor.Reuse;

public sealed class InvalidMapException : Exception
{
    public InvalidMapException(string message) : base(message) { }
    public InvalidMapException(string message, Exception inner) : base(message, inner) { }
}
