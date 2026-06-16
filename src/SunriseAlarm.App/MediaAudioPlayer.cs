using System.IO;
using System.Windows.Media;
using SunriseAlarm.Core;

namespace SunriseAlarm.App;

/// <summary>
/// Real <see cref="IAudioPlayer"/> backed by WPF's <see cref="MediaPlayer"/>. Loops the loaded
/// track and resolves track ids to the bundled WAVs under <c>Assets/</c>. Must be constructed and
/// used on the UI thread (that's where <see cref="MediaPlayer"/> raises its events).
/// </summary>
public sealed class MediaAudioPlayer : IAudioPlayer
{
    private readonly MediaPlayer _player = new();
    private bool _loop;

    public MediaAudioPlayer()
    {
        _player.MediaEnded += (_, _) =>
        {
            if (!_loop) return;
            _player.Position = TimeSpan.Zero;
            _player.Play();
        };
    }

    public double Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0.0, 1.0);
    }

    public void Load(string trackId)
    {
        var path = ResolveTrack(trackId);
        if (path is not null)
            _player.Open(new Uri(path, UriKind.Absolute));
    }

    public void Play()
    {
        _loop = true;
        _player.Play();
    }

    public void Pause() => _player.Pause();

    public void Resume()
    {
        _loop = true;
        _player.Play();
    }

    public void Stop()
    {
        _loop = false;
        _player.Stop();
    }

    /// <summary>Find <c>Assets/{trackId}.wav</c>, falling back to any bundled WAV.</summary>
    private static string? ResolveTrack(string trackId)
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "Assets");
        var exact = Path.Combine(dir, trackId + ".wav");
        if (File.Exists(exact)) return exact;
        if (Directory.Exists(dir))
            return Directory.GetFiles(dir, "*.wav").FirstOrDefault();
        return null;
    }
}
