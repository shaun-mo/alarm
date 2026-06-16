namespace SunriseAlarm.Core;

/// <summary>
/// All user-configurable preferences for the alarm. Plain immutable record so it round-trips
/// cleanly through <see cref="SettingsStore"/> as JSON. Defaults represent a sensible first run.
/// </summary>
public sealed record Settings
{
    /// <summary>Target wake time, in the PC's local time.</summary>
    public TimeOnly WakeTime { get; init; } = new(7, 0);

    /// <summary>How long the light/sound ramp lasts before <see cref="WakeTime"/>.</summary>
    public int RampMinutes { get; init; } = 30;

    /// <summary>When false, the screen jumps to full warm light at wake time instead of ramping.</summary>
    public bool GradualBrightness { get; init; } = true;

    /// <summary>When false, the sound jumps to full volume at wake time instead of ramping.</summary>
    public bool GradualLoudness { get; init; } = true;

    /// <summary>Upper bound on screen brightness, 0..1.</summary>
    public double MaxBrightness { get; init; } = 1.0;

    /// <summary>Upper bound on playback volume, 0..1.</summary>
    public double MaxVolume { get; init; } = 0.8;

    /// <summary>Identifier of the selected bundled soundscape (e.g. "ocean", "rain").</summary>
    public string Track { get; init; } = "ocean";

    /// <summary>Minutes added by each snooze.</summary>
    public int SnoozeMinutes { get; init; } = 9;

    /// <summary>Whether the app launches with Windows.</summary>
    public bool LaunchOnStartup { get; init; } = false;
}
