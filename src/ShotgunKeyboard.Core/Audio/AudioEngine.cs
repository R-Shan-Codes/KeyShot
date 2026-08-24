using NAudio.Wave;
using ShotgunKeyboard.Core.Playlists;
using ShotgunKeyboard.Core.Weapons;

namespace ShotgunKeyboard.Core.Audio;

public enum PlaybackMode
{
    Random,
    Selected
}

public sealed class AudioEngine : IDisposable
{
    private const int MaxSimultaneousShots = 8;
    private const long MinimumNormalShotIntervalTicks = TimeSpan.TicksPerMillisecond * 20;

    private readonly WaveFormat _fallbackFormat;
    private readonly byte[] _fallbackSoundData;
    private readonly SoundBank? _heavySoundBank;
    private readonly object _sync = new();
    private readonly List<IWavePlayer> _players = [];
    private readonly string? _soundDirectory;
    private long _lastNormalShotTicks;
    private bool _disposed;
    private bool _enabled = true;
    private float _masterVolume = 0.78f;
    private WeaponDefinition _weapon;
    private PlaylistSoundBank? _playlistSoundBank;

    public AudioEngine(string? soundDirectory = null, int sampleRate = 44100, WeaponDefinition? weapon = null, PlaylistDefinition? playlist = null)
    {
        _fallbackFormat = new WaveFormat(sampleRate, 16, 1);
        _fallbackSoundData = ShotgunTestSoundGenerator.CreatePcm16Mono(sampleRate);
        _soundDirectory = soundDirectory;
        _weapon = weapon ?? WeaponCatalog.Get("shotgun");

        if (playlist is not null)
        {
            SetPlaylist(playlist);
        }

        if (!string.IsNullOrWhiteSpace(_soundDirectory))
        {
            var heavyFile = ResolvePreferredFileName(_weapon.HeavySounds);
            if (heavyFile is not null)
            {
                var heavyBank = new SoundBank(_soundDirectory, [heavyFile]);
                _heavySoundBank = heavyBank.Count > 0 ? heavyBank : null;
                if (_heavySoundBank is null)
                {
                    heavyBank.Dispose();
                }
            }
        }
    }

    public WeaponDefinition Weapon => _weapon;
    public int NormalSoundVariationCount => _playlistSoundBank?.Count ?? 1;
    public PlaybackMode Mode { get; set; } = PlaybackMode.Random;

    public void SetPlaylist(PlaylistDefinition playlist)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var next = new PlaylistSoundBank(playlist);
            var previous = _playlistSoundBank;
            _playlistSoundBank = next.Count > 0 ? next : null;
            previous?.Dispose();

            if (_playlistSoundBank is null)
            {
                next.Dispose();
            }
        }
    }

    public bool Enabled
    {
        get => _enabled;
        set
        {
            lock (_sync)
            {
                _enabled = value;
                if (!_enabled)
                {
                    StopAllPlayers();
                }
            }
        }
    }

    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            var clamped = Math.Clamp(value, 0f, 1f);
            lock (_sync)
            {
                _masterVolume = clamped;
                foreach (var player in _players.OfType<WaveOutEvent>())
                {
                    player.Volume = clamped;
                }
            }
        }
    }

    public void PlayShot(bool heavy = false, string? selectedSound = null)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (!_enabled)
            {
                return;
            }

            var now = DateTime.UtcNow.Ticks;
            if (!heavy && now - _lastNormalShotTicks < MinimumNormalShotIntervalTicks)
            {
                return;
            }

            if (!heavy)
            {
                _lastNormalShotTicks = now;
            }

            CleanupFinishedPlayers();
            if (_players.Count >= MaxSimultaneousShots)
            {
                return;
            }

            var stream = CreateNextStream(heavy, selectedSound);
            var player = new WaveOutEvent
            {
                Volume = Math.Clamp(_masterVolume * _weapon.VolumeMultiplier * (heavy ? 1.15f : 1f), 0f, 1f)
            };

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

    private WaveStream CreateNextStream(bool heavy, string? selectedSound)
    {
        if (Mode == PlaybackMode.Selected && !string.IsNullOrWhiteSpace(selectedSound) && _playlistSoundBank is not null)
        {
            return _playlistSoundBank.CreateStreamForSound(selectedSound);
        }

        if (heavy && _heavySoundBank is not null)
        {
            return _heavySoundBank.CreateStream(0);
        }

        if (_playlistSoundBank is not null)
        {
            return _playlistSoundBank.CreateNextStream();
        }

        return new RawSourceWaveStream(
            new MemoryStream(_fallbackSoundData, writable: false),
            _fallbackFormat);
    }

    private string? ResolvePreferredFileName(IEnumerable<string> candidates)
    {
        if (string.IsNullOrWhiteSpace(_soundDirectory))
        {
            return null;
        }

        return candidates.FirstOrDefault(fileName => File.Exists(Path.Combine(_soundDirectory, fileName)));
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

    private void StopAllPlayers()
    {
        var players = _players.ToArray();
        foreach (var player in players)
        {
            player.Stop();
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

        _playlistSoundBank?.Dispose();
        _heavySoundBank?.Dispose();
        GC.SuppressFinalize(this);
    }
}
