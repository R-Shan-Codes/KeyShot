namespace ShotgunKeyboard.Core.Playlists;

public sealed class ActivePlaylist : IDisposable
{
    private PlaylistSoundBank? _bank;

    public PlaylistDefinition? Definition { get; private set; }
    public int SoundCount => _bank?.Count ?? 0;

    public void Set(PlaylistDefinition playlist)
    {
        var next = new PlaylistSoundBank(playlist);
        _bank?.Dispose();
        _bank = next;
        Definition = playlist;
    }

    public bool TryCreateNextStream(out NAudio.Wave.WaveStream? stream)
    {
        if (_bank is null || _bank.Count == 0)
        {
            stream = null;
            return false;
        }

        stream = _bank.CreateNextStream();
        return true;
    }

    public void Dispose()
    {
        _bank?.Dispose();
        _bank = null;
        Definition = null;
    }
}
