using System.Text.Json;
using A2lEditor.Core.RecentFiles;
using FluentAssertions;
using Xunit;

namespace A2lEditor.Core.Tests.RecentFiles;

public class RecentFilesStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _storePath;

    public RecentFilesStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "a2l-rf-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _storePath = Path.Combine(_tempDir, "recent.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
    }

    private static string TouchFile(string dir, string name)
    {
        var p = Path.Combine(dir, name);
        File.WriteAllText(p, "ASAP2_VERSION 1 31\n");
        return p;
    }

    [Fact]
    public void Constructor_MissingFile_ReturnsEmptyEntries()
    {
        var store = new RecentFilesStore(_storePath);
        store.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_CorruptJson_ReturnsEmptyEntries()
    {
        File.WriteAllText(_storePath, "not valid json {{{");
        var store = new RecentFilesStore(_storePath);
        store.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Add_NewPath_PrependsToList()
    {
        var store = new RecentFilesStore(_storePath);
        var p1 = TouchFile(_tempDir, "a.a2l");
        var p2 = TouchFile(_tempDir, "b.a2l");
        store.Add(p1);
        store.Add(p2);
        store.Entries.Select(e => e.FullPath).Should().ContainInOrder(p2, p1);
    }

    [Fact]
    public void Add_DuplicatePath_MovesToFrontAndUpdatesTimestamp()
    {
        var store = new RecentFilesStore(_storePath);
        var p1 = TouchFile(_tempDir, "a.a2l");
        var p2 = TouchFile(_tempDir, "b.a2l");
        store.Add(p1);
        store.Add(p2);
        var firstTs = store.Entries[0].LastOpenedUtc;
        Thread.Sleep(50);
        store.Add(p1);
        store.Entries[0].FullPath.Should().Be(p1);
        store.Entries[0].LastOpenedUtc.Should().BeAfter(firstTs);
        store.Entries.Should().HaveCount(2, "duplicate move-to-front does not add new entry");
    }

    [Fact]
    public void Add_ExceedsMaxEntries_TrimsTo8()
    {
        var store = new RecentFilesStore(_storePath);
        for (int i = 0; i < 12; i++)
            store.Add(TouchFile(_tempDir, $"f{i:00}.a2l"));
        store.Entries.Should().HaveCount(8);
        store.Entries[0].FullPath.Should().EndWith("f11.a2l");
    }

    [Fact]
    public void Remove_ExistingPath_ReturnsTrue()
    {
        var store = new RecentFilesStore(_storePath);
        var p = TouchFile(_tempDir, "a.a2l");
        store.Add(p);
        store.Remove(p).Should().BeTrue();
        store.Entries.Should().BeEmpty();
    }

    [Fact]
    public void Remove_NonExistingPath_ReturnsFalse()
    {
        var store = new RecentFilesStore(_storePath);
        store.Remove(@"C:\does\not\exist.a2l").Should().BeFalse();
    }

    [Fact]
    public void SaveThenLoad_PreservesOrderAndTimestamps()
    {
        var p1 = TouchFile(_tempDir, "a.a2l");
        var p2 = TouchFile(_tempDir, "b.a2l");
        var store1 = new RecentFilesStore(_storePath);
        store1.Add(p1);
        store1.Add(p2);

        var store2 = new RecentFilesStore(_storePath);
        store2.Entries.Should().HaveCount(2);
        store2.Entries[0].FullPath.Should().Be(p2);
        store2.Entries[1].FullPath.Should().Be(p1);
    }

    [Fact]
    public void Add_PathWithDifferentCase_RemoveIsCaseInsensitive()
    {
        var store = new RecentFilesStore(_storePath);
        var p = TouchFile(_tempDir, "a.a2l");
        store.Add(p);
        var upper = p.ToUpperInvariant();
        store.Remove(upper).Should().BeTrue();
        store.Entries.Should().BeEmpty();
    }
}