using A2lEditor.Core.Highlighting;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.Highlighting;

public class TokenClassifierTests
{
    [Fact]
    public void Classify_EmptyText_ReturnsEmptyList()
    {
        var spans = TokenClassifier.Classify("");
        spans.Should().BeEmpty();
    }

    [Fact]
    public void Classify_BlockKeywords_BeginEnd_AreBlockKeyword()
    {
        var spans = TokenClassifier.Classify("/begin /end");
        spans.Should().HaveCount(2);
        spans.Should().AllSatisfy(s => s.Category.Should().Be(TokenCategory.BlockKeyword));
    }

    [Fact]
    public void Classify_StructuralKeywords_ProjectModule_AreStructuralKeyword()
    {
        var spans = TokenClassifier.Classify("PROJECT MODULE");
        spans.Where(s => s.Category == TokenCategory.StructuralKeyword)
            .Select(s => s.StartOffset)
            .Should().BeEquivalentTo(new[] { 0, 8 });
    }

    [Fact]
    public void Classify_DataTypes_UbyteSbyte_AreDataTypeKeyword()
    {
        var spans = TokenClassifier.Classify("UBYTE SBYTE UWORD SWORD");
        spans.Where(s => s.Category == TokenCategory.DataTypeKeyword)
            .Should().HaveCount(4);
    }

    [Fact]
    public void Classify_StringLiteral_HasCorrectOffset()
    {
        var spans = TokenClassifier.Classify("\"hello\"");
        spans.Should().ContainSingle(s => s.Category == TokenCategory.StringLiteral)
            .Which.StartOffset.Should().Be(0);
    }

    [Fact]
    public void Classify_Number_HexAndDecimal_BothNumber()
    {
        var spans = TokenClassifier.Classify("0x1A 42");
        spans.Where(s => s.Category == TokenCategory.Number)
            .Should().HaveCount(2);
    }

    [Fact]
    public void Classify_Comments_BlockAndLine_AreCommentCategory()
    {
        var spans = TokenClassifier.Classify("/* block */ // line\n");
        spans.Where(s => s.Category == TokenCategory.Comment)
            .Should().HaveCount(2);
    }

    [Fact]
    public void Classify_FullSample_BmsModel_SpansAreSortedAndNonOverlapping()
    {
        var text = File.ReadAllText(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "samples", "BmsModel.a2l"));
        var spans = TokenClassifier.Classify(text);
        spans.Should().NotBeEmpty();
        for (var i = 1; i < spans.Count; i++)
        {
            spans[i].StartOffset.Should().BeGreaterThanOrEqualTo(
                spans[i - 1].StartOffset + spans[i - 1].Length,
                "spans must be sorted and non-overlapping");
        }
    }
}
