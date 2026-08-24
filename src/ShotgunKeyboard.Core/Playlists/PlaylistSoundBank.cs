using NAudio.Wave;
using ShotgunKeyboard.Core.Audio;

namespace ShotgunKeyboard.Core.Playlists;

/// <summary>
/// Loads every supported sound in a playlist and provides randomized streams.
/// </summary>
public sealed class PlaylistSoundBank : IDisposable
{
    private readonly SoundBank _bank;
    private readonly SoundVariationSelector _selector = new();

    public PlaylistSoundBank(PlaylistDefinition playlist)
    {
        var files = playlist.Sounds.Select(sound => sound.FileName).ToArray();
        _bank = new SoundBank(playlist.Directory, files);
    }

    public int Count => _bank.Count;

    public WaveStream CreateNextStream()
    {
        if (_bank.Count == 0)
        {
            throw new InvalidOperationException("The playlist contains no supported audio files.");
        }

        return _bank.CreateStream(_selector.Select(_bank.Count));
    }

    public WaveStream CreateStreamForKey(uint virtualKeyCode)
    {
        if (_bank.Count == 0)
        {
            throw new InvalidOperationException("The playlist contains no supported audio files.");
        }

        // Deterministic key mapping: the same key always selects the same
        // playlist sound. This is intentionally stable across launches.
        var index = (int)(virtualKeyCode % (uint)_bank.Count);
        return _bank.CreateStream(index);
    }

    public void Dispose() => _bank.Dispose();
}
