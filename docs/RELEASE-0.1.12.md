# 0.1.12 prerelease — 2026-10-23

Release scope: standalone Windows x64 prototype, separate from Gravity Desktop.

## Implemented

1. Native desktop attachment retries after missing/recreated hosts. Destroyed native children are detected; surviving WPF renderers create replacement children. Failed binding remains in desktop mode and reports a pending status. Folded groups stay folded during retries. Child positions account for a nonzero host client origin.
2. Monitor identity, physical scale and work-area snapshots preserve positions on unchanged displays, remap display-relative positions after geometry/DPI changes, and clamp groups onto a remaining work area. Different monitors cannot snap merely because their logical coordinates overlap. Extremely tall/wide groups may exceed the available area; their header remains reachable.
3. Per-city optional routines: sleep, contact hours, break and days off. Overnight shifts belong to their starting day. Next-contact search follows actual UTC minutes through daylight-saving transitions. Invalid or cancelled edits leave existing settings intact; unset routines inherit global defaults.
4. Calendar-relative yesterday/today/tomorrow labels, with explicit dates and localized labels in the four existing languages. Compact next-contact hints retain tomorrow/date context.

## Verification

- Release build: 0 warnings, 0 errors.
- Actual app EXE `--self-test`: **529 PASS**.
- Actual app EXE `--ui-test`: **735 PASS**.
- Actual app EXE `--layer-test`: **16 PASS**.
- Actual app EXE `--layered-child-test`: UpdateLayeredWindow child success; Win32 error 0.
- UI renders inspected: custom overnight routine editor and yesterday label across New Year. These are real WPF renders at synthetic times, not live desktop screenshots.
- Recovery test destroys only temporary windows owned by the isolated test process, simulates unavailable/recreated hosts and tests a shifted host origin. It does not restart Explorer.
- Model checks cover missing monitors, negative coordinates, DPI changes, work-area shrink, overnight shifts, configurable rest days, DST gaps/repeated hours and date-line differences.

Local evidence: `artifacts/*-0.1.12.txt`, `artifacts/ui-test-0.1.12-images/`. These ignored artifacts are not public release evidence.

## Unverified acceptance

Actual Explorer restart; real sleep/resume; physical mixed-DPI multi-monitor movement, removal and reconnection; native context-menu placement across mixed DPI. Programmatic checks do not establish these as passed. Existing magnetic pointer gestures still need physical acceptance on the target configuration.

## Data and scope

Existing city IDs, groups, defaults and opacity are preserved; no custom routines are assigned automatically. Saved state gains monitor metadata and optional routine fields. A backup precedes replacement of the local runtime. Only WorldTime source, local documentation and its own runtime are changed; no Gravity Desktop, Core, Safety Library or Explorer configuration is modified.

The Windows desktop integration remains a prototype. The user confirmed that all three clocks remain visible after Win+D on the tested computer running 0.1.12. This is single-environment acceptance, not a compatibility guarantee.
