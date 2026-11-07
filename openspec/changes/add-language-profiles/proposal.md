# Proposal

## Why

Rekey can only tell a wrong-layout Word from a real one if it knows what real words look like in each Language. The original app ships one data file per Language (common words, short words, letter-pair and letter-triple frequencies, layouts). We need our own equivalent for English, Russian and Ukrainian before any scoring can be built (see ADR 0003: we do not reuse the original's data).

## What Changes

- Define the Language Profile format: one JSON file per Language with common words, valid one- and two-letter words, letter-pair and letter-triple frequencies, and the Windows Layout ids that type the Language.
- Add a generator tool that builds Language Profiles from openly licensed word-frequency lists, reproducibly, with source and licence recorded.
- Ship generated profiles for English (`en`), Russian (`ru`) and Ukrainian (`uk`) inside the app.
- Load profiles at start-up; activate only Languages that have at least one installed Layout.
- Normalise lookups: case-insensitive, Russian `ё` treated as `е`, Ukrainian apostrophe variants treated as one character.

## Capabilities

### New Capabilities

- `language-profiles`: What recognition data Rekey has for each Language, how it is loaded, and which Languages are active for the current user's Layouts.

### Modified Capabilities

None.

## Impact

- New code: `Rekey.Core/Languages` (profile model, loader, normaliser), `tools/Rekey.ProfileBuilder` (console app).
- New data: `src/Rekey.Core/Profiles/{en,ru,uk}.json` (embedded resources), `data/sources/` with source lists and `SOURCES.md`.
- Depends on `bootstrap-solution`.
