# The engine returns actions instead of calling Windows

`Rekey.Core` exposes one engine that takes input events plus a context snapshot and returns a list of actions (replace word, switch layout, play sound…); Windows adapters in `Rekey.Win32` execute them. A reader might expect the engine to call `IKeyboard`/`ILayouts` interfaces directly. We chose events-in/actions-out so all correction logic is testable as plain key sequences without mocks, and so the engine owns its state on one thread with nothing shared.
