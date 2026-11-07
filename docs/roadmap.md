# Roadmap

Implementation order of the OpenSpec changes in `openspec/changes/`. Apply them in this order with `/opsx:apply <change>`; each one lists its dependencies in `proposal.md`.

| # | Change | Capability | Depends on |
|---|---|---|---|
| 1 | `bootstrap-solution` | app-lifecycle | — |
| 2 | `add-language-profiles` | language-profiles | 1 |
| 3 | `add-language-scorer` | wrong-layout-detection | 2 |
| 4 | `add-keyboard-capture-and-layouts` | keyboard-capture, layout-management | 1, 3 |
| 5 | `add-text-replacement` | text-replacement | 4 |
| 6 | `add-auto-correction` | auto-correction | 3, 4, 5 |
| 7 | `add-manual-correction` | manual-correction | 5 |
| 8 | `add-word-learning` | word-learning | 3, 5, 7 |
| 9 | `add-tray-and-settings-ui` | tray-and-settings | 6, 7, 8 |
| 10 | `add-statistics-and-diagnostics` | statistics-and-diagnostics | 6, 9 |
| 11 | `add-auto-update-and-installer` | auto-update | 1, 9 |
| 12 | `add-onboarding` | onboarding | 9, 11 |
| 13 | `add-telemetry` | telemetry | 10, 12 |
| 14 | `add-ui-localization` | localization | 9–13 |

Changes 2–3 (pure logic) and 4 (Windows interop) can be built in parallel after 1. Changes 10 and 11 can run in parallel after 9. Onboarding comes after the installer because its "install properly" offer and its installed-location check need the Velopack install root.

Sources: the owner's reverse-engineering notes (kept outside the repository), `GLOSSARY.md` (terms), `docs/adr/` (decisions).
