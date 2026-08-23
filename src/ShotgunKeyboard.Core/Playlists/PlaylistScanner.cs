namespace ShotgunKeyboard.Core.Playlists;

public static class PlaylistScanner
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3"
    };

    public static IReadOnlyList<PlaylistDefinition> Scan(string rootDirectory)
    {
        if (!Directory.Exists(rootDirectory))
        {
            return [];
        }

        return Directory.EnumerateDirectories(rootDirectory)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(directory => CreateDefinition(directory))
            .ToArray();
    }

    public static PlaylistDefinition CreateDefinition(string directory)
    {
        var id = Path.GetFileName(directory);
        var displayName = id.Replace('_', ' ');
        var sounds = Directory.EnumerateFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(file => SupportedExtensions.Contains(Path.GetExtension(file)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(file => new SoundItem(
                Path.GetFileName(file),
                Path.GetFileNameWithoutExtension(file)))
            .ToArray();

        return new PlaylistDefinition(id, displayName, directory, sounds);
    }
}
