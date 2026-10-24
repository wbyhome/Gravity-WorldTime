# Local 0.1.14 — simpler clock date column

Removed the yesterday/today/tomorrow (and two-day difference) labels from clock rows, along with their relative-date color changes and viewer-date comparison. The date column again shows the city's month/day in a consistent color; 12-hour mode retains AM/PM. Hovering the date shows the full local timestamp and UTC offset.

The date column returns from 64 to 44 DIP, leaving more room for existing contact hints. A timeline is deferred; no replacement indicator is introduced.

0.1.13's Windows time-zone switching and original-zone restoration are retained. This update does not change the computer's time zone, saved city routines or layout. Contact suggestions may still say tomorrow when describing the next contact window; this change removes the standalone relative-day labels from clock rows.

Verification: Release build with zero warnings/errors; 544 logic/catalog checks and 791 programmatic WPF checks passed. Existing date checks now verify a destination-local date across New Year and the 12-hour AM/PM display. Inspected actual WPF render `artifacts/ui-test-0.1.14-images/date-only-zh-Hans.png`. No new tests were added for this presentation change. The lower logic count reflects removal of seven obsolete relative-date checks.

Local runtime only. No commit, push or GitHub release. Published 0.1.12 documentation remains a record of that release.

中文：移除时钟日期栏的“昨天／今天／明天”等标签及变色，保留月日、时间与 12 小时制 AM/PM。时间线以后再设计。保留系统时区切换功能，未执行真实时区切换，未上传 GitHub。
