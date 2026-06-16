using SunriseAlarm.Core;

namespace SunriseAlarm.Core.Tests;

/// <summary>A hand-cranked clock so timing tests are deterministic and instant.</summary>
internal sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset start) => Now = start;

    public DateTimeOffset Now { get; private set; }

    public void Advance(TimeSpan by) => Now += by;

    public void Set(DateTimeOffset to) => Now = to;
}
