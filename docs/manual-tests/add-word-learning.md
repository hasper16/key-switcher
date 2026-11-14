# Desktop checks: add-word-learning

The learning rules (Undo protects, Manual Correction learns, Undo forgets), the effect of both lists on Decisions, the list bounds, the `words.json` contract and the debounced save are covered by unit tests in `Rekey.Core.Tests` (`WordLearningTests` and `Words/*`). The change adds no Windows interop beyond loading the file at start and saving it from the app, so it adds no `tools/Rekey.DesktopChecks` set.

The app-level checks (a real Undo in a real app, the file across a restart) need a person and are deferred to the final acceptance pass: see the `add-word-learning` section of [deferred-by-hand.md](deferred-by-hand.md).
