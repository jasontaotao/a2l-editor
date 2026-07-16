using System.Globalization;
using System.Text;
using A2lEditor.Core.Model;

namespace A2lEditor.Core.Parsing;

public sealed class Asap131Parser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _pos;
    private readonly List<ParseError> _errors = new();
    private readonly string _rawText;

    private Asap131Parser(string text)
    {
        _rawText = text;
        var lexer = new Asap131Lexer(text);
        _tokens = lexer.Tokenize();
    }

    public static ParseResult<A2lDocument> ParseText(string text)
    {
        var p = new Asap131Parser(text);
        return p.Parse();
    }

    public static ParseResult<A2lDocument> ParseFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        // 简化处理：尝试 UTF-8，如果失败回退到 GB18030
        string text;
        try
        {
            text = Encoding.UTF8.GetString(bytes);
            // 验证是否为有效 UTF-8（无双字节无效）
            var _ = Encoding.UTF8.GetEncoder();
            text = File.ReadAllText(path, Encoding.UTF8);
        }
        catch
        {
            text = File.ReadAllText(path, Encoding.GetEncoding("GB18030"));
        }
        return ParseText(text);
    }

    private ParseResult<A2lDocument> Parse()
    {
        var version = A2lVersion.V1_31;
        string projectName = "", projectComment = "", headerComment = "";
        var modules = new List<A2lModule>();

        // ASAP2_VERSION line
        if (Current.Kind == TokenKind.Keyword && Current.Text == "ASAP2_VERSION")
        {
            Consume();
            // 期望两个数字（major minor）
            ExpectNumber(out var major);
            ExpectNumber(out var minor);
            if (major != 1 || minor > 70)
                _errors.Add(Error($"Unsupported ASAP2 version {major}.{minor}", ErrorSeverity.Fatal));
            else if (minor >= 60)
                version = A2lVersion.V1_6x;
        }

        // PROJECT block
        if (TryConsumeKeyword("/begin"))
        {
            if (TryConsumeKeyword("PROJECT"))
            {
                if (Current.Kind == TokenKind.Identifier) { projectName = Consume().Text; }
                if (Current.Kind == TokenKind.StringLiteral) { projectComment = Consume().Text; }

                // HEADER (optional) — peek before consume to avoid speculative /begin consumption
                if (Current.Kind == TokenKind.Keyword && Current.Text == "/begin" &&
                    _pos + 1 < _tokens.Count && _tokens[_pos + 1].Text == "HEADER")
                {
                    Consume(); // /begin
                    Consume(); // HEADER
                    if (Current.Kind == TokenKind.StringLiteral)
                        headerComment = Consume().Text;
                    TryConsumeKeyword("/end");
                }

                // MODULE blocks
                while (TryConsumeKeyword("/begin") && TryConsumeKeyword("MODULE"))
                {
                    var module = ParseModule(out var moduleRange);
                    modules.Add(module);
                    TryConsumeKeyword("/end");
                }
            }
        }

        var doc = new A2lDocument(
            version, projectName, projectComment, headerComment,
            null, modules, _rawText, _rawText.Count(c => c == '\n') + 1);

        return _errors.Any(e => e.Severity == ErrorSeverity.Fatal)
            ? ParseResult<A2lDocument>.Failure(_errors)
            : ParseResult<A2lDocument>.Partial(doc, _errors);
    }

    private A2lModule ParseModule(out LineRange range)
    {
        int startLine = Current.Line;
        string name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<unnamed>";
        string comment = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";

        var measurements = new List<A2lMeasurement>();
        var characteristics = new List<A2lCharacteristic>();
        var axisPts = new List<A2lAxisPts>();
        var compuMethods = new List<A2lCompuMethod>();
        var recordLayouts = new List<A2lRecordLayout>();
        var groups = new List<A2lGroup>();

        while (!(Current.Kind == TokenKind.Keyword && Current.Text == "/end"))
        {
            if (Current.Kind == TokenKind.Eof)
            {
                _errors.Add(Error("Unexpected EOF inside MODULE", ErrorSeverity.Fatal));
                break;
            }
            if (Current.Kind == TokenKind.Keyword && Current.Text == "/begin")
            {
                Consume();
                var blockName = Current.Kind == TokenKind.Keyword ? Consume().Text : "?";
                switch (blockName)
                {
                    case "MEASUREMENT":
                        measurements.Add(ParseMeasurement());
                        break;
                    case "CHARACTERISTIC":
                        characteristics.Add(ParseCharacteristic());
                        break;
                    case "AXIS_PTS":
                        axisPts.Add(ParseAxisPts());
                        break;
                    case "COMPU_METHOD":
                        compuMethods.Add(ParseCompuMethod());
                        break;
                    case "RECORD_LAYOUT":
                        recordLayouts.Add(ParseRecordLayout());
                        break;
                    case "GROUP":
                        groups.Add(ParseGroup());
                        break;
                    default:
                        _errors.Add(Error($"Unknown block {blockName}, skipped", ErrorSeverity.Warning));
                        SkipToMatchingEnd();
                        break;
                }
            }
            else
            {
                Consume();
            }
        }

        int endLine = Current.Line;
        range = new LineRange(startLine, endLine);
        return new A2lModule(name, comment, measurements, characteristics, axisPts,
            compuMethods, recordLayouts, groups, null, range);
    }

    private A2lMeasurement ParseMeasurement()
    {
        int startLine = Current.Line;
        string name;
        if (ConsumeIdentifierOrString(out var nameStr))
        {
            name = nameStr;
        }
        else
        {
            _errors.Add(Error("Expected MEASUREMENT name", ErrorSeverity.Error));
            name = "<error>";
        }

        string longId = "";
        if (Current.Kind == TokenKind.StringLiteral) longId = Consume().Text;

        var dataType = ParseDataType(out var dt) ? dt : A2lDataType.UBYTE;
        var compuMethod = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        var resolution = ParseNumber();
        var accuracy = ParseNumber();
        var lowerLimit = ParseNumber();
        var upperLimit = ParseNumber();

        ulong ecuAddress = 0;
        if (TryConsumeKeyword("ECU_ADDRESS"))
            ulong.TryParse(Current.Text.TrimStart('0', 'x', 'X'),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out ecuAddress);

        TryConsumeKeyword("/end");
        return new A2lMeasurement(name, longId, dataType, compuMethod,
            resolution, accuracy, lowerLimit, upperLimit, ecuAddress,
            new LineRange(startLine, Current.Line));
    }

    private A2lCharacteristic ParseCharacteristic()
    {
        int startLine = Current.Line;
        var name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<error>";
        string longId = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        var recordLayout = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        ulong addr = 0;
        if (Current.Kind == TokenKind.Number) ulong.TryParse(
            Current.Text.TrimStart('0', 'x', 'X'),
            NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr);
        if (Current.Kind == TokenKind.Number) Consume();
        var lo = ParseNumber();
        var hi = ParseNumber();
        TryConsumeKeyword("/end");
        return new A2lCharacteristic(name, longId, recordLayout, addr, lo, hi,
            new LineRange(startLine, Current.Line));
    }

    private A2lAxisPts ParseAxisPts()
    {
        int startLine = Current.Line;
        var name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<error>";
        string longId = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        var recordLayout = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        ulong addr = 0;
        if (Current.Kind == TokenKind.Number)
            ulong.TryParse(Current.Text.TrimStart('0', 'x', 'X'),
                NumberStyles.HexNumber, CultureInfo.InvariantCulture, out addr);
        if (Current.Kind == TokenKind.Number) Consume();
        var inputQty = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        var compuMethod = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        int n = 0;
        if (Current.Kind == TokenKind.Number) int.TryParse(Current.Text, out n);
        if (Current.Kind == TokenKind.Number) Consume();
        var lo = ParseNumber();
        var hi = ParseNumber();
        TryConsumeKeyword("/end");
        return new A2lAxisPts(name, longId, recordLayout, addr, inputQty, compuMethod,
            n, lo, hi, new LineRange(startLine, Current.Line));
    }

    private A2lCompuMethod ParseCompuMethod()
    {
        int startLine = Current.Line;
        var name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<error>";
        string longId = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        var type = Current.Kind == TokenKind.Identifier ? Consume().Text : "";
        var fmt = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        var unit = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        var coeffs = new double[6];
        if (TryConsumeKeyword("COEFFS"))
            for (int i = 0; i < 6; i++)
            {
                if (Current.Kind == TokenKind.Number &&
                    double.TryParse(Current.Text, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var d))
                {
                    coeffs[i] = d; Consume();
                }
            }
        TryConsumeKeyword("/end");
        return new A2lCompuMethod(name, longId, type, fmt, unit,
            coeffs[0], coeffs[1], coeffs[2], coeffs[3], coeffs[4], coeffs[5],
            new LineRange(startLine, Current.Line));
    }

    private A2lRecordLayout ParseRecordLayout()
    {
        int startLine = Current.Line;
        var name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<error>";
        var entries = new List<RecordLayoutEntry>();
        while (!(Current.Kind == TokenKind.Keyword && Current.Text == "/end"))
        {
            if (Current.Kind == TokenKind.Identifier || Current.Kind == TokenKind.Keyword)
            {
                var kw = Consume().Text;
                var pos = 0;
                var dt = "";
                var idx = "";
                var addr = "";
                if (Current.Kind == TokenKind.Number) { pos = int.Parse(Consume().Text); }
                if (Current.Kind == TokenKind.Identifier) { dt = Consume().Text; }
                if (Current.Kind == TokenKind.Identifier) { idx = Consume().Text; }
                if (Current.Kind == TokenKind.Identifier) { addr = Consume().Text; }
                entries.Add(new RecordLayoutEntry(kw, pos, dt, idx, addr));
            }
            else Consume();
        }
        TryConsumeKeyword("/end");
        return new A2lRecordLayout(name, entries, new LineRange(startLine, Current.Line));
    }

    private A2lGroup ParseGroup()
    {
        int startLine = Current.Line;
        var name = Current.Kind == TokenKind.Identifier ? Consume().Text : "<error>";
        string longId = Current.Kind == TokenKind.StringLiteral ? Consume().Text : "";
        bool isRoot = TryConsumeKeyword("ROOT");

        var refMeasurements = new List<string>();
        var refCharacteristics = new List<string>();
        while (TryConsumeKeyword("/begin"))
        {
            if (TryConsumeKeyword("REF_MEASUREMENT"))
                while (Current.Kind == TokenKind.Identifier || Current.Kind == TokenKind.StringLiteral)
                    refMeasurements.Add(Consume().Text);
            else if (TryConsumeKeyword("REF_CHARACTERISTIC"))
                while (Current.Kind == TokenKind.Identifier || Current.Kind == TokenKind.StringLiteral)
                    refCharacteristics.Add(Consume().Text);
            else
                SkipToMatchingEnd();
            TryConsumeKeyword("/end");
        }
        TryConsumeKeyword("/end");
        return new A2lGroup(name, longId, isRoot, refMeasurements, refCharacteristics,
            new LineRange(startLine, Current.Line));
    }

    private bool ParseDataType(out A2lDataType dt)
    {
        dt = A2lDataType.UBYTE;
        if (Current.Kind != TokenKind.Identifier) return false;
        dt = Current.Text switch
        {
            "UBYTE" => A2lDataType.UBYTE,
            "SBYTE" => A2lDataType.SBYTE,
            "UWORD" => A2lDataType.UWORD,
            "SWORD" => A2lDataType.SWORD,
            "ULONG" => A2lDataType.ULONG,
            "SLONG" => A2lDataType.SLONG,
            "FLOAT32_IEEE" => A2lDataType.FLOAT32_IEEE,
            "FLOAT64_IEEE" => A2lDataType.FLOAT64_IEEE,
            _ => A2lDataType.UBYTE
        };
        Consume();
        return true;
    }

    private bool ConsumeIdentifierOrString(out string s)
    {
        if (Current.Kind == TokenKind.Identifier || Current.Kind == TokenKind.StringLiteral)
        {
            s = Consume().Text;
            return true;
        }
        s = "";
        return false;
    }

    private string ParseNumber()
    {
        if (Current.Kind == TokenKind.Number) return Consume().Text;
        return "0";
    }

    private void ExpectNumber(out int value)
    {
        if (Current.Kind == TokenKind.Number && int.TryParse(Current.Text, out value))
            Consume();
        else { value = 0; _errors.Add(Error("Expected number", ErrorSeverity.Error)); }
    }

    private void SkipToMatchingEnd()
    {
        int depth = 1;
        while (_pos < _tokens.Count && depth > 0)
        {
            if (Current.Kind == TokenKind.Keyword)
            {
                if (Current.Text == "/begin") depth++;
                else if (Current.Text == "/end") depth--;
            }
            if (depth > 0) Consume();
        }
    }

    private Token Current => _tokens[_pos];
    private Token Consume() => _tokens[_pos++];
    private bool TryConsumeKeyword(string kw)
    {
        if (Current.Kind == TokenKind.Keyword &&
            string.Equals(Current.Text, kw, StringComparison.OrdinalIgnoreCase))
        {
            Consume();
            // A2L grammar: "/end BLOCK_NAME" is written as two tokens. After consuming
            // /end, also consume the matching block-name keyword when present (so callers
            // like TryConsumeKeyword("/end") don't leak the trailing BLOCK_NAME token).
            if (kw == "/end" && _pos < _tokens.Count &&
                Current.Kind == TokenKind.Keyword &&
                Current.Text != "/begin" && Current.Text != "/end")
            {
                Consume();
            }
            return true;
        }
        return false;
    }
    private ParseError Error(string msg, ErrorSeverity sev) =>
        new(Current.Line, Current.Column, msg, sev);
}
