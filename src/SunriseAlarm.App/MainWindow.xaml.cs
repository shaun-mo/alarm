using System.Windows;
using System.Windows.Threading;
using SunriseAlarm.Core;

namespace SunriseAlarm.App;

/// <summary>
/// The settings + arming surface. Owns a <see cref="DispatcherTimer"/> that ticks the
/// <see cref="AlarmScheduler"/> once a second and, once the ramp begins, drives a fullscreen
/// <see cref="SunriseWindow"/> for light plus a <see cref="SoundRamp"/> for sound. While an alarm
/// is armed it holds <see cref="StayAwake"/> so the PC doesn't sleep before wake time. The timing
/// logic lives in the Core library; this class is the thin UI shell over it.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AlarmScheduler _scheduler = new(new SystemClock());
    private readonly DispatcherTimer _timer;
    private readonly PowerGuard _powerGuard = new();
    private readonly SettingsStore _store = new(SettingsStore.DefaultPath);
    private readonly SoundRamp _sound;

    private Settings _settings;
    private SunriseWindow? _sunrise;
    private bool _brightnessEnabled = true;
    private bool _loudnessEnabled = true;
    private bool _soundStarted;
    private bool _soundPaused;

    public MainWindow()
    {
        InitializeComponent();

        _settings = _store.Load();
        _sound = new SoundRamp(new MediaAudioPlayer(), _settings.MaxVolume);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTick();

        // Reflect saved preferences in the UI.
        WakeTimeBox.Text = _settings.WakeTime.ToString("HH:mm");
        RampBox.Text = _settings.RampMinutes.ToString();
        BrightnessToggle.IsChecked = _settings.GradualBrightness;
        LoudnessToggle.IsChecked = _settings.GradualLoudness;

        // If we were armed when last closed (or the wake task launched us), re-arm immediately.
        if (_settings.Armed)
            ArmCore(_settings.WakeTime, _settings.RampMinutes,
                    _settings.GradualBrightness, _settings.GradualLoudness);
    }

    private void ArmButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TimeOnly.TryParse(WakeTimeBox.Text, out var wake))
        {
            StatusText.Text = "Enter a wake time as HH:mm (e.g. 07:00).";
            return;
        }
        if (!int.TryParse(RampBox.Text, out var rampMinutes) || rampMinutes <= 0)
        {
            StatusText.Text = "Ramp minutes must be a positive whole number.";
            return;
        }

        bool brightness = BrightnessToggle.IsChecked == true;
        bool loudness = LoudnessToggle.IsChecked == true;

        // Persist so the alarm survives a close/crash/cold-wake and re-arms on next launch.
        _settings = _settings with
        {
            WakeTime = wake,
            RampMinutes = rampMinutes,
            GradualBrightness = brightness,
            GradualLoudness = loudness,
            Armed = true,
        };
        _store.Save(_settings);

        ArmCore(wake, rampMinutes, brightness, loudness);
    }

    private void ArmCore(TimeOnly wake, int rampMinutes, bool brightness, bool loudness)
    {
        _brightnessEnabled = brightness;
        _loudnessEnabled = loudness;

        var now = DateTimeOffset.Now;
        var wakeTime = NextOccurrence(now, wake);
        var ramp = TimeSpan.FromMinutes(rampMinutes);
        _scheduler.Arm(wakeTime, ramp, TimeSpan.FromMinutes(_settings.SnoozeMinutes));

        // Primary reliability mechanism on Modern Standby hardware: don't let the PC sleep.
        StayAwake.KeepSystemAwake();

        var rampStart = wakeTime - ramp;
        bool wakeScheduled = _powerGuard.ScheduleWake(rampStart);

        _timer.Start();
        OnTick();

        string backup = wakeScheduled ? "wake-timer backup set" : "no wake-timer backup";
        StatusText.Text =
            $"Armed for {wakeTime:t}. Keeping this PC awake; ramp starts {rampStart:t} ({backup}).";
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSunrise();
        StayAwake.KeepSystemAndDisplayAwake();
        _sunrise?.SetProgress(1.0, 1.0);
        DriveSound(1.0);
    }

    private void OnTick()
    {
        var state = _scheduler.Tick();

        if (state.Phase is AlarmPhase.Ramping or AlarmPhase.Firing)
        {
            ShowSunrise();
            StayAwake.KeepSystemAndDisplayAwake();   // force the screen on for the sunrise

            double full = state.Phase == AlarmPhase.Firing ? 1.0 : 0.0;
            double brightness = _brightnessEnabled ? state.Progress : full;
            double volume = _loudnessEnabled ? state.Progress : full;

            _sunrise?.SetProgress(brightness, volume);
            DriveSound(volume);
        }
        else if (state.Phase is not AlarmPhase.Idle)
        {
            StatusText.Text = $"Armed. {state.Phase} · {state.Progress:P0}";
        }
    }

    /// <summary>Ensure the soundscape is playing (resuming from snooze if needed) and set its level.</summary>
    private void DriveSound(double volumeProgress)
    {
        if (!_soundStarted)
        {
            _sound.Start(_settings.Track);
            _soundStarted = true;
            _soundPaused = false;
        }
        else if (_soundPaused)
        {
            _sound.Resume();
            _soundPaused = false;
        }

        _sound.SetProgress(volumeProgress);
    }

    private void StopSound()
    {
        _sound.Stop();
        _soundStarted = false;
        _soundPaused = false;
    }

    private void ShowSunrise()
    {
        if (_sunrise is not null) return;

        _sunrise = new SunriseWindow();
        _sunrise.Snoozed += () =>
        {
            _scheduler.Snooze();
            _sound.Pause();
            _soundPaused = true;
            StayAwake.KeepSystemAwake();   // still armed; let the display sleep during snooze
            CloseSunrise();
        };
        _sunrise.Dismissed += () =>
        {
            _scheduler.Disarm();
            _timer.Stop();
            _powerGuard.CancelWake();
            StopSound();
            StayAwake.Release();
            _settings = _settings with { Armed = false };
            _store.Save(_settings);
            CloseSunrise();
            StatusText.Text = "Not armed.";
        };
        _sunrise.Closed += (_, _) => _sunrise = null;
        _sunrise.Show();
        _sunrise.Activate();
    }

    private void CloseSunrise()
    {
        _sunrise?.Close();
        _sunrise = null;
    }

    /// <summary>The next future moment matching <paramref name="time"/>, today or tomorrow.</summary>
    private static DateTimeOffset NextOccurrence(DateTimeOffset now, TimeOnly time)
    {
        var candidate = new DateTimeOffset(
            now.Year, now.Month, now.Day, time.Hour, time.Minute, 0, now.Offset);
        return candidate > now ? candidate : candidate.AddDays(1);
    }
}
