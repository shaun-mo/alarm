using System.Runtime.InteropServices;

namespace SunriseAlarm.App;

/// <summary>
/// Thin wrapper over Win32 <c>SetThreadExecutionState</c>. On this product's target hardware
/// (Modern Standby, no real S3 sleep, hibernate after a few hours) the only reliable way to
/// guarantee the alarm fires is to stop the PC sleeping while an alarm is armed — then turn the
/// display back on when the ramp starts. Manual sleep by the user still overrides this; it only
/// prevents the automatic idle transition.
/// </summary>
internal static class StayAwake
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
        AwayModeRequired = 0x00000040,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

    /// <summary>Keep the system from sleeping (display may still turn off). Use while waiting to fire.</summary>
    public static void KeepSystemAwake() =>
        SetThreadExecutionState(
            ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.AwayModeRequired);

    /// <summary>Keep the system awake and force the display on. Use during the ramp so the sunrise shows.</summary>
    public static void KeepSystemAndDisplayAwake() =>
        SetThreadExecutionState(
            ExecutionState.Continuous | ExecutionState.SystemRequired |
            ExecutionState.DisplayRequired | ExecutionState.AwayModeRequired);

    /// <summary>Drop all locks so the PC can sleep normally again.</summary>
    public static void Release() =>
        SetThreadExecutionState(ExecutionState.Continuous);
}
