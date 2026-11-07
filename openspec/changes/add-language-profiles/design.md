# Design

## Context

The original ships one data file per language: a code, a display name, the macOS input sources that type it, the valid one- and two-letter words, a frequency-ordered common-word list (about 70k words for English) and the 300 most frequent letter pairs and triples with normalised weights; it builds packed lookup tables at load time. We keep the logical shape, replace macOS input-source ids with Windows Layout ids, and generate our own data (ADR 0003).

## Goals / Non-Goals

**Goals:**
- A profile format the scorer can query in O(1) per word and per n-gram.
- A generator that anyone can rerun to get the same files.

**Non-Goals:**
- Scoring logic (change `add-language-scorer`).
- Languages beyond en/ru/uk (format allows them; data later).
- Learned Words storage (change `add-word-learning`).

## Decisions

### D1. File format: JSON on disk, frozen sets in memory
```json
{
  "format": 1,
  "code": "ru",
  "displayName": "Russian",
  "layoutIds": ["00000419"],
  "alphabet": "абвгдеёжзийклмнопрстуфхцчшщъыьэюя",
  "words1": ["я", "в", "и", "с", "у", "а", "о", "к"],
  "words2": ["не", "на", "он", "..."],
  "commonWords": ["и", "в", "не", "..."],
  "bigrams": { "ст": 0.0123 },
  "trigrams": { "ост": 0.0041 }
}
```
- `layoutIds` are Windows KLIDs (`00000409` English US, `00000419` Russian, `00000422` Ukrainian, `00020422` Ukrainian Enhanced, plus common variants). Matching is on the low word (language id) as a fallback so unlisted variants of a covered language still map.
- `alphabet` is new: it lets the scorer tell scripts apart without hard-coding Unicode ranges.
- At load: words go into `FrozenSet<string>` after normalisation; n-grams into `FrozenDictionary<string, float>`. Frequency rank of common words kept as `FrozenDictionary<string,int>` for the scorer.
- Alternative: a compact binary format. Rejected for now: JSON is diffable in review and load time for ~3×70k words is well under a second. Revisit if start-up exceeds 500 ms.

### D2. Embedded resources
Profiles are embedded in `Rekey.Core` so the app is a single file. A loader takes a `Stream`, so tests load profiles from strings.

### D3. Normalisation in one place
One `WordNormaliser` used by both the generator and the loader: lower-case with invariant culture, `ё→е`, apostrophes `’ ʼ → '`. One decision lives in one place, so generated data and runtime lookups cannot drift.

### D4. Generator (`tools/Rekey.ProfileBuilder`)
- Inputs: a frequency list per language (`word count` lines) and a small hand-curated short-word list per language (`data/sources/<code>/short-words.txt`), because frequency lists contain noise among one- and two-letter tokens.
- Steps: normalise → keep tokens made only of the language alphabet (plus apostrophe/hyphen inside the word) → drop count < threshold → take top N (default 80,000) → compute bigram/trigram frequencies from the kept words weighted by count, keep top 300 → write JSON with sorted keys and fixed number formatting (`R` → 6 significant digits) for byte-stable output.
- Candidate source: OpenSubtitles-derived frequency lists (e.g. the FrequencyWords project, CC BY-SA 4.0) for all three languages. Licence and attribution go into `data/sources/SOURCES.md` and the app's About/licences text.

### D5. Active Languages
`LanguageSet.Activate(installedLayoutIds)` returns the profiles whose `layoutIds` (or language id fallback) intersect the installed list. It is recomputed when the Win32 side reports a layout list change (`WM_INPUTLANGCHANGE` / periodic check — done in `add-keyboard-capture-and-layouts`).

## Risks / Trade-offs

- [Subtitle corpora skew toward spoken language, missing technical words] → Learned Words and the Protected Word list cover gaps; scorer also uses n-grams, not only word lists.
- [CC BY-SA data imposes share-alike on the derived profile files] → profiles stay separate data files under CC BY-SA 4.0 with attribution in `data/sources/SOURCES.md`; CC BY-SA 4.0 is one-way compatible with GPLv3 (Creative Commons, 2015), so shipping them inside the GPL-3.0-or-later app is allowed.
- [Ukrainian/Russian overlap: both Cyrillic, most keys produce the same letters, many words are valid in both] → profiles stay independent; same-script Corrections get stricter rules in the scorer (change `add-language-scorer`).

## Open Questions

- Final top-N size per language (80k default) — tune once scorer accuracy tests exist; does not change format or tasks.
