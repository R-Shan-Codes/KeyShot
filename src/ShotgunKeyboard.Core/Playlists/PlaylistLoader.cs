using System.Text.Json;

namespace ShotgunKeyboard.Core.Playlists;

public sealed record PlaylistSound(string FileName, string DisplayName);

public sealed record PlaylistDefinition(string Id, string DisplayName, string DirectoryPath, IReadOnlyList<PlaylistSound> Sounds);

public sealed class PlaylistLoader
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav", ".mp3"
    };

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public IReadOnlyList<PlaylistDefinition> Load(string playlistsDirectory)
    {
        Directory.CreateDirectory(playlistsDirectory);

        return Directory.EnumerateDirectories(playlistsDirectory)
            .Select(path => LoadPlaylist(path))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static PlaylistDefinition LoadPlaylist(string directory)
    {
        var manifestPath = Path.Combine(directory, "playlist.json");
        var displayName = Path.GetFileName(directory);
        var id = SanitizeId(displayName);

        if (File.Exists(manifestPath))
        {
            try
            {
                var manifest = JsonSerializer.Deserialize<PlaylistManifest>(File.ReadAllText(manifestPath), JsonOptions);
                if (!string.IsNullOrWhiteSpace(manifest?.Name))
                {
                    displayName = manifest.Name;
                }

                if (!string.IsNullOrWhiteSpace(manifest?.Id))
                {
                    id = manifest.Id;
                }
            }
            catch
            {
                // A malformed manifest should not prevent the remaining playlists from loading.
            }
        }

        var sounds = Directory.EnumerateFiles(directory)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path)))
            .Select(path => new PlaylistSound(Path.GetFileName(path), Path.GetFileNameWithoutExtension(path)))
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PlaylistDefinition(id, displayName, directory, sounds);
    }

    private static string SanitizeId(string value)
        => string.Concat(value.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');

    private sealed record PlaylistManifest(string? Id, string? Name);
}
