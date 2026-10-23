# Validation notes

This is an early Windows prototype. A successful automated check is not a promise of compatibility with every desktop environment.

## 0.1.12

See [0.1.12 verification and limitations](RELEASE-0.1.12.md): 529 logic/catalog checks, 735 WPF integration checks and 16 native-window checks passed. The user confirmed three clocks remain visible after Win+D on the tested computer. Actual Explorer restart, sleep/resume and mixed-DPI monitor reconfiguration remain unverified.

## Previous 0.1.11 checks

The published code was built in Release configuration. The 0.1.11 checks reported:

- 495 model/catalog checks, including per-location Windows time-zone resolution, existing saved-city identity, multilingual country search and catalog completeness.
- 574 programmatic WPF integration checks, including resource checks, city-picker filtering/selection, correct time-zone persistence, layout preservation, native desktop bitmap upload and repeated layer/fold/expand cycles.

These totals include resource checks and individual data rows; they are not counts of independent human test scenarios.

The user confirmed that all three clocks became visible in desktop mode with the native presentation implementation introduced in 0.1.9. Physical pointer-driven split/merge/auto-fold behavior, mixed-DPI multi-monitor setups and Explorer restart recovery still require separate acceptance. These are not marked passed here.

The hint example in `media/clock-example-zh-CN.png` is a real WPF control render at a fixed synthetic sample time (04:26 in Los Angeles). The picker image is an isolated WPF render with a synthetic search. Neither image is a capture of a user's private desktop or layout.

Automated tests use temporary directories and synthetic city data. The native layer probe creates temporary probe windows. Run UI/native tests in an appropriate Windows desktop session; they are not headless tests.