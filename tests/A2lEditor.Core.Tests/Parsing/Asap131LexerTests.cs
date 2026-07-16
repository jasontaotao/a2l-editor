using A2lEditor.Core.Parsing;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Parsing;

public sealed class Asap131LexerTests
{
    [Fact]
    public void EmptyInputProducesOnlyEof()
    {
        var tokens = new Asap131Lexer(string.Empty).Tokenize();

        tokens.Should().ContainSingle();
        tokens[0].Kind.Should().Be(TokenKind.Eof);
    }

    [Fact]
    public void KeywordIsRecognized()
    {
        var tokens = new Asap131Lexer("/begin PROJECT").Tokenize();

        tokens[0].Should().BeEquivalentTo(new Token(TokenKind.Keyword, "/begin", 1, 1));
        tokens[1].Should().BeEquivalentTo(new Token(TokenKind.Keyword, "PROJECT", 1, 8));
    }

    [Fact]
    public void StringLiteralPreservesText()
    {
        var tokens = new Asap131Lexer("\"BMS Model\"").Tokenize();

        tokens[0].Should().BeEquivalentTo(new Token(TokenKind.StringLiteral, "BMS Model", 1, 1));
    }

    [Fact]
    public void HexNumberIsRecognized()
    {
        var tokens = new Asap131Lexer("0x1A").Tokenize();

        tokens[0].Should().BeEquivalentTo(new Token(TokenKind.Number, "0x1A", 1, 1));
    }

    [Fact]
    public void CommentsAreSkipped()
    {
        var tokens = new Asap131Lexer("/* comment */ PROJECT // comment\n MODULE").Tokenize();

        tokens.Select(token => token.Text).Should().Equal("PROJECT", "MODULE", string.Empty);
    }

    [Fact]
    public void BmsModelFixtureProducesTokens()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples", "BmsModel.a2l");
        var text = File.ReadAllText(fixturePath);

        var tokens = new Asap131Lexer(text).Tokenize();

        tokens.Should().NotBeEmpty();
        tokens.Should().Contain(token => token.Kind == TokenKind.Keyword && token.Text == "/begin");
        tokens[^1].Kind.Should().Be(TokenKind.Eof);
    }
}
