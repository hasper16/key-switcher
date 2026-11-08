# Tasks

## 1. Repository and solution skeleton

- [x] 1.1 Extend `.gitignore` with the VisualStudio template (keep the existing secrets section) and add `global.json` pinning .NET 10 SDK; verify `dotnet --version` in the repo matches the pinned SDK
- [x] 1.2 Create `Rekey.slnx` with `src/Rekey.Core` (`net10.0`), `src/Rekey.Win32` (`net10.0-windows`), `src/Rekey.App` (WPF, `net10.0-windows`), `tests/Rekey.Core.Tests`, `tests/Rekey.Win32.Tests` (xUnit); verify `dotnet build` succeeds
- [x] 1.3 Add `Directory.Build.props` (nullable, warnings as errors, latest-recommended analyzers) and `Directory.Packages.props` (central package versions); verify `dotnet build` still succeeds with zero warnings
- [x] 1.4 Add SonarAnalyzer.CSharp, StyleCop.Analyzers and code-style enforcement to `Directory.Build.props`, severities and the Google-style exclusions from design D9 in the existing `.editorconfig`, settings in `stylecop.json`; verify `dotnet build` passes with zero warnings, that `dotnet format whitespace --verify-no-changes` passes on the Google-style sample from the guide (empty blocks as `{ }`, see D9), that deliberate violations (Allman brace, field without `_`, unused private field, public method without XML docs) fail the build, and that an undocumented private method does not
- [x] 1.5 Add a project-reference guard test in `Rekey.Core.Tests` asserting `Rekey.Core` references no `Microsoft.Windows.*`/`System.Windows.*` assemblies; verify the test passes

## 2. Engine seam (data shape first)

- [x] 2.1 Define `InputEvent`, `Context`, `EngineAction` (incl. `RecordIncident`) and `Incident` records and an `Engine.Handle` that returns no actions; write a test that a key event yields an empty action list and verify it passes
- [x] 2.2 Define the immutable `Settings` record with defaults (version 1, `AutoCorrectionEnabled` on) and verify a test that default settings round-trip through JSON unchanged

## 3. Settings storage

- [x] 3.1 Write failing tests for `SettingsStore`: missing file → defaults + file created; changed value survives reload; verify they fail for the expected reason
- [x] 3.2 Implement load/save with source-generated `System.Text.Json` and atomic `File.Replace`; verify tests from 3.1 pass
- [x] 3.3 Write a failing test for corrupt JSON → defaults + `settings.json.bak` kept; implement; verify it passes
- [x] 3.4 Write a test that unknown fields are ignored and missing fields get defaults; verify it passes

## 4. Windows host

- [x] 4.1 Add CsWin32 with `NativeMethods.txt` to `Rekey.Win32`; verify generated code compiles
- [x] 4.2 Implement the single-instance guard (`Local\Rekey-<SID>` mutex); verify with a `Rekey.Win32.Tests` test that a second acquire in the same session fails
- [x] 4.3 Create the WPF host: `OnExplicitShutdown`, no main window, H.NotifyIcon tray icon with a Quit item; verify manually that the icon appears, no taskbar button exists, and Quit exits with code 0
- [x] 4.4 Wire the single-instance guard into start-up; verify manually that a second launch exits within 2 s and only one tray icon remains
- [x] 4.5 Handle `SessionEnding` and `Exit`: flush settings, dispose adapters; verify manually that `settings.json`, deleted while Rekey runs, is back on disk after sign-out and sign-in

## 5. CI and docs

- [ ] 5.1 Add `.github/workflows/ci.yml` (windows-latest: restore, build Release, test; job id `build`); verify the workflow passes on a push to a branch, then add `build` as a required status check to the existing `main` branch protection (`gh api -X PUT repos/hasper16/key-switcher/branches/main/protection` with the current settings plus `required_status_checks: {strict: true, contexts: ["build"]}`) and verify a PR shows it as required
- [x] 5.2 Write `README.md`: what Rekey is, prerequisites (.NET 10 SDK), build/test/run commands, project layout, licence (GPL-3.0-or-later code, CC BY-SA 4.0 language data); verify the documented commands run as written

## Workflow follow-up

- Archive the change after review so `openspec/specs/app-lifecycle/spec.md` is created.
