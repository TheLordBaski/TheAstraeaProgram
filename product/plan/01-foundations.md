# 01 · Foundations (FND)

Everything after the vertical slice needs these first. The slice was built around two bodies and one game mode:

- `CelestialSystem.Root` is Tellus.
- The sun is a fixed light direction (`system.json → sun.direction`).
- `TerrainGenerator` has two hard-coded generators (`"tellus"`, `"luma"`).
- `MissionTracker`, `DesignAnalysis` and the developer window name Luma directly.
- Saves are `GameSave.version = 1`.
- Keys are hard-coded in `FlightInput`, and there is no settings menu.
- Builds are Windows only, and tests run from local scripts.

These tasks turn the slice into a base that 15 more worlds, three game modes and two platforms can stand on.

Task format used in every plan file: **Milestone** (when it ships) · **Claude** (build-and-test days at the
vertical-slice pace) · **You** (your days: decisions, review, playtesting, art) · **Needs** (tasks that must land first).

---

### FND-01 · The star becomes the root of the system
**Milestone** M1 · **Claude** 4 d · **You** 1 d · **Needs** FND-15

Planets orbit a star, and transfers, launch windows, day and night, solar power and heating all depend on it. Today
Tellus sits still at the origin and the sunlight comes from a constant direction. Build this on the multi-system data
model (FND-15): the home system is the first of several, so nothing may assume there is only one star.

- `system.json`: a new root body, the star (id, name, radius, GM, luminosity, colour, surface temperature). Tellus gets
  an orbit around it (working value a ≈ 14 Gm, a year of about 440 Tellus days); Luma is unchanged.
- The star has no terrain, atmosphere or SOI limit. `BodyPosition` already chains parents; audit every position path
  for double precision at 10¹¹ m (floats only for origin-relative Unity coordinates).
- `ReferenceFrame` accepts the star as frame body (heliocentric coasts); floating origin and Krakensbane keep working
  at solar distances.
- Lighting: the sun direction is recomputed each frame from the star's position to the camera, replacing
  `sun.direction`. Eclipses test every body between the camera and the star, not only the frame body.
- Calendar: the year is Tellus's orbital period, and the HUD clock shows year/day/hour from it.
- Rails warp in the star frame; warp limits for the star (no limit beyond 1 Gm).
- Saves: vessel states are stored relative to their body and remain valid; the migration comes with FND-05.

**Done when**
- [x] Unit test: a vessel on a solar orbit returns to its start after one period within 1 m.
- [x] Unit test: leaving Tellus's SOI into the star frame and coming back in is continuous: under 0.1 m in position
      and 10⁻⁴ m/s in velocity at each transition.
- [x] New autotest `escape`: leave Tellus on a hyperbola whose solar orbit has Tellus's own period, circle the star
      for a year at maximum warp and re-enter Tellus's SOI where the patched-conic prediction says. It passes in the
      build.
- [x] All existing missions (`orbit`, `lunar`, `persistence`, `docking`, `failures`, `suborbital`) pass in the build.
- [x] Screenshots from two dates half a year apart show the sunlight coming from a different direction.
      ([day 1](../../Screenshots/sun_direction_day1.png), [day 220](../../Screenshots/sun_direction_halfyear.png))

### FND-02 · Data-driven terrain and biome framework
**Milestone** M1 · **Claude** 5 d · **You** 1 d · **Needs** —

Fifteen new worlds can't each be a hard-coded generator, and science needs biomes on every body.

- A layered generator described in `system.json` replaces the `"tellus"`/`"luma"` switch. Each layer is data with its
  own seed, blending and masks:
  - Base shape: radius, minimum and maximum height.
  - Noise layers: fBm, ridged, billow.
  - Crater fields: size distribution, depth, rim, age and erosion.
  - Rifts/canyons, terraces, dunes, polar caps, ocean level.
- **Biomes**: ordered rules. Each rule can test height band, latitude, slope, distance to shore, noise regions and
  named special areas. A biome has an id, a display name, a map-overlay colour and a material index for rendering.
- Flattened areas (the launch complex today) become a generic `flatAreas` list. Bases (your decision, 2026-09-28)
  flatten ground on any body and biome while the game runs: a plane that may tilt with the land, since buildings get
  foundations that make up the difference.
- Chunk and collider generation runs in jobs (Burst if it helps). Heights are deterministic, identical on Windows
  and Linux.
- Tellus and Luma are re-expressed in the new format. Either heights stay within 1 m of today's at 10,000 sample
  points, or a deliberate visual change is documented with before/after screenshots. (Your decision, 2026-09-28: they
  may look different.)

Built: the format is in [Docs/TERRAIN_FORMAT.md](../../Docs/TERRAIN_FORMAT.md), and Tellus and Luma are presets in
`Resources/Data/Terrain`.
- **Layers:** noise (fBm, ridged, billow), crater fields with size classes, age and erosion, rifts along paths, canyon
  networks, terraces, dunes, ellipsoid shapes (for Granum), and masks by field, height, slope, latitude, distance to
  the shore or to a site, with ragged edges.
- **Polar caps** are masks. **Ocean level** is `seaLevel`.
- **Flat areas:**
  - They take a fitted or level plane.
  - They are added and removed at runtime, which remakes the chunks and colliders under them.
  - A landed vessel whose ground moved while it was away is set back on the ground when it loads.
- **Jobs:**
  - Chunks were already made on worker threads.
  - Collider cooking moved into Unity jobs (`Physics.BakeMesh`).
  - Burst was left out: the data-driven layers are plain C#, and a Burst copy would need the same bits as the managed
    code that answers height queries. A chunk takes about 9 ms on Tellus and 13 ms on Luma, which keeps up.
- **Determinism:** heights and biomes use plain arithmetic and the terrain's own maths (`DetMath`), and are checked
  against a recorded fingerprint.
- **Launch complex:** it moved to a natural coastal plain at 84.6° W, with the sea 38 km east. Tellus is turned 84.6°
  at UT 0, so the pad starts the game where it did before.

**Done when**
- [x] Tellus and Luma look as before (screenshots) and the `lunar` mission passes. (They changed on purpose, as you
      allowed: [map before](../../Screenshots/fnd02_tellus_map_before.png) and
      [after](../../Screenshots/Terrain/home_tellus_surface.png), launch site
      [before](../../Screenshots/fnd02_launch_site_before.png) and [after](../../Screenshots/fnd02_launch_site_after.png),
      Luma [before](../../Screenshots/fnd02_luma_before.png) and [after](../../Screenshots/fnd02_luma_after.png); `lunar`
      19/19 in the build, and every other mission passes.)
- [x] `BiomeAt(body, lat, lon)` gives at least 8 biomes on Tellus and 6 on Luma, and a debug map overlay shows them.
      (Tellus 11, Luma 7; developer window → Biome map: [Tellus](../../Screenshots/fnd02_map_biomes_tellus.png),
      [Luma](../../Screenshots/fnd02_map_biomes_luma.png); flat maps with shares in `Screenshots/Terrain`.)
- [x] Unit tests cover determinism (the same heights twice, and the same values on Linux) and biome rule order.
      (Covered:
      - the tests: the same heights, colours and biomes twice, and the first matching rule wins;
      - a fingerprint of 1,500 heights and biomes matches bit for bit in Unity and in the same source built with
        .NET 9, on Windows and on Linux (WSL Ubuntu 24.04, 2026-10-01);
      - the check catches one height changed in its last bit.)
- [x] A new body can be added with JSON alone. SYS-03 (Pruina) proves it. (Proved first by the debug system's
      Testrock, which uses every kind of layer: [view](../../Screenshots/fnd02_testrock.png),
      [biomes](../../Screenshots/Terrain/debug_testrock_biomes.png). SYS-03 repeats it for the home system.)

### FND-03 · Scaled-space rendering: planets, star and sky from anywhere
**Milestone** M1 · **Claude** 4 d · **You** 1 d · **Needs** FND-01

- Distant bodies are drawn in a scaled-space layer: a separate camera at a 1:N scale, with a seamless hand-over to
  the near terrain LOD.
- The star has a disc, corona, bloom and lens flare. There's a starfield skybox. Night sides get a faint ambient.
- Atmosphere shells work for every atmospheric body (today only Tellus). Bodies too small to resolve become a point
  of light plus a label.
- No z-fighting or jitter anywhere from low orbit to 10¹¹ m.

**Done when**
- [x] Screenshots from low Tellus orbit, the Luma surface and 20 Gm out show every body in the right place
      (`Screenshots/far_view_low_orbit_sun.png`, `far_view_low_orbit_luma.png`, `far_view_luma_surface.png`,
      `far_view_20gm.png`; every body within 0.00 px of its true direction).
- [x] No flicker while zooming the map from 1 km to 100 Gm (17 zoom steps, two consecutive frames identical at each;
      `map_zoom_1km.png`, `map_zoom_100gm.png`).
- [x] The extra cost is under 1 ms per frame: 0.8 ms CPU and 0.5 ms GPU in the editor (profiler recorders on the far
      camera). FND-13 repeats the measurement in its player benchmark.

### FND-04 · Content validation and hot reload
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Schema checks at load for `parts.json` and `system.json`, and later for tech, experiments, contracts and
  strategies. Errors are readable: file, entry, field, expected vs found.
- Editor menu *TAP → Validate Content*, plus an EditMode test that loads all content.
- Development hot reload of data in play mode (part stats, body parameters; meshes are excluded), so balancing
  doesn't need a restart.

Built: [Docs/CONTENT_VALIDATION.md](../../Docs/CONTENT_VALIDATION.md).
- **Schema:** the data classes are the schema. Every field must be one they have and of the right kind; rules per
  kind of content cover parts, resources, systems with their terrain presets, the galaxy and the starter craft.
- **Readable errors:** each names the file, line, entry, field, what was expected and what was found (for example
  `Data/parts.json line 175: part eng_hornet, engine.ispVac: expected a number above 0 (seconds), found nothing.`).
  A value a body takes from a terrain preset is reported in the preset's file.
- **At load:**
  - A part or resource with errors is left out and the rest load.
  - A craft using it isn't opened, a saved vessel using it isn't flown (it stays in the save), and a launch with it
    is refused, each saying why.
  - A system file with errors isn't loaded.
  - Problems are logged once and listed on the main menu.
- **Tools:** *TAP → Validate Content* and `ContentTests` (9 tests: all shipped content clean, the broken Isp, kinds
  and unknown fields, syntax errors, system and preset lines, the family tree, the galaxy, hot reload of parts and
  of an atmosphere). The Celestial Body Lab checks its drafts with the same rules.
- **F8:**
  - Part definitions are updated in place, so parts already flying use the new values from their next step.
  - A body's atmosphere, names, map colour, warp limits and a star's light change at once.
  - Models, nodes and what a part carries reach vessels built afterwards; a body's size, mass, orbit, spin and
    terrain need a restart.
  - Data with errors keeps the previous values.

**Done when**
- [x] A broken entry (for example a missing engine Isp) gives one clear error, not a NullReferenceException in
      flight. (In the editor with the Hornet's `ispVac` removed: one console error and the main menu list; the
      Meridian refused in the assembly building; a saved Pathfinder held back instead of flown; a direct launch
      refused, with no exception. Screenshots `Screenshots/fnd04_*.png`.)
- [x] Changing a thrust value in the JSON and pressing F8 in the editor is visible in the next ignition. (Goliath
      thrust raised in `parts.json` with the Pathfinder on the pad, F8: the next ignition gave 1,140.9 kN, twice the
      old engine's at that pressure; `Screenshots/fnd04_hot_reload_ignition.png`.)

### FND-05 · Save format v2: versioning, migration, backups
**Milestone** M1 · **Claude** 3 d · **You** 0.5 d · **Needs** FND-06

- `GameSave.version` with a migration chain (v1 → v2 → …). Old saves always load, and the Early Access promise is
  that saves survive updates (see decision D-13).
- New sections: mode, science archive, tech, funds and reputation, contracts, facilities, crew traits.
- Atomic writes (a temporary file, then rename) and rotating backups: the last 5 `persistent` saves and all
  quicksaves.
- An autosave interval (setting). Save metadata for the save browser: mode, in-game date, vessel count, play time
  and a thumbnail.
- A corrupted save is detected on load and restored from the newest good backup, with a message.
- A save corpus in `Assets/TAP/Tests/Saves` (v1 saves from the slice), loaded by a test on every build.

**Done when**
- [x] Every save in the corpus loads, with the same vessels, crew and positions.
- [x] Killing the game in the middle of a save never corrupts it (a test truncates the temp file).
- [x] Backups rotate.

Built (`SaveMigration.cs`, `SaveStorage.cs`, `SaveTests.cs`, 13 tests; format 2):
- **Migration:** `SaveMigration` upgrades the JSON one version at a time; a save from a newer game is refused and left
  alone. To change the format: bump `Current`, add a step, add a corpus save of the old format.
- **Corpus:** six v1 saves in `Assets/TAP/Tests/Saves` (the slice and the star-systems builds).
- **Writes and backups:** a flushed temporary file replaces the save; the last 5 `persistent` saves are kept in
  `Saves/<name>/Backups`. A damaged file is never rotated in.
- **Damage:** the newest good backup is restored (the bad file stays as `persistent.json.damaged`) and a toast says
  so; with no usable backup the main menu lists the save as unreadable and never starts a new game over it.
- **Metadata:** `mode`, `playTime`, in-game date and vessel count show in the load list. Autosave:
  `SaveStorage.AutosaveMinutes` (default 5, 0 = off), for the settings screen (FND-08).
- **Not yet:**
  - The save thumbnail, with the save browser (UX-02).
  - The sections for science, tech, funds, contracts, facilities and crew traits: each task adds its fields with a
    migration step. `mode` stays a string until FND-06.
  - Quicksaves keep their single slot, with no backup history.

### FND-06 · Game modes: sandbox, science, career
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- The mode lives in `GameSave`.
- One service answers the feature gates: is this part researched, can I afford it, is this facility level enough,
  are contracts on, can crew be hired.
- Sandbox answers "yes" to all of them, which is today's behaviour. Science gates parts by tech. Career gates
  everything.
- A stub new-game dialog (mode, name, difficulty placeholder); the full flow is in UX-02.

**Done when**
- [x] One code path queries the gates, and a unit test covers each mode's answers.
- [x] Sandbox saves from the slice load as sandbox.

Built (`ModeRules.cs`, `GameModeTests.cs`, 4 tests). Only sandbox can be started; Science and Career are prepared:
- **The mode:** `GameSave.mode` is a `GameMode` (Sandbox, Science, Career), saved by name. `GameSession.NewGame` takes a
  mode. The slice's saves load as sandbox (the corpus test checks it).
- **The gates:** `ModeRules.Current` answers for the game being played: `PartAvailable`, `CanAfford`, `FacilityAllows`,
  `ContractsOn`, `CanHireCrew`. Sandbox allows everything and offers no contracts (the plan said "yes to all", but
  contracts on in sandbox would be wrong). The assembly building's part list already asks `PartAvailable`.
- **Fail closed:** Science and Career answer no where they need state that doesn't exist yet. Each gate names the task
  that will replace its placeholder: tech state (TEC-02), funds (CAR-01), facility levels (CAR-03).
- **Not built:** the new-game dialog with a mode choice, and any Science or Career behaviour.

### FND-07 · Scene flow with the space center hub
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-06

- Main menu → space center hub (placeholder: the launch complex with clickable building markers) → assembly
  building, tracking station, launch. The finished hub is HUB-02.
- Leaving a flight (recover, back to the center, revert) returns to the hub. Revert to launch and revert to assembly
  keep working.
- Scenes load asynchronously behind a loading screen with tips (UX-12).

**Done when**
- [x] This loop works with no dead ends, and the `orbit` mission's recovery lands you back in the hub:
      menu → hub → assembly → launch → recover → hub → tracking station → fly a vessel → hub.
      (The new `flow` autotest clicks through the real screens: 10/10 in the editor and in the build. The `orbit`
      mission now ends with a recovery and checks the next scene is the hub.)

Built (`SpaceCenterScene.cs`, `SpaceCenterUI.cs`, `LoadingScreen.cs`, `FlowTest.cs`, `SceneFlowTests.cs`):
- **The hub:** a `SpaceCenter` scene with the launch complex as placeholder boxes (assembly building, pad with a rocket,
  tracking station) and a marker over each. Clicking a marker or a building opens it: the assembly building loads the
  assembly scene; the pad flies the rocket waiting on it (with none, a note says to build one); the tracking station lists
  every vessel with a Fly button. A top bar shows the save, mode and date, with Main menu.
- **Flow:** the main menu (Continue, New sandbox, Load) always goes to the hub. Recovering a vessel, *Space center* in the
  pause menu, and a flight that has no vessel go to the hub. *Revert to launch* and *Revert to assembly building* work as
  before. The assembly building has a *Space center* button next to *Menu*.
- **Loading:** `GameSession.LoadScene` is the only way scenes change. It loads asynchronously behind a loading screen with
  the scene's name, a progress bar and a tip. The tips are in code for now; FND-10 moves them into the string tables.
- **Tests:** autotest `flow` (menu → hub → assembly → launch → recover → hub → assembly → launch → leave → tracking
  station → fly → hub → pad); `SceneFlowTests` checks every scene is in the build.
- **Fixed on the way:** `UIKit.Show(element, true)` did nothing for an element hidden by a USS class that had never been
  hidden from code.
- **Not built:** hub camera and facility levels (HUB-02), facilities other than the three, a tracking-station map (NAV-06).

### FND-08 · Settings system
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Settings are a JSON file in the user folder, next to saves, not the registry. The zoom-speed preference migrates.
- Graphics: resolution, window mode, vsync, frame cap, quality preset, shadows, anti-aliasing, render scale, terrain
  detail, scatter density.
- Audio: master, music, effects, interface, ambience.
- Gameplay: autosave interval, tooltips, units.
- Interface: UI scale and HUD scale.
- Everything applies at runtime without a restart where Unity allows. Safe defaults on first start, and command-line
  overrides (`-screen-width`, `-safe-mode`).

**Done when**
- [x] Every setting survives a restart. (`SettingsTests.EverySetting_SurvivesARestart_...` saves every field, reloads, and
      compares the JSON.)
- [x] Deleting the settings file restores the defaults. (The same test; the file is deleted and the defaults come back.)
- [x] `-safe-mode` starts at 1280×720, windowed, low quality. (In the build the log says `window is 1280x720 Windowed,
      preset Low`; without the flag, `1920x1080 Windowed, preset High`.)

Built (`GameSettings.cs`, `SettingsApply.cs`, `SettingsUI.cs`, `SettingsTests.cs`, 8 tests):
- **The file:** `settings.json` in the user folder next to the saves (`SaveStorage.Root`), written atomically a moment after
  the last change and when the game ends. A damaged file is kept as `settings.json.damaged` and the defaults are used.
  Values out of range are brought back.
- **Migration:** the first run without a file takes the old PlayerPrefs (zoom speed, autosave, planet quality, clouds,
  rocks) into it and removes the keys.
- **Graphics:** resolution, window mode, V-sync, frame cap, a preset (Low, Medium, High, Ultra, Custom), shadows,
  anti-aliasing (MSAA), render scale, terrain detail, surface rocks, planet quality and clouds. The pipeline asset is copied
  at start and the copy is edited, so the editor never writes a setting into the project.
- **Audio:** master, music, effects, interface, ambience. Master sets `AudioListener.volume`; the game has no sound yet
  (AUD tasks), so the others are read through `GameSettings.Current.audio.Volume(bus)` when it does.
- **Gameplay:** autosave interval, tooltips, mouse-wheel zoom speed. `units` is stored; its choice appears with the
  formatting helpers in FND-10.
- **Interface:** interface scale (the panel's reference size) and flight display scale (each HUD block grows about the
  screen edge it hugs).
- **Screens:** *Settings* in the main menu, the space center and the pause menu, which replaces the old *Planet graphics*
  panel. Everything applies at once.
- **Command line:** `-safe-mode` (1280×720, windowed, Low, nothing saved), `-screen-width`, `-screen-height`,
  `-window-mode windowed|borderless|fullscreen`. The window size from the command line is not written to the file.

### FND-09 · Input: rebinding, joysticks, precision controls
**Milestone** M1 · **Claude** 3 d · **You** 1 d · **Needs** FND-08

- Every key (flight, EVA, map, assembly, developer) moves into the Input System actions asset. The in-game control
  guide (F1) is generated from the bindings, so it can never be out of date.
- Rebinding with conflict detection and reset to defaults, saved per user; the UI is in UX-03.
- Bindings follow physical key positions, so QWERTZ and AZERTY keyboards get the same layout.
- Joystick and HOTAS axes (pitch, yaw, roll, throttle, translation) with deadzone, sensitivity and inversion. A
  basic gamepad map.
- **Precision controls**: Caps Lock for fine attitude control (reduced authority, as in KSP) and trim
  (Alt + W/A/S/D/Q/E, Alt+X to reset).
- Action-group keys (FLT-07).

**Done when**
- [x] Every action can be rebound. (`InputTests`: every button and every axis is rebound and the new key or stick works;
      Settings → Controls does it in game, checked with a simulated key press.)
- [ ] A joystick flies the `orbit` mission by hand (you). The bindings, dead zones and the throttle lever are there; it
      needs a real stick.
- [x] Trim holds a pitch without input. (`Trim_HoldsAPitchWithoutInput`; in the editor Alt + W for 2 s gave 0.47 trim,
      still 0.47 with the keys up, and Alt + X cleared it.)
- [x] F1 shows the rebound keys. (`TheGuide_ShowsTheReboundKeys`; the guide is made again each time it opens.)

Built (`ControlTable.cs`, `Controls.cs`, `InputTests.cs`, 15 tests):
- **One table:** every key of flight, EVA, map, assembly building and developer tools is a row in `ControlTable` (about 120
  controls: id, guide line, keyboard, gamepad and joystick defaults). It is in code rather than a `.inputactions` file, so
  the guide, the rebinding screen, the conflict rule and the tests all read the same rows. `Controls` builds an Input
  System action asset from it. Code asks `Controls.Down("Flight/Stage")`; a test checks that every id used in code is in
  the table. Mouse buttons are not in it; a few appear as text in the guide.
- **Guides:** the F1 guide, the main menu's Controls list and the assembly building's help are made from the table, with the
  keys as bound now.
- **Rebinding:** *Settings → Controls* (click, press a key, Esc cancels; *Default* and *Reset all*). A key another control
  used is taken from it, with a note. Flight and EVA share keys on purpose and never conflict. Saved per user in
  `controls.json`. Keys are bound by physical position (`<Keyboard>/w`), so QWERTZ and AZERTY keep the layout; a test
  checks that no default binds by the letter shown.
- **Sticks:** gamepad (left stick pitch and yaw, right stick roll, triggers throttle, buttons for stage, SAS, RCS, legs, warp,
  pause) and joystick (stick pitch and yaw, trigger stage) defaults. Each axis has a dead zone, sensitivity and inversion in
  Settings → Controls; a throttle lever can set the throttle (HOTAS) instead of changing it. A HOTAS twist or slider is
  bound by pressing it in the rebinding screen.
- **Precision and trim:** Caps Lock cuts the attitude input to a quarter. Hold Alt and press W A S D Q E to trim; trim stays
  with the keys released, is saved with the vessel (`ControlRecord`), and Alt + X clears it.
- **Action groups:** Alt + 1 to 0 are in the table and raise `FlightInput.ActionGroup`; FLT-07 gives them effects.
- **Shortcuts:** Input System shortcut consumption is on, so Ctrl + S saves and the S that rotates a held part stays quiet.

### FND-10 · Localization-ready text
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- String tables (a JSON file per language) with keys; UIKit labels take keys.
- Part, body, experiment, contract and handbook texts live in the tables.
- Formatting helpers for numbers, units, dates and plurals.
- A pseudo-locale (+40% length, accented letters) to catch overflow and hard-coded strings.
- English only through 1.0 (D-12). Adding a language later must need no code changes.

**Done when**
- [x] With the pseudo-locale, no visible hard-coded English is left in the menus, HUD, assembly building or map. (The
      `locale` autotest walks 30 screens and states with the pseudo-locale on and lists every text that is not in
      `Strings/en.json`: 0 of 455 patterns. Its screenshots show the longer text.)

Built (`Loc.cs`, `Localizer.cs`, `LocalizationCatalog.cs`, `LocaleTest.cs`, `Docs/LOCALIZATION.md`, 9 tests):
- **Tables:** a language is `Resources/Strings/<code>.json` (`name`, `culture`, `strings`). The English text is the key, with
  numbers cut out (`Surface {0} m/s`), so a translation can move them. Adding a language needs no code; it appears in
  Settings → Gameplay → Language. `en.json` (549 texts) is made by *TAP → Localization → Update English Table* and the
  audit run, and holds the screen layouts' text, part, resource, body and biome names and descriptions, the control guide
  and the loading tips. The keys are the English texts rather than
  names: every UI text is translated by one pass over the screens (`Localizer`), so nothing had to change at the hundreds
  of places that set text.
- **Pseudo-locale:** accented letters, tags and `{0}` holes kept, 40% padding, in brackets. It showed the assembly bar and
  some HUD lines overflowing; the bar's buttons now grow with their text.
- **Helpers:** `Loc.Number`, `Loc.Plural`, `Loc.Date` follow the language's culture; `Loc.Name` and `Loc.Data` keep the
  player's own text and data titles out of the lookup.
- **Units:** *Settings → Gameplay → Units → Imperial* converts the number in front of a metric unit as the text is shown
  (ft, mi, ft/s, lb, short tons, psi), in every screen at once.
- **Audit:** autotest `locale` (`-locale pseudo|en|<code>`) writes the missing texts per screen and screenshots.
- **Limits:** see [Docs/LOCALIZATION.md](../../Docs/LOCALIZATION.md): lines that mix data into a sentence must wrap the data
  in `Loc.Name`; plural forms are English's; non-Latin scripts need a font fallback.

### FND-11 · Linux build and continuous integration
**Milestone** M1 · **Claude** 3 d · **You** 1 d · **Needs** —

- A Linux x86_64 player from `BuildScript`, with the Mono scripting backend (plugin mods need it, D-11).
- A first run on a real Linux machine (Ubuntu LTS with the Steam runtime), running the batch-mode autotests.
- **GitHub Actions** (GameCI) on every push to `main`:
  - EditMode tests;
  - Windows and Linux builds kept as artifacts.
- **Nightly**, on a self-hosted runner (your PC): all mission autotests in batch mode, with the results in the job
  summary.
- The version from `git describe` is stamped into the build and shown in the main menu and the logs.

**Done when**
- [ ] A push produces both builds and a test report. (The workflow is written; it needs the Unity licence secrets in the
      repository and a push to run. [Docs/CI.md](../../Docs/CI.md) lists the steps.)
- [ ] The nightly run posts pass/fail per mission. (Workflow and runner scripts are written; it needs the self-hosted runner on
      your PC. The script itself ran locally: `flow` PASSED 10/10 and a failing mission gives exit code 1.)
- [ ] The Linux build passes `orbit` and `lunar`. (`BuildScript.BuildLinux` is written, and the editor reports "build target was
      unsupported": the *Linux Build Support (Mono)* module is not installed; `unity install-modules -e 6000.5.7f1 -m linux-mono`,
      56 MB, adds it.)

Built (`BuildScript.cs`, `ci.yml`, `nightly.yml`, `tools/ci/*`, [Docs/CI.md](../../Docs/CI.md)):
- **Players:** `BuildWindows`, `BuildLinux` (Mono), batch entry points and `CiBuild` for GameCI. Each stamps `git describe
  --tags --always --dirty` into `Application.version` (restored afterwards), shown in the main menu's footer and the first log line
  with Unity version, OS and GPU.
- **CI:** `ci.yml` on every push to `main` and on pull requests: EditMode tests with a report (the two window tests are in the
  category `NeedsGraphics` and left out), then the Windows and Linux players as artifacts. `nightly.yml`: on a self-hosted runner,
  builds Windows and runs every mission, with a table in the job summary.
- **Mission runners:** `run-missions.ps1` and `run-missions.sh` run a list of autotests in batch mode, write one table, and exit
  with 1 when a mission did not pass (`.sh` also tested here with a stand-in player).
- **Not done:** everything that needs the licence secrets, the runner or the Linux module (the boxes above).

### FND-12 · Logs, crash handling and in-game bug reports
**Milestone** M1 · **Claude** 1.5 d · **You** 0.5 d · **Needs** FND-05

- Rotating log files with version, OS, GPU and settings.
- *Report a problem* in the main menu and pause menu. It zips the logs, current save, a screenshot, the settings and
  the mod list into the user folder, then opens the feedback page (REL-05).
- An unhandled exception shows a small non-blocking notice (write a report?) instead of failing silently.

**Done when**
- [x] A forced exception in flight produces a notice, and the report zip contains everything needed to reproduce it. (In the editor,
      in flight: an exception raised a notice in the corner, *Write a report* opened the dialog with the exception in it, and the
      zip held `report.txt`, `logs/` (four runs and Unity's log), `save/current-state.json` and `persistent.json`, `settings.json`,
      `screenshot.png`, `mods.txt` and `system.txt`. `BugReportTests` checks the zip's contents and the exception line.)

Built (`GameLog.cs`, `BugReport.cs`, `ProblemReportUI.cs`, `BugReportTests.cs`, 6 tests; [Docs/LOGS.md](../../Docs/LOGS.md)):
- **Logs:** `Logs/tap-<date>.log` in the user folder, one per run, the last five kept. The header has the version, OS, CPU, GPU,
  screen, command line and settings; then everything Unity logs, with stacks. Errors flush at once, the rest every two seconds.
- **Notice:** an unhandled exception shows *Something went wrong: …* with *Write a report* and *Dismiss*, once a minute per
  exception, and the game goes on. *Developer tools → Throw a test exception* tries it.
- **Report:** *Report a problem* in the main menu, the pause menu and the notice. It takes the screenshot, asks what happened, and
  writes `Reports/report-<date>.zip` with the logs, the game as it is now, the save files, settings, controls, screenshot, mod list
  and machine, then offers to open the folder and the feedback page (the issue tracker until REL-05).
- **Not done:** a native crash (the process dies) is not caught; the log is flushed up to the last write and the next start does not
  offer a report. The feedback page is a placeholder.

### FND-13 · Performance baseline and budgets
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Benchmark scenes:
  - launches of 50, 150 and 300-part rockets;
  - a landed base of 10 vessels;
  - the map with 40 vessels;
  - maximum warp.
- **Budgets**:
  - 60 fps at 150 parts and 30 fps at 300 parts, on a GTX 1660 / Ryzen 5 class PC at 1080p;
  - under 20 s from the main menu to the hub;
  - under 4 GB of memory.
- Profiler markers around each simulation step. `-benchmark` writes a CSV, and CI keeps the trend.

**Done when**
- [ ] The benchmark runs headless in CI. (It runs headless on Windows, with and without `-nographics`: all scenarios, exit 0. The Linux job
      in `ci.yml` and the nightly steps are written but have not run; they need the CI set-up of FND-11.)
- [x] The baseline numbers are recorded in `Docs/PERFORMANCE.md`. (The CPU's share of a frame, no window, on the development PC. The frame rate with a GPU,
      and the reference PC, are not measured: see "Not measured" there.)
- [ ] Regressions over 10% show in the job summary. (`compare-benchmark.ps1` marks them and writes the table to `GITHUB_STEP_SUMMARY`; checked against a
      second run on this PC, not in a job.)

Built (`SimTimers.cs`, `BenchmarkRunner.cs`, `BenchmarkCraft.cs`, `compare-benchmark.ps1`, [Docs/PERFORMANCE.md](../../Docs/PERFORMANCE.md)):
- **Markers:** `SimTimers.Measure(SimStep)` around pre-step, post-step, rails, render update, planets, trajectory, map, HUD and effects. A scope is a
  `TAP.<step>` profiler marker (editor and development builds) and a stopwatch in every build.
- **`-benchmark`:** six scenarios (launches of 50, 150 and 300 parts, a base of 10 landed vessels, the map with 40, maximum warp) plus the time to the menu and
  to the hub; writes one CSV row each with frame statistics, the time in each step and the memory. `-benchmark-out`, `-benchmark-scenarios`, `-benchmark-seconds`.
- **CI:** `compare-benchmark.ps1` against a baseline in `Docs/benchmarks/` (more than 10% slower is marked, budgets are checked); steps in `ci.yml` (Linux,
  `-nographics`) and `nightly.yml` (Windows, with and without a window).
- **Numbers:** 1.2 to 2.4 ms of CPU per frame for 50 to 300 parts, 330 to 380 MB, 1.1 s from start to the space center. The planets cost more than the physics.
- **Not done:** the frame rate with a GPU and the budgets on the reference PC (the windowed run is yours); `UITKTextHandle.ShapeText` throws a
  NullReferenceException about 17 times per `-batchmode` start (it did before this task; the run goes on).

### FND-14 · Remove single-mission assumptions; game events and objectives
**Milestone** M1 · **Claude** 3 d · **You** 0.5 d · **Needs** FND-01

- The hard-coded bodies become data or a selectable destination:
  - `"luma"` and `"tellus"` in `MissionTracker`;
  - the Δv readout against Luma in `DesignAnalysis`;
  - the body buttons in the developer window.
- **A game-event bus**: launched, staged, reached space, orbit, SOI change, landed or splashed, EVA, flag planted,
  docked, crew recovered, science collected, part destroyed, crew lost. It feeds the mission guide, progression,
  contracts, achievements and tutorials.
- **A generic objectives system**: conditions from the events plus the vessel state, and a panel in flight. The Luma
  mission guide becomes one objective set in data.

**Done when**
- [x] No gameplay code names a body. (`SystemTests.GameplayCode_LooksUpNoBodyById` finds no body id outside `Core/`;
      `ObjectiveTests.PresentationCode_NamesNoBody` finds no body name in what the UI, the flight, assembly and localisation code say; the
      objective files are checked the same way. What is left is the names of starter crafts, "Luma Pathfinder", which are data, and the mission
      autopilot, the test harness for the one mission (it finds the moon as `HomeMoon`; its log lines say "Luma").)
- [x] The Luma mission guide behaves exactly as before, now driven by data. (The full `lunar` mission autotest in a Windows build passes 21/21; its new
      last check finds 14 of the 15 objectives done after the splashdown at home (the 15th, recovering, is the next step). The `orbit` autotest passes
      11/11 and its new check finds launch, space and orbit done once in orbit. The ids and the conditions are those of the old `MissionTracker`, so older
      saves carry on; the editor showed the title, the list, the hint and the `»` switch to the other set. Not compared step by step with the old tracker.)
- [x] A second objective set ("reach Pruina orbit") works without code changes. (`orbit_moon.json` is a second set for the home system, in the game
      now: the `»` button of the guide switches to it. `ObjectiveTests.ASetCanLeadToAnotherBody_WithoutCodeChanges` runs a set for another body of
      the debug system, written in the test as JSON only. There is no body called Pruina yet; a set for it is a file.)

Built (`GameEvents.cs`, `EventWatcher.cs`, `ObjectiveSet.cs`, `ObjectiveTracker.cs`, `Data/Objectives/*.json`, `ObjectiveTests.cs`, 14 tests; [Docs/OBJECTIVES.md](../../Docs/OBJECTIVES.md)):
- **Bus:** `GameEvents.Raise` in `TAP.Core`, a static event with the last 100 kept; a listener that throws does not stop the others. The simulation raises
  `Launched`, `Staged`, `PartDestroyed`, `CrewLost`, `EvaStarted`, `EvaEnded`, `Docked`, `FlagPlanted`, `CrewRecovered` and `VesselRecovered` where they happen;
  `EventWatcher` raises `ReachedSpace`, `Orbit`, `SoiChanged`, `Landed`, `Splashed` and `ReentrySurvived` from the vessels' state.
- **Objectives:** a set is a JSON file; each objective has a condition (event, body, situation, altitude, crew, flags, what is done, `any`, `not`) and
  texts with `{home}`, `{moon}`, `{body:REF}` and `{space}`. `ObjectiveTracker` checks them in order until nothing changes, writes what is done to the
  save (`milestones`; the first set keeps the old plain ids, so older saves carry on) and shows the notice. A file with a mistake is left out and logged.
- **Bodies gone from code:** `MissionTracker` is deleted; the guide's title and hints, the editor's readout (`EditorEnvironment` is a body and air or
  vacuum, cycled over every body of the system), the developer window's body buttons, the help texts, the menu's picture and footer and the loading
  tips come from the system.
- **Not done:** `ScienceCollected` is defined and nothing raises it (SCI tasks). Only the home system's two sets exist. The guide shows one set at a time and
  has no list of every set. The objectives' texts are in `en.json` for the home system only.

### FND-15 · Multi-system data model: a galaxy of one system, ready for more
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

More planetary systems are planned (see [20-multi-system-and-trade.md](20-multi-system-and-trade.md)). They arrive
after 1.0, but retrofitting a single-system game is far more expensive than starting multi-system now.

- **Galaxy file** (`galaxy.json`): the list of star systems, each with an id, a name, its position in light-years and
  its system file. 1.0 ships one entry, the home system.
- **Namespaced ids**: body ids belong to a system (`home/tellus`), with the bare ids of the slice kept as aliases.
- **One simulation per system.** Vessels, frames, rails, the map and scaled space live inside their system. Saves
  store the system id for every vessel, flag and landmark; nothing is positioned in a galaxy frame.
- **No single-star assumptions.** Code asks the current system for its star instead of reading a single
  `System.Root`. Scaled-space layers, the sky and the map can switch systems.
- **A test system**: a small debug system (a star and one planet) that the game can load instead of the home
  system. It proves nothing names a home body.
- **Interstellar travel is not built here.** The data model only has to allow it (vessels changing system, long
  transits); the mechanics are decided later (D-17).

**Done when**
- [x] The game runs end to end with the debug system as the home system: hub, assembly, launch, orbit, map, save and
      load.
- [x] Saves carry system ids, and slice saves migrate to `home/...`.
- [x] Nothing in gameplay code assumes a single star (a test searches for `System.Root` use outside the system
      service, and another for literal body ids).

### FND-16 · Terrain streaming benchmark
**Milestone** M1 · **Claude** 1 d · **You** — · **Needs** FND-13

Step 1 of the terrain plan in [TERRAIN_LOD_RESEARCH.md](../../Docs/TERRAIN_LOD_RESEARCH.md): measure terrain streaming before
optimising it. The FND-13 scenarios launch from the pad, where the terrain barely changes.

- Two scenarios: `descent` (a dive from orbit to the surface of Tellus) and `low-pass` (1 km above the ground at 2 km/s).
- Terrain columns: worker time per chunk, the queue, uploads per frame, garbage and collections, and how long the
  ground under the camera stays coarser than the LOD wants.

**Done when**
- [x] Both scenarios run headless in the Windows player and in the editor.
- [x] The CSV has the terrain columns, and `compare-benchmark.ps1` compares them.
- [x] A baseline with these numbers is recorded in `Docs/PERFORMANCE.md` ("Baseline: 2026-10-18").

Built (`BenchmarkRunner.cs`, counters in `CubeSphere.cs` and `PlanetTerrain.cs`, `compare-benchmark.ps1`,
[Docs/PERFORMANCE.md](../../Docs/PERFORMANCE.md)):
- **Numbers (player, no window):** 3.8 ms of worker time per chunk; the terrain's main-thread share is 0.35 to 0.41 ms.
  The chunk under the camera is never more than a frame or two coarser than wanted. Uploads come in bursts that hit the
  cap of 12 a frame.
- **What it says:** six workers could make about 1,500 chunks a second, and the paths ask for 145 to 176, so the
  first-in-first-out queue costs nothing yet. The cost that shows is garbage: 89 to 106 collections in 10 s while
  streaming, against 28 to 31 on the pad (FND-17).
- **Not done:** the same run found `map-40`, `warp-max` and the start-up slower than on 2026-10-12. The home system has
  grown to 22 bodies since; not investigated here.

### FND-17 · Garbage-free terrain chunks
**Milestone** EA1 · **Claude** 1.5 d · **You** — · **Needs** FND-16

Every chunk allocates about ten managed arrays (roughly 100 to 160 KB), and the main thread copies them into the mesh.
While streaming, Mono collects three times as often as on the pad, and every collection stops the main thread.

- Pooled native buffers per worker for samples, fields and vertices.
- Meshes filled with `Mesh.AllocateWritableMeshData` off the main thread and applied with
  `ApplyAndDisposeWritableMeshData`; one native index buffer per resolution, handedness and skirt setting.
- Collision chunks use the same path.

**Done when**
- [x] `descent` and `low-pass` collect garbage no more often than the pad scenarios (within 10%). (Two player runs: `descent` 7 and 13, `low-pass`
      6 and 6, against 10 to 16 on the pad; they were 89 and 106.)
- [x] The editor's allocation counter shows no managed allocation per chunk made. (A chunk made and given back allocates 0 B, against 64 KB
      without the pool; `descent` allocates 18 MB in the editor instead of 223 MB. What is left per chunk is its quadtree node, mesh object and
      work item, a few hundred bytes.)
- [x] The terrain fingerprint and the collision tests are unchanged. (All 222 EditMode and 40 body lab tests pass, the fingerprint among them.)

Built (`CubeSphere.cs`, `PlanetTerrain.cs`, `PlanetManager.cs`, `TerrainTests.ReusedChunk_IsTheSameAsAFreshOne`,
[Docs/PERFORMANCE.md](../../Docs/PERFORMANCE.md) "after FND-17"):
- **A pool, not native buffers.** `TerrainChunkGenerator` rents `ChunkData` per layout (resolution, skirts, colours), with at most 64 spare per
  layout. `Release` gives a chunk back: the terrain does that once the mesh has its copy, the colliders once they are applied, and both do it for a
  dropped result. Samples, fields and directions are scratch per thread, and the skirt perimeter is cached. Callers that keep their chunk (the far
  view, the body lab) need no change.
- **Not done as planned:** meshes are still filled with `SetVertices` and the other array setters, not `Mesh.AllocateWritableMeshData`. The main
  thread's share did not change (0.35 to 0.42 ms), so the pool was enough. Native buffers come with FND-19, where the Burst job needs them.
- **Side effects:** worker time per chunk fell from 3.8 to 3.2 ms. The longest frame while streaming fell from 15.6 to 6.2 ms.

### FND-18 · Terrain LOD morphing
**Milestone** EA1 · **Claude** 1.5 d · **You** 0.5 d · **Needs** FND-02

A chunk swaps to its four children in one frame, which shows as popping on descent. The ocean is flattened into the
same mesh, so the coastline jumps too.

- Each vertex also stores its height as the parent chunk would give it, in a spare vertex channel.
- The vertex shader blends from that height to the real one over the last part of the split distance (as in CDLOD).
- Collisions are unchanged: they use their own full-detail grid.

**Done when**
- [x] You see no popping in a recorded descent from orbit to the launch site, and coastlines do not jump. (Approved on 2026-10-18 from the
      benchmark descent at a quarter speed, morphing on and off side by side, levels 9 to 13: no popping, and the coast is fine. A render test through the terrain shader finds a child
      chunk, at the distance where it replaces its parent, within 1 colour level of the parent; off, it differs by up to 142 levels.)
- [x] `planets_ms` in `descent` and `low-pass` is no more than 10% higher. (+4% and +5%, FND-17 and FND-18 players run alternately;
      `PERFORMANCE.md`.)

Built (`CubeSphere.cs`, `TerrainMorph.hlsl`, `Terrain.shader`, `PlanetTerrain.cs`, `TerrainTests.ChunkMorphTarget_IsItsParentsSurface`):
- **What a vertex carries.**
  - Its point on the parent's surface: a parent vertex, the middle of a parent edge, or the middle of the parent quad's diagonal (a, c), where the
    parent splits it.
  - The parent's normal there (octahedral) and its colour and water flag, in UV2 and UV3, with the parent's arc.
  - The parent's own normals and colours at its vertices are worked out as the parent does: every second vertex of the chunk, with a 2-sample border.
- **The shader.** All three passes (lit, depth, depth-normals) move from the parent's surface, normal and colour to the chunk's own over 90% to 60%
  of the parent's split distance. That distance is the parent's arc × `_TerrainMorphSplit` (a global: SplitFactor × the detail setting). The
  chunk's own children appear only under 50%, once it has settled. Root chunks and the far view never morph.
- **Checks.**
  - A test checks, on every body and for all four children, that the fully morphed position, normal, colour and water flag are the parent's.
  - `PlanetTerrain.MorphEnabled` switches morphing off, for comparisons only.
- **Costs:** +4 to 5% main-thread terrain time, +23% worker time per chunk, and about +70 MB for 32 more bytes per vertex. FND-19 can win the memory
  back with half-precision channels.
- **Not done:** a chunk that arrives late (its parent kept waiting for it) starts partly morphed and still moves a little. Measured lag is 0.00 to
  0.06 s, so this is rare.
- **Fixed on 2026-10-19.** In the map, Tellus showed as a bulging, part-black blob.
  - The cause: the map's sphere and the far view use this shader with meshes that have no UV2/UV3, and D3D12 does not read missing
    channels as zero.
  - The fix: morphing now needs the material's `_LodMorph`, which only `PlanetTerrain` sets on its own material (the map's copy turns it off).
    With it off, the shader reads nothing from UV2/UV3.
  - `PlanetVisualTests.TerrainShaderLeavesMeshesWithoutMorphDataAlone` renders such a mesh to keep it that way.

### FND-19 · Burst terrain evaluator
**Milestone** EA1 · **Claude** 3 d · **You** — · **Needs** FND-17

`LayeredTerrain.Sample` interprets the terrain definition one sample at a time in managed code. BurstPQS showed this
step about six times faster when every operation runs over a whole chunk in Burst. That buys room for more noise,
finer chunks and a deeper quadtree.

- Each body's terrain definition compiles once into a flat list of operations (native arrays of plain structs). The JSON
  format does not change.
- A Burst job runs each operation over all of a chunk's samples before the next one.
- Chunk buffers move from FND-17's managed pool to native arrays the job can write, and meshes are filled from them with
  `Mesh.AllocateWritableMeshData`. The two LOD morph channels of FND-18 can then be half floats (about 70 MB back).
- `DetMath` and the noise are ported to Burst with `FloatMode.Strict` (no reassociation, no fused multiply-add).
- The managed path stays as the reference and the fallback. Colliders, altitude queries and map exports use the new
  evaluator too.

**Done when**
- [ ] The terrain fingerprint is bit for bit identical on the Burst path, in the editor and in the Windows player.
- [ ] Worker time per chunk in `descent` is at least three times lower.
- [ ] A setting or command-line switch selects the managed path, and a test compares the two.

### FND-20 · Terrain request scheduling: priority and look-ahead
**Milestone** EA2 · **Claude** 1 d · **You** — · **Needs** FND-16

The work queue is first in, first out, so a far chunk asked for earlier is built before a near one. FND-16 found that
this costs nothing yet: the workers are mostly idle. Build this task when more detail (FND-19, more octaves, a deeper
quadtree) makes the queue fill up.

- A priority queue ranked by distance or screen error, re-ranked each frame; chunks in the view frustum first.
- Requests around where the camera will be 1 to 2 s ahead, along its velocity.
- Stale requests dropped before they run; the work moved to Unity's job system.

**Done when**
- [ ] With the queue full, `detail_lag_s` in `descent` and `low-pass` is lower than with first in, first out.
