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
- [ ] Every save in the corpus loads, with the same vessels, crew and positions.
- [ ] Killing the game in the middle of a save never corrupts it (a test truncates the temp file).
- [ ] Backups rotate.

### FND-06 · Game modes: sandbox, science, career
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- The mode lives in `GameSave`.
- One service answers the feature gates: is this part researched, can I afford it, is this facility level enough,
  are contracts on, can crew be hired.
- Sandbox answers "yes" to all of them, which is today's behaviour. Science gates parts by tech. Career gates
  everything.
- A stub new-game dialog (mode, name, difficulty placeholder); the full flow is in UX-02.

**Done when**
- [ ] One code path queries the gates, and a unit test covers each mode's answers.
- [ ] Sandbox saves from the slice load as sandbox.

### FND-07 · Scene flow with the space center hub
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-06

- Main menu → space center hub (placeholder: the launch complex with clickable building markers) → assembly
  building, tracking station, launch. The finished hub is HUB-02.
- Leaving a flight (recover, back to the center, revert) returns to the hub. Revert to launch and revert to assembly
  keep working.
- Scenes load asynchronously behind a loading screen with tips (UX-12).

**Done when**
- [ ] This loop works with no dead ends, and the `orbit` mission's recovery lands you back in the hub:
      menu → hub → assembly → launch → recover → hub → tracking station → fly a vessel → hub.

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
- [ ] Every setting survives a restart.
- [ ] Deleting the settings file restores the defaults.
- [ ] `-safe-mode` starts at 1280×720, windowed, low quality.

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
- [ ] Every action can be rebound.
- [ ] A joystick flies the `orbit` mission by hand (you).
- [ ] Trim holds a pitch without input.
- [ ] F1 shows the rebound keys.

### FND-10 · Localization-ready text
**Milestone** M1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- String tables (a JSON file per language) with keys; UIKit labels take keys.
- Part, body, experiment, contract and handbook texts live in the tables.
- Formatting helpers for numbers, units, dates and plurals.
- A pseudo-locale (+40% length, accented letters) to catch overflow and hard-coded strings.
- English only through 1.0 (D-12). Adding a language later must need no code changes.

**Done when**
- [ ] With the pseudo-locale, no visible hard-coded English is left in the menus, HUD, assembly building or map.

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
- [ ] A push produces both builds and a test report.
- [ ] The nightly run posts pass/fail per mission.
- [ ] The Linux build passes `orbit` and `lunar`.

### FND-12 · Logs, crash handling and in-game bug reports
**Milestone** M1 · **Claude** 1.5 d · **You** 0.5 d · **Needs** FND-05

- Rotating log files with version, OS, GPU and settings.
- *Report a problem* in the main menu and pause menu. It zips the logs, current save, a screenshot, the settings and
  the mod list into the user folder, then opens the feedback page (REL-05).
- An unhandled exception shows a small non-blocking notice (write a report?) instead of failing silently.

**Done when**
- [ ] A forced exception in flight produces a notice, and the report zip contains everything needed to reproduce it.

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
- [ ] The benchmark runs headless in CI.
- [ ] The baseline numbers are recorded in `Docs/PERFORMANCE.md`.
- [ ] Regressions over 10% show in the job summary.

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
- [ ] No gameplay code names a body.
- [ ] The Luma mission guide behaves exactly as before, now driven by data.
- [ ] A second objective set ("reach Pruina orbit") works without code changes.

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
