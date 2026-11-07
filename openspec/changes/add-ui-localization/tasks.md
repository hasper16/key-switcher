# Tasks

## 1. Language selection and plurals

- [ ] 1.1 Add `UiLanguage` (`system`) to `Settings`; write failing tests for default mapping (ru → ru, uk → uk, de → en); implement; verify they pass
- [ ] 1.2 Write failing tests for `Plural.Select` (en, ru, uk on 0–25, 101, 111, 121) and the spec examples ("1 минута / 2 минуты / 5 минут", "1 хвилина / 2 хвилини / 5 хвилин"); implement; verify they pass

## 2. Resources and live switching

- [ ] 2.1 Write the completeness test (same keys and placeholder counts across `.resx`); verify it fails on a deliberately missing key and passes when fixed
- [ ] 2.2 Implement `Localizer` and the `Loc` markup extension; convert all existing XAML and code strings to keys; verify a test that no literal user-facing strings remain in XAML (grep check)
- [ ] 2.3 Add `Strings.ru.resx` and `Strings.uk.resx` with full translations; verify the completeness test passes
- [ ] 2.4 Add the interface language selector to General settings with live switching; verify manually that windows and tray menu change language immediately

## 3. Review

- [ ] 3.1 Get native-speaker review of Russian and Ukrainian texts and check layouts in Russian (longest); record sign-off in `docs/manual-tests/localization.md`; verify all screens are checked
