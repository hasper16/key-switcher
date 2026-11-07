# Proposal

## Why

Rekey for Windows starts from an empty repository. Every later change (dictionaries, scorer, keyboard hook, UI) needs a buildable .NET solution with the agreed module split, a test harness, CI, and a running background process to plug into. Getting this foundation right first helps every later phase.

## What Changes

- Create a .NET 10 solution with three projects: `Rekey.Core` (pure logic), `Rekey.Win32` (Windows interop), `Rekey.App` (WPF host), plus test projects.
- Define the core seam: the engine consumes key events and returns actions; Windows adapters execute those actions. No Windows API types cross into `Rekey.Core`.
- Ship a minimal app host: starts in the background, shows a tray icon with a Quit item, refuses to start a second instance.
- Add per-user settings storage (JSON in `%LOCALAPPDATA%\Rekey`) with safe fallback to defaults.
- Add CI (GitHub Actions on `windows-latest`) that builds and runs all tests on every push and pull request.
- Add repository hygiene: `.gitignore`, `.editorconfig`, `Directory.Build.props` (nullable, warnings as errors), `README.md`.
- Add static analysis that fails the build on violations: SonarAnalyzer.CSharp and StyleCop.Analyzers.

## Capabilities

### New Capabilities

- `app-lifecycle`: How the app process starts, runs in the background, stays single-instance, stores per-user settings, and shuts down.

### Modified Capabilities

None.

## Impact

- New code: `src/Rekey.Core`, `src/Rekey.Win32`, `src/Rekey.App`, `tests/Rekey.Core.Tests`, `tests/Rekey.Win32.Tests`.
- New dependencies: CommunityToolkit.Mvvm, H.NotifyIcon.Wpf, Microsoft.Windows.CsWin32, xUnit (built-in assertions only).
- Static analysis in every build: SonarAnalyzer.CSharp (SonarLint rules) and StyleCop.Analyzers (Checkstyle equivalent), plus .NET code-style enforcement.
- New CI workflow: `.github/workflows/ci.yml`.
- Developer prerequisite: .NET 10 SDK on Windows.
