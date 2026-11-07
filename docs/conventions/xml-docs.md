# XML documentation and comments

Source: the `csharp-docs` skill from github/awesome-copilot, adapted to this project's commenting rule. StyleCop enforces the parts marked (SA…).

## Where docs and comments go

- **Public types and members**: XML docs (SA1600 for public elements).
- **Internal members**: XML docs only when the member is complex or its behavior is not obvious from its signature.
- **Private members and method bodies**: a comment only when the logic is non-obvious, tricky (Win32 interop, threading, timing), or needs business context from the specs or the original app. Self-explanatory code stays uncommented. Keep comments short, plain and direct.

## Wording

- `<summary>`: one sentence starting with a present-tense, third-person verb ("Returns…", "Switches…").
- Constructors: "Initializes a new instance of the `<Class>` class." (or struct) (SA1642).
- Properties: "Gets…" for read-only, "Gets or sets…" for read-write (SA1623); Boolean: "Gets [or sets] a value indicating whether…" (the exact phrase SA1623 checks; an `init` accessor takes "Gets…"). Use `<value>` for a noun-phrase description; state a default in its own sentence: "The default is `<see langword="false" />`."
- `<param>` and `<returns>`: noun phrase, starting with an article, without the data type.
  - Boolean: "`<see langword="true" />` to …; otherwise, `<see langword="false" />`." (for `<returns>`: "… if …; otherwise, …").
  - Non-flag enum: "One of the enumeration values that specifies…". Flag enum: "A bitwise combination of the enumeration values that specifies…".
  - `out` parameter: "When this method returns, contains…. This parameter is treated as uninitialized."
- `<exception cref>`: every exception the member throws directly, plus the ones callers most likely meet from nested calls. State the condition directly, without "Thrown if…".

## Tags

- `<see langword="null" />` for keywords (`null`, `true`, `false`, `int`…); `<c>` for inline code.
- `<see cref>` inside a sentence; `<seealso>` for a standalone "See also" link.
- `<paramref>` / `<typeparamref>` to refer to parameters; `<typeparam>` to describe them.
- `<remarks>` for implementation details and usage notes; `<example>` with `<code language="csharp">` for examples.
- `<inheritdoc/>` on overrides and interface implementations, unless the behavior differs — then document the difference.
