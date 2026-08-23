namespace ShotgunKeyboard.Core.Playlists;

public sealed class PlaylistCatalog
{
    public PlaylistCatalog(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        Refresh();
    }

    public string RootDirectory { get; }
    public IReadOnlyList<PlaylistDefinition> Playlists { get; private set; } = [];

    public void Refresh()
    {
        Directory.CreateDirectory(RootDirectory);
        Playlists = PlaylistScanner.Scan(RootDirectory);
    }

    public PlaylistDefinition? Find(string? id)
        => Playlists.FirstOrDefault(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
}
