namespace ShotgunKeyboard.Core.Playlists;

public sealed class PlaylistDefinition
{
    public PlaylistDefinition(string id, string displayName, string directory, IReadOnlyList<SoundItem> sounds)
    {
        Id = id;
        DisplayName = displayName;
        Directory = directory;
        Sounds = sounds;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Directory { get; }
    public IReadOnlyList<SoundItem> Sounds { get; }
}
