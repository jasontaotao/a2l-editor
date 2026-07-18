using System.Text;

namespace A2lEditor.Core.Serialization;

public static class StringLiteralEscaper
{
    /// Escape special characters for A2L string literal output.
    /// A2L lexer (Asap131Lexer.ReadString) only recognizes \ and " as
    /// escape-prefix sequences (\ → skip backslash, keep next char).
    /// Control characters (\n, \r, \t) are NOT interpreted as escapes by
    /// the lexer, so this escaper does NOT emit them — it passes real
    /// control characters through for multi-line string support.
    /// Order matters: escape backslash FIRST to avoid double-escaping.
    public static string Escape(string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));
        if (s.Length == 0) return s;

        var sb = new StringBuilder(s.Length + 8);
        foreach (var c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"':  sb.Append("\\\""); break;
                default:   sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    /// Emit a multi-line string literal directly to a TextWriter, preserving real
    /// newlines (\n) instead of escaping them (sibling of Escape). Used for
    /// comment fields where multi-line emit is the v0.6 spec choice. Quotes and
    /// backslashes are still escaped; CR is skipped to handle Windows line endings.
    public static void EmitMultiLine(TextWriter sw, string? s)
    {
        if (string.IsNullOrEmpty(s)) return;
        sw.Write('"');
        foreach (var ch in s)
        {
            switch (ch)
            {
                case '"':  sw.Write("\\\""); break;
                case '\\': sw.Write("\\\\"); break;
                case '\n': sw.Write('\n'); break;     // 真实换行 (vs Escape 写 \\n)
                case '\r': break;                     // skip CR (Windows line endings)
                default:  sw.Write(ch); break;
            }
        }
        sw.Write('"');
    }
}