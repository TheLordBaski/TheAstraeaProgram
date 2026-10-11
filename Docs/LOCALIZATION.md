# Localization

All text the player reads goes through `Loc` (`Assets/TAP/Scripts/Game/Localization/Loc.cs`). English is the only language
through 1.0 (D-12), but adding another needs no code change.

## How it works

- **The English text in the code is the key.** A language is a JSON file in `Assets/TAP/Resources/Strings/`:

  ```json
  { "name": "Deutsch", "culture": "de-DE",
    "strings": { "Pitch down": "Nach unten neigen",
                 "Surface {0} m/s   Orbital {1} m/s": "Orbit {1} m/s | Boden {0} m/s" } }
  ```

  The file name is the language code; the language appears in *Settings → Gameplay → Language*. A text with no entry
  stays English.
- **Numbers are cut out of the lookup**: "Surface 16.9 m/s" looks up `Surface {0} m/s`, and a translation may move the
  `{0}`. Lines longer than 90 characters keep their numbers (they are prose). A text of several lines is looked up line by
  line. Tags (`<b>`, `<color>`) stay in the pattern.
- **The player's own text** (a craft or save name, the clock) is wrapped in `Loc.Name(...)` where the code builds the line, so
  it is not part of the pattern and is never translated. `Loc.Data(title)` does the same for data that has its own
  translation (a part title).
- **Every UI text is translated by one pass** (`Localizer` in TAP.UI): after the game writes English into a label, and before
  the frame is drawn, the label shows the player's language. Nothing needs a call at the place that sets the text. The pass
  is off in English unless the units are imperial or an audit runs. Add the class `no-loc` to an element to leave it alone.
- **Units**: *Settings → Gameplay → Units → Imperial* converts the number in front of `m`, `km`, `m/s`, `m/s²`, `kg`, `t` and
  `kPa` as the text is shown (feet, miles, ft/s, pounds, short tons, psi).
- Helpers for code that formats numbers itself: `Loc.Number`, `Loc.Plural`, `Loc.Date` (they follow the language's culture).

## The English table

`Strings/en.json` lists every text the game shows, so a translator has the list. It is made, not written:

1. *TAP → Localization → Update English Table* adds the texts of the screen layouts, the part, resource, body and biome names
   and descriptions, the control guide and the loading tips.
2. Texts built in code are added from a run: `-autotest locale` (editor: PlayerPrefs `TAP.AutoTest` = `locale`) walks the
   menu, space center, assembly building, flight display and map, writes `autotest_locale.md` (texts not in the table) and
   `autotest_locale.seen.txt` (every pattern shown). Add the seen texts with `LocalizationCatalog.Write(lines)`.

## The pseudo-locale

*Language: Pseudo-locale* (or `-locale pseudo` with the audit) shows every text as accented letters, 40% longer, in brackets:
`[Ĝöĺĩåţĥ ñåmé ~~~~]`. A button too small for it, a label that wraps badly or a text that stays plain English is a text
the translation would break on. The audit's screenshots are in `locale_pseudo/` next to its report.

## Known limits

- A line built from several pieces of data in code (a name inside a sentence) must wrap the data in `Loc.Name`, or its
  pattern changes with the data. The audit lists such lines.
- Plural forms use the English rule (one, other); a language with more forms needs `Loc.Plural` to learn them.
- Fonts: the panel's text settings fall back to a system font for symbols; a language with its own script needs a font
  fallback added there (`UIKit.CreateTextSettings`).
