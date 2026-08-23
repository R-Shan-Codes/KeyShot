using System;
using NAudio.Wave;

namespace ShotgunKeyboard.Core.Audio;

public sealed class AudioEngine : IDisposable
{
    private const int MaxSimultaneousShots = 8;

    private readonly WaveFormat _format;
    private readonly byte[] _soundData;
    private readonly object _sync = new();
    private readonly List<IWavePlayer> _players = [];
    private bool _disposed;

    public AudioEngine(int sampleRate = 44100)
    {
        _format = new WaveFormat(sampleRate, 16, 1);
        _soundData = ShotgunTestSoundGenerator.CreatePcm16Mono(sampleRate);
    }

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

            var waveStream = new RawSourceWaveStream(
                new MemoryStream(_soundData, writable: false),
                _format);
            var player = new WaveOutEvent { Volume = 0.85f };

            player.Init(waveStream);
            player.PlaybackStopped += (_, _) =>
            {
                player.Dispose();
                waveStream.Dispose();

                lock (_sync)
                {
                    _players.Remove(player);
                }
            };

            _players.Add(player);
            player.Play();
        }
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

        GC.SuppressFinalize(this);
    }
}
