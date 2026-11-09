"""Draws the held-out sentence samples in this folder from Tatoeba per-language exports.

Usage: python -I -X utf8 sample_tatoeba.py <export folder> <output folder>
The export folder holds eng_sentences.tsv, rus_sentences.tsv and ukr_sentences.tsv
(id, language, text per line). The same exports give the same samples.
"""
import random
import sys

SAMPLE_SIZE = 3000
SEED = 2026
LANGUAGES = {"eng": "en", "rus": "ru", "ukr": "uk"}


def main(source, target):
    for tatoeba, code in LANGUAGES.items():
        with open(f"{source}/{tatoeba}_sentences.tsv", encoding="utf-8") as f:
            rows = [line.rstrip("\n").split("\t") for line in f]
        rows = sorted(
            (int(row[0]), row[2]) for row in rows
            if len(row) == 3 and len(row[2].split()) >= 2)
        sample = sorted(random.Random(SEED).sample(rows, SAMPLE_SIZE))
        with open(f"{target}/{code}.tsv", "w", encoding="utf-8", newline="\n") as f:
            for sentence_id, text in sample:
                f.write(f"{sentence_id}\t{text}\n")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
