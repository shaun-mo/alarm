namespace SunriseAlarm.Core;

/// <summary>The real clock, backed by <see cref="DateTimeOffset.Now"/>.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset Now => DateTimeOffset.Now;
}
