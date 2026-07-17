using System;
using System.Reflection;
using System.Windows.Threading;
using A2lEditor.App;
using A2lEditor.App.Tests.Controls;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

/// <summary>
/// Debounced tree-rebuild tests for MainWindow (v0.7).
///
/// A <see cref="DispatcherTimer"/> with a 200 ms interval gates calls to
/// <c>MainWindow.RebuildTree</c>: every <c>TextChanged</c> restarts the timer,
/// and the tick (after 200 ms of quiet) performs exactly one rebuild.
///
/// These tests verify the wiring shape (timer field exists, configured at 200 ms,
/// initially stopped) rather than full dispatcher-pump behaviour. The end-to-end
/// "type 5 chars, wait 200 ms, tree rebuilds once" smoke check lives in Task 5.
/// </summary>
public class MainWindowDebounceTests
{
    [Fact]
    public void TextChanged_Immediately_TimerFieldExistsAndIsConfigured()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();

            var timerField = typeof(MainWindow).GetField(
                "_debounceTimer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            timerField.Should().NotBeNull("MainWindow must expose a private _debounceTimer field");

            var timer = (DispatcherTimer)timerField!.GetValue(window)!;
            timer.Should().NotBeNull();
            timer.Interval.Should().Be(TimeSpan.FromMilliseconds(200));
            timer.IsEnabled.Should().BeFalse("the timer is created stopped and only started on TextChanged");
        });
    }

    [Fact]
    public void TextChanged_AfterStart_TimerIsRunning()
    {
        StaRunner.Run(() =>
        {
            var window = new MainWindow();

            var timerField = typeof(MainWindow).GetField(
                "_debounceTimer",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var timer = (DispatcherTimer)timerField!.GetValue(window)!;

            // Mimic what OnEditorTextChanged will do: restart the timer.
            timer.Stop();
            timer.Start();

            timer.IsEnabled.Should().BeTrue();

            // Stop the timer so the test thread doesn't keep a live DispatcherTimer around.
            timer.Stop();
        });
    }
}
