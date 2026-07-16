using A2lEditor.Core.Parsing;

namespace A2lEditor.Core.Highlighting;

public static class TokenClassifier
{
    private static readonly HashSet<string> DataTypeSet = new(StringComparer.Ordinal)
    {
        "UBYTE", "SBYTE", "UWORD", "SWORD",
        "ULONG", "SLONG", "FLOAT32_IEEE", "FLOAT64_IEEE",
    };

    /// Re-tokenize text and emit spans sorted by StartOffset, non-overlapping.
    /// Pure function. On lexer exception (e.g., unterminated comment) returns partial spans.
    public static IReadOnlyList<TokenSpan> Classify(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<TokenSpan>();

        var spans = new List<TokenSpan>();

        // 1. Emit Comment spans (Lexer skips them, so we scan ourselves).
        EmitCommentSpans(text, spans);

        // 2. Emit token spans from Lexer. Catch exception → return partial.
        try
        {
            var tokens = new Asap131Lexer(text).Tokenize();
            int cursor = 0; // absolute offset into text
            foreach (var token in tokens)
            {
                if (token.Kind == TokenKind.Eof) break;
                int absStart = FindOffset(text, cursor, token);
                if (absStart < 0) break; // safety

                // For string literals, the Lexer drops the surrounding quotes;
                // back up to include them so the span covers the entire A2L literal.
                int length = token.Text.Length;
                if (token.Kind == TokenKind.StringLiteral)
                {
                    absStart = Math.Max(0, absStart - 1);
                    length = token.Text.Length + 2;
                }

                spans.Add(new TokenSpan(
                    absStart,
                    length,
                    MapCategory(token)));
                cursor = absStart + length;
            }
        }
        catch (InvalidOperationException)
        {
            // Lexer threw. Keep what we have (comments + partial tokens).
        }

        // 3. Sort by StartOffset and merge overlaps (defensive).
        spans.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
        var deduped = new List<TokenSpan>(spans.Count);
        foreach (var s in spans)
        {
            if (deduped.Count == 0 || s.StartOffset >= deduped[^1].StartOffset + deduped[^1].Length)
                deduped.Add(s);
            // else: overlap; skip the second span (Comment spans can clash with token starts; prefer tokens)
        }
        return deduped;
    }

    private static int FindOffset(string text, int fromCursor, Token token)
    {
        // Lexer skips whitespace + comments; track cursor manually.
        // For v0.2 simplicity, use a naive search from cursor for token.Text.
        // (Real production would track line/col from token → absolute offset via Lexer extension.)
        int start = text.IndexOf(token.Text, fromCursor, StringComparison.Ordinal);
        return start >= 0 ? start : fromCursor; // fallback: assume contiguous
    }

    private static TokenCategory MapCategory(Token token)
    {
        // DataTypeSet check runs first (overrides Kind) because the Lexer emits
        // A2L data-type names (UBYTE, SBYTE, ...) as TokenKind.Identifier.
        if (DataTypeSet.Contains(token.Text))
            return TokenCategory.DataTypeKeyword;

        return token.Kind switch
        {
            TokenKind.Keyword when token.Text == "/begin" || token.Text == "/end"
                => TokenCategory.BlockKeyword,
            TokenKind.Keyword
                => TokenCategory.StructuralKeyword,
            TokenKind.Identifier
                => TokenCategory.Identifier,
            TokenKind.StringLiteral
                => TokenCategory.StringLiteral,
            TokenKind.Number
                => TokenCategory.Number,
            TokenKind.Symbol
                => TokenCategory.Symbol,
            _ => TokenCategory.Unknown,
        };
    }

    private static void EmitCommentSpans(string text, List<TokenSpan> spans)
    {
        int i = 0;
        while (i < text.Length)
        {
            if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '*')
            {
                int start = i;
                i += 2;
                while (i + 1 < text.Length && !(text[i] == '*' && text[i + 1] == '/'))
                    i++;
                if (i + 1 < text.Length) i += 2;
                else i = text.Length;
                spans.Add(new TokenSpan(start, i - start, TokenCategory.Comment));
            }
            else if (i + 1 < text.Length && text[i] == '/' && text[i + 1] == '/')
            {
                int start = i;
                while (i < text.Length && text[i] != '\n') i++;
                spans.Add(new TokenSpan(start, i - start, TokenCategory.Comment));
            }
            else
            {
                i++;
            }
        }
    }
}
