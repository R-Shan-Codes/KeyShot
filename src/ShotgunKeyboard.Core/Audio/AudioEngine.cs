using NAudio.Wave;

namespace ShotgunKeyboard.Core.Audio;

public sealed class AudioEngine : IDisposable
{
    private const int MaxSimultaneousShots = 8;

    private readonly WaveFormat _format;
    private readonly byte[] _fallbackSoundData;
    private readonly SoundBank? _soundBank;
    private readonly SoundVariationSelector _variationSelector = new();
    private readonly object _sync = new();
    private readonly List<IWavePlayer> _players = [];
    private bool _disposed;

    public AudioEngine(string? soundDirectory = null, int sampleRate = 44100)
    {
        _format = new WaveFormat(sampleRate, 16, 1);
        _fallbackSoundData = ShotgunTestSoundGenerator.CreatePcm16Mono(sampleRate);

        if (!string.IsNullOrWhiteSpace(soundDirectory))
        {
            var bank = new SoundBank(soundDirectory, sampleRate);
            _soundBank = bank.Count > 0 ? bank : null;
            if (_soundBank is null)
            {
                bank.Dispose();
            }
        }
    }

    public int SoundVariationCount => _soundBank?.Count ?? 1;

    public void PlayShot()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            CleanupFinishedPlayers();

            if (_players.Count >= MaxSimultaneousShots)
            {
                return;
            }

            var stream = CreateNextStream();
            var player = new WaveOutEvent { Volume = 0.85f };

            player.Init(stream);
            player.PlaybackStopped += (_, _) =>
            {
                stream.Dispose();
                player.Dispose();

                lock (_sync)
                {
                    _players.Remove(player);
                }
            };

            _players.Add(player);
            player.Play();
        }
    }

    private WaveStream CreateNextStream()
    {
        if (_soundBank is not null)
        {
            var index = _variationSelector.Select(_soundBank.Count);
            return _soundBank.CreateStream(index);
        }

        return new RawSourceWaveStream(
            new MemoryStream(_fallbackSoundData, writable: false),
            _format);
    }

    private void CleanupFinishedPlayers()
    {
        for (var i = _players.Count - 1; i >= 0; i--)
        {
            if (_players[i].PlaybackState == PlaybackState.Stopped)
            {
                _players[i].Dispose();
                _players.RemoveAt(i);
            }
        }
    }

    public void Dispose()
    {
        IWavePlayer[] players;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            players = [.. _players];
            _players.Clear();
        }

        foreach (var player in players)
        {
            player.Stop();
            player.Dispose();
        }

        _soundBank?.Dispose();
        GC.SuppressFinalize(this);
    }
}
