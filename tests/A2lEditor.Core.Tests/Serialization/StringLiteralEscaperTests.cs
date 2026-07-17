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
    public void Escape_NewlinesCarriageReturnsTabs_EscapesCorrectly()
    {
        // Input:  line1\nline2\r\ttab
        // Output: line1\nline2\r\ttab  (with \n \r \t escape sequences)
        StringLiteralEscaper.Escape("line1\nline2\r\ttab")
            .Should().Be("line1\\nline2\\r\\ttab");
    }
}