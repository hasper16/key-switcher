# Accuracy corpus sources

Held-out text for `tools/Rekey.Accuracy`. None of it is used to generate Language Profiles.

| File | Origin | Licence |
|---|---|---|
| `en.tsv` | Tatoeba English sentences, export `eng_sentences.tsv.bz2` of 2026-11-07, SHA-256 `1a2d75ac07aa5d035bff4e2bf053139dfd3b1f1645f6fd3d76fc852cc9ef6020` | CC BY 2.0 FR |
| `ru.tsv` | Tatoeba Russian sentences, export `rus_sentences.tsv.bz2` of 2026-11-07, SHA-256 `fe81d065a2fe60789ab359a6f680e7b623e45d70215e3e877dd8bb1f9be96c0a` | CC BY 2.0 FR |
| `uk.tsv` | Tatoeba Ukrainian sentences, export `ukr_sentences.tsv.bz2` of 2026-11-07, SHA-256 `072ed7636fc9c4e3c6ded52a46891d671d41e58c7c3e1667e4ef46826bf8bff5` | CC BY 2.0 FR |
| `technical.tsv` | Written for this project: technical and mixed-language text | GPL-3.0-or-later, as the repository |
| `sample_tatoeba.py` | Written for this project: draws the Tatoeba samples | GPL-3.0-or-later, as the repository |

Tatoeba exports come from `https://downloads.tatoeba.org/exports/per_language/<code>/`. Attribution: sentences from Tatoeba (https://tatoeba.org), each line starting with its Tatoeba sentence id, so the author is found at `https://tatoeba.org/sentences/show/<id>`.

`sample_tatoeba.py` keeps sentences of two or more words and draws 3,000 per language with a fixed seed, sorted by id. The same exports give the same files:

```bash
python -I -X utf8 data/accuracy/sample_tatoeba.py <export folder> data/accuracy
```

## File format

- `<code>.tsv`: `<Tatoeba id>` tab `<sentence>`, one per line.
- `technical.tsv`: `<code>` tab `<text>`, where the code names the Language of the Cyrillic words; Latin words are typed in English (US).
