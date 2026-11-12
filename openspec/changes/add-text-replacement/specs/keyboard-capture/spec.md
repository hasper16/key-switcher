# Spec Delta

## MODIFIED Requirements

### Requirement: Keys and events that end a Word
Space, Enter (on the main keyboard or the numeric keypad) and Tab SHALL end the Word whatever character they produce, as in the original; only a Trigger Key makes Rekey evaluate it (spec `auto-correction`). Arrow keys, Home, End, Page Up, Page Down, Delete, Escape, any key combined with Ctrl, Alt or Win, a mouse click, or a focus change SHALL end it without evaluation, except as stated under "Suggestion list focus keeps the Word".

#### Scenario: Space that is not a Trigger Key
- **WHEN** Space is not a Trigger Key and the user types "ghb", Space, then "dtn"
- **THEN** the current Word is "dtn" and "ghb" was not evaluated

#### Scenario: Caret moved
- **WHEN** the user types "ghb", presses Left Arrow, then types "dtn"
- **THEN** the current Word is "dtn"

#### Scenario: Mouse click
- **WHEN** the user types "ghb" and clicks elsewhere in the text
- **THEN** the current Word is empty

## ADDED Requirements

### Requirement: Suggestion list focus keeps the Word
While the user types the Keystrokes of a Word into a text control, a focus move inside the same window to an element that takes no text and is not a menu, menu item, button or Password Field (such as a row of an autocomplete suggestion list) SHALL NOT end the Word, and the text control stays the Word's control (owner decision). Such a move that follows any other key or a click SHALL end the Word.

#### Scenario: Suggestion list opens while typing
- **WHEN** the user types "ghbdtn" into an editor, its suggestion list takes focus after "g" while keys still reach the editor, and the user presses Space
- **THEN** the Word "ghbdtn" is evaluated, and focus coming back to the editor is not a focus change

#### Scenario: Navigation key, then a list
- **WHEN** the user types "ghb", presses Tab, and focus moves to a list row in the same window
- **THEN** the current Word is empty, and the keys typed next belong to the list row, not the editor

#### Scenario: Menu or another text control
- **WHEN** the user types "ghb" and focus moves to a menu item or to another text control in the same window
- **THEN** the current Word is empty
