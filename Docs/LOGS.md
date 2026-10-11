# Logs and problem reports

## Where things are

The user folder is `%USERPROFILE%\AppData\LocalLow\Astraea Works\The Astraea Program\TAP` on Windows and
`~/.config/unity3d/Astraea Works/The Astraea Program/TAP` on Linux.

| Folder or file | What |
|---|---|
| `Logs/tap-<date>.log` | The game's own log, one file per run, the last five kept. |
| `Reports/report-<date>.zip` | A problem report (below). |
| `settings.json`, `controls.json` | The settings and the rebinds. |
| `Saves/<name>/` | The saves and their backups. |

Unity's own `Player.log` is next to the user folder's parent (`…\Astraea Works\The Astraea Program\Player.log`); a report includes it.

## The log

Each file starts with the version, the start time, the OS, CPU, memory, GPU and driver, the screen, the command line and the whole settings
file, then holds everything Unity logs (`Debug.Log`, warnings, errors, exceptions with their stack traces). Errors and exceptions are written to
disk at once, the rest every two seconds, so a crash loses at most the last two seconds of ordinary messages.

## Unhandled exceptions

An exception that nobody catches (in an update, a button handler, a coroutine) is logged and shows a small notice in the corner:
*Something went wrong: …* with **Write a report** and **Dismiss**. The game goes on. The same exception shows one notice a minute, not one per frame.
*Developer tools → Throw a test exception* tries it.

## Report a problem

*Main menu → Report a problem* and *Pause menu → Report a problem* (and the notice's button) take a screenshot first, ask what happened, and
write one zip in `Reports/`:

| Entry | What |
|---|---|
| `report.txt` | What the player wrote, the version and machine, the scene, the save's mode, date and vessel count, the unhandled exceptions, the game-data problems, and anything that could not be added. |
| `logs/` | Every log file of the last five runs, and Unity's own log. |
| `save/current-state.json` | The game as it is this moment (in flight a capture of every vessel); `persistent.json` and `quicksave.json` as they are on disk. |
| `settings.json`, `controls.json` | The settings and rebinds. |
| `screenshot.png` | The screen before the dialog opened. |
| `mods.txt`, `system.txt` | The contents of the `Mods` folder, and the machine. |

Nothing is sent. The dialog offers to open the folder and the feedback page (until the community channels exist, REL-05, the project's
issue tracker); the player attaches the zip. A save in a report is a plain JSON file of the game's state: it holds the save's name, crew names
and vessel names, and nothing about the player's machine beyond `system.txt`.

## Limits

A native crash (the player process dies) leaves the log up to its last flush and Unity's crash dump in the temp folder; the report button
is not there to catch it. The next start does not offer a report for it yet.
