namespace SunriseAlarm.Core;

/// <summary>
/// The timing core of the alarm. Owns the armed wake time and ramp duration, and on every
/// <see cref="Tick"/> reports a single normalized <c>progress</c> value (0 before the ramp,
/// rising to 1 at wake time) plus the current <see cref="AlarmPhase"/>.
///
/// All time comes from an injected <see cref="IClock"/>, so the whole class is deterministic
/// and testable without real waits. It performs no UI, audio, or OS calls.
/// </summary>
public sealed class AlarmScheduler
{
    private const double Epsilon = 1e-9;

    private readonly IClock _clock;

    private DateTimeOffset _wakeTime;
    private TimeSpan _rampDuration;
    private TimeSpan _snoozeLength;
    private DateTimeOffset? _snoozeUntil;
    private AlarmPhase _phase = AlarmPhase.Idle;
    private bool _wakeAnnounced;
    private double _lastProgress = -1;

    /// <summary>Raised when the reported ramp progress changes.</summary>
    public event Action<double>? RampProgressChanged;

    /// <summary>Raised once each time the alarm transitions into <see cref="AlarmPhase.Firing"/>.</summary>
    public event Action? WakeTimeReached;

    public AlarmScheduler(IClock clock) =>
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public AlarmPhase Phase => _phase;
    public DateTimeOffset WakeTime => _wakeTime;

    /// <summary>The moment the gentle ramp begins (<see cref="WakeTime"/> minus the ramp duration).</summary>
    public DateTimeOffset RampStart => _wakeTime - _rampDuration;

    /// <summary>
    /// Arm the alarm for <paramref name="wakeTime"/>, ramping over <paramref name="rampDuration"/>,
    /// with <paramref name="snoozeLength"/> added by each snooze. Returns the immediate state.
    /// </summary>
    public AlarmState Arm(DateTimeOffset wakeTime, TimeSpan rampDuration, TimeSpan snoozeLength)
    {
        if (rampDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(rampDuration), "Ramp duration must be positive.");
        if (snoozeLength < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(snoozeLength), "Snooze length cannot be negative.");

        _wakeTime = wakeTime;
        _rampDuration = rampDuration;
        _snoozeLength = snoozeLength;
        _snoozeUntil = null;
        _wakeAnnounced = false;
        _lastProgress = -1;
        _phase = AlarmPhase.Armed;
        return Tick();
    }

    /// <summary>Cancel the alarm entirely and return to <see cref="AlarmPhase.Idle"/>.</summary>
    public void Disarm()
    {
        _phase = AlarmPhase.Idle;
        _snoozeUntil = null;
        _wakeAnnounced = false;
        _lastProgress = -1;
    }

    /// <summary>
    /// Silence the alarm and re-fire after the snooze length. No-op when idle.
    /// </summary>
    public void Snooze()
    {
        if (_phase is AlarmPhase.Idle) return;
        _snoozeUntil = _clock.Now + _snoozeLength;
        _wakeAnnounced = false;
        _phase = AlarmPhase.Snoozed;
        SetProgress(0);
    }

    /// <summary>
    /// Advance the alarm to the current time and return the resulting state.
    /// Call this on a UI timer (e.g. once a second); it is idempotent for a given clock value.
    /// </summary>
    public AlarmState Tick()
    {
        var now = _clock.Now;

        switch (_phase)
        {
            case AlarmPhase.Idle:
                return new AlarmState(AlarmPhase.Idle, 0);

            case AlarmPhase.Snoozed:
                if (_snoozeUntil is { } until && now < until)
                {
                    SetProgress(0);
                    return new AlarmState(AlarmPhase.Snoozed, 0);
                }
                return Fire();

            default:
                if (now >= _wakeTime)
                    return Fire();

                if (now < RampStart)
                {
                    _phase = AlarmPhase.Armed;
                    SetProgress(0);
                    return new AlarmState(AlarmPhase.Armed, 0);
                }

                _phase = AlarmPhase.Ramping;
                double progress = Math.Clamp((now - RampStart) / _rampDuration, 0.0, 1.0);
                SetProgress(progress);
                return new AlarmState(AlarmPhase.Ramping, progress);
        }
    }

    private AlarmState Fire()
    {
        _phase = AlarmPhase.Firing;
        SetProgress(1.0);
        if (!_wakeAnnounced)
        {
            _wakeAnnounced = true;
            WakeTimeReached?.Invoke();
        }
        return new AlarmState(AlarmPhase.Firing, 1.0);
    }

    private void SetProgress(double progress)
    {
        if (Math.Abs(progress - _lastProgress) <= Epsilon) return;
        _lastProgress = progress;
        RampProgressChanged?.Invoke(progress);
    }
}
