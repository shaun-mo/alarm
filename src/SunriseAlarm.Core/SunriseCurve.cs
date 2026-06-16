namespace SunriseAlarm.Core;

/// <summary>
/// Pure mapping from ramp progress (0..1) to a warm, blue-free sunrise color.
///
/// Every keyframe is ordered so the red channel dominates green, which dominates blue
/// (R &gt;= G &gt;= B). Linear interpolation between two such ordered triples preserves the
/// ordering, so the blue channel is <i>never</i> the dominant channel at any progress value —
/// the light cannot stray into the cool, sleep-disrupting blues this product exists to avoid.
/// </summary>
public static class SunriseCurve
{
    private static readonly (double P, double R, double G, double B)[] Keyframes =
    {
        (0.00,  40,   4,   0),   // dim ember red — barely-there glow
        (0.35, 170,  45,   6),   // deep red-orange
        (0.70, 255, 125,  38),   // warm orange
        (1.00, 255, 188, 122),   // soft warm amber-white (~2700K feel)
    };

    /// <summary>
    /// Evaluate the sunrise color at <paramref name="progress"/> (clamped to 0..1),
    /// scaled by <paramref name="maxBrightness"/> (clamped to 0..1) so the user can cap
    /// how bright the screen ever gets.
    /// </summary>
    public static SunriseColor Evaluate(double progress, double maxBrightness = 1.0)
    {
        progress = Math.Clamp(progress, 0.0, 1.0);
        maxBrightness = Math.Clamp(maxBrightness, 0.0, 1.0);

        var (r, g, b) = Sample(progress);
        r *= maxBrightness;
        g *= maxBrightness;
        b *= maxBrightness;

        double luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0;
        return new SunriseColor(ToByte(r), ToByte(g), ToByte(b), luminance);
    }

    private static (double R, double G, double B) Sample(double p)
    {
        for (int i = 1; i < Keyframes.Length; i++)
        {
            var hi = Keyframes[i];
            if (p <= hi.P)
            {
                var lo = Keyframes[i - 1];
                double span = hi.P - lo.P;
                double t = span <= 0 ? 0 : (p - lo.P) / span;
                return (Lerp(lo.R, hi.R, t), Lerp(lo.G, hi.G, t), Lerp(lo.B, hi.B, t));
            }
        }

        var last = Keyframes[^1];
        return (last.R, last.G, last.B);
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;

    private static byte ToByte(double v) => (byte)Math.Clamp(Math.Round(v), 0, 255);
}
