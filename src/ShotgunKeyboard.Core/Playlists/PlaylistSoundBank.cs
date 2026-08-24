using NAudio.Wave;
using ShotgunKeyboard.Core.Audio;

namespace ShotgunKeyboard.Core.Playlists;

public sealed class PlaylistSoundBank : IDisposable
{
    private readonly SoundBank _bank;
    private readonly string[] _files;
    private readonly SoundVariationSelector _selector = new();

    public PlaylistSoundBank(PlaylistDefinition playlist)
    {
        _files = playlist.Sounds
            .Select(sound => sound.FileName)
            .Where(file => File.Exists(Path.Combine(playlist.Directory, file)))
            .ToArray();

        _bank = new SoundBank(playlist.Directory, _files);
    }

    public int Count => _bank.Count;

    public WaveStream CreateNextStream()
    {
        if (_bank.Count == 0)
            throw new InvalidOperationException("The playlist contains no supported audio files.");

        return _bank.CreateStream(_selector.Select(_bank.Count));
    }

    public WaveStream CreateStreamForSound(string fileName)
    {
        if (_bank.Count == 0)
            throw new InvalidOperationException("The playlist contains no supported audio files.");

        var index = Array.FindIndex(_files, file =>
            string.Equals(file, fileName, StringComparison.OrdinalIgnoreCase));

        return index >= 0 ? _bank.CreateStream(index) : CreateNextStream();
    }

    public WaveStream CreateStreamForKey(uint virtualKeyCode)
    {
        if (_bank.Count == 0)
            throw new InvalidOperationException("The playlist contains no supported audio files.");

        var index = (int)(virtualKeyCode % (uint)_bank.Count);
        return _bank.CreateStream(index);
    }

    public void Dispose() => _bank.Dispose();
}
