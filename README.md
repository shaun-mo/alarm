# Sunrise Alarm

A native Windows sunrise alarm clock for your desktop — a software take on the Hatch.
Instead of a jarring buzzer, it eases you awake: starting ~30 minutes before your set
time, the screen fills with a warm, **blue-free** sunrise that gradually brightens while
gentle ambient sound (birds, rain, ocean) gradually rises in volume. By your wake time
the room is bright and the sound is at full, comfortable volume.

Built for people — especially kids and teens — who keep a PC in their bedroom and are
tired of phone alarms that fail to wake them or wake them in a bad mood.

See [PRD.md](PRD.md) for the full product spec.

## Status

Early scaffold. The timing/light/sound **logic core is built and unit-tested**; the WPF
UI shell and OS integration are in place as a working skeleton.

## How it works

One normalized `progress` value (0 → 1) runs from the start of the ramp to wake time and
drives everything:

- **Light** — a fullscreen window paints a warm gradient sampled from `SunriseCurve`
  (dim ember red → amber → soft warm white). Because the app paints its own pixels, the
  "no blue" promise is guaranteed in code, not left to monitor hardware.
- **Sound** — `SoundRamp` eases playback volume in quadratically so it starts almost
  inaudible and rises gently.
- **Wake-from-sleep** — `PowerGuard` schedules a Windows wake timer so the PC comes out of
  sleep before the ramp. If that can't be scheduled, the app warns you to keep the PC awake.

## Project layout

| Project | What it is |
|---|---|
| `src/SunriseAlarm.Core` | Pure, testable logic — no UI, no OS calls. The four deep modules live here. |
| `src/SunriseAlarm.App` | WPF app (`net8.0-windows`): settings window, fullscreen sunrise, `PowerGuard`. |
| `tests/SunriseAlarm.Core.Tests` | xUnit tests for the four core modules, driven by a `FakeClock`. |

### The four core modules

- **`AlarmScheduler`** — owns the wake time + ramp; emits `progress` and a one-shot
  "wake reached" event. Uses an injected `IClock`, so it's fully deterministic in tests.
- **`SunriseCurve`** — pure `progress → warm color`. Guarantees blue is never the dominant channel.
- **`SoundRamp` / `VolumeCurve`** — pure `progress → volume`, capped by the user's max.
- **`SettingsStore`** — JSON load/save of preferences; missing/corrupt files fall back to defaults.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Build, test, run

```sh
dotnet build
dotnet test
dotnet run --project src/SunriseAlarm.App
```

## Roadmap

Deferred from v1 (see PRD): bundled soundscape assets, recurring/weekly schedules,
multiple alarms, a standalone warm reading-light mode, and an installer package.
