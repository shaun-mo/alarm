using System.Windows;
using System.Windows.Media;
using SunriseAlarm.Core;

namespace SunriseAlarm.App;

/// <summary>
/// The fullscreen warm sunrise. Paints a blue-free color sampled from <see cref="SunriseCurve"/>
/// and exposes large Snooze / Stop controls. Audio ramping will hang off
/// <paramref name="volumeProgress"/> once soundscapes are bundled.
/// </summary>
public partial class SunriseWindow : Window
{
    public event Action? Snoozed;
    public event Action? Dismissed;

    public SunriseWindow()
    {
        InitializeComponent();
    }

    /// <summary>Update the on-screen light (and, later, the audio) for the given progress values.</summary>
    public void SetProgress(double brightnessProgress, double volumeProgress)
    {
        var c = SunriseCurve.Evaluate(brightnessProgress);
        BackdropBrush.Color = Color.FromRgb(c.R, c.G, c.B);
        // volumeProgress -> SoundRamp.SetProgress(...) once audio assets are wired in.
        _ = volumeProgress;
    }

    private void SnoozeButton_Click(object sender, RoutedEventArgs e) => Snoozed?.Invoke();

    private void DismissButton_Click(object sender, RoutedEventArgs e) => Dismissed?.Invoke();
}
