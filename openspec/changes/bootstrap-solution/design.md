# Design

## Context

The repository is empty. Requirements come from the owner's reverse-engineering notes (kept outside the repository). The original macOS app is split into about 25 classes (keyboard tap, scorer, replacer, watchdog, UI controllers). See proposal.md for motivation. This design fixes the module layout and seams that all later changes build on.

## Goals / Non-Goals

**Goals:**
- A module layout where all correction logic is testable without Windows.
- One seam between "decide" and "do", so later changes add behavior behind it instead of across it.
- A runnable host to attach the hook, tray menu, and windows to later.

**Non-Goals:**
- Any keyboard hooking, layout switching, or correction (later changes).
- Installer, signing, auto-update (change `add-auto-update-and-installer`).
- Settings UI (change `add-tray-and-settings-ui`).

## Decisions

### D1. Three projects, dependency direction App → Win32 → Core
- `Rekey.Core` — `net10.0`, no Windows references. Holds the engine, dictionaries, scorer, learning, settings model.
- `Rekey.Win32` — `net10.0-windows`. Adapters: keyboard hook, layouts, input injection, UI Automation, foreground app, single-instance mutex.
- `Rekey.App` — `net10.0-windows` WPF. Composition root, tray, windows (MVVM).
- Alternative considered: one project with folders. Rejected: nothing would stop Win32 types leaking into logic, and Core tests would need a Windows runner.

### D2. The engine is a deep module: events in, actions out
`Rekey.Core` exposes one engine with a small interface:

```
Engine.Handle(InputEvent e, Context ctx) -> IReadOnlyList<EngineAction>
```

- `InputEvent`: key down/up with the character produced in the current layout, modifiers, timestamp; plus focus-change and layout-change events.
- `Context`: current layout id, foreground app id, is-password-field, settings snapshot.
- `EngineAction`: `ReplaceWord(backspaces, text)`, `SwitchLayout(layoutId)`, `PlaySound`, `ShowIndicator`, `RecordStat`, `RecordIncident`.
- `Incident`: time, `IncidentType` (enum, extended by later changes) and enum-coded details. It has no string field for free text, so typed text cannot reach it by construction. Persisting Incidents is added in `add-statistics-and-diagnostics`; until then the host keeps them in memory.

The engine returns results and produces no side effects. Win32 adapters execute the actions. Tests feed key sequences and assert actions; no fakes for hooks are needed. This follows "return results, don't produce side effects" and keeps the decision logic in one place (locality).
- Alternative considered: Core calls `IKeyboard`, `ILayouts`, `ITextInjector` interfaces. Rejected for now: each interface would have one real adapter (a hypothetical seam), and tests would need mocks of call order.

### D3. Threading
- Keyboard hook thread: own thread with a Win32 message loop; the callback only copies the event into a lock-free queue and returns (target < 1 ms).
- Engine thread: single consumer of the queue; owns all engine state, so no shared mutable state.
- UI thread: WPF dispatcher; receives settings changes and stats via immutable snapshots.
- Settings are immutable records; a change produces a new snapshot sent to the engine thread.

### D4. Settings storage
- One JSON file `%LOCALAPPDATA%\Rekey\settings.json`, `System.Text.Json` source-generated context. `%LOCALAPPDATA%\Rekey` is the user-data folder for every later change; the installed app lives elsewhere (`add-auto-update-and-installer` D1), because Velopack deletes its whole install root on uninstall.
- Atomic write: write to `settings.json.tmp`, then `File.Replace`.
- Unknown fields ignored; missing fields take defaults; parse failure → defaults + rename to `settings.json.bak`.
- Schema version field `version: 1` for future migrations.

### D5. Single instance
Named mutex `Local\Rekey-<user SID>`. `Local\` scopes it to the session, so other sessions run their own instance. A second process exits immediately.

### D6. Tray host
`H.NotifyIcon.Wpf` with a WPF `ContextMenu`. `ShutdownMode = OnExplicitShutdown`; no main window is created. `SessionEnding` and `Exit` handlers flush settings and dispose adapters.

### D7. Interop generation
`Microsoft.Windows.CsWin32` generates P/Invoke from `NativeMethods.txt`. No hand-written `DllImport` unless CsWin32 cannot express the call.

### D8. Build and CI
- `Directory.Build.props`: `Nullable=enable`, `TreatWarningsAsErrors=true`, `AnalysisLevel=latest-recommended`, `LangVersion=latest`, `EnforceCodeStyleInBuild=true`, `GenerateDocumentationFile=true` (needed by documentation rules; only public members require XML docs, see D9).

### D9. Static analysis (SonarLint / Checkstyle equivalents)
Analyzers run inside every build and IDE session, so the same rules apply locally, in CI and for every AI agent:
- **SonarAnalyzer.CSharp** (SonarSource, stable 10.x) — the rule set behind SonarLint / SonarQube for C#: bugs, code smells, security hotspots, cognitive complexity.
- **StyleCop.Analyzers** (1.2/1.3 beta line; the last stable 1.1.118 predates modern C# syntax) — the Checkstyle equivalent: layout, ordering, naming, documentation.
- **.NET analyzers** (`Microsoft.CodeAnalysis.NetAnalyzers`, built into the SDK) and IDE code-style rules enforced in build.
- All three referenced once in `Directory.Build.props` with `PrivateAssets="all"`; versions pinned in `Directory.Packages.props`.
- Severities live in the root `.editorconfig` (`dotnet_diagnostic.<ID>.severity`); `stylecop.json` holds StyleCop settings only. Rules we disable are listed in `.editorconfig` with a one-line reason each.
- Code style is the Google C# Style Guide (https://google.github.io/styleguide/csharpguide.html). Its formatting and naming rules are already in the root `.editorconfig`; StyleCop is configured to agree with it:
  - `stylecop.json`: `indentation.indentationSize = 2`, `usingDirectivesPlacement = outsideNamespace`, `systemUsingDirectivesFirst = true`.
  - Disabled because they contradict the guide: SA1101 (`this.` prefix — guide does not qualify), SA1309 and SA1306 (guide uses `_camelCase` for non-public fields), SA1303 (guide: `const` does not change naming, so private constants are `_camelCase`), SA1500 (guide puts opening braces on the same line), SA1201 (guide's member order — nested types; static/const/readonly fields; fields and properties; constructors; methods — differs from StyleCop's element order).
  - Disabled as noise not covered by the guide: SA1633 (file headers), SA1413 (trailing commas).
  - Kept and matching the guide: SA1202 (public before private within a group), SA1503 (braces even when optional), SA1027 (spaces, no tabs), SA1208 (System usings first), SA1210 (alphabetical usings).
  - Documentation: XML docs required on public types and members only (`stylecop.json`: `documentInternalElements = false`, `documentPrivateElements = false`, `documentPrivateFields = false`), worded per `docs/conventions/xml-docs.md` (the `csharp-docs` conventions); SA1623 and SA1642 stay on because they enforce that wording. Other comments follow the project commenting rule (only for non-obvious logic), which overrides the guide's "comment almost every method".
- Guide rules no analyzer checks are enforced elsewhere: the 100-character column limit by `.githooks/pre-commit`; member grouping order, 4-space continuation indent, `var` judgement, LINQ/extension-method guidance by code review against the guide.
- Suppressions in code use `[SuppressMessage(..., Justification = "...")]`; `#pragma warning disable` without a justification comment is not accepted.
- Optional per developer: SonarQube for IDE (former SonarLint) extension in VS / VS Code / Rider uses the same rules; not required, since the analyzers already run in build.
- Alternative considered: a SonarQube/SonarCloud server. Deferred: adds an external service; the analyzer package gives the same rules offline.
- GitHub Actions on `windows-latest`: `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`.
- Publish profile (used later): self-contained, single-file, ReadyToRun, `win-x64`. WPF does not support NativeAOT or trimming.

## Risks / Trade-offs

- [The events-in/actions-out engine may not fit UI Automation replacement, which needs to read text from the focused control] → `Context` can carry an optional "text before caret" snapshot that the Win32 side fills in before calling the engine. Decide in `add-text-replacement`.
- [Single-file WPF start-up is slower than NativeAOT] → ReadyToRun; the app starts once per logon, so start-up time is not critical.
- [`TreatWarningsAsErrors` slows early prototyping] → accepted; keeps CsWin32 and nullable warnings from piling up.
