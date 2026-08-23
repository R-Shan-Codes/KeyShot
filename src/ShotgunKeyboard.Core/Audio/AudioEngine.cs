using NAudio.Wave;

namespace ShotgunKeyboard.Core.Audio;

public sealed class AudioEngine : IDisposable
{
    private const int MaxSimultaneousShots = 8;
    private const string HeavySound = "shot_1.mp3";
    private static readonly string[] NormalSounds = ["shot_2.mp3", "shot_3.mp3", "shot_4.mp3", "shot_5.mp3"];

    private readonly WaveFormat _fallbackFormat;
    private readonly byte[] _fallbackSoundData;
    private readonly SoundBank? _normalSoundBank;
    private readonly SoundBank? _heavySoundBank;
    private readonly SoundVariationSelector _normalVariationSelector = new();
    private readonly object _sync = new();
    private readonly List<IWavePlayer> _players = [];
    private bool _disposed;

    public AudioEngine(string? soundDirectory = null, int sampleRate = 44100)
    {
        _fallbackFormat = new WaveFormat(sampleRate, 16, 1);
        _fallbackSoundData = ShotgunTestSoundGenerator.CreatePcm16Mono(sampleRate);

        if (!string.IsNullOrWhiteSpace(soundDirectory))
        {
            var normalBank = new SoundBank(soundDirectory, NormalSounds);
            _normalSoundBank = normalBank.Count > 0 ? normalBank : null;
            if (_normalSoundBank is null)
            {
                normalBank.Dispose();
            }

            var heavyBank = new SoundBank(soundDirectory, [HeavySound]);
            _heavySoundBank = heavyBank.Count > 0 ? heavyBank : null;
            if (_heavySoundBank is null)
            {
                heavyBank.Dispose();
            }
        }
    }

    public int NormalSoundVariationCount => _normalSoundBank?.Count ?? 1;

    public void PlayShot(bool heavy = false)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            CleanupFinishedPlayers();

            if (_players.Count >= MaxSimultaneousShots)
            {
                return;
            }

            var stream = CreateNextStream(heavy);
            var player = new WaveOutEvent { Volume = heavy ? 0.9f : 0.78f };

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

    private WaveStream CreateNextStream(bool heavy)
    {
        if (heavy && _heavySoundBank is not null)
        {
            return _heavySoundBank.CreateStream(0);
        }

        if (!heavy && _normalSoundBank is not null)
        {
            var index = _normalVariationSelector.Select(_normalSoundBank.Count);
            return _normalSoundBank.CreateStream(index);
        }

        return new RawSourceWaveStream(
            new MemoryStream(_fallbackSoundData, writable: false),
            _fallbackFormat);
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

        _normalSoundBank?.Dispose();
        _heavySoundBank?.Dispose();
        GC.SuppressFinalize(this);
    }
}
