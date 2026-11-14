# Rekey user guide

## Teaching Rekey

No word list covers every name, brand, slang word or abbreviation you type. Rekey learns from you in two ways and keeps what it learns in `%LOCALAPPDATA%\Rekey\words.json`, so it is still there after a restart.

### Protected Words: words Rekey leaves alone

A Protected Word is never corrected automatically. If Rekey corrects a word you meant to type as it was, undo the Correction (Ctrl+Z or the Hotkey right after it): Rekey restores your text and from then on leaves that word alone.

- Example: you type "ntcn" and Space in English (US) and Rekey turns it into "тест". You press Ctrl+Z. The next "ntcn" you type stays "ntcn".
- Words shorter than 3 letters are not protected this way, so one Undo of "yt" does not stop Rekey from correcting "yt" later.
- When several Words were corrected together, each of them with 3 letters or more becomes protected, and a group of Words that contains a Protected Word is not corrected.
- Protection is matched without regard to case or to punctuation around the word.
- The Hotkey still corrects a Protected Word: when you ask explicitly, Rekey does what you ask.

### Learned Words: words Rekey accepts

When you correct a word with the Hotkey and the result is not a word Rekey knows in that Language, Rekey learns it. From then on it treats the word as a common word of that Language and corrects it automatically like any other.

- Example: you type "rhby;" in English (US); Rekey does not know "кринж", so it leaves the word. You press the Hotkey and it becomes "кринж". The next time you type "rhby;" and Space, Rekey corrects it to "кринж" on its own.
- If Rekey learned a word by mistake, undo a Correction that typed it: the word is forgotten.
- Converting selected text with the Hotkey teaches Rekey nothing, since a selection can be a whole sentence.

### Managing the lists

The settings window will show both lists, where you can add and remove words, with a change taking effect for the next word you type; until that window arrives, the lists are kept in `words.json` and change only through Undo and the Hotkey as described above. Each list holds up to 10,000 words. When a list is full, Rekey drops the word it learned on its own that was used least recently; words you added yourself are never dropped, and adding one more to a list full of your own words is refused.
