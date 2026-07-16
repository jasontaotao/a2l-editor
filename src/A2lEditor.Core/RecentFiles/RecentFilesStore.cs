using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2lEditor.Core.RecentFiles;

public sealed class RecentFilesStore
{
    public const int MaxEntries = 8;

    public static string DefaultPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "a2l-editor", "recent.json");

    private readonly string _path;
    private readonly List<RecentFileEntry> _entries = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public IReadOnlyList<RecentFileEntry> Entries => _entries;

    public RecentFilesStore() : this(DefaultPath) { }

    public RecentFilesStore(string customPath)
    {
        _path = customPath;
        Load();
    }

    public void Add(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return;
        if (!File.Exists(fullPath)) return; // silently skip non-existent

        var now = DateTime.UtcNow;
        _entries.RemoveAll(e => PathsEqual(e.FullPath, fullPath));
        _entries.Insert(0, new RecentFileEntry(fullPath, now));
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);
        Save();
    }

    public bool Remove(string fullPath)
    {
        var removed = _entries.RemoveAll(e => PathsEqual(e.FullPath, fullPath));
        if (removed > 0) Save();
        return removed > 0;
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            var dto = new RecentFilesDto { Version = 1, Entries = _entries.ToList() };
            File.WriteAllText(_path, JsonSerializer.Serialize(dto, _jsonOptions));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new RecentFilesStoreException(
                $"Failed to save recent files to {_path}", ex);
        }
    }

    private void Load()
    {
        _entries.Clear();
        if (!File.Exists(_path)) return;
        try
        {
            var dto = JsonSerializer.Deserialize<RecentFilesDto>(File.ReadAllText(_path), _jsonOptions);
            if (dto?.Entries is not null) _entries.AddRange(dto.Entries);
        }
        catch (JsonException)
        {
            // Corrupt JSON — silently reset to empty (spec section 6).
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new RecentFilesStoreException(
                $"Failed to load recent files from {_path}", ex);
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a), Path.GetFullPath(b),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private sealed class RecentFilesDto
    {
        [JsonPropertyName("version")] public int Version { get; set; }
        [JsonPropertyName("entries")] public List<RecentFileEntry> Entries { get; set; } = new();
    }
}