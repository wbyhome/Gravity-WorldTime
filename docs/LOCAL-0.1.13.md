# Local 0.1.13 — Windows time-zone switching

## Use

Right-click a city clock → **Set computer time zone**. Review the current zone, target Windows zone ID and target-local time preview, then choose **Apply to Windows**. The preview is a snapshot, not a live clock. Cancel/closing the preview does not change Windows.

The first original zone is saved before a system write. Subsequent city switches preserve that original. **Restore original computer time zone** is available in clock menus and the tray manager while recovery information exists. Restoration preserves an original `_dstoff` setting, where present. Keep the application's data folder to retain recovery information.

If Windows rejects the operation, review the error and optionally choose **Retry as administrator**, which uses Windows UAC. No automatic elevation occurs. Windows automatic time-zone policies may override the choice; this feature does not disable them or change time synchronization.

## Scope

- Uses `%SystemRoot%/System32/tzutil.exe` through the Windows system directory, validated Windows IDs, and process arguments rather than a command shell.
- Changes Windows' time zone, affecting local-time displays across the computer. Does not set the UTC clock or disable NTP.
- Reads the current zone before applying; a changed/stale preview is rejected. Reads back after success; mismatches are errors.
- Persists recovery information before a system write. Cancelling UAC or a failed operation retains recovery information for inspection/retry.
- English, Simplified Chinese, Traditional Chinese and Japanese interface labels.

## Verification

Release build: zero warnings/errors. Actual EXE: **551 PASS** logic/catalog checks and **791 PASS** programmatic WPF checks. Added tests use a fake system-zone service: no real `/s` command or UAC operation is executed during tests. Checked argument boundaries, unknown/injected IDs, read-only `/g`, explicit elevation construction, original-zone persistence, repeated switches, restoration including DST-disabled mode, permission failure, failed saves, stale previews, readback mismatch, UI preview/cancel/apply/restore paths.

Inspected real WPF preview image at `artifacts/ui-test-0.1.13-images/computer-time-zone-zh-Hans.png` using synthetic city inputs. Only the live current time-zone read was checked using `/g`; it returned `China Standard Time`. Real Windows mutation and actual UAC acceptance remain untested.

Local runtime update preserves existing city data and does not execute a time-zone change. No Gravity Desktop/Core files or state are modified. No GitHub commit, push or release is part of this feature iteration.

Command reference: [Microsoft tzutil](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/tzutil).

## 中文使用方法

右键目标城市时钟 → **将电脑时区设为此城市** → 核对当前、目标时区和当地时间预览 → **应用到 Windows**。

首次切换前会保存原时区。之后可从时钟右键菜单或托盘管理界面选择 **恢复电脑原时区**。权限不足可主动点击 **以管理员身份重试**；Windows 自动时区可能覆盖设置。此功能更改全机时区，不修改实际 UTC 时间。

当前只更新本地 0.1.13，公开版本仍为 0.1.12。真实切换与 UAC 尚未验收；测试使用模拟服务，没有切换本机时区。
