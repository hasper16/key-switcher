# Tasks

## 1. Profile model and normalisation (data shape first)

- [x] 1.1 Write failing tests for `WordNormaliser`: case folding, `ё→е`, apostrophe variants; implement; verify tests pass
- [x] 1.2 Define the `LanguageProfile` record and JSON contract (`format: 1`); verify a round-trip test of a small hand-written profile passes
- [x] 1.3 Write failing loader tests: valid profile loads into frozen sets; invalid JSON or missing required field throws a typed `ProfileLoadException`; implement; verify tests pass

## 2. Active Languages

- [x] 2.1 Write failing tests for `LanguageSet.Activate`: only languages with an installed layout are active; language-id fallback maps an unlisted variant; unsupported layout maps to none; implement; verify tests pass
- [x] 2.2 Write a failing test that one corrupt profile among three yields two active languages and one Incident record; implement the tolerant loading; verify it passes

## 3. Profile generator

- [ ] 3.1 Create `tools/Rekey.ProfileBuilder` console app using `WordNormaliser` from `Rekey.Core`; verify it builds
- [ ] 3.2 Write a failing test: a tiny frequency list produces the expected words, short words and top n-grams; implement the pipeline; verify it passes
- [ ] 3.3 Write a failing test that two runs over the same input are byte-identical; make output deterministic; verify it passes
- [ ] 3.4 Download source lists for en/ru/uk into `data/sources/`, write curated `short-words.txt` and `language.json` per language and `SOURCES.md` with origin and licence; verify `SOURCES.md` lists every file in `data/sources/`
- [ ] 3.5 Generate `en.json`, `ru.json`, `uk.json` into `src/Rekey.Core/Profiles/` as embedded resources; verify a test loads all three and checks spec examples ("я", "не", "еще", "п'ять", Ukrainian layout ids)

## 4. Docs

- [ ] 4.1 Document the profile format and how to regenerate profiles in `docs/language-profiles.md`; verify the documented command regenerates identical files
