# 0.1.14 — multi-row translucent magnetic clocks

Published edition: Windows x64 prototype prerelease. This is the last local 0.1.x multi-row iteration being published; it does not contain the later 0.2.x time-ruler previews.

## Changes since 0.1.12

- 0.1.13 added optional computer-wide Windows time-zone switching and original-zone restoration. Preview the target before applying; administrator retry is an explicit user action. This changes the time zone, not UTC or network time synchronization.
- 0.1.14 removed standalone yesterday/today/tomorrow labels and their date-column colors. The column shows city-local month/day, with AM/PM in 12-hour mode. Date tooltips retain full timestamps and offsets. Contact advice can still mention the next suitable day.
- Magnetic grouping, lower-header splitting, release-at-edge folding, translucent backgrounds, city routines, the 419-place offline catalog and four interface languages are retained.

## Download, update and use

Download the Windows x64 ZIP from the [0.1.14 release](https://github.com/wbyhome/Gravity-WorldTime/releases/tag/v0.1.14). Extract the entire archive to a writable folder and run `Gravity.WorldTime.exe`. Requires **.NET 8 Desktop Runtime x64**, including WPF; the runtime is not bundled. Target environment: Windows 10/11 x64 with an interactive desktop. This is a portable prototype, not an installer or Microsoft Store package.

For updates, exit WorldTime from its tray menu, back up the existing `data` folder, and replace application files while retaining that folder. The archive contains no personal data folder, layouts, backups, diagnostics, PDBs or credentials. Verify the ZIP using `SHA256SUMS.txt`.

Right-click a clock → **Set computer time zone** → review current/target zones → **Apply to Windows**. The first original zone is saved before a system write; **Restore original computer time zone** uses that record. Preserve the data folder if you need this recovery entry. Automatic time-zone settings or organizational policy can override a selection. Actual system changes and UAC have not been physically accepted; do not confuse the simulated tests below with that acceptance. [Detailed usage](LOCAL-0.1.13.md)

## Binary and source provenance

The four original 0.1.14 application files were copied unchanged from the preserved local runtime; they were not rebuilt, re-signed or upgraded for publication. The DLL reports **0.1.14.0**. All **26 C# compiler inputs** were matched byte-for-byte against the SHA-256 checksums in the original portable PDB, including historical mixed line endings. [Source checksums](SOURCE-CHECKSUMS-0.1.14.json)

Twenty-two files came directly from matching local files/snapshots; four were reconstructed from the 0.1.12 commit and recorded 0.1.13 edits, then verified against those original checksums. Source line-ending normalization is disabled for this C# snapshot to preserve that evidence. The public package intentionally omits the PDB and local recovery records. The repository project and manifest identify 0.1.14; the embedded location catalog is retained from the existing published catalog.

## Verification on the actual packaged files, 2026-10-25

| Check | Result | Scope |
|---|---:|---|
| `--self-test` | 544 PASS | Model/catalog checks, routines, date boundaries and simulated system-zone operations |
| `--ui-test` | 791 PASS | Actual WPF controls with temporary synthetic data, preview/cancel and simulated switching/restoration |
| `--layer-test` | 16 PASS | Native parenting, ordering and minimize checks using disposable reference windows |
| `--layered-child-test` | Upload succeeded, Win32 error 0 | Native layered-child render capability using own invisible windows |

Counts include resource/catalog assertions, not independent human usage scenarios. No real Windows time-zone write or UAC approval was executed during this publication. The user's prior physical Win+D confirmation belongs to 0.1.12 on one computer, not a new 0.1.14 acceptance result.

Still unverified: actual 0.1.14 pointer-driven split/merge/folding matrix and Win+D, real Explorer restart, sleep/resume, mixed-DPI multi-monitor removal/reconnection, system time-zone mutation/UAC/restoration, and long-term operation across Windows configurations. Friendly hints estimate typical availability; they do not detect anyone's real status.

Screenshots on the homepage are genuine isolated WPF renders using synthetic city/time data, not a user's private desktop.

## 中文

本次发布**多条半透明磁吸时钟0.1.14**，不包含0.2.x时间尺。保留0.1.13的电脑时区切换与原时区恢复；移除日期栏“昨天／今天／明天”等标签，只保留当地月日及12小时制AM/PM。独立作息、磁吸分组、下方头部拆分、边缘松手折叠、离线城市目录和四语言能力继续保留。

下载并完整解压Windows x64 ZIP，需要.NET 8 Desktop Runtime x64（含WPF）。升级前从托盘退出并备份data，替换程序文件时保留data；下载包不含个人城市配置、诊断或PDB。

发布使用原0.1.14二进制，未重新构建或替换当前本机运行版本。26个C#源码文件均与原调试符号内的编译输入校验值一致。实际发布文件重新执行544项逻辑/目录、791项WPF、16项原生窗口检查，分层子窗口上传成功、错误码0。真实时区更改、UAC和上述多屏/系统恢复场景尚未验收；没有在本轮修改电脑时区。原0.1.12的Win+D确认不能冒充0.1.14实机验收。

Copyright © 2026 Gravity Desktop. Existing copyright and source/third-party licensing terms remain unchanged. No additional open-source license is granted.
