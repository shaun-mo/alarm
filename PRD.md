# PRD: Sunrise Desktop Alarm Clock (Windows)

**Status:** Draft — `needs-triage`
**Type:** New product (greenfield)
**Platform:** Native Windows desktop application (C# / .NET, WPF)
**Author:** Generated from product conversation, 2026-06-15

---

## Problem Statement

People — especially kids and teens who keep a desktop or laptop in their bedroom —
are woken up by jarring phone alarms. Those alarms either get silenced/ignored and
fail to wake them, or they jolt them awake into a bad mood. There's no gentle,
phone-free way to wake up using the computer that's already sitting in the room.

Hatch-style sunrise alarm clocks solve this with gradual light and sound, but they're
a separate $100–170 physical device. The user wants the same gentle-wake experience
delivered through hardware they already own: their Windows PC.

## Solution

A downloadable, installable **native Windows application** (not a web app) that turns
the PC into a sunrise alarm clock. The user picks a wake time; the app starts a gentle
ramp roughly 30 minutes earlier. During the ramp, the screen displays a **fullscreen
sunrise** — a warm, blue-free gradient that gradually brightens from a dim deep red,
through amber, to a soft warm white — while **ambient audio** (bird chirps, soft
nature/ambient music) gradually rises in volume. By the chosen wake time, the screen
is at full warm brightness and the sound is at full target volume, so the person wakes
naturally and in a pleasant mood instead of to a buzzer.

The app runs off the PC's system clock, persists the user's settings between runs, and
exposes large, easy on-screen buttons (snooze, pause/stop, volume) so the user never
has to hunt for a phone in the dark. Gradual brightness and gradual loudness can each
be toggled on or off independently.

To survive the PC sleeping overnight, the app **schedules a Windows wake timer** so the
machine wakes from sleep in time to begin the ramp.

## User Stories

1. As a sleeper, I want to set the exact time I need to wake up, so that the alarm fires when I actually need to get up.
2. As a sleeper, I want the alarm to use my PC's own clock, so that I don't have to configure or sync time separately.
3. As a sleeper, I want the wake-up to begin gently about 30 minutes before my set time, so that I'm eased awake instead of jolted.
4. As a sleeper, I want to choose how long the ramp lasts (e.g. 15 / 30 / 45 minutes), so that I can tune how gradual the wake-up feels.
5. As a sleeper, I want the screen to start very dim and warm, so that the light doesn't shock me awake in the middle of the night.
6. As a sleeper, I want the on-screen light to contain no blue hues, so that it doesn't strain my eyes or fight my body's wind-down.
7. As a sleeper, I want the screen to gradually brighten to a comfortable warm white by my wake time, so that I'm fully but gently woken.
8. As a sleeper, I want soft bird-chirp / ambient sounds to start quietly, so that the audio eases me out of sleep.
9. As a sleeper, I want the sound to gradually get louder as wake time approaches, so that I keep surfacing toward wakefulness.
10. As a sleeper, I want the light and the sound to ramp together on the same schedule, so that the whole experience feels like one coherent sunrise.
11. As a sleeper, I want to turn the gradual brightness off, so that I can have sound-only wake-ups if I prefer.
12. As a sleeper, I want to turn the gradual loudness off, so that I can have light-only wake-ups if I prefer.
13. As a sleeper, I want to set the maximum brightness the sunrise reaches, so that it's bright enough to wake me but not painfully so.
14. As a sleeper, I want to set the maximum volume the sound reaches, so that it's loud enough to wake me without being startling.
15. As a sleeper, I want to choose which soundscape plays (e.g. birds, rain, ocean, white noise), so that I wake to a sound I like.
16. As a sleeper, I want a large, obvious "snooze" button on screen, so that I can buy a few more minutes without finding my phone.
17. As a sleeper, I want a large "stop / dismiss" button, so that I can end the alarm easily when I'm awake.
18. As a sleeper, I want a large pause button for the audio, so that I can quiet it without fully dismissing the alarm.
19. As a sleeper, I want easy volume up/down controls during the alarm, so that I can adjust loudness without going into settings.
20. As a sleeper, I want to control snooze/stop/volume with keyboard or media keys too, so that I don't have to reach the mouse in the dark.
21. As a sleeper, I want my settings to be remembered between sessions, so that I don't have to reconfigure the alarm every night.
22. As a sleeper, I want to see clearly whether the alarm is armed and for what time, so that I can trust it before I go to sleep.
23. As a sleeper, I want to disarm tonight's alarm quickly, so that I can skip it on a day I don't need to wake early.
24. As a sleeper, I want the PC to wake itself from sleep in time for the ramp, so that the alarm still works even if the computer sleeps overnight.
25. As a sleeper, I want a clear warning if my PC's power settings would prevent the wake timer from working, so that I'm not surprised by a silent morning.
26. As a sleeper, I want the sunrise window to take over the full screen, so that the only thing I see on waking is the gentle light, not my desktop.
27. As a sleeper, I want to dismiss the fullscreen sunrise and return to my desktop, so that I can start using the computer after I'm up.
28. As a sleeper, I want a configurable snooze length, so that snooze adds the amount of time I want.
29. As a user, I want to install the app with a normal Windows installer, so that setup is simple and familiar.
30. As a user, I want the app to look like a normal native Windows app, so that it feels trustworthy and consistent with my system.
31. As a user, I want the app to optionally launch when Windows starts, so that the alarm is ready without me reopening it each day.
32. As a user, I want the app to keep running quietly (e.g. in the tray) until alarm time, so that it's out of the way but still armed.
33. As a sleeper, I want a way to preview/test the sunrise and sound, so that I can confirm the experience before relying on it overnight.
34. As a sleeper, I want the alarm to still ring/brighten at full strength if I never interact, so that a missed snooze doesn't mean I oversleep.

## Implementation Decisions

**Stack & shape**
- Native Windows desktop app built in **C# / .NET using WPF**. Distributed as a standard Windows installer producing a single launchable app.
- The app runs off the **local system clock**. No accounts, no network, no backend in v1.
- Settings persisted **locally** (e.g. a JSON file under the user's app-data folder).

**Brightness approach**
- The sunrise is rendered by the app itself as a **fullscreen window painting a warm gradient**, not by manipulating the physical monitor backlight. This guarantees the no-blue color promise, works on any monitor, and is fully controllable. Real monitor-backlight control is explicitly out of scope for v1.

**Sleep/wake approach**
- The app **schedules a Windows wake timer** (via a Scheduled Task configured with "wake the computer to run this task," or the equivalent wake-timer API) so the machine wakes from sleep before the ramp begins.
- Because wake timers depend on the machine's power configuration and BIOS/firmware, the app must **detect and warn** the user when wake timers are disabled or unsupported, and recommend a fallback (keep PC awake during the ramp window / don't let it sleep).

**Core modules (logic kept pure and isolated from OS/UI shells)**
- **AlarmScheduler** — owns the armed wake time and ramp duration. Computes the ramp start (`wakeTime − rampDuration`) and emits a single normalized `progress` value in `[0,1]` plus a "wake time reached" signal. Depends on an **injected clock abstraction** so it can be driven deterministically in tests. No UI or OS calls.
  - Interface (conceptual): `Arm(wakeTime, rampDuration, snoozeLength)`, `Disarm()`, `Snooze()`, `Tick(now) → AlarmState { Phase, Progress }`, events `RampProgressChanged(progress)`, `WakeTimeReached()`.
- **SunriseCurve** — pure function mapping `progress ∈ [0,1]` → a warm, blue-free color plus a brightness level, honoring the user's max-brightness cap. Encapsulates the dark-red → amber → warm-white curve and the "no blue channel above warm threshold" guarantee. No state.
  - Interface (conceptual): `Evaluate(progress, maxBrightness) → SunriseColor { R, G, B, Brightness }`.
- **SoundRamp** — maps `progress ∈ [0,1]` → playback volume honoring the user's max-volume cap, and wraps the chosen-track audio player. The curve is a pure function; playback is a thin shell over the OS audio API.
  - Interface (conceptual): `Start(track)`, `SetProgress(progress)`, `Pause()`, `Resume()`, `Stop()`; pure helper `VolumeFor(progress, maxVolume) → volume`.
- **SettingsStore** — load/save the user's preferences: wake time, ramp duration, brightness on/off, loudness on/off, max brightness, max volume, selected track, snooze length, launch-on-startup. Round-trips through a local file.
  - Interface (conceptual): `Load() → Settings`, `Save(Settings)`.
- **PowerGuard** — schedules/cancels the Windows wake timer for the next ramp start and reports whether wake timers are currently permitted by the system. Thin OS-integration shell; verified via integration testing, not unit tests.
  - Interface (conceptual): `ScheduleWake(rampStartTime)`, `CancelWake()`, `WakeTimersAvailable() → bool`.
- **App shell (UI)** — the fullscreen sunrise window, the settings screen, the armed-status display, and the large snooze / stop / pause / volume controls (mouse, keyboard, and media-key driven). Thin presentation layer that observes `AlarmScheduler` and drives `SunriseCurve` / `SoundRamp`.

**Toggles & caps**
- Gradual brightness and gradual loudness are independent on/off toggles. With a ramp disabled, that channel jumps to its target at wake time instead of ramping.
- Max-brightness and max-volume caps are user-configurable and enforced inside `SunriseCurve` and `SoundRamp` respectively.

**Bundled content (v1)**
- A small set of built-in royalty-free soundscapes (e.g. birds, rain, ocean, white/brown noise) shipped with the installer. No streaming library, podcasts, or downloadable content in v1.

## Testing Decisions

**What makes a good test here:** tests assert **external, observable behavior** through each
module's public interface — not internal implementation details. Timing is made
deterministic by injecting a fake clock, so tests never rely on real wall-clock waits
and are not flaky.

**Modules to be unit-tested in v1 (all four confirmed):**
- **AlarmScheduler** — drive a fake clock across the ramp window and assert: `progress`
  is `0` before ramp start, increases monotonically to `1` at wake time, `WakeTimeReached`
  fires exactly once at the right moment, and snooze/disarm change state correctly.
  Highest-value tests in the product — they protect the core "wakes me at the right time" promise.
- **SunriseCurve** — assert the mapping is monotonic in brightness, respects the
  max-brightness cap, and **never emits a blue-dominant color** at any progress value
  (guards the no-blue promise). Pure function → cheap, exhaustive sampling possible.
- **SoundRamp** — assert volume rises monotonically with progress, starts at/near zero,
  and never exceeds the max-volume cap. Pure curve helper tested directly; playback shell mocked.
- **SettingsStore** — round-trip test: save a settings object, load it back, assert
  equality, including defaults for missing fields and graceful handling of a missing/corrupt file.

**Not unit-tested (integration only):**
- **PowerGuard** and the WPF **app shell** — OS- and UI-bound; covered by manual/integration
  verification (does the PC actually wake, does the fullscreen window actually display).

**Prior art:** none yet (greenfield). These become the reference patterns — injected-clock
timing tests, pure-function curve tests, and file round-trip tests — for future work.

## Out of Scope

- Web app / browser version of any kind (explicitly excluded).
- macOS, Linux, or mobile versions.
- Controlling the **physical monitor backlight** or color temperature via hardware APIs.
- Accounts, cloud sync, or any backend service.
- Streaming or an "ever-growing" content library: podcasts, guided meditations, sleep
  stories. v1 ships a small fixed set of bundled soundscapes only.
- Customizable multi-step wind-down / bedtime routines (e.g. red light + timed meditation).
- A separate companion mobile app (the Hatch app analog).
- Multiple alarms, per-day-of-week schedules, and recurring weekly schedules (candidate
  for a fast follow, but not v1).
- Auto-dimmable reading light mode as a standalone feature.

## Further Notes

- **The wake-timer caveat is the biggest product risk.** Windows wake timers depend on the
  user's power plan and firmware; on some machines they're disabled by default or blocked
  entirely. The app must fail loudly (clear warning + recommended fix) rather than silently
  not waking someone for school. A "keep the PC awake during the ramp window" fallback should
  be available for machines where wake timers don't work.
- **Target user is kids/teens with a bedroom PC.** Favor a dead-simple default flow (set a
  time, arm, sleep) over configurability. Caps on brightness/volume protect against a too-harsh
  default experience.
- **Fast-follow candidates** (explicitly deferred from v1): recurring weekly schedules,
  multiple alarms, more soundscapes, and a standalone warm reading-light mode.
- **No git repo or issue tracker exists yet.** This PRD lives at `PRD.md` in the project
  root; if/when a tracker is set up it can be imported and the `needs-triage` label applied there.
