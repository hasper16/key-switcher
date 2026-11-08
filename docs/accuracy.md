# Detection accuracy

How well the wrong-layout Decision works on held-out text, measured by `tools/Rekey.Accuracy` (design D7 of `add-language-scorer`). The spec `wrong-layout-detection` requires, under Balanced Sensitivity, at most 0.5% false corrections and at least 95% corrections.

## Run it

From the repository root:

```bash
dotnet run --project tools/Rekey.Accuracy -c Release -- data/accuracy tests/fixtures/keymaps
```

Add `--sample <n>` to read only the first `n` sentences per Language, and `--check` to exit with code 1 when Balanced breaks the spec limits (CI runs both). The counts are deterministic; the latency figures depend on the machine.

## What it measures

- **Corpus**: 3,000 Tatoeba sentences per Language and a technical, mixed-language set (`data/accuracy/SOURCES.md`). None of it is used to build Language Profiles.
- **Active Layouts**: English (US), Russian, Ukrainian (Enhanced), from the captured Key Maps.
- **Correctly typed Words**: each sentence typed in its own Layout; Latin words of the technical set typed in English (US). A false correction is any Correction of such a Word.
- **Wrong-layout Words**: each sentence typed with its own Layout's keys while another Layout is active. A Word counts only when it reads differently there (most Russian words read the same in Ukrainian). It is corrected when a Correction, direct or through deferral, makes it the intended text.
- **Streams**: each source Language and Layout pair is typed as one continuous stream, so a deferred last Word of a sentence can be confirmed by the next sentence.
- Thresholds live in `DetectionThresholds` and the score calibration in `RenderingScorer`.

## Tuning record

| Run | Balanced corrections | Balanced false corrections |
|---|---:|---:|
| Initial design values | 74.77% | 0.01% |
| Same-script Renderings without evidence dropped before ambiguity | 76.31% | 0.02% |
| Success counted as the intended text | 78.97% | 0.02% |
| Deferred Word confirmed by any Layout it reads in | 81.29% | 0.02% |
| Edge punctuation not code-like; two word Renderings defer | 90.03% | 0.02% |
| Split English contractions are words | 92.36% | 0.03% |
| Valid current word with another word Rendering defers | 95.08% | 0.03% |
| Balanced defers weak letter evidence | 95.80% | 0.03% |

The gibberish ceiling (0.35) barely matters: 0.10 gives 95.93%, 0.50 gives 95.64%, with the same false corrections, so it keeps its initial value. Most remaining misses are Russian typed in a Ukrainian Layout (and back), where the typed text is often itself a word ("ті" for "ты"), and short Words whose run the next Word does not confirm.

## Latest report

### Cautious

False corrections: 1 of 55609 correctly typed Words (0.00 %)
Corrections: 79880 of 84158 wrong-layout Words (94.92 %)
Decision latency: p50 0.005 ms, p99 0.016 ms

| Stream | Words | Corrected | Share |
|---|---:|---:|---:|
| en typed correctly | 23050 | 0 | 0.00 % |
| en typed in ru | 23025 | 22911 | 99.50 % |
| en typed in uk | 23025 | 22894 | 99.43 % |
| ru typed correctly | 16963 | 0 | 0.00 % |
| ru typed in en | 16889 | 16698 | 98.87 % |
| ru typed in uk | 2740 | 1394 | 50.88 % |
| uk typed correctly | 14825 | 1 | 0.01 % |
| uk typed in en | 14789 | 13792 | 93.26 % |
| uk typed in ru | 3690 | 2191 | 59.38 % |
| technical | 771 | 0 | 0.00 % |

Most frequent problems:

```text
  110  miss   ru typed in uk: "Ті" kept, meant "Ты"
  110  miss   ru typed in uk: "ті" kept, meant "ты"
   98  miss   uk typed in en: "oj" kept, meant "що"
   87  miss   ru typed in uk: "Ві" kept, meant "Вы"
   80  miss   ru typed in uk: "бі" kept, meant "бы"
   80  miss   uk typed in ru: "Меры" kept, meant "Мері"
   70  miss   ru typed in uk: "ві" kept, meant "вы"
   69  miss   ru typed in uk: "Мі" kept, meant "Мы"
   58  miss   uk typed in en: "p" kept, meant "з"
   57  miss   uk typed in en: "Z" kept, meant "Я"
   52  miss   ru typed in uk: "біл" kept, meant "был"
   44  miss   ru typed in uk: "е'" kept, meant "её"
   41  miss   ru typed in uk: "біло" kept, meant "было"
   41  miss   uk typed in ru: "мены" kept, meant "мені"
   37  miss   ru typed in uk: "ещ'" kept, meant "ещё"
   37  miss   uk typed in en: "yt" kept, meant "не"
   36  miss   ru typed in uk: "мі" kept, meant "мы"
   35  miss   ru typed in uk: "Мєри" kept, meant "Мэри"
   35  miss   uk typed in ru: "э" kept, meant "є"
   33  miss   uk typed in ru: "ы" kept, meant "і"
   32  miss   uk typed in ru: "Мены" kept, meant "Мені"
   31  miss   uk typed in ru: "Меры." kept, meant "Мері."
   30  miss   uk typed in ru: "тобы" kept, meant "тобі"
   28  miss   ru typed in uk: "біла" kept, meant "была"
   28  miss   uk typed in ru: "выд" kept, meant "від"
   25  miss   uk typed in ru: "подобаэться" kept, meant "подобається"
   20  miss   uk typed in ru: "хотыла" kept, meant "хотіла"
   18  miss   uk typed in en: "]]" kept, meant "її"
   18  miss   uk typed in en: "nb" kept, meant "ти"
   18  miss   uk typed in en: "z" kept, meant "я"
   18  miss   uk typed in ru: "мый" kept, meant "мій"
   18  miss   uk typed in ru: "ъъ" kept, meant "її"
   17  miss   ru typed in uk: "Мєри." kept, meant "Мэри."
   16  miss   uk typed in en: "Oj" kept, meant "Що"
   14  miss   uk typed in en: "Nb" kept, meant "Ти"
   14  miss   uk typed in en: "db" kept, meant "ви"
   13  miss   uk typed in en: "wt" kept, meant "це"
   13  miss   uk typed in en: "yf" kept, meant "на"
   12  miss   uk typed in en: "Vths" became "Меры", meant "Мері"
   12  miss   uk typed in ru: "Бостоны." kept, meant "Бостоні."
   12  miss   uk typed in ru: "зы" kept, meant "зі"
   11  miss   uk typed in en: "'" became "э", meant "є"
   11  miss   uk typed in en: "Vths/" became "Меры.", meant "Мері."
   11  miss   uk typed in en: "w." kept, meant "цю"
   10  miss   en typed in uk: "еру" kept, meant "the"
   10  miss   ru typed in en: "b" kept, meant "и"
   10  miss   ru typed in en: "c" kept, meant "с"
   10  miss   uk typed in en: "dsl" kept, meant "від"
   10  miss   uk typed in en: "e" kept, meant "у"
   10  miss   uk typed in ru: "знаэте," kept, meant "знаєте,"
```

### Balanced

False corrections: 16 of 55609 correctly typed Words (0.03 %)
Corrections: 80621 of 84158 wrong-layout Words (95.80 %)
Decision latency: p50 0.002 ms, p99 0.005 ms

| Stream | Words | Corrected | Share |
|---|---:|---:|---:|
| en typed correctly | 23050 | 0 | 0.00 % |
| en typed in ru | 23025 | 22911 | 99.50 % |
| en typed in uk | 23025 | 22894 | 99.43 % |
| ru typed correctly | 16963 | 5 | 0.03 % |
| ru typed in en | 16889 | 16702 | 98.89 % |
| ru typed in uk | 2740 | 1584 | 57.81 % |
| uk typed correctly | 14825 | 11 | 0.07 % |
| uk typed in en | 14789 | 13827 | 93.50 % |
| uk typed in ru | 3690 | 2703 | 73.25 % |
| technical | 771 | 0 | 0.00 % |

Most frequent problems:

```text
  110  miss   ru typed in uk: "ті" kept, meant "ты"
  109  miss   ru typed in uk: "Ті" kept, meant "Ты"
   98  miss   uk typed in en: "oj" kept, meant "що"
   84  miss   ru typed in uk: "Ві" kept, meant "Вы"
   80  miss   ru typed in uk: "бі" kept, meant "бы"
   79  miss   uk typed in ru: "Меры" kept, meant "Мері"
   69  miss   ru typed in uk: "ві" kept, meant "вы"
   68  miss   ru typed in uk: "Мі" kept, meant "Мы"
   58  miss   uk typed in en: "p" kept, meant "з"
   52  miss   uk typed in en: "Z" kept, meant "Я"
   48  miss   ru typed in uk: "біл" kept, meant "был"
   44  miss   ru typed in uk: "е'" kept, meant "её"
   41  miss   ru typed in uk: "біло" kept, meant "было"
   36  miss   ru typed in uk: "ещ'" kept, meant "ещё"
   36  miss   ru typed in uk: "мі" kept, meant "мы"
   35  miss   ru typed in uk: "Мєри" kept, meant "Мэри"
   35  miss   uk typed in en: "yt" kept, meant "не"
   34  miss   uk typed in ru: "э" kept, meant "є"
   33  miss   uk typed in ru: "ы" kept, meant "і"
   29  miss   uk typed in ru: "тобы" kept, meant "тобі"
   28  miss   ru typed in uk: "біла" kept, meant "была"
   28  miss   uk typed in ru: "Меры." kept, meant "Мері."
   28  miss   uk typed in ru: "выд" kept, meant "від"
   18  miss   uk typed in en: "]]" kept, meant "її"
   18  miss   uk typed in en: "nb" kept, meant "ти"
   18  miss   uk typed in en: "z" kept, meant "я"
   18  miss   uk typed in ru: "ъъ" kept, meant "її"
   16  miss   ru typed in uk: "Мєри." kept, meant "Мэри."
   16  miss   uk typed in en: "Oj" kept, meant "Що"
   14  miss   uk typed in en: "Nb" kept, meant "Ти"
   14  miss   uk typed in en: "db" kept, meant "ви"
   13  miss   uk typed in en: "wt" kept, meant "це"
   12  miss   uk typed in en: "Vths" became "Меры", meant "Мері"
   12  miss   uk typed in en: "yf" kept, meant "на"
   12  miss   uk typed in ru: "зы" kept, meant "зі"
   11  miss   uk typed in en: "'" became "э", meant "є"
   11  miss   uk typed in en: "Vths/" became "Меры.", meant "Мері."
   10  miss   en typed in uk: "еру" kept, meant "the"
   10  miss   ru typed in en: "b" kept, meant "и"
   10  miss   ru typed in en: "c" kept, meant "с"
   10  miss   uk typed in en: "dsl" kept, meant "від"
   10  miss   uk typed in en: "e" kept, meant "у"
   10  miss   uk typed in en: "w." kept, meant "цю"
   10  miss   uk typed in ru: "ыз" kept, meant "із"
    9  miss   en typed in uk: "ещ" kept, meant "to"
    9  miss   uk typed in en: "s" kept, meant "і"
    8  miss   ru typed in en: "d" kept, meant "в"
    8  miss   ru typed in uk: "не'" kept, meant "неё"
    8  miss   uk typed in ru: "Всы" kept, meant "Всі"
    8  miss   uk typed in ru: "ъм" kept, meant "їм"
```

### Aggressive

False corrections: 50 of 55609 correctly typed Words (0.09 %)
Corrections: 81429 of 84158 wrong-layout Words (96.76 %)
Decision latency: p50 0.002 ms, p99 0.004 ms

| Stream | Words | Corrected | Share |
|---|---:|---:|---:|
| en typed correctly | 23050 | 5 | 0.02 % |
| en typed in ru | 23025 | 22851 | 99.24 % |
| en typed in uk | 23025 | 22836 | 99.18 % |
| ru typed correctly | 16963 | 11 | 0.06 % |
| ru typed in en | 16889 | 16663 | 98.66 % |
| ru typed in uk | 2740 | 2000 | 72.99 % |
| uk typed correctly | 14825 | 28 | 0.19 % |
| uk typed in en | 14789 | 14261 | 96.43 % |
| uk typed in ru | 3690 | 2818 | 76.37 % |
| technical | 771 | 6 | 0.78 % |

Most frequent problems:

```text
  110  miss   ru typed in uk: "ті" kept, meant "ты"
  105  miss   ru typed in uk: "Ті" kept, meant "Ты"
   79  miss   uk typed in ru: "Меры" kept, meant "Мері"
   65  miss   en typed in ru: "Яшкш" kept, meant "Ziri"
   65  miss   en typed in uk: "Яшкш" kept, meant "Ziri"
   46  miss   ru typed in uk: "біл" kept, meant "был"
   39  miss   ru typed in uk: "біло" kept, meant "было"
   36  miss   ru typed in uk: "ещ'" kept, meant "ещё"
   35  miss   ru typed in uk: "Мєри" kept, meant "Мэри"
   34  miss   uk typed in ru: "э" kept, meant "є"
   29  miss   uk typed in ru: "тобы" kept, meant "тобі"
   28  miss   ru typed in uk: "біла" kept, meant "была"
   28  miss   uk typed in ru: "Меры." kept, meant "Мері."
   26  miss   uk typed in ru: "выд" kept, meant "від"
   25  miss   uk typed in en: "Vths" became "Меры", meant "Мері"
   18  miss   uk typed in en: "Vths/" became "Меры.", meant "Мері."
   18  miss   uk typed in en: "]]" kept, meant "її"
   18  miss   uk typed in ru: "ъъ" kept, meant "її"
   15  miss   en typed in ru: "Ылгкф" kept, meant "Skura"
   15  miss   en typed in uk: "Ілгкф" kept, meant "Skura"
   15  miss   ru typed in uk: "Мєри." kept, meant "Мэри."
   13  miss   en typed in uk: "еру" kept, meant "the"
   12  miss   uk typed in en: "'" became "э", meant "є"
   11  miss   uk typed in en: "dsl" kept, meant "від"
    9  miss   uk typed in en: "nj,s" became "тобы", meant "тобі"
    8  miss   ru typed in en: ",sk" became "біл", meant "был"
    8  miss   ru typed in en: "to`" kept, meant "ещё"
    8  miss   ru typed in uk: "не'" kept, meant "неё"
    7  miss   ru typed in en: "Ns" became "Ті", meant "Ты"
    7  miss   uk typed in en: "levf'i?" became "думаэш,", meant "думаєш,"
    7  miss   uk typed in ru: "Всы" kept, meant "Всі"
    7  miss   uk typed in ru: "думаэш," kept, meant "думаєш,"
    6  miss   ru typed in en: "ns" became "ті", meant "ты"
    6  miss   ru typed in uk: "ч'м" kept, meant "чём"
    6  miss   uk typed in ru: "Меры," kept, meant "Мері,"
    6  miss   uk typed in ru: "Меры?" kept, meant "Мері?"
    5  miss   en typed in ru: "Яшкшэы" kept, meant "Ziri's"
    5  miss   en typed in uk: "Яшкшєі" kept, meant "Ziri's"
    5  miss   ru typed in en: "V'hb" kept, meant "Мэри"
    5  miss   ru typed in uk: "н'м" kept, meant "нём"
    5  miss   ru typed in uk: "новій" kept, meant "новый"
    5  miss   uk typed in en: "Levf'nt?" became "Думаэте,", meant "Думаєте,"
    5  miss   uk typed in en: "levf'nt?" became "думаэте,", meant "думаєте,"
    5  miss   uk typed in en: "}]" kept, meant "Її"
    5  miss   uk typed in ru: "Думаэте," kept, meant "Думаєте,"
    5  miss   uk typed in ru: "Тобы" kept, meant "Тобі"
    5  miss   uk typed in ru: "Усы" kept, meant "Усі"
    5  miss   uk typed in ru: "Ъъ" kept, meant "Її"
    5  miss   uk typed in ru: "всы" kept, meant "всі"
    5  miss   uk typed in ru: "думаэте," kept, meant "думаєте,"
```

Balanced is within the spec limits.
