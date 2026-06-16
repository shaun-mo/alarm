namespace SunriseAlarm.Core;

/// <summary>Pure mapping from ramp progress (0..1) to playback volume (0..1).</summary>
public static class VolumeCurve
{
    /// <summary>
    /// Eases in quadratically so the sound starts almost inaudible and rises gently —
    /// matching how the sunrise light grows — and is capped at <paramref name="maxVolume"/>.
    /// </summary>
    public static double VolumeFor(double progress, double maxVolume = 1.0)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);
        maxVolume = Math.Clamp(maxVolume, 0.0, 1.0);
        return progress * progress * maxVolume;
    }
}
