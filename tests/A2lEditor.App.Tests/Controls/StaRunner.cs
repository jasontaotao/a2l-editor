using System.Threading;

namespace A2lEditor.App.Tests.Controls;

/// <summary>
/// Helper to run WPF UserControl / FrameworkElement tests on an STA thread.
/// xUnit 2.x defaults test threads to MTA, but WPF FrameworkElement base
/// constructors require STA. This helper spawns a fresh STA worker thread.
/// </summary>
internal static class StaRunner
{
    public static void Run(System.Action action)
    {
        Exception? caught = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (caught != null) throw caught;
    }
}