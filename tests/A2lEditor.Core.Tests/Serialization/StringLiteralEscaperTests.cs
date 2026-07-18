using System;
using A2lEditor.Core.Serialization;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Serialization;

public class StringLiteralEscaperTests
{
    [Fact]
    public void Escape_Null_Throws()
    {
        var act = () => StringLiteralEscaper.Escape(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Escape_Empty_ReturnsEmpty()
    {
        StringLiteralEscaper.Escape("").Should().Be("");
    }

    [Fact]
    public void Escape_PlainText_ReturnsUnchanged()
    {
        StringLiteralEscaper.Escape("hello world").Should().Be("hello world");
    }

    [Fact]
    public void Escape_QuotesAndBackslashes_EscapesCorrectly()
    {
        // Input:  hello "world" with \backslash
        // Output: hello \"world\" with \\backslash
        StringLiteralEscaper.Escape("hello \"world\" with \\backslash")
            .Should().Be("hello \\\"world\\\" with \\\\backslash");
    }

    [Fact]
    public void Escape_NewlinesCarriageReturnsTabs_PassThrough()
    {
        // 控制字符不经转义直接通过——A2L lexer (ReadString) 不解释 \n \r \t 转义序列，
        // 所以 escaper 也不产生它们。多行字符串由 lexer 原生支持（读取到闭合 " 为止）。
        var input = "line1\nline2\r\ttab";
        StringLiteralEscaper.Escape(input).Should().Be(input);
    }
}