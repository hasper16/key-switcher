# Language Profiles

A Language Profile is the recognition data Rekey holds for one Language (see `GLOSSARY.md`). Rekey ships one profile per Language as an embedded resource of `Rekey.Core`, generated from openly licensed word lists (ADR 0003). The requirements are in the `language-profiles` spec; the decisions are in the design of the `add-language-profiles` change.

## Regenerate the profiles

From the repository root:

```bash
dotnet run --project tools/Rekey.ProfileBuilder -c Release -- data/sources src/Rekey.Core/Profiles
```

The run takes a few seconds and rewrites `src/Rekey.Core/Profiles/<code>.json` for each Language folder in `data/sources/`. The output is byte-identical for the same inputs, so on an unchanged checkout `git status` stays clean afterwards. A diff therefore always comes from a changed source, short-word list, `language.json` or generator.

The generator stops with exit code 1, before writing anything, when `data/sources/SOURCES.md` does not name a file of `data/sources/` in backticks. Record every new file there with its origin and licence first.

## Sources folder

```text
data/sources/
  SOURCES.md            origin, upstream commit, SHA-256 and licence of every file below
  <code>/
    language.json       hand-written facts about the Language
    short-words.txt     curated one- and two-letter words
    <frequency list>    word-frequency list named by language.json
```

`language.json`:

| Field | Meaning |
|---|---|
| `code` | ISO 639-1 code; one of the `Language` enum values (`en`, `ru`, `uk`) |
| `displayName` | English name of the Language |
| `alphabet` | Letters of the Language; words with any other character are dropped |
| `layoutIds` | Windows Layout ids (KLIDs) that type the Language |
| `frequencyList` | File name of the word-frequency list in the same folder |

A frequency list has one `word count` pair per line, most frequent first. `short-words.txt` has one word per line; blank lines and lines starting with `#` are skipped, and a word longer than two letters or outside the alphabet stops the generator.

## How a profile is built

1. Every word is normalised by `WordNormaliser`: lower case (invariant culture), Russian `ё` as `е`, and `’` `ʼ` as `'`. The runtime lookup uses the same class, so data and lookups cannot drift.
2. Words that normalise to the same form have their counts added.
3. A word is kept when it is made of alphabet letters, with `'` or `-` allowed only between letters, and its count is at least 3.
4. The 80,000 most frequent words become `commonWords`, most frequent first; equal counts are ordered by ordinal string comparison.
5. Letter pairs and triples are counted inside the kept words, weighted by word count, skipping any group with `'` or `-`. The 300 most frequent of each are stored as their share of all groups, rounded to six significant digits.
6. `words1` and `words2` come from `short-words.txt`, sorted.

Settings live in `tools/Rekey.ProfileBuilder/BuildOptions.cs`.

## Profile format (format 1)

UTF-8 JSON without a byte-order mark, two-space indent, LF line ends, letters unescaped, object keys sorted by ordinal comparison.

| Field | Meaning |
|---|---|
| `format` | Always `1`; the loader rejects other values |
| `code` | ISO 639-1 code of the Language |
| `displayName` | English name of the Language |
| `layoutIds` | Non-empty list of 8-digit hexadecimal KLIDs, such as `00000419` |
| `alphabet` | Letters of the Language |
| `words1` | Valid one-letter words |
| `words2` | Valid two-letter words |
| `commonWords` | Common words, most frequent first; the index is the frequency rank |
| `bigrams` | Letter pair → share of all letter pairs |
| `trigrams` | Letter triple → share of all letter triples |

Every field is required and none may be `null`. `LanguageProfileLoader` validates a file, normalises its words and loads them into frozen sets and dictionaries; a file that fails throws `ProfileLoadException`. `LanguageSet.LoadBundled` skips such a profile and records a `LanguageProfileSkipped` Incident that names its Language.

## Active Languages

`LanguageSet.Activate` returns the profiles whose Layouts are installed. A Layout maps to the profile that lists its KLID; otherwise to a profile listing a KLID with the same language id (the low word), so unlisted variants of a covered Language still map. Other Layouts map to no profile and are never a source or target of a Correction.

## Add a Language

1. Add a value to the `Language` enum with its ISO 639-1 code.
2. Add `data/sources/<code>/` with `language.json`, `short-words.txt` and an openly licensed frequency list, and record each file in `SOURCES.md`.
3. Run the regenerate command above and commit the new profile.
