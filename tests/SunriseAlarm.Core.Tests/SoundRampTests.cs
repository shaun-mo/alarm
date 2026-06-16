using SunriseAlarm.Core;
using Xunit;

namespace SunriseAlarm.Core.Tests;

public class SoundRampTests
{
    /// <summary>Records what the ramp asks the player to do.</summary>
    private sealed class FakePlayer : IAudioPlayer
    {
        public double Volume { get; set; }
        public string? LoadedTrack { get; private set; }
        public bool Playing { get; private set; }

        public void Load(string trackId) => LoadedTrack = trackId;
        public void Play() => Playing = true;
        public void Pause() => Playing = false;
        public void Resume() => Playing = true;
        public void Stop() => Playing = false;
    }

    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.5, 0.25)]
    [InlineData(1.0, 1.0)]
    public void Volume_curve_eases_in_quadratically(double progress, double expected)
    {
        Assert.Equal(expected, VolumeCurve.VolumeFor(progress), precision: 6);
    }

    [Fact]
    public void Volume_rises_monotonically_and_starts_near_silent()
    {
        Assert.Equal(0, VolumeCurve.VolumeFor(0));
        double previous = -1;
        for (int i = 0; i <= 100; i++)
        {
            double v = VolumeCurve.VolumeFor(i / 100.0);
            Assert.True(v >= previous);
            previous = v;
        }
    }

    [Fact]
    public void Volume_never_exceeds_the_cap()
    {
        for (int i = 0; i <= 100; i++)
        {
            double v = VolumeCurve.VolumeFor(i / 100.0, maxVolume: 0.6);
            Assert.True(v <= 0.6 + 1e-9, $"volume {v} exceeded cap at progress {i / 100.0}");
        }
    }

    [Fact]
    public void Start_loads_the_track_and_begins_silent()
    {
        var player = new FakePlayer();
        var ramp = new SoundRamp(player);

        ramp.Start("birds");

        Assert.Equal("birds", player.LoadedTrack);
        Assert.True(player.Playing);
        Assert.Equal(0, player.Volume);
    }

    [Fact]
    public void SetProgress_drives_the_player_volume_through_the_cap()
    {
        var player = new FakePlayer();
        var ramp = new SoundRamp(player, maxVolume: 0.5);

        ramp.SetProgress(1.0);

        Assert.Equal(0.5, player.Volume, precision: 6);
    }
}
