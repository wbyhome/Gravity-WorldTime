# Gravity WorldTime

**A living world-time ruler for your Windows desktop.**

See where friends are in their day: each person sits at their local hour on one compact ruler, with their own date and a small routine-based life illustration. Quiet stars, soft sun and moon glow, and slowly drifting clouds add movement without a large dashboard.

[Download 0.2.8 time-ruler preview](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.2.8) · [简体中文](README.zh-CN.md) · [Earlier multi-row clocks](docs/CLASSIC-CLOCKS.md)

![WorldTime time-ruler animation](media/ruler-0.2.8/028-motion-preview.gif)

*Actual product WPF render with synthetic people and a fixed example time. The dark backdrop is illustrative; this is not an OS screen recording. Life illustrations estimate a routine, not someone's real activity.*

## One ruler, different days

Instead of reading several separate clock rows, look along the ruler to compare local times. A person near 04:00 may be asleep while another is starting the afternoon. Each representative has a nearby date, so people on different calendar days remain distinguishable. Nearby people can aggregate; open the group to see each person's details.

The central scale is a daily clock cycle, **not one global chronological timeline**. The two edge hours repeat the cycle for continuity. The gradient and sky are generic ambience, not live weather or a person's measured daylight.

## What's in 0.2.8

- **Quiet sky animation:** staggered star brightness, gentle sun/moon glow and clouds moving within the ruler.
- **Routine-based people:** waking, getting ready, daily activity, resting and sleeping. Preset cartoons have small gestures; imported photos retain their appearance and receive only a small state marker.
- **Your own names:** rename contacts and your own display name without changing city, time zone, avatar or routine.
- **Three motion modes:** Dynamic, Low motion and Completely static. Optionally follow Windows' animation preference; choosing Dynamic explicitly overrides it only inside this app.
- **The compact ruler remains:** desktop residence, optional pin-on-top, translucent background, width adjustment, contact details, avatars, personal routines and nearby-person aggregation.
- **Four interface languages:** English, Simplified Chinese, Traditional Chinese and Japanese. Twelve- and 24-hour time formats are supported.

![Ruler overview](media/ruler-0.2.8/028-overall-on-dark.png)

### Small gestures, careful meanings

| Waking | Getting ready | Sleeping |
|---|---|---|
| ![Waking gesture](media/ruler-0.2.8/028-action-waking.gif) | ![Preparation gesture](media/ruler-0.2.8/028-action-preparing.gif) | ![Sleep marker](media/ruler-0.2.8/028-action-sleeping.gif) |

*Crops enlarged from real renderer output; avatars stay small in the application.*

The first 30 minutes after configured waking show a waking illustration, the following hour a preparation illustration. Explicit breaks and the period before sleep can show resting. Sleep is evaluated first, so a night-shift person's daytime sleep works too. Other configured waking hours use neutral daily activity. **Contactable hours do not imply work.** The app does not detect presence, activity, calendars or actual sleep.

## Download and start the time ruler

1. Download `Gravity-WorldTime-v0.2.8-win-x64.zip` from the [0.2.8 prerelease](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.2.8).
2. Extract the entire ZIP into a new writable folder.
3. Install **.NET 8 Desktop Runtime x64**, including WPF, if needed. Target: Windows 10/11 x64 with an interactive Explorer desktop; not every Windows build is certified.
4. Double-click **`Start-WorldTime-Ruler.cmd`**. It runs `Gravity.WorldTime.exe --ruler-desktop` and creates a separate `ruler-preview-data` profile.
5. Right-click the ruler to add contacts and change its appearance. The tray icon opens management and exit controls.

Keep all application files together. This is a framework-dependent portable prototype, not an installer or Store package. The plain EXE without `--ruler-desktop` retains the classic multi-row mode; **use the included launcher for this release's ruler UI**. Download archives include no personal configuration or PDBs. Verify the ZIP with the release's `SHA256SUMS.txt`.

## Everyday controls

| Action | Where |
|---|---|
| Move the ruler | Drag its embedded left grip |
| Resize | Drag the right end, or right-click → Display size |
| Add a person | Right-click → Add contact; select a city or zone and display name |
| Open individual details | Click a person or aggregate group |
| Rename a person | Person menu/details → Contact name |
| Rename yourself | More settings → My display name… |
| Change avatar/routine | Person edit actions and the existing routine editor |
| Change transparency | Right-click → Adjust transparency… |
| Change motion | More settings → Animation effects |
| Float above ordinary windows | Right-click → Pin on top; uncheck to return to the desktop |
| Recover a misplaced ruler | Right-click → Reset ruler position |

The location catalog has 419 places across 247 countries/regions, searchable offline. Place names derive from CLDR; current offsets and DST use Windows' time-zone database. See [catalog provenance](docs/catalog-provenance.json) and [nature estimates](docs/NATURE-DATA-0.2.1.md). Coordinates, where known, are reference city coordinates, not your location.

### Prefer a still desktop?

Low motion retains static life markers; Completely static keeps the clock and dates without animated decorations or life badges. Both stop the animation timer. Menus, settings and hidden rulers also pause it. When following Windows and system animations are disabled, Dynamic becomes effectively Low motion; explicitly selecting Dynamic disables that following option for this app only.

![Low-motion rendering](media/ruler-0.2.8/028-low-motion-on-dark.png)

## Updates and data

This ruler uses `ruler-preview-data` beside the application, separately from the classic `data` folder. It stores contacts, ruler settings and any imported avatar copies there. Exit before backing up or replacing files. Prefer extracting this preview into a new folder: old 0.1.x or 0.2.x directories are not automatically migrated or overwritten. To carry forward an existing ruler profile, back it up and copy the **whole** marked ruler-profile directory while both applications are closed; do not pass the classic profile as ruler data.

Adding/removing contacts does not organize or delete your desktop files. The existing classic manager includes optional computer-time-zone changes; those are explicit system-wide actions, not a requirement for viewing city clocks. Real Windows time-zone/UAC acceptance remains separate.

## Validation and limits

The released DLL and EXE are the locally reviewed binaries, packaged unchanged. All 64 C# compiler input documents match the original portable PDB, and both embedded data resources match the published source. The final build passed 104 ruler-specific, 68 information-layer, 216 nature/date, 544 logic, 1243 WPF/localization and 22 own-native-window checks. The user confirmed **stable display and visible small gestures after Win+D** on the tested machine.

These are not a certification of all Windows configurations. Long-running stability, Explorer restart, sleep/resume, multi-monitor hot-plug and mixed-DPI physical acceptance remain incomplete. The 100-contact resource sample aggregates into six visible representatives, not 100 independently animated avatars. Native frames still upload the whole window even though static content is cached.

[Release evidence and known limits](docs/RELEASE-0.2.8.md) · [Compiler input checksums](docs/evidence-0.2.8/compiler-source-check.json) · [Media provenance](media/ruler-0.2.8/PROVENANCE.md)

## Earlier multi-row magnetic clocks

Prefer stacked translucent city rows, magnetic grouping and automatic edge folding? The preserved [0.1.14 download](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.1.14) remains available with its [English guide](docs/CLASSIC-CLOCKS.md) and [完整中文介绍](docs/CLASSIC-CLOCKS.zh-CN.md). Those are classic-mode features, not claims that the ruler has the same row-splitting/folding interaction.

## Build

Use a Windows x64 .NET SDK capable of targeting .NET 8:

```powershell
dotnet build .\src\WorldTime.csproj -c Release
.\src\bin\Release\net8.0-windows\Gravity.WorldTime.exe --ruler-desktop
```

This is a standalone WorldTime prototype, separate from Gravity Desktop. [Source code and licensing](SOURCE_AND_LICENSING.md) · [Third-party notices](THIRD_PARTY_NOTICES.md)

Copyright © 2026 Gravity Desktop. All rights reserved unless otherwise specified. No additional open-source license is granted.
