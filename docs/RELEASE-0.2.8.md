# Gravity WorldTime 0.2.8 — time-ruler preview

Publication date: 2026-10-25 (Asia/Shanghai). This prerelease publishes the locally reviewed time-ruler build without recompiling it. Earlier ruler iterations were local previews; 0.1.14 remains the separate multi-row download.

## What changed

- A compact, native desktop-hosted ruler with people positioned by their local hour and individual dates; nearby-person aggregation, grip movement, width control, avatars, routines, translucent body and optional topmost mode.
- Quiet stars, sun/moon glow and drifting clouds; preset cartoon waking/preparing/daily-activity/resting/sleeping scenes. Imported photos are not animated or deformed, only overlaid with small markers.
- Editable contact/self display names. Dynamic, Low motion and Completely static modes, plus optional following of Windows reduced-motion preference.
- Animation frame uploads no longer re-show/reorder the native window every frame. Actual launch defaults to classic mode unless `--ruler-desktop` is supplied; the ZIP launcher supplies it.

## Source and binary identity

Final DLL SHA-256: `B814CF2C2EF2CA5F6E63046853820500022E4936B2C50CD26FBDACF087446D28`.

All **64 C# source documents** match compiler checksums from the original PDB. Both embedded resources (cities.json and zone.tab) match the source data. [Full relative-path checksums](evidence-0.2.8/compiler-source-check.json). Source is published from those matching files, not reconstructed from older classic sources. Original runtime EXE/DLL/config/deps are packaged byte-for-byte; PDB and personal profiles are excluded.

## Evidence levels

| Evidence | Result | Limit |
|---|---|---|
| Build | 0 warnings/errors | Locally built and then preserved |
| [0.2.8 checks](evidence-0.2.8/ruler028-test.txt) | 104 PASS | Synthetic states, real WPF render and own native host |
| Existing information-layer/nature-date checks | 68 / 216 PASS | Programmatic regression |
| Logic and WPF/localization | 544 / 1243 PASS | Not physical tray acceptance |
| Own native-window checks | 22 PASS | Not Explorer lifecycle certification |
| User Win+D observation of this DLL | Stable, small gestures visible | One tested computer |
| Independent read-only review | No unresolved P0/P1/P2 found in the 0.2.8 increment or delivery wording | Reviewer did not rerun tests |

Name persistence includes a fresh executable process reading the saved synthetic profile, not a complete manual desktop restart acceptance. Photo byte/selection identity, night-shift sleep, midnight, DST, AM/PM, languages, mode stopping/restarting and transparent alpha boundaries were checked. A 24-refresh regression confirms ordinary animation frames do not repeat Show/placement API calls.

## Resource samples, not long-term guarantees

Final build, synthetic offscreen native host, 1000-wide ruler, 2-second warmup, roughly 10 seconds per mode: 4-contact Dynamic ~0.16% of one core, 85.6–97.1 MiB private memory; Static/Low recorded zero animation uploads during those samples. 100 contacts aggregate to **six visible representatives**: the Dynamic 60.16-second sample used ~0.42% of one core, 82.4–90.7 MiB, 602–607 handles, 343 animation frames / 344 uploads. These samples do not establish a 100-independent-avatar animation limit or no-leak result.

The normal desktop process was passively sampled over **55.16 seconds** between first/last samples: ~0.99% of one core, 49.3–58.9 MiB, 372–377 handles, one unchanged native window and no native-paint errors. Diagnostic observation was enabled and normal desktop activity could occur; do not compare those figures as a benchmark against the offscreen host. Native surface counters reset after recreation, so differences are valid only for the unchanged handle in this sample.

One timer targets at most about 6fps, lowers its rate under battery/slow-frame pressure, and pauses while hidden or during menus/settings. Static text/tick/avatar raster content is reused, but UpdateLayeredWindow still uploads the whole composed window. Timer and synchronous upload cost are not end-to-end input latency measurements.

## Privacy, compatibility and unresolved acceptance

The ZIP and repository additions contain no local contacts, layouts, avatar imports, personal native frame exports, credentials or diagnostic journals. Demonstrations use synthetic people and renderer frames; **not OS screen recordings**. Existing 0.2.7/older runtime and profile files were protected; 89 registered files rehashed unchanged. Windows time zone and running user state were not changed for publication.

Target Windows 10/11 x64 and .NET 8 Desktop Runtime x64. Actual multi-screen/mixed-DPI changes, Explorer restart, sleep/resume, long-running memory/resource behavior and the complete final-build pointer/pinning/settings matrix remain incomplete. Controlled 100%/150%/200% rendering is not a physical multi-monitor test. Earlier user pin/unpin and transparent-area click checks apply to the earlier 0.2.8 build, not a complete retest of this final hash.

Life states are routine estimates, never presence/activity/work detection. Generic sky is not weather. City coordinates and daylight estimates have the limits in [nature-data documentation](NATURE-DATA-0.2.1.md). The archived 0.2.6 diagnostic-window closing issue was not repaired by this publication; normal startup does not open that test window. Optional system-time-zone/UAC actions still have separate acceptance limits.

## Start and keep data separate

Extract a fresh ZIP and use `Start-WorldTime-Ruler.cmd`; it passes `--ruler-desktop` and creates `ruler-preview-data`, separate from classic `data`. Do not overwrite old installations or point the ruler at classic state. Back up the entire marked ruler profile before carrying it forward, with both applications closed. No installation, automatic migration, telemetry/presence service or Store delivery is claimed.

## 中文摘要

0.2.8 为时间尺预发布，保留原本地验收二进制。新增轻量天空和人物情境动画、联系人与自己改名、三种动态模式；人物状态只是作息推测，不以方便联系时段推断工作。双击 ZIP 中的 Start-WorldTime-Ruler.cmd 启动时间尺，需 .NET 8 Desktop Runtime x64；直接运行 EXE 不带参数仍是经典模式。独立配置目录 ruler-preview-data 不含个人数据，不自动覆盖旧版。Win+D 已有人工作业确认；长期、多屏、Explorer 重启和睡眠唤醒仍待验证。原生源帧不等于系统录屏，100 联系人测试实际是 6 个聚合代表。版权与第三方条款保留，没有新增开源许可。
