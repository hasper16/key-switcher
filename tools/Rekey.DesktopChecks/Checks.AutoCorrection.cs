using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Automation;
using Rekey.Core;
using Rekey.Core.Languages;
using Rekey.Win32.Keyboard;
using Rekey.Win32.Replacement;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Rekey.DesktopChecks {
  // The checks of change add-auto-correction: the worker evaluates Words itself, as the app
  // does, and the settings of Auto Correction change while it runs. Only the spec's example
  // Words and invented ones are typed; reports carry no text.
  internal sealed partial class Checks {
    private const string _calculatorProcess = "CalculatorApp";

    // Invented: reads as no word in any Layout, so it is evaluated and kept.
    private const string _keptWord = "zxqwr";
    private const int _measuredWords = 20;

    private static readonly TimeSpan _quiet = TimeSpan.FromMilliseconds(700);
    private static readonly Settings _allTriggers =
        new(TriggerKeys: TriggerKeys.Space | TriggerKeys.Enter | TriggerKeys.Tab);

    public int RunAutoCorrection() {
      Begin(minutes: 4);
      LayoutId[] installed = [.. InstalledLayouts()];
      if (!installed.Contains(SpecWords.EnglishUs) || !installed.Contains(SpecWords.Russian)) {
        Expect(
            "prerequisites",
            false,
            "install the English (US) and Russian Layouts, then run again");
        return End();
      }

      uint clipboard = PInvoke.GetClipboardSequenceNumber();
      Step("2.1 Trigger Keys", CheckTriggerKeys);
      Step("2.1 turned off", CheckTurnedOff);
      Step("2.3 Pause", CheckPause);
      Step("3.3 Layout Switch", CheckLayoutSwitch);
      Step("3.1 Excluded App", CheckExcludedApp);
      Step("3.2 packaged app", CheckPackagedApp);
      Step("4.1 typing speed", MeasureTypingSpeeds);
      Step("4.1 self-pause", CheckSelfPauseGate);
      bool untouched = PInvoke.GetClipboardSequenceNumber() == clipboard;
      Expect("4.1 the clipboard is untouched", untouched, $"same sequence number {untouched}");
      return End();
    }

    private static bool PasswordBoxHasFocus() {
      try {
        return AutomationElement.FocusedElement?.Current.IsPassword == true;
      } catch (ElementNotAvailableException) {
        return false;
      }
    }

    // The window that has keyboard focus in the foreground window's thread.
    private static HWND FocusedWindow() {
      HWND foreground = PInvoke.GetForegroundWindow();
      uint thread = PInvoke.GetWindowThreadProcessId(foreground);
      var info = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
      return PInvoke.GetGUIThreadInfo(thread, ref info) && !info.hwndFocus.IsNull
          ? info.hwndFocus
          : foreground;
    }

    private static void CloseCalculator() {
      foreach (Process process in Process.GetProcessesByName(_calculatorProcess)) {
        using (process) {
          process.CloseMainWindow();
          if (!process.WaitForExit(TimeSpan.FromSeconds(3))) {
            process.Kill();
          }
        }
      }
    }

    private void CheckTriggerKeys() {
      Point box = First().Ordinary;
      Apply(new Settings());
      Attempt space = Corrected(box, Input.Space);
      Expect(
          "2.1 Space corrects the Word and switches to Russian",
          space.Replaced && space.Text == "привет " && WaitForLayout(SpecWords.Russian),
          $"{space}, {Compare(space.Text, "привет ")}");

      Attempt tab = NotCorrected(box, () => Input.Tab());
      Focus(box);
      Expect(
          "2.1 Tab is not a Trigger Key by default: the Word is left as typed",
          !tab.Replaced && tab.HeldTriggers == 0 && FocusedText() == "ghbdtn",
          $"{tab}, {Compare(FocusedText(), "ghbdtn")}");

      Attempt enter = Corrected(box, Input.Enter);
      Expect(
          "2.1 Enter corrects the Word",
          enter.Replaced && enter.Text == "привет",
          $"{enter}, {Compare(enter.Text, "привет")}");

      Attempt keypad = Corrected(box, Input.KeypadEnter);
      Expect(
          "2.1 Enter on the numeric keypad corrects the Word",
          keypad.Replaced && keypad.Text == "привет",
          $"{keypad}, {Compare(keypad.Text, "привет")}");

      Apply(_allTriggers);
      Attempt tabOn = Corrected(box, () => Input.Tab());
      bool tabDelivered = PasswordBoxHasFocus();
      Focus(box);
      Expect(
          "2.1 Tab enabled: the Word is corrected and the Tab delivered after it",
          tabOn.Replaced && tabDelivered && FocusedText() == "привет",
          $"{tabOn}, Tab moved focus on {tabDelivered}, {Compare(FocusedText(), "привет")}");

      Apply(new Settings(TriggerKeys: TriggerKeys.Space));
      Attempt enterOff = NotCorrected(box, Input.Enter);
      Expect(
          "2.1 Enter disabled: the Word is left as typed",
          !enterOff.Replaced && enterOff.HeldTriggers == 0 && enterOff.Text == "ghbdtn",
          $"{enterOff}, {Compare(enterOff.Text, "ghbdtn")}");
      Apply(new Settings());
    }

    private void CheckTurnedOff() {
      Apply(new Settings(AutoCorrectionEnabled: false));
      Attempt off = NotCorrected(First().Ordinary, Input.Space);
      GateResult gate = Gate();
      bool leftAlone = !off.Replaced && off.HeldTriggers == 0 && off.Text == "ghbdtn ";
      Expect(
          "2.1 turned off: the Word is left as typed and Space is not held",
          leftAlone && gate == GateResult.Off,
          $"{off}, gate {gate}, {Compare(off.Text, "ghbdtn ")}");
      Apply(new Settings());
    }

    private void CheckPause() {
      DateTimeOffset until = DateTimeOffset.UtcNow.AddSeconds(8);
      Apply(new Settings(PausedUntil: until));
      Attempt paused = NotCorrected(First().Ordinary, Input.Space);
      GateResult during = Gate();
      Expect(
          "2.3 paused: the Word is left as typed and Space is not held",
          !paused.Replaced && paused.HeldTriggers == 0 && during == GateResult.Paused,
          $"{paused}, gate {during}");

      // Nothing tells the worker that the Pause ended: it resumes with the clock.
      TimeSpan left = until - DateTimeOffset.UtcNow + TimeSpan.FromSeconds(1);
      Thread.Sleep(left > TimeSpan.Zero ? left : TimeSpan.Zero);
      Attempt resumed = Corrected(First().Ordinary, Input.Space);
      Expect(
          "2.3 the Pause ends on its own: the next Word is corrected",
          resumed.Replaced && resumed.Text == "привет ",
          $"{resumed}, {Compare(resumed.Text, "привет ")}");
      Apply(new Settings());
    }

    private void CheckLayoutSwitch() {
      Point box = First().Ordinary;
      Apply(new Settings());

      // The Word begins in Russian; a switch that Rekey did not ask for comes mid-Word.
      (Attempt skipped, bool byUser) = MidWordSwitch(box);
      Expect(
          "3.3 a Layout Switch Rekey did not make is taken for the user's",
          byUser,
          $"reported as the user's {byUser}");
      Expect(
          "3.3 a Word with the user's Layout Switch is left as typed",
          !skipped.Replaced && skipped.Text == "приdtn ",
          $"{skipped}, {Compare(skipped.Text, "приdtn ")}");

      Apply(new Settings(SkipAfterManualLayoutSwitch: false));
      (Attempt evaluated, _) = MidWordSwitch(box);

      // The text on screen is not the Word in one Layout, so the replacement is cancelled;
      // that it was tried shows the Word was evaluated.
      Expect(
          "3.3 with the setting off the same Word is evaluated",
          evaluated.Result is not null && evaluated.Text == "приdtn ",
          $"{evaluated}, {Compare(evaluated.Text, "приdtn ")}");
      Apply(new Settings());

      // A switch before the first key covers the Word typed next, and only that Word.
      SwitchIn(box, SpecWords.Russian);
      Input.ClearBox();
      SwitchAsUser(SpecWords.EnglishUs);
      Attempt first = Type(Input.Space, expectReplacement: false);
      Attempt second = Type(Input.Space, expectReplacement: true);
      Expect(
          "3.3 the Word typed after the user's switch is left alone, the next one corrected",
          !first.Replaced && second.Replaced && second.Text == "ghbdtn привет ",
          $"first {first}, second {second}, {Compare(second.Text, "ghbdtn привет ")}");

      // Rekey's own switch to Russian must not protect the Word typed after it.
      Attempt own = Corrected(box, Input.Space);
      bool russian = WaitForLayout(SpecWords.Russian);
      EventLog.Position mark = _log.Mark();
      Input.Type("hello");
      Input.Space();
      bool back = EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
      Thread.Sleep(_settle);
      string detail = $"{own}, Russian {russian}, next Word corrected {back}, "
          + Compare(FocusedText(), "привет hello ");
      Expect(
          "3.3 Rekey's own Layout Switch is not taken for the user's",
          own.Replaced && russian && back && FocusedText() == "привет hello ",
          detail);
    }

    private void CheckExcludedApp() {
      Point box = First().Ordinary;
      if (OpenNotepad() is not { } notepad) {
        Expect("3.1 Excluded App", false, "Notepad did not open the test file");
        return;
      }

      // The test windows run this tool's own program.
      Apply(new Settings(ExcludedApps: new ExcludedAppList([Environment.ProcessPath!])));
      Attempt excluded = NotCorrected(box, Input.Space);
      GateResult gate = Gate();
      bool leftAlone = !excluded.Replaced && excluded.HeldTriggers == 0
          && excluded.Text == "ghbdtn " && gate == GateResult.ExcludedApp;
      Expect(
          "3.1 Excluded App: the Word is left as typed and Space is never held",
          leftAlone,
          $"{excluded}, gate {gate}, {Compare(excluded.Text, "ghbdtn ")}");
      Attempt other = Corrected(notepad, Input.Space);
      Expect(
          "3.1 another app is still corrected",
          other.Replaced && other.Text == "привет ",
          $"{other}, {Compare(other.Text, "привет ")}");

      // Now the other way round, by the identity the worker reported for Notepad.
      Focus(notepad);
      AppId? app = FocusedApp();
      Apply(new Settings(ExcludedApps: new ExcludedAppList([app?.Key ?? string.Empty])));
      Attempt inNotepad = NotCorrected(notepad, Input.Space);
      Attempt inBox = Corrected(box, Input.Space);
      Expect(
          "3.2 Notepad excluded by its reported identity: left as typed there, corrected elsewhere",
          app is not null && !inNotepad.Replaced && inNotepad.HeldTriggers == 0 && inBox.Replaced,
          $"packaged identity {app?.Aumid is not null}, Notepad {inNotepad}, test window {inBox}");
      Apply(new Settings());
    }

    // The Store Calculator's window belongs to the frame host process; nothing is typed there.
    private void CheckPackagedApp() {
      if (Process.GetProcessesByName(_calculatorProcess).Length > 0) {
        Expect("3.2 Store app in a frame host window", false, "close Calculator first");
        return;
      }

      EventLog.Position mark = _log.Mark();
      string calculator = Path.Join(Environment.SystemDirectory, "calc.exe");
      Process.Start(new ProcessStartInfo(calculator) { UseShellExecute = true })?.Dispose();
      _restorer.Defer("Calculator", CloseCalculator);
      bool shown = EventLog.WaitFor(
          () => FocusedAppSince(mark)?.Aumid?.Contains(
              "Calculator", StringComparison.OrdinalIgnoreCase) == true,
          TimeSpan.FromSeconds(15));
      AppId? app = FocusedAppSince(mark);
      bool hostedPath = app is not null && !app.ExePath.EndsWith(
          "ApplicationFrameHost.exe", StringComparison.OrdinalIgnoreCase);
      Expect(
          "3.2 a Store app in a frame host window is identified as the app",
          shown && hostedPath,
          $"application user model id names Calculator {shown}, path is the app's {hostedPath}");
      if (!shown || app is null) {
        return;
      }

      Apply(new Settings(ExcludedApps: new ExcludedAppList([app.Key])));
      bool off = EventLog.WaitFor(() => Gate() == GateResult.ExcludedApp, _eventWait);
      Attempt elsewhere = Corrected(First().Ordinary, Input.Space);
      Expect(
          "3.2 excluding the Store app turns Auto Correction off there and not elsewhere",
          off && elsewhere.Replaced,
          $"gate in Calculator is ExcludedApp {off}, test window {elsewhere}");
      Apply(new Settings());
    }

    // Design D8: how many Words get no evaluation because a key could not be confirmed, at
    // several typing speeds. The numbers are the result; a line fails only when Words went
    // missing altogether.
    private void MeasureTypingSpeeds() {
      Apply(new Settings());
      foreach (int gap in new[] { 120, 60, 30, 10 }) {
        Measure($"{gap} ms between keys", () => {
          Input.TypeWithGap(_keptWord, TimeSpan.FromMilliseconds(gap));
          Input.TypeWithGap(" ", TimeSpan.FromMilliseconds(gap));
        });
      }

      Measure("each Word in one burst", () => Input.TypeAtOnce(_keptWord + " "));
    }

    private void Measure(string pace, Action typeWord) {
      SwitchIn(First().Ordinary, SpecWords.EnglishUs);
      Input.ClearBox();
      Thread.Sleep(_settle);
      Counts? before = Volatile.Read(ref _counts);
      EventLog.Position mark = _log.Mark();
      for (int i = 0; i < _measuredWords; i++) {
        typeWord();
      }

      EventLog.WaitFor(
          () => _log.KeyDownsSince(mark).Count(key => key.Kind == KeyKind.Space) >= _measuredWords,
          _eventWait);
      Thread.Sleep(_settle);
      Counts? after = Volatile.Read(ref _counts);
      WordCounts words = after?.Words ?? default;
      WordCounts start = before?.Words ?? default;
      KeyRejects rejects = after?.Rejects ?? default;
      KeyRejects rejectsStart = before?.Rejects ?? default;
      long ended = words.Ended - start.Ended;
      string detail = $"Words ended {ended} of {_measuredWords}, evaluated "
          + $"{words.Evaluated - start.Evaluated}, dropped for an unconfirmed key "
          + $"{words.Unconfirmed - start.Unconfirmed}; keys unconfirmed: queued "
          + $"{rejects.Queued - rejectsStart.Queued}, during the check "
          + $"{rejects.DuringCheck - rejectsStart.DuringCheck}, Password Field "
          + $"{rejects.Password - rejectsStart.Password}";
      Expect($"4.1 typing speed, {pace}", ended == _measuredWords, detail);
    }

#if DEBUG
    private void CheckSelfPauseGate() {
      TimeSpan stall = TimeSpan.FromMilliseconds(HookTimeoutMilliseconds() + 1500);
      Apply(new Settings());
      Focus(First().Ordinary);
      EventLog.Position mark = _log.Mark();
      _worker.StallHookForCheck(stall);
      Input.Type("q");
      bool first = TypeUntil(
          () => _log.IncidentsSince(mark).Contains(IncidentType.HookReinstalled));
      Thread.Sleep(_settle);
      _worker.StallHookForCheck(stall);
      Input.Type("q");
      bool second = TypeUntil(() => _log.IncidentsSince(mark).Contains(IncidentType.SelfPaused))
          && EventLog.WaitFor(
              () => _log.SelfPausesSince(mark).Exists(until => until is not null), _watchdogWait);
      Thread.Sleep(_settle);
      Attempt paused = NotCorrected(First().Ordinary, Input.Space);
      GateResult gate = Gate();
      bool leftAlone = first && second && !paused.Replaced && paused.HeldTriggers == 0
          && gate == GateResult.SelfPaused;
      Expect(
          "4.1 self-paused: the Word is left as typed and Space is not held",
          leftAlone,
          $"hook dropped twice {first && second}, {paused}, gate {gate}");

      EventLog.Position resume = _log.Mark();
      _worker.ResumeSelfPause();
      bool resumed = EventLog.WaitFor(
          () => _log.SelfPausesSince(resume).Contains(null), _watchdogWait);
      Attempt after = Corrected(First().Ordinary, Input.Space);
      Expect(
          "4.1 after Resume the next Word is corrected",
          resumed && after.Replaced,
          $"resumed {resumed}, {after}");
    }
#else
    private void CheckSelfPauseGate() {
      Expect("4.1 self-pause", false, "run the checks from a Debug build");
    }
#endif

    // Types "ghb" in Russian, switches to English (US) as the user would, types "dtn" and Space.
    private (Attempt Attempt, bool ByUser) MidWordSwitch(Point box) {
      SwitchIn(box, SpecWords.Russian);
      Input.ClearBox();
      EventLog.Position mark = _log.Mark();
      Input.Type("ghb");
      bool byUser = SwitchAsUser(SpecWords.EnglishUs);
      Input.Type("dtn");
      Input.Space();
      Thread.Sleep(_quiet);
      return (Read(mark), byUser);
    }

    // Asks the focused window for a Layout the way the language bar does, so the request does
    // not come from the worker; returns whether the worker reported the change as the user's.
    private bool SwitchAsUser(LayoutId target) {
      var handles = new HKL[32];
      int count = PInvoke.GetKeyboardLayoutList(handles);
      HKL handle = handles.Take(count).FirstOrDefault(
          hkl => unchecked((ushort)(nint)hkl) == target.LanguageId);
      EventLog.Position mark = _log.Mark();
      PInvoke.PostMessage(
          FocusedWindow(), PInvoke.WM_INPUTLANGCHANGEREQUEST, default, (nint)handle);
      EventLog.WaitFor(
          () => _log.EventsSince(mark).Exists(
              seen => seen.Event is LayoutChanged changed && changed.Layout == target),
          TimeSpan.FromSeconds(2));
      return _log.EventsSince(mark)
          .Select(seen => seen.Event)
          .OfType<LayoutChanged>()
          .LastOrDefault(changed => changed.Layout == target)?.ByUser == true;
    }

    // Types "ghbdtn" and the key in the emptied box, starting in English (US), and waits for
    // the replacement.
    private Attempt Corrected(Point box, Action key) {
      SwitchIn(box, SpecWords.EnglishUs);
      Input.ClearBox();
      return Type(key, expectReplacement: true);
    }

    // The same where no replacement should come: waits a moment instead.
    private Attempt NotCorrected(Point box, Action key) {
      SwitchIn(box, SpecWords.EnglishUs);
      Input.ClearBox();
      return Type(key, expectReplacement: false);
    }

    private Attempt Type(Action key, bool expectReplacement) {
      EventLog.Position mark = _log.Mark();
      Input.Type("ghbdtn");
      key();
      if (expectReplacement) {
        EventLog.WaitFor(() => _log.ReplacementsSince(mark).Count > 0, _replaceWait);
        Thread.Sleep(TimeSpan.FromMilliseconds(300));
      } else {
        Thread.Sleep(_quiet);
      }

      return Read(mark);
    }

    private Attempt Read(EventLog.Position mark) {
      List<ReplacementResult> results = _log.ReplacementsSince(mark);
      int held = _log.KeyDownsSince(mark).Count(
          key => key.Held && key.Kind is KeyKind.Space or KeyKind.Enter or KeyKind.Tab);

      // A Tab may have moved focus to the masked box, whose text must not be asked for.
      string? text = PasswordBoxHasFocus() ? null : FocusedText();
      return new Attempt(results.Count == 0 ? null : results[^1], held, text);
    }

    // Gives the worker new settings and waits until its engine has them.
    private void Apply(Settings settings) {
      EventLog.Position mark = _log.Mark();
      _worker.UseSettings(settings);
      EventLog.WaitFor(
          () => _log.EventsSince(mark).Exists(seen => seen.Event is SettingsChanged), _eventWait);
    }

    private GateResult Gate() {
      return (GateResult)Volatile.Read(ref _gate);
    }

    private bool WaitForLayout(LayoutId layout) {
      return EventLog.WaitFor(() => CurrentLayout() == layout, TimeSpan.FromSeconds(1));
    }

    // The application of the latest focus report.
    private AppId? FocusedApp() {
      return FocusedAppSince(default);
    }

    private AppId? FocusedAppSince(EventLog.Position mark) {
      return _log.EventsSince(mark)
          .Select(seen => seen.Event)
          .OfType<FocusChanged>()
          .LastOrDefault()?.App;
    }

    // What one typed Word led to; the text is compared by the caller and never printed.
    private sealed record Attempt(ReplacementResult? Result, int HeldTriggers, string? Text) {
      public bool Replaced => Result is ReplacementResult.Verified or ReplacementResult.Retyped;

      public override string ToString() {
        return $"replacement {Result?.ToString() ?? "none"}, Trigger Keys held {HeldTriggers}";
      }
    }

    // The worker's counters as of the latest input event.
    private sealed record Counts(WordCounts Words, KeyRejects Rejects);
  }
}
