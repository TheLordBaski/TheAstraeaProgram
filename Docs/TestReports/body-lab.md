# Celestial Body Lab verification

Verified on 2026-09-30 in the running Windows Unity Editor, **6000.5.7f1 / URP 17.5**.

## Automated results

The final complete EditMode run after the authoring expansion finished in 7.81 s: **96 passed, 0 failed, 0 skipped, 0 inconclusive**.

| Assembly | Passed | Coverage |
|---|---:|---|
| `TAP.Tests.BodyLab` | 40 | Draft history, session restoration, inheritance, safe saves and colour precision, renaming, generation round trips over positive/negative/extreme seeds, sampled bounds over 24 worlds, moon/planet SOI safety, all biome themes, biome insertion and Undo/Redo, every DTO property's hover help, correct contextual units, actual system/parent orbit propagation, attached orbit drawing/UI callbacks, cancellation and resource disposal |
| `TAP.Tests.EditMode` | 56 | Existing core, system, terrain, orbit, construction, persistence and simulation regressions, including project body-lookup conventions |

The original lab's 18 tests are retained. The existing suite initially caught a hard-coded default body ID; default selection reads `launchSite.body` from the selected system. Expansion regressions caught detached JSON edits and colour round-trip precision; both are fixed and covered by the final suite.

## Live editor checks

- The lab window loaded body controls and rendered Tellus using the game's terrain and atmosphere shaders. The attached-panel test changed a radius field and verified that its value reached the draft.
- Visually inspected the surface globe and north-up surface map, biome globe/map, and local surface patch. Diagnostic captures changed only preview view state and restored it afterward.
- Compilation completed with `EditorUtility.scriptCompilationFailed == false`.
- `CompilationPipeline.GetAssemblies(AssembliesType.Player)` contained neither `TAP.Editor` nor `TAP.Tests.BodyLab`.
- The development scene was absent from enabled `EditorBuildSettings.scenes`. Its saved `SceneRoots` list is empty; generated objects are temporary.
- After the test runner's scene cleanup, the development preview was reopened with the user's Tellus selection and clean draft preserved.
- Verified the live window contains Random Planet, Random Moon, Random Biomes and Add Random Biome controls.
- An isolated attached orbit view included a generated moon alongside all three existing bodies. Parent focus showed the planet and both moons; advancing by one quarter of the generated moon's period moved its marker about 30.3 million metres without editing the user's draft.
- The attached-panel regression exercised orbit drawing; data tests compare markers and inclined orbit paths against the game's own propagation in system and parent frames.
- Final live check confirmed the user's current unsaved `newbody` draft was retained, its terrain preview was ready, compilation succeeded, and the editor assemblies/development scene remained excluded from player builds.

Captures: [surface globe](../../Screenshots/CelestialBodyLab/live_preview.png), [surface map](../../Screenshots/CelestialBodyLab/live_map.png), [biome globe](../../Screenshots/CelestialBodyLab/biomes_preview.png), [biome map](../../Screenshots/CelestialBodyLab/biomes_map.png), [surface patch](../../Screenshots/CelestialBodyLab/surface_patch.png), [height map](../../Screenshots/CelestialBodyLab/height_map.png).

## Verification limits

Linux editor operation remains untested. No player build was needed for this editor-only change; exclusion was checked using the live compilation pipeline and build settings. Preview maps and geometry have finite resolution, so coverage and extrema are sampled estimates. No performance claim is made for complex user-authored terrain.
