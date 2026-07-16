using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using A2lEditor.Core.Parsing;

namespace A2lEditor.App.Controls;

public partial class ErrorListPanel : UserControl
{
    private bool _hasAutoExpandedOnce;

    public event Action<int>? NavigateToLineRequested;

    public ErrorListPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += (_, _) => UpdateCount();
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged old)
            old.CollectionChanged -= OnErrorsChanged;
        if (e.NewValue is INotifyCollectionChanged neu)
            neu.CollectionChanged += OnErrorsChanged;
        UpdateCount();
    }

    private void OnErrorsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateCount();
    }

    private void UpdateCount()
    {
        if (DataContext is System.Collections.IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            ErrorCountText.Text = count.ToString();
            if (count > 0 && !_hasAutoExpandedOnce)
            {
                ErrorExpander.IsExpanded = true;
                _hasAutoExpandedOnce = true;
            }
        }
        else
        {
            ErrorCountText.Text = "0";
        }
    }

    private void ErrorList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ErrorList.SelectedItem is ParseError err && err.Line > 0)
        {
            NavigateToLineRequested?.Invoke(err.Line);
        }
    }
}