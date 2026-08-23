using NAudio.Wave;

namespace ShotgunKeyboard.Core.Audio;

public sealed class SoundBank : IDisposable
{
    private readonly List<byte[]> _sounds = [];
    private readonly WaveFormat _format;

    public SoundBank(string directory, int sampleRate = 44100)
    {
        _format = new WaveFormat(sampleRate, 16, 1);

        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var path in Directory.EnumerateFiles(directory, "*.wav", SearchOption.TopDirectoryOnly).OrderBy(x => x))
        {
            using var reader = new WaveFileReader(path);
            if (reader.WaveFormat.Encoding != WaveFormatEncoding.Pcm ||
                reader.WaveFormat.BitsPerSample != 16 ||
                reader.WaveFormat.Channels != 1 ||
                reader.WaveFormat.SampleRate != sampleRate)
            {
                throw new InvalidDataException($"Sound '{Path.GetFileName(path)}' must be PCM 16-bit mono WAV at {sampleRate} Hz.");
            }

            using var stream = new MemoryStream();
            reader.CopyTo(stream);
            _sounds.Add(stream.ToArray());
        }
    }

    public int Count => _sounds.Count;

    public WaveStream CreateStream(int index)
    {
        if ((uint)index >= (uint)_sounds.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new RawSourceWaveStream(
            new MemoryStream(_sounds[index], writable: false),
            _format);
    }

    public void Dispose()
    {
        _sounds.Clear();
    }
}
