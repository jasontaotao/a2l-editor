using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace A2lEditor.App.Controls;

public partial class A2lTextEditor : UserControl
{
    private SearchPanel? _searchPanel;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(A2lTextEditor),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnTextChanged));

    public static readonly DependencyProperty IsDirtyProperty =
        DependencyProperty.Register(nameof(IsDirty), typeof(bool), typeof(A2lTextEditor),
            new PropertyMetadata(false));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsDirty
    {
        get => (bool)GetValue(IsDirtyProperty);
        set => SetValue(IsDirtyProperty, value);
    }

    public A2lTextEditor()
    {
        InitializeComponent();
        _searchPanel = SearchPanel.Install(Editor);
    }

    /// <summary>Show the find/replace panel (toggled by Ctrl+F).</summary>
    public void ShowSearch() => _searchPanel?.Open();

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (A2lTextEditor)d;
        if (ctrl.Editor.Text != (string)e.NewValue)
            ctrl.Editor.Text = (string)e.NewValue;
    }

    private void Editor_TextChanged(object sender, EventArgs e)
    {
        if (Text != Editor.Text)
        {
            Text = Editor.Text;
            IsDirty = true;
        }
    }

    private DispatcherTimer? _highlightTimer;

    /// <summary>
    /// Navigate to 1-based line and select entire line.
    /// Out-of-range lines are clamped (no exception).
    /// </summary>
    public void ScrollToLine(int line)
    {
        if (string.IsNullOrEmpty(Editor.Text)) return;
        if (line < 1) line = 1;
        var docLine = Editor.Document.GetLineByNumber(Math.Min(line, Editor.Document.LineCount));
        Editor.ScrollToLine(docLine.LineNumber);
        Editor.Select(docLine.Offset, docLine.Length);
    }

    /// <summary>
    /// Briefly highlight a line for 0.5s. Out-of-range lines are ignored.
    /// </summary>
    public void HighlightLine(int line)
    {
        if (string.IsNullOrEmpty(Editor.Text)) return;
        if (line < 1 || line > Editor.Document.LineCount) return;

        var bgColor = Color.FromArgb(80, 255, 255, 0); // semi-transparent yellow
        var docLine = Editor.Document.GetLineByNumber(line);
        var marker = new HighlightLineBackgroundMarker(docLine.Offset, docLine.Length, bgColor);
        Editor.TextArea.TextView.LineTransformers.Add(marker);

        _highlightTimer?.Stop();
        _highlightTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _highlightTimer.Tick += (_, _) =>
        {
            Editor.TextArea.TextView.LineTransformers.Remove(marker);
            _highlightTimer!.Stop();
        };
        _highlightTimer.Start();
    }

    private sealed class HighlightLineBackgroundMarker : DocumentColorizingTransformer
    {
        private readonly int _start;
        private readonly int _end;
        private readonly Color _color;

        public HighlightLineBackgroundMarker(int start, int end, Color color)
        {
            _start = start;
            _end = end;
            _color = color;
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            if (line.Offset + line.Length < _start || line.Offset > _end) return;
            ChangeLinePart(
                Math.Max(line.Offset, _start),
                Math.Min(line.Offset + line.Length, _end),
                visualLine =>
                {
                    visualLine.TextRunProperties.SetBackgroundBrush(new SolidColorBrush(_color));
                });
        }
    }
}