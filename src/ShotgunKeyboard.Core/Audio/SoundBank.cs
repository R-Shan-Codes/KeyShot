using NAudio.Wave;

namespace ShotgunKeyboard.Core.Audio;

public sealed class SoundBank : IDisposable
{
    private readonly string[] _paths;

    public SoundBank(string directory, IEnumerable<string> fileNames)
    {
        if (!Directory.Exists(directory))
        {
            _paths = [];
            return;
        }

        _paths = fileNames
            .Select(fileName => Path.Combine(directory, fileName))
            .Where(File.Exists)
            .ToArray();
    }

    public int Count => _paths.Length;
    public IReadOnlyList<string> Files => _paths;

    public WaveStream CreateStream(int index)
    {
        if ((uint)index >= (uint)_paths.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new AudioFileReader(_paths[index]);
    }

    public void Dispose()
    {
        // AudioFileReader instances are owned and disposed by AudioEngine after playback.
    }
}
