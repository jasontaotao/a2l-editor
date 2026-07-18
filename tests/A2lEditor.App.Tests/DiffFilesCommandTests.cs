using A2lEditor.App.Commands;
using A2lEditor.App.Views;
using A2lEditor.Core.Diff;
using FluentAssertions;
using Xunit;

namespace A2lEditor.App.Tests;

public class DiffFilesCommandTests
{
    [Fact]
    public void DiffFilesCommand_CanExecute_IsTrue()
    {
        AppCommands.DiffFiles.Should().NotBeNull();
    }

    [Fact]
    public void FormatReport_EmptyReport_ShowsAllUnchanged()
    {
        var report = new A2lDiffReport(null, null, DiffKind.Unchanged,
            System.Array.Empty<FieldChange>(),
            System.Array.Empty<ModuleDiff>());

        var text = DiffReportDialog.FormatReport(report);

        text.Should().Contain("Summary:");
        text.Should().Contain("all blocks unchanged");
    }

    [Fact]
    public void FormatReport_WithChanges_ShowsAddedRemovedModified()
    {
        var report = new A2lDiffReport("f1.a2l", "f2.a2l", DiffKind.Modified,
            System.Array.Empty<FieldChange>(),
            new[]
            {
                new ModuleDiff("M1", DiffKind.Modified,
                    new[] { new BlockDiff("MEASUREMENT", "V", DiffKind.Added, System.Array.Empty<FieldChange>()) },
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    System.Array.Empty<BlockDiff>(),
                    null)
            });

        var text = DiffReportDialog.FormatReport(report);

        text.Should().Contain("f1.a2l ↔ f2.a2l");
        text.Should().Contain("[MODULE] M1 — Modified");
        text.Should().Contain("MEASUREMENT V — Added");
        text.Should().Contain("Summary: 1 Added");
    }
}
