namespace SunriseAlarm.Core;

/// <summary>The lifecycle phase of the alarm.</summary>
public enum AlarmPhase
{
    /// <summary>No alarm armed.</summary>
    Idle,

    /// <summary>Armed and waiting; the ramp window has not started yet.</summary>
    Armed,

    /// <summary>Inside the ramp window; light and sound are rising toward wake time.</summary>
    Ramping,

    /// <summary>Wake time reached; full intensity, awaiting snooze or dismiss.</summary>
    Firing,

    /// <summary>Temporarily silenced after a snooze; will re-fire when the snooze elapses.</summary>
    Snoozed
}

/// <summary>
/// An immutable snapshot of the alarm at a moment in time.
/// <paramref name="Progress"/> is a normalized 0..1 value driving both light and sound.
/// </summary>
public readonly record struct AlarmState(AlarmPhase Phase, double Progress);
