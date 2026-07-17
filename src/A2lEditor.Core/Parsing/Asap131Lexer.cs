using System.Globalization;
using System.Text;

namespace A2lEditor.Core.Parsing;

public enum TokenKind
{
    Keyword,        // /begin /end ASAP2_VERSION
    Identifier,     // PROJECT MODULE MEASUREMENT 等
    StringLiteral,  // "..."
    Number,         // 1234 0x1A 1.5
    Symbol,         // ( ) , [ ]
    Eof
}

public sealed record Token(
    TokenKind Kind,
    string Text,
    int Line,
    int Column);

public sealed class Asap131Lexer
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "/begin", "/end", "ASAP2_VERSION",
        "PROJECT", "MODULE", "HEADER",
        "MOD_PAR", "MOD_COMMON", "BYTE_ORDER", "MSB_LAST", "MSB_FIRST",
        "DATA_SIZE", "ALIGNMENT_BYTE_ORDER",
        "RECORD_LAYOUT", "FNC_VALUES", "COLUMN_DIR", "DIRECT", "ROW_DIR",
        "AXIS_PTS_X", "INDEX_INCR", "INDEX_DECR",
        "COMPU_METHOD", "RAT_FUNC", "TAB_VERB", "TAB_NOINTP", "IDENTICAL",
        "COEFFS", "COEFFS_LINEAR",
        "MEASUREMENT", "ECU_ADDRESS", "RESOLUTION", "ACCURACY",
        "LOWER_LIMIT", "UPPER_LIMIT",
        "CHARACTERISTIC", "AXIS_PTS", "INPUT_QUANTITY", "NUMBER",
        "GROUP", "ROOT", "REF_MEASUREMENT", "REF_CHARACTERISTIC",
        "AXIS_DESCR", "USER_RIGHTS", "VERSION"
    };

    private static readonly HashSet<string> DataTypes = new(StringComparer.Ordinal)
    {
        "UBYTE", "SBYTE", "UWORD", "SWORD",
        "ULONG", "SLONG", "FLOAT32_IEEE", "FLOAT64_IEEE"
    };

    private readonly string _text;
    private int _pos;
    private int _line = 1;
    private int _col = 1;

    public Asap131Lexer(string text) => _text = text;

    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (_pos < _text.Length)
        {
            SkipWhitespaceAndComments();
            if (_pos >= _text.Length) break;
            tokens.Add(NextToken());
        }
        tokens.Add(new Token(TokenKind.Eof, "", _line, _col));
        return tokens;
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _text.Length)
        {
            char c = _text[_pos];
            if (char.IsWhiteSpace(c))
            {
                Advance(c == '\n');
            }
            else if (c == '/' && _pos + 1 < _text.Length && _text[_pos + 1] == '*')
            {
                // /* ... */ 注释
                _pos += 2;
                int startLine = _line, startCol = _col;
                Advance(Advance(0));
                while (_pos + 1 < _text.Length &&
                       !(_text[_pos] == '*' && _text[_pos + 1] == '/'))
                {
                    Advance(_text[_pos] == '\n');
                }
                if (_pos + 1 < _text.Length) { Advance(Advance(0)); }
                else
                {
                    throw new InvalidOperationException(
                        $"Unterminated comment starting at line {startLine} col {startCol}");
                }
            }
            else if (c == '/' && _pos + 1 < _text.Length && _text[_pos + 1] == '/')
            {
                // // 行注释
                while (_pos < _text.Length && _text[_pos] != '\n') _pos++;
            }
            else
            {
                break;
            }
        }
    }

    private Token NextToken()
    {
        char c = _text[_pos];
        int startLine = _line, startCol = _col;
        if (c == '"') return ReadString(startLine, startCol);
        if (char.IsDigit(c) || (c == '0' && _pos + 1 < _text.Length &&
                                (_text[_pos + 1] == 'x' || _text[_pos + 1] == 'X')))
            return ReadNumber(startLine, startCol);
        if (c == '(' || c == ')' || c == ',' || c == '[' || c == ']')
            return ReadSymbol(startLine, startCol);
        return ReadIdentifierOrKeyword(startLine, startCol);
    }

    private Token ReadString(int line, int col)
    {
        var sb = new StringBuilder();
        Advance(false); // consume opening "
        while (_pos < _text.Length && _text[_pos] != '"')
        {
            if (_text[_pos] == '\\' && _pos + 1 < _text.Length)
            {
                sb.Append(_text[_pos + 1]);
                Advance(Advance(0));
            }
            else
            {
                sb.Append(_text[_pos]);
                Advance(_text[_pos] == '\n');
            }
        }
        if (_pos < _text.Length) Advance(false); // consume closing "
        return new Token(TokenKind.StringLiteral, sb.ToString(), line, col);
    }

    private Token ReadNumber(int line, int col)
    {
        var sb = new StringBuilder();
        if (_text[_pos] == '0' && _pos + 1 < _text.Length &&
            (_text[_pos + 1] == 'x' || _text[_pos + 1] == 'X'))
        {
            sb.Append("0x");
            Advance(Advance(0));
            while (_pos < _text.Length && IsHexDigit(_text[_pos]))
            {
                sb.Append(_text[_pos]); Advance(false);
            }
        }
        else
        {
            while (_pos < _text.Length &&
                   (char.IsDigit(_text[_pos]) || _text[_pos] == '.' ||
                    _text[_pos] == 'e' || _text[_pos] == 'E' ||
                    _text[_pos] == '+' || _text[_pos] == '-'))
            {
                sb.Append(_text[_pos]); Advance(false);
            }
        }
        return new Token(TokenKind.Number, sb.ToString(), line, col);
    }

    private Token ReadSymbol(int line, int col)
    {
        char c = _text[_pos];
        Advance(false);
        return new Token(TokenKind.Symbol, c.ToString(), line, col);
    }

    private Token ReadIdentifierOrKeyword(int line, int col)
    {
        var sb = new StringBuilder();
        while (_pos < _text.Length && !char.IsWhiteSpace(_text[_pos]) &&
               _text[_pos] != '(' && _text[_pos] != ')' &&
               _text[_pos] != ',' && _text[_pos] != '[' && _text[_pos] != ']' &&
               _text[_pos] != '"')
        {
            sb.Append(_text[_pos]); Advance(false);
        }
        string text = sb.ToString();
        var kind = Keywords.Contains(text) ? TokenKind.Keyword : TokenKind.Identifier;
        // 注意: 数据类型作为 Identifier 返回（parser 阶段类型化）
        return new Token(kind, text, line, col);
    }

    private static bool IsHexDigit(char c) =>
        (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');

    private void Advance(bool newline)
    {
        _pos++;
        if (newline) { _line++; _col = 1; }
        else { _col++; }
    }

    private int Advance(int _) => _pos++; // overload helper
}