using SunriseAlarm.Core;
using Xunit;

namespace SunriseAlarm.Core.Tests;

public class AlarmSchedulerTests
{
    private static readonly DateTimeOffset Wake =
        new(2026, 6, 16, 7, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan Ramp = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan Snooze = TimeSpan.FromMinutes(9);

    private static (AlarmScheduler scheduler, FakeClock clock) Armed(DateTimeOffset now)
    {
        var clock = new FakeClock(now);
        var scheduler = new AlarmScheduler(clock);
        scheduler.Arm(Wake, Ramp, Snooze);
        return (scheduler, clock);
    }

    [Fact]
    public void Before_ramp_window_is_armed_at_zero_progress()
    {
        var (scheduler, _) = Armed(Wake - TimeSpan.FromHours(1));
        var state = scheduler.Tick();

        Assert.Equal(AlarmPhase.Armed, state.Phase);
        Assert.Equal(0, state.Progress);
    }

    [Fact]
    public void At_ramp_start_progress_is_zero_and_phase_is_ramping()
    {
        var (scheduler, _) = Armed(Wake - Ramp);
        var state = scheduler.Tick();

        Assert.Equal(AlarmPhase.Ramping, state.Phase);
        Assert.Equal(0, state.Progress, precision: 6);
    }

    [Fact]
    public void Halfway_through_ramp_progress_is_one_half()
    {
        var (scheduler, _) = Armed(Wake - TimeSpan.FromMinutes(15));
        var state = scheduler.Tick();

        Assert.Equal(AlarmPhase.Ramping, state.Phase);
        Assert.Equal(0.5, state.Progress, precision: 6);
    }

    [Fact]
    public void Progress_increases_monotonically_across_the_window()
    {
        var (scheduler, clock) = Armed(Wake - Ramp);
        double previous = -1;

        for (int minute = 0; minute <= 30; minute++)
        {
            var state = scheduler.Tick();
            Assert.True(state.Progress >= previous,
                $"progress dropped at minute {minute}: {state.Progress} < {previous}");
            previous = state.Progress;
            clock.Advance(TimeSpan.FromMinutes(1));
        }
    }

    [Fact]
    public void At_wake_time_it_fires_at_full_progress()
    {
        var (scheduler, _) = Armed(Wake);
        var state = scheduler.Tick();

        Assert.Equal(AlarmPhase.Firing, state.Phase);
        Assert.Equal(1.0, state.Progress);
    }

    [Fact]
    public void WakeTimeReached_fires_exactly_once_even_across_many_ticks()
    {
        var (scheduler, clock) = Armed(Wake - TimeSpan.FromMinutes(1));
        int count = 0;
        scheduler.WakeTimeReached += () => count++;

        clock.Set(Wake);
        scheduler.Tick();
        clock.Advance(TimeSpan.FromMinutes(5));
        scheduler.Tick();
        scheduler.Tick();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Disarm_returns_to_idle()
    {
        var (scheduler, _) = Armed(Wake - TimeSpan.FromMinutes(10));
        scheduler.Disarm();

        var state = scheduler.Tick();
        Assert.Equal(AlarmPhase.Idle, state.Phase);
        Assert.Equal(0, state.Progress);
    }

    [Fact]
    public void Snooze_silences_then_refires_after_the_snooze_length()
    {
        var (scheduler, clock) = Armed(Wake);
        Assert.Equal(AlarmPhase.Firing, scheduler.Tick().Phase);

        scheduler.Snooze();
        Assert.Equal(AlarmPhase.Snoozed, scheduler.Tick().Phase);

        clock.Advance(Snooze - TimeSpan.FromSeconds(1));
        Assert.Equal(AlarmPhase.Snoozed, scheduler.Tick().Phase);

        clock.Advance(TimeSpan.FromSeconds(2));
        var state = scheduler.Tick();
        Assert.Equal(AlarmPhase.Firing, state.Phase);
        Assert.Equal(1.0, state.Progress);
    }

    [Fact]
    public void Arm_with_nonpositive_ramp_throws()
    {
        var scheduler = new AlarmScheduler(new FakeClock(Wake));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => scheduler.Arm(Wake, TimeSpan.Zero, Snooze));
    }

    [Fact]
    public void RampProgressChanged_reports_rising_values_during_the_ramp()
    {
        var (scheduler, clock) = Armed(Wake - Ramp);
        var seen = new List<double>();
        scheduler.RampProgressChanged += seen.Add;

        for (int minute = 0; minute <= 30; minute++)
        {
            scheduler.Tick();
            clock.Advance(TimeSpan.FromMinutes(1));
        }

        Assert.NotEmpty(seen);
        Assert.Equal(seen.OrderBy(v => v), seen);
        Assert.Equal(1.0, seen[^1]);
    }
}
