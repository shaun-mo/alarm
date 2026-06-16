namespace SunriseAlarm.Core;

/// <summary>
/// Drives an <see cref="IAudioPlayer"/>'s volume from ramp progress via <see cref="VolumeCurve"/>,
/// honoring a user-configurable <see cref="MaxVolume"/> cap. The curve math is pure and testable;
/// this thin wrapper just forwards to the player.
/// </summary>
public sealed class SoundRamp
{
    private readonly IAudioPlayer _player;
    private double _maxVolume;

    public SoundRamp(IAudioPlayer player, double maxVolume = 1.0)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
        _maxVolume = Math.Clamp(maxVolume, 0.0, 1.0);
    }

    public double MaxVolume
    {
        get => _maxVolume;
        set => _maxVolume = Math.Clamp(value, 0.0, 1.0);
    }

    /// <summary>Load <paramref name="trackId"/>, start it silent, and begin playback.</summary>
    public void Start(string trackId)
    {
        _player.Load(trackId);
        _player.Volume = 0;
        _player.Play();
    }

    /// <summary>Update the player volume for the given ramp progress (0..1).</summary>
    public void SetProgress(double progress) =>
        _player.Volume = VolumeCurve.VolumeFor(progress, _maxVolume);

    public void Pause() => _player.Pause();
    public void Resume() => _player.Resume();
    public void Stop() => _player.Stop();
}
