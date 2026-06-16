namespace SunriseAlarm.Core;

/// <summary>
/// Minimal playback surface the <see cref="SoundRamp"/> drives. The WPF app supplies a real
/// implementation (e.g. over MediaPlayer / NAudio); tests supply a fake.
/// </summary>
public interface IAudioPlayer
{
    /// <summary>Current output volume, 0..1.</summary>
    double Volume { get; set; }

    void Load(string trackId);
    void Play();
    void Pause();
    void Resume();
    void Stop();
}
