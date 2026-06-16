using System.Diagnostics;

namespace SunriseAlarm.App;

/// <summary>
/// Schedules a Windows wake timer (a Task Scheduler task with <c>WakeToRun</c>) so the PC wakes
/// from sleep before the ramp begins. This is the one inherently OS-bound module — it shells out
/// to PowerShell and is verified by integration testing, not unit tests. Every call is best-effort
/// and never throws: callers check the boolean result and warn the user when scheduling fails.
/// </summary>
public sealed class PowerGuard
{
    private const string TaskName = "SunriseAlarmWake";

    /// <summary>
    /// Register (or replace) a wake task firing at <paramref name="rampStart"/>. The task's action
    /// is a no-op; its only job is to bring the machine out of sleep so the running app can take over.
    /// Returns false if the task could not be scheduled.
    /// </summary>
    public bool ScheduleWake(DateTimeOffset rampStart)
    {
        string time = rampStart.ToLocalTime().ToString("HH:mm");
        string script =
            "$action = New-ScheduledTaskAction -Execute 'cmd.exe' -Argument '/c exit'; " +
            $"$trigger = New-ScheduledTaskTrigger -Once -At {time}; " +
            "$settings = New-ScheduledTaskSettingsSet -WakeToRun -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries; " +
            $"Register-ScheduledTask -TaskName '{TaskName}' -Action $action -Trigger $trigger -Settings $settings -Force";
        return RunPowerShell(script);
    }

    /// <summary>Remove the wake task if present.</summary>
    public bool CancelWake() =>
        RunPowerShell($"Unregister-ScheduledTask -TaskName '{TaskName}' -Confirm:$false -ErrorAction SilentlyContinue");

    private static bool RunPowerShell(string script)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            using var process = Process.Start(psi);
            if (process is null) return false;

            process.WaitForExit(15_000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
