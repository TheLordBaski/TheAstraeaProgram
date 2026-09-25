# The Astraea Program (TAP) — a rocket spaceflight sandbox

The Astraea Program (TAP) is a playable spaceflight game in the spirit of Kerbal Space Program, focused entirely on
rockets. You build a rocket from parts in the assembly building, launch it from the pad on the planet **Tellus**, fly it to orbit, plan a
transfer with manoeuvre nodes, land on the airless moon **Luma**, walk around and plant a flag, fly home, survive reentry
and land under a parachute. Names, parts, planets, characters, models and interface are original.

Built with **Unity 6.5 (6000.5.7f1), Universal Render Pipeline 17.5**, new Input System, uGUI/TextMeshPro. All part and
planet models are generated procedurally from the data files at runtime (no imported art packs).

## Getting the code

Repository: <https://github.com/TheLordBaski/TheAstraeaProgram>. Images and other binary files are stored with
**Git LFS**, so install it before cloning (`git lfs install`), then open the folder in Unity 6000.5.7f1. Player builds
(`Build/`), `Library/` and IDE files are not in the repository; the menu *TAP → Build Windows Player* recreates the build.

## Running it

* **Standalone build**: `Build/Windows/TAP.exe` (Windows 64-bit). Menu → *New sandbox* → the assembly building.
* **From the editor**: open the project folder in Unity 6000.5.7f1, open `Assets/TAP/Scenes/MainMenu.unity`
  and press Play. `Flight.unity` can also be played directly (it launches the Luma Pathfinder on the pad).
* **Rebuild**: menu *TAP → Build Windows Player* (or `TAP.EditorTools.BuildScript.BuildWindows`).
  *TAP → Build All Assets* regenerates textures, icons, materials, starter craft and scenes.

## Documentation

| Document | Contents |
|---|---|
| [Docs/CONTROLS.md](Docs/CONTROLS.md) | Every key and mouse control: flight, EVA, map, assembly building |
| [Docs/LUNAR_WALKTHROUGH.md](Docs/LUNAR_WALKTHROUGH.md) | Step-by-step guide to the complete Luma mission with the starter rocket |
| [Docs/PLANETARY_SYSTEM.md](Docs/PLANETARY_SYSTEM.md) | Radii, masses, gravity, atmosphere, rotation, orbits, SOI, warp limits, Δv budget |
| [Docs/FEATURES_AND_LIMITATIONS.md](Docs/FEATURES_AND_LIMITATIONS.md) | What is implemented, what was verified and how, known limitations |
| [Docs/IMPLEMENTATION_CHECKLIST.md](Docs/IMPLEMENTATION_CHECKLIST.md) | Milestone checklist and verification log |

## Starter vehicles (assembly building → Load)

| Rocket | Purpose | Stages | Mass | Vacuum Δv | Lift-off TWR |
|---|---|---|---|---|---|
| **Skylark Suborbital** | Solid-fuel hop into space (apex ~170 km) and back under a parachute | finned SRB (80% thrust) → capsule separation → chute | 9.2 t | 2,450 m/s | 2.2 |
| **Meridian Orbiter** | Two-stage liquid rocket to low orbit and back, heat-shielded capsule | lifter → vacuum upper stage → capsule | 10.1 t | 3,900 m/s | 1.8 |
| **Luma Pathfinder** | The complete crewed lunar mission | 4 boosters + core → transfer stage → wide lander → capsule | 80.0 t | 7,640 m/s | 2.0 |

## How the game is organised

Code lives in `Assets/TAP/Scripts`, split into assemblies with one-way dependencies:

| Assembly | Responsibility |
|---|---|
| `Core` | Double-precision math (Vector3d, QuaternionD), Kepler orbits (universal variables), celestial bodies, atmosphere model, procedural terrain generators |
| `Parts` | Data-driven part catalogue (`Resources/Data/parts.json`), procedural part meshes, staging model, Δv calculator |
| `Persistence` | Serializable records (craft designs, vessels, parts, crew, saves) and JSON storage |
| `Trajectory` | Patched-conic predictor (SOI entry/exit, encounters, closest approach) |
| `Construction` | Attachment maths (stack nodes, surface attach, symmetry), starter craft |
| `Simulation` | Flight physics: vessels as single rigid bodies, engines, fuel flow, aerodynamics, heating/ablation, structural loads, collisions, parachutes, legs, EVA, docking, rails/physics time warp, floating origin + Krakensbane, planets and terrain streaming |
| `Game` | Scene controllers, launch/recovery/saves, assembly editor logic, map view, manoeuvre planning, mission tracker, scripted test pilot |
| `UI` | All interface (built from code): flight HUD and navball, map UI, pause menu, part menus, assembly editor UI, main menu |
| `App` | Scene bootstraps and command line |

Data files: `Resources/Data/system.json` (planets, launch site), `Resources/Data/parts.json` (36 parts + EVA suit and flag),
`Resources/Craft/*.json` (starter rockets). User data (saves, craft, test reports) goes to
`%USERPROFILE%/AppData/LocalLow/Astraea Works/The Astraea Program/TAP/` (on first start the game copies ships
and saves from the folder it used while it was called Starwright).

## Verification

* **EditMode unit tests** (Unity Test Runner, 18 tests): Kepler propagation against numerical integration (elliptic,
  hyperbolic, radial), element round trips, apsis timing, manoeuvre nodes several orbits ahead in the patched-conic
  predictor, attachment maths, radial symmetry, staging order, rocket-equation Δv, fuel-flow blocking by decouplers,
  starter-rocket soundness, lossless craft and save JSON.
* **Automated missions** flown by a scripted pilot that uses only the player's controls
  (`TAP.exe -autotest <mission> -report <file>`; in the editor set `PlayerPrefs "TAP.AutoTest"`):
  `orbit` (launch, orbit, drift checks, rails warp, orbital EVA and boarding, reentry, splashdown), `lunar`
  (the complete Luma mission), `suborbital`, `failures` (structural overload, overheating, unsafe parachute, impact),
  `persistence` (quicksave → warp → quickload → compare → continue), `docking` (RCS approach, capture, undock),
  `lunar-landing` / `lunar-surface` (resume from
  the snapshots the lunar test saves in Luma orbit and after touchdown, then fly the rest of the mission),
  `ascent` with `-craft <file>` (flies any design to orbit and reports the Δv left — used to size the Pathfinder),
  `luma-idle` (loads the landed snapshot and waits, for inspection).
  Reports of the latest runs are in [Docs/TestReports](Docs/TestReports): `<mission>.md` from the editor and
  `build_<mission>.md` from the standalone build (`build_lunar.md` = the complete lunar mission). Results are listed in
  [Docs/FEATURES_AND_LIMITATIONS.md](Docs/FEATURES_AND_LIMITATIONS.md).
