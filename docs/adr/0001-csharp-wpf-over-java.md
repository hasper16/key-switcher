# C# / .NET + WPF instead of Java

The primary developer knows Java, but Rekey is built in C# / .NET 10 with WPF. A Java app would run its low-level keyboard hook through JNA/FFM, where GC and JIT pauses risk exceeding Windows' `LowLevelHooksTimeout` (Windows then silently removes the hook), would idle at ~100 MB, and would reach UI Automation only through awkward COM interop. .NET calls Win32 and UI Automation directly and looks native on Windows 11. WPF rules out NativeAOT, so the app ships self-contained with ReadyToRun.
