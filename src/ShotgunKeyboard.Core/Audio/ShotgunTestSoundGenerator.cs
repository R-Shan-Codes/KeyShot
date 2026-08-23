using System;

namespace ShotgunKeyboard.Core.Audio;

/// <summary>
/// Generates a short procedural placeholder while the real shotgun recordings are being selected.
/// This keeps Phase 2 independent from copyrighted audio assets.
/// </summary>
public static class ShotgunTestSoundGenerator
{
    public static byte[] CreatePcm16Mono(int sampleRate = 44100)
    {
        const double durationSeconds = 0.18;
        var sampleCount = (int)(sampleRate * durationSeconds);
        var bytes = new byte[sampleCount * sizeof(short)];
        var random = new Random(42);

        for (var i = 0; i < sampleCount; i++)
        {
            var time = i / (double)sampleRate;
            var envelope = Math.Exp(-time * 32.0);
            var lowBody = Math.Sin(2.0 * Math.PI * 85.0 * time) * 0.55;
            var crack = Math.Sin(2.0 * Math.PI * 1850.0 * time) * 0.15;
            var noise = (random.NextDouble() * 2.0 - 1.0) * 0.75 * Math.Exp(-time * 70.0);
            var sample = Math.Clamp((lowBody + crack + noise) * envelope, -1.0, 1.0);
            var pcm = (short)(sample * short.MaxValue * 0.72);

            bytes[i * 2] = (byte)(pcm & 0xFF);
            bytes[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
        }

        return bytes;
    }
}
