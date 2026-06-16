using SunriseAlarm.Core;
using Xunit;

namespace SunriseAlarm.Core.Tests;

public class SunriseCurveTests
{
    public static IEnumerable<object[]> ProgressSamples()
    {
        for (int i = 0; i <= 100; i++)
            yield return new object[] { i / 100.0 };
    }

    [Theory]
    [MemberData(nameof(ProgressSamples))]
    public void Blue_is_never_the_dominant_channel(double progress)
    {
        var c = SunriseCurve.Evaluate(progress);

        // The whole point of the product: warm light only. Blue must never lead.
        Assert.True(c.B <= c.G, $"blue {c.B} exceeded green {c.G} at progress {progress}");
        Assert.True(c.G <= c.R, $"green {c.G} exceeded red {c.R} at progress {progress}");
    }

    [Fact]
    public void Brightness_rises_monotonically_with_progress()
    {
        double previous = -1;
        for (int i = 0; i <= 100; i++)
        {
            var c = SunriseCurve.Evaluate(i / 100.0);
            Assert.True(c.Brightness >= previous,
                $"brightness dropped at progress {i / 100.0}: {c.Brightness} < {previous}");
            previous = c.Brightness;
        }
    }

    [Fact]
    public void Starts_nearly_dark_and_ends_bright()
    {
        Assert.True(SunriseCurve.Evaluate(0).Brightness < 0.1);
        Assert.True(SunriseCurve.Evaluate(1).Brightness > 0.6);
    }

    [Fact]
    public void Max_brightness_cap_scales_the_result_down()
    {
        var full = SunriseCurve.Evaluate(1.0, maxBrightness: 1.0);
        var capped = SunriseCurve.Evaluate(1.0, maxBrightness: 0.5);

        Assert.True(capped.Brightness < full.Brightness);
        Assert.True(capped.R < full.R);
    }

    [Theory]
    [InlineData(-5.0)]
    [InlineData(5.0)]
    public void Out_of_range_progress_is_clamped(double progress)
    {
        var c = SunriseCurve.Evaluate(progress);
        var atEdge = SunriseCurve.Evaluate(progress < 0 ? 0 : 1);
        Assert.Equal(atEdge, c);
    }
}
