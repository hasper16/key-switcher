# Rekey

Rekey watches what the user types and, when a word was typed in the wrong keyboard layout, retypes it in the right one and switches the layout.

## Typing

**Layout**:
A keyboard layout installed for the Windows user, mapping physical keys to characters (e.g. "English (US)", "Russian").
_Avoid_: input source, keyboard, input language

**Keystroke**:
One physical key press recorded by key position, independent of the layout that was active.
_Avoid_: key code, key event

**Word**:
The keystrokes typed since the last word boundary, still known to Rekey and still on screen.
_Avoid_: token, buffer

**Rendering**:
The text a Word produces when its keystrokes are read in a given Layout ("ghbdtn" in English (US) is "привет" in Russian).
_Avoid_: translation, conversion

**Trigger Key**:
A key that ends a Word and makes Rekey evaluate it automatically: Space, Enter or Tab, as configured. Space, Enter and Tab end a Word even when they are not Trigger Keys, but then Rekey does not evaluate it.
_Avoid_: delimiter, terminator

**Password Field**:
A focused text control that hides what is typed; Rekey never reads or changes text there.
_Avoid_: secure input

## Languages

**Language**:
A natural language Rekey can recognise (English, Russian, Ukrainian).
_Avoid_: locale, culture

**Language Profile**:
The recognition data for one Language: common words, valid one- and two-letter words, letter-pair and letter-triple frequencies, and the Layouts that type it.
_Avoid_: dictionary file, model

**Score**:
How plausible a Rendering is as text in a Language.
_Avoid_: confidence, probability

**Ambiguous Word**:
A Word whose Renderings are valid words in more than one Language, so Rekey leaves it as typed.
_Avoid_: conflict word

**Sensitivity**:
How much evidence Rekey needs before correcting: Cautious, Balanced (default) or Aggressive.
_Avoid_: aggressiveness, mode

## Corrections

**Correction**:
Replacing a Word on screen with its Rendering in another Layout and switching to that Layout.
_Avoid_: switch, fix, conversion

**Layout Switch**:
Changing the active Layout without changing any text.
_Avoid_: switch (alone), toggle

**Auto Correction**:
A Correction Rekey makes on its own when a Trigger Key ends a wrong-layout Word.
_Avoid_: auto-switch

**Manual Correction**:
A Correction the user asks for with the Hotkey, applied to the last Word.
_Avoid_: manual switch, forced switch

**Hotkey**:
The user-chosen gesture that asks for a Manual Correction, such as a double tap of Shift.
_Avoid_: shortcut, combo

**Decision**:
The outcome of evaluating a Word: correct it, keep it, or defer it.
_Avoid_: verdict, result

**Deferred Word**:
A Word whose Decision waits until the next Word shows which Layout the user is typing in.
_Avoid_: pending word

**Undo**:
The user reverting the last Correction with Ctrl+Z or the Hotkey within the Undo Window, which restores the typed text and the previous Layout.
_Avoid_: revert, rollback

**Undo Window**:
The time after a Correction during which Ctrl+Z is treated as an Undo (5 seconds, as in the original).

## User control

**Protected Word**:
A word Rekey never auto-corrects, added by the user or learned after repeated Undos of the same Correction.
_Avoid_: no-switch word, ignored word, exception

**Learned Word**:
A word Rekey accepts as valid in a Language although its Language Profile lacks it.
_Avoid_: custom word, user dictionary

**Excluded App**:
An application in which Auto Correction is off.
_Avoid_: blacklisted app, ignored app

**Pause**:
Auto Correction turned off everywhere until a set time.
_Avoid_: snooze, disable

## Health

**Incident**:
A recorded moment when Rekey lost or recovered its view of the keyboard, or stopped itself to stay safe.
_Avoid_: error, crash

**Usage Digest**:
A daily set of counters (Words analysed, Corrections, Undos) the user may agree to send; it never contains typed text.
_Avoid_: analytics, report
