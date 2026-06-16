namespace SunriseAlarm.Core;

/// <summary>
/// Abstraction over the system clock so timing logic can be driven deterministically in tests.
/// </summary>
public interface IClock
{
    DateTimeOffset Now { get; }
}
