namespace A2lEditor.Core.RecentFiles;

public sealed class RecentFilesStoreException : Exception
{
    public RecentFilesStoreException(string message, Exception? inner = null) : base(message, inner) { }
}