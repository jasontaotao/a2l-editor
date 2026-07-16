using A2lEditor.Core.Highlighting;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace A2lEditor.App.Highlighting;

public sealed class A2lSyntaxHighlighter : DocumentColorizingTransformer
{
    private IReadOnlyList<TokenSpan> _spans = Array.Empty<TokenSpan>();

    /// Invalidate the cached spans and trigger a re-colorization.
    public void Refresh(string text)
    {
        _spans = TokenClassifier.Classify(text);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        if (_spans.Count == 0) return;

        int lineStart = line.Offset;
        int lineEnd = line.Offset + line.Length;

        // Binary search for first span with offset >= lineStart
        int lo = 0, hi = _spans.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_spans[mid].StartOffset + _spans[mid].Length <= lineStart) lo = mid + 1;
            else hi = mid;
        }
        int idx = lo;

        // Iterate spans overlapping this line.
        while (idx < _spans.Count)
        {
            var span = _spans[idx];
            if (span.StartOffset >= lineEnd) break;
            int spanStart = Math.Max(span.StartOffset, lineStart);
            int spanEnd = Math.Min(span.StartOffset + span.Length, lineEnd);
            if (spanEnd > spanStart)
            {
                var brush = TokenCategoryToBrush.ForCategory(span.Category);
                ChangeLinePart(spanStart, spanEnd, visualLine =>
                {
                    visualLine.TextRunProperties.SetForegroundBrush(brush);
                });
            }
            idx++;
        }
    }
}