# Design

## Context

The original embeds about 40 interface languages in its binary (observed strings). We write our own texts (ADR 0003) in three languages. UI changes before this one already keep strings in `Strings.resx`; this change adds translations, switching and checks.

## Goals / Non-Goals

**Goals:**
- Live language switching for WPF bindings.
- Grammar-correct plurals in Russian and Ukrainian.

**Non-Goals:**
- More languages (format supports them; translation later).
- Translating Language Profile display names beyond the three languages.

## Decisions

### D1. Resources
`.resx` per language with the generated `Strings` class; keys named by screen and purpose (`Tray_PauseFor30Min`). Text style follows `add-tray-and-settings-ui` D1 (sentence case, plain verbs).

### D2. Live switching
A `Localizer` singleton exposes an indexer `this[string key]` and raises `PropertyChanged("Item[]")` on language change; XAML binds through a small markup extension `{l:Loc Tray_Settings}`. `CultureInfo.CurrentUICulture` is set for the UI thread and new windows. The tray menu is rebuilt on change.

### D3. Plurals
Keys with plural variants use suffixes `_One`, `_Few`, `_Many`, `_Other`. A `Plural.Select(culture, n)` function implements CLDR rules for en (one/other) and ru/uk (one/few/many/other); the same function is used for every numeric string. Unit-tested on 0–25, 101, 111, 121.

### D4. Completeness test
A unit test loads all `.resx` files and asserts identical key sets and identical `{0}` placeholder counts per key; runs in CI.

### D5. Default language
`UiLanguage = system` → `GetUserDefaultUILanguage` mapped to `en`/`ru`/`uk`, else `en`.

## Risks / Trade-offs

- [Machine-quality translations] → native-speaker review of ru/uk before release, tracked as a task.
- [Text length differences break layouts] → windows use auto-sizing; Russian is the longest, used for layout checks.
