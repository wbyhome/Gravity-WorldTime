# WorldTime 0.3.3 release evidence

Published as an explicitly versioned historical prerelease. Original DLL SHA-256: `8BCE5B72AFC14570FE051E150E71CF12B99AB12B35E9FA7924B390941F21E2C0` (FileVersion 0.3.3.0). Original EXE/DLL are not rebuilt, patched or re-signed. The source snapshot was preserved immediately before the 0.3.3.1 hotfix; its 74 compiler documents and three data resources match the original 0.3.3 portable-PDB checksums and assembly resources. Later 0.3.3.1–0.3.5.3 source and user data are excluded.

Original checks: 78 city/search, 544 logic/catalog, 1415 programmatic WPF/localization, 455 ring/usability. New packaged-binary verification is separately recorded in [publication checks](evidence-0.3.3/publication-checks.json). Counts are assertions, not independent real-user scenarios. See [compiler inputs](evidence-0.3.3/compiler-source-check.json), [data provenance](../src/Data/places033-provenance.json) and [media provenance](../media/city-0.3.3/PROVENANCE.md).

The user confirmed real Chinese IME submission without auto-adding. A later control run of the original DLL found 旧金山 through the actual native route. The earlier all-empty report was not reproduced; version mix was plausible, not a proven historical cause. Full concatenated Chinese addresses were confirmed unsupported and are not advertised for 0.3.3.

Known limits: cold index ~2 seconds on the original test machine; substantial retained search-cache memory; variable multilingual coverage and current-Windows time-zone mappings; physical offline/Japanese IME/low-end/long-running/Explorer restart/sleep/mixed-DPI acceptance incomplete. The full 0.3.3 human Win+D/pin/transparency matrix was not rerun. No About/version dialog, full-body ruler drag, pair-comparison card, network calibration service or installer is included; those are later changes.

The ZIP is a .NET 8 Desktop Runtime x64-dependent portable preview for Windows 10/11 x64. Use Start-WorldTime-Ruler.cmd and a new writable folder. Profile: ruler-preview-data, not classic data or newer installed user data. Exit and back up before transferring a whole compatible marked profile. No backwards migration from later installed versions is provided.

Source visibility grants no extra application license. Existing notices apply. GeoNames-derived data retains CC BY 4.0; Unicode and IANA retain their respective terms. No private contacts, layouts, avatars, diagnostics or PDBs are published.

## Packaging recheck difference (2026-10-29)

The packaged binary passed 544 logic and 78 city/search assertions. The ring suite passed 441 assertions, then failed `Amber grip retains fully opaque color at background opacity 0`; one assertion failed and the remaining 13 original assertions were not reached. Earlier original records contain 455 passes. Current rendered images show a resize arrow at ordinary opacity, but the all-transparent exact-RGBA assertion remains unresolved. No rendering-equivalence or full-opacity claim is made. Use the ring-size menu if the grip is difficult to see. This historical prerelease preserves the original binary and does not silently backport a fix. The failed log is retained alongside the passed logs.

Count clarification: the first publication summary grouped the failed assertion with 14 remaining assertions. The precise result is 441 passed, 1 failed and 13 not executed. The immutable v0.3.3 tag and original ZIP are retained; this documentation correction does not change application source or binaries.
