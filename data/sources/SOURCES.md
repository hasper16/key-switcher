# Language Profile sources

Every file in this folder, where it came from, and its licence. `tools/Rekey.ProfileBuilder` refuses to run while a file here is missing from this record (see `docs/language-profiles.md`).

## Word-frequency lists

Each file here holds the first 100,000 lines of its upstream file, unchanged otherwise (`head -n 100000`). That cut is our only change.

### English and Russian: FrequencyWords

From [FrequencyWords](https://github.com/hermitdave/FrequencyWords) by Hermit Dave, built from the [OpenSubtitles 2018](https://opus.nlpl.eu/OpenSubtitles2018.php) corpus, at commit `525f9b560de45753a5ea01069454e72e9aa541c6`. The lists are licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/); the project's code is MIT.

| File | Upstream file | Upstream SHA-256 | Trimmed SHA-256 |
|---|---|---|---|
| `en/en_full_top100k.txt` | `content/2018/en/en_full.txt` | `7fea67ab954e2c01df6c608c9826e594cf36f8823b3243554f88245fb75dc506` | `bf4d4bed26672cc1817500f798df2986679af45a54c624ebebad247c955cddbb` |
| `ru/ru_full_top100k.txt` | `content/2018/ru/ru_full.txt` | `32dfd94138aea2667ec455dcc8512029e39bd4b8ea70b09410d569b69d511a30` | `bd1e38d4bca71068c494fff73c2185ef0a0f3863a40c51fbfdc46d5b6b679cc1` |

### Ukrainian: wikipedia-word-frequency

From [wikipedia-word-frequency](https://github.com/IlyaSemenov/wikipedia-word-frequency) by Ilya Semenov, built from the Ukrainian Wikipedia dump of 2022-08-30, at commit `d11c4fab211156e96ab5943850dd6f98604707fb`. The project's code is MIT; the word counts are derived from [Wikipedia](https://uk.wikipedia.org/) text, licensed under [CC BY-SA](https://creativecommons.org/licenses/by-sa/4.0/). The FrequencyWords Ukrainian list is not used: its tokenizer drops apostrophes and it holds many Russian words.

| File | Upstream file | Upstream SHA-256 | Trimmed SHA-256 |
|---|---|---|---|
| `uk/ukwiki-2022-08-30_top100k.txt` | `results/ukwiki-2022-08-30.txt` | `5bf9a83681d138a6db663b3d618e96e668a7e20a9c9d2812c39c7704d28cd784` | `dcbcb6611427423431ee9f1252463155190b41ee955eec7f61e014f32eaee5fe` |

## Hand-written files

Written for Rekey and licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/), so that each generated profile carries a single licence.

| File | Content |
|---|---|
| `en/language.json` | English name, alphabet, Layout ids, frequency list name |
| `en/short-words.txt` | Curated English one- and two-letter words |
| `ru/language.json` | Russian name, alphabet, Layout ids, frequency list name |
| `ru/short-words.txt` | Curated Russian one- and two-letter words |
| `uk/language.json` | Ukrainian name, alphabet, Layout ids, frequency list name |
| `uk/short-words.txt` | Curated Ukrainian one- and two-letter words |

## Generated profiles

`src/Rekey.Core/Profiles/{en,ru,uk}.json` are adaptations of the files above and are licensed under [CC BY-SA 4.0](https://creativecommons.org/licenses/by-sa/4.0/). Attribution: English and Russian word frequencies from FrequencyWords by Hermit Dave (OpenSubtitles 2018); Ukrainian word frequencies from wikipedia-word-frequency by Ilya Semenov (Ukrainian Wikipedia contributors). CC BY-SA 4.0 is one-way compatible with GPLv3, so the profiles ship inside the GPL-3.0-or-later app as separate data files.
