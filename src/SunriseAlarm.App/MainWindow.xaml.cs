using System.Windows;
using System.Windows.Threading;
using SunriseAlarm.Core;

namespace SunriseAlarm.App;

/// <summary>
/// The settings + arming surface. Owns a <see cref="DispatcherTimer"/> that ticks the
/// <see cref="AlarmScheduler"/> once a second and, once the ramp begins, drives a fullscreen
/// <see cref="SunriseWindow"/> for light plus a <see cref="SoundRamp"/> for sound. The interesting
/// timing logic lives in the Core library; this class is the thin UI shell over it.
/// </summary>
public partial class MainWindow : Window
{
    private readonly AlarmScheduler _scheduler = new(new SystemClock());
    private readonly DispatcherTimer _timer;
    private readonly PowerGuard _powerGuard = new();

    private readonly Settings _settings = new SettingsStore(SettingsStore.DefaultPath).Load();
    private readonly SoundRamp _sound;

    private SunriseWindow? _sunrise;
    private bool _brightnessEnabled = true;
    private bool _loudnessEnabled = true;
    private bool _soundStarted;
    private bool _soundPaused;

    public MainWindow()
    {
        InitializeComponent();
        _sound = new SoundRamp(new MediaAudioPlayer(), _settings.MaxVolume);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => OnTick();
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

        _brightnessEnabled = BrightnessToggle.IsChecked == true;
        _loudnessEnabled = LoudnessToggle.IsChecked == true;

        var now = DateTimeOffset.Now;
        var wakeTime = NextOccurrence(now, wake);
        var ramp = TimeSpan.FromMinutes(rampMinutes);
        _scheduler.Arm(wakeTime, ramp, TimeSpan.FromMinutes(_settings.SnoozeMinutes));

        var rampStart = wakeTime - ramp;
        bool wakeScheduled = _powerGuard.ScheduleWake(rampStart);

        _timer.Start();
        StatusText.Text = wakeScheduled
            ? $"Armed for {wakeTime:t}. Ramp starts {rampStart:t}."
            : $"Armed for {wakeTime:t}. WARNING: couldn't set a wake timer — keep this PC awake.";
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSunrise();
        _sunrise?.SetProgress(1.0, 1.0);
        DriveSound(1.0);
    }

    private void OnTick()
    {
        var state = _scheduler.Tick();
        StatusText.Text = $"{state.Phase} · {state.Progress:P0}";

        if (state.Phase is AlarmPhase.Ramping or AlarmPhase.Firing)
        {
            ShowSunrise();
            double full = state.Phase == AlarmPhase.Firing ? 1.0 : 0.0;
            double brightness = _brightnessEnabled ? state.Progress : full;
            double volume = _loudnessEnabled ? state.Progress : full;

            _sunrise?.SetProgress(brightness, volume);
            DriveSound(volume);
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
            CloseSunrise();
        };
        _sunrise.Dismissed += () =>
        {
            _scheduler.Disarm();
            _timer.Stop();
            _powerGuard.CancelWake();
            StopSound();
            CloseSunrise();
            StatusText.Text = "Not armed.";
        };
        _sunrise.Closed += (_, _) => _sunrise = null;
        _sunrise.Show();
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
