# Proposal

## Why

Rekey's main audience types in Russian and Ukrainian as well as English, and many of them use Windows in those languages. The original ships its interface in about 40 languages. For the MVP we need the interface in English, Russian and Ukrainian, with the same plural and wording quality as the source text.

## What Changes

- All UI text (tray, menus, windows, notifications, installer prompts) moved to resources.
- Interface languages: English, Russian, Ukrainian; default follows the Windows display language, falling back to English.
- User override of the interface language in settings, applied without restart.
- Correct plural forms for Russian and Ukrainian ("1 минута", "2 минуты", "5 минут").
- A check in CI that every language has every string.

## Capabilities

### New Capabilities

- `localization`: Which interface languages Rekey offers, how the language is chosen, and the completeness and grammar rules for translated text.

### Modified Capabilities

None.

## Impact

- `Rekey.App`: `.resx` resources (`Strings.resx`, `Strings.ru.resx`, `Strings.uk.resx`), a localization service with change notification for bindings.
- Settings: `UiLanguage` (`system` default).
- CI: resource completeness test.
- Touches every UI change; scheduled last so all strings exist, but earlier UI changes already put strings in `Strings.resx`.
- Depends on `add-tray-and-settings-ui`, `add-statistics-and-diagnostics`, `add-auto-update-and-installer`, `add-onboarding` and `add-telemetry`.
