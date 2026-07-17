using System.Text;

namespace A2lEditor.Core.Serialization;

public static class StringLiteralEscaper
{
    /// Escape special characters for A2L string literal output.
    /// Mirrors Asap131Lexer.ReadString (L122-127) in reverse.
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
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                case '\t': sb.Append("\\t"); break;
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
                case '\t': sw.Write("\\t"); break;
                default:  sw.Write(ch); break;
            }
        }
        sw.Write('"');
    }
}