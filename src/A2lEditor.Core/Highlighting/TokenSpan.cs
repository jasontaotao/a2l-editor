namespace A2lEditor.Core.Highlighting;

public sealed record TokenSpan(int StartOffset, int Length, TokenCategory Category);
