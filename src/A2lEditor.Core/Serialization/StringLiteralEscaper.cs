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
}