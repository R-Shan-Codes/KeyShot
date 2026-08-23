namespace ShotgunKeyboard.Core.Playlists;

public sealed record SoundItem(string FileName, string DisplayName)
{
    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();
}
