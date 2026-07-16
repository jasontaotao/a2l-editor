using System.Windows;
using System.Windows.Controls;

namespace A2lEditor.App.Controls;

public partial class A2lTextEditor : UserControl
{
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

    public A2lTextEditor() => InitializeComponent();

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
}