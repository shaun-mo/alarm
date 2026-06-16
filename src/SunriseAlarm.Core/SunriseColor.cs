namespace SunriseAlarm.Core;

/// <summary>
/// An sRGB color together with its perceived <paramref name="Brightness"/> (0..1),
/// produced by <see cref="SunriseCurve"/>.
/// </summary>
public readonly record struct SunriseColor(byte R, byte G, byte B, double Brightness);
