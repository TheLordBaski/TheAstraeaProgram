# Celestial Body Lab

Editor-only authoring for the existing body and FND-02 terrain JSON. Open **TAP > Terrain > Celestial Body Lab**.

## Authoring workflow

1. Open the lab from **TAP > Terrain > Celestial Body Lab**. It opens `Assets/TAP/Editor/Scenes/CelestialBodyLab.unity` additively and selects the system's launch body initially (Tellus in the home system).
2. Select another **System JSON** or **Body**, or use **New Body** / **Duplicate Draft**. A new body is an unsaved moon with a valid initial orbit and noise terrain. Set its ID, display name, type and physical parameters before saving.
3. Expand the parameter groups. Controls are generated from the existing body and terrain DTOs: atmosphere curves, orbit, rotation, ocean, terrain fields/layers, masks and terms, shaping, biomes, colours, shading and flat areas. Source/Shape selectors switch between supported alternatives; list rows support add, duplicate, remove and reordering. Optional groups can be added or disabled. **Draft JSON** also permits advanced edits and preserves unknown properties.
4. Watch the preview update after a short pause in editing. A coarse preview appears first, followed by a refined preview. Invalid settings show an error and retain the previous valid image until corrected.
5. Use **Save to System** to update or append the selected body. **Revert Draft** returns to the last save (or the initial new-body draft). Switching a dirty body prompts to save, cancel or discard.

### Undo and session lifetime

**Undo / Redo**, **Ctrl+Z**, **Ctrl+Y** and **Ctrl+Shift+Z** operate on the draft. Consecutive edits to one control are grouped into a gesture; structural edits are individual steps. History holds up to 128 undo snapshots. Saving retains history, so a saved change can still be undone and saved again.

Drafts and history live in memory, including Unity's session memory across script reloads and closing/reopening the window. Explicitly discarding a draft clears it. Ending the Unity Editor session clears the session state. History is never written into body, system or preset JSON.

### Preset inheritance and saving

The lab resolves presets with the same merge rules as the game. Editing an inherited scalar/object creates an override; editing an inherited list copies the entire resolved list, because JSON arrays replace their inherited arrays. Reset controls restore inherited values or DTO defaults.

The runtime merge ignores null overrides inside inherited objects. Use **Make Terrain Inline** to copy the resolved terrain into the body before removing an inherited object. This keeps shared presets intact.

**Save to System** validates the body, terrain and system graph and reads the latest source JSON before writing. It preserves other bodies, unknown properties and unrelated concurrent edits. If the selected body changed externally, saving is refused until reloaded. Renaming a body updates its parent references and the launch-site body reference.

- **Export Body JSON** writes the body object, suitable for a system's `bodies` array.
- **Export Terrain Preset** writes resolved terrain. Choosing an existing preset explicitly replaces it and affects its other consumers.
- **Export Current Map PNG** writes the current surface, biome or height map with north at the top.

### Random worlds and biome starting points

Use **Random Planet** or **Random Moon** to create an unsaved body from **Generation Seed**. **Shuffle Seed** chooses another seed. Random operations advance the seed after success; enter an earlier seed to reproduce its settings. A generated body's description records its seed.

Generated worlds include a radius, density-derived mass/GM, rotation, orbit, ocean/atmosphere choices, pressure/temperature curves, terrain layers, shading, colours and biome conditions. Moon generation prefers the selected saved planet (or a selected moon's parent), unless **Moon Parent** selects another planet. Save a new planet before generating moons around it. Moon orbits clear the parent's surface and remain within 35% of its sphere of influence; a generated body's own sphere of influence also encloses its surface and atmosphere. Generated planets are placed beyond the existing outermost orbit. These are editable starting points for the game's Kepler model.

**Biome Theme** provides Temperate, Arid, Frozen, Volcanic and Cratered starting palettes/rules. **Apply Theme** replaces biome and colour rules while preserving geometry, atmosphere and shared presets. **Random Biomes** varies a theme's colours and thresholds. Both keep an unconditional fallback last. **+ Random Biome** inserts a new conditional latitude/longitude region before existing rules and adds matching surface paint, preserving the existing fallback.

Each biome action is one Undo step. It remains a draft until explicitly saved. Hover parameter labels/controls and group headers for descriptions of their role and units. Context matters: crater depth/radius use ratios, canyon width uses noise-value units, and body/spot radii use metres.

## Preview and diagnostics

Choose **Surface**, **Biomes** or **Height** for the globe and map. Enable **Surface Patch** to inspect local terrain. Click the map or set latitude/longitude to move the sample and patch. Drag the 3D image to orbit, scroll to zoom and double-click or use **Frame** to reset the camera. Patch width is in metres.

**Preview UT** rotates the body and updates orbital readouts; **Lighting angle** changes illumination. Readouts include sampled height/slope/biome, gravity, mass, escape speed, day, orbital period, SOI and position. Atmosphere curves show pressure, density and temperature versus altitude. Biome coverage is weighted by surface area; flat-area readouts report the runtime fitted height and tilt.

The lab uses `LayeredTerrain`, `TerrainChunkGenerator`, `PlanetMaps`, atmosphere/orbit models and the game's terrain, atmosphere and star shaders. It creates an independent `CelestialSystem`; it does not alter the game's active system. Generation runs on one cancellable CPU worker, with revision checks before applying results and Unity resources created on the editor thread.

Preview quality is bounded: coarse geometry uses 20 subdivisions per face and a 96 × 48 map; refined geometry uses 56 and a 256 × 128 map. Small biomes/features can be missed at these resolutions. Coverage and sampled bounds are estimates. Surface patches are clamped within one cube face. Material indices are editable data for ART-07a, whose material rendering is not yet implemented by the game.

### System orbit preview

Expand **System Orbits** in the preview pane. Gold highlights the current draft. The default view shows every body in the selected system, including the unsaved body and its edited orbit. **Parent View** focuses on the draft's parent and its children, making a moon's orbit visible without compressing the system's distances.

Drag the plot to tilt the orbital planes, scroll to zoom and use **Fit** to reset. Hover markers for the parent, orbital period and periapsis/apoapsis altitudes. **Play / Pause** advances the shared **Preview UT** at **Time scale (s/s)**; negative rates play backward. Quarter-orbit buttons step by the draft's period; **Reset UT** returns to the system's start time. Viewing/animating does not change saved epochs or parameters.

Positions use `CelestialBody.GetPositionAtUT`; orbit lines use `Orbit.PositionAtTrueAnomaly` and are centred on each parent's position at the current UT. Thus moon orbit lines move with their parent while markers propagate in the shared system frame. Distances and inclination are preserved; marker sizes are enlarged. This previews the game's Kepler hierarchy and does not simulate mutual gravity or prove long-term orbital stability.

## Editor-only boundary

The code lives in the Editor assembly. The development scene has no gameplay bootstrap, is absent from player build settings, and its generated objects/resources are temporary. A build guard rejects accidentally enabling this scene in a player build. Entering Play mode or closing the scene disposes the preview; use **Open Dev Scene** to resume editing afterward.

## Implementation checkpoint

- [x] In-memory drafts and Undo/Redo, preserving optional JSON and preset inheritance.
- [x] Controls for the current body/terrain schema, including ordered lists and nested terms.
- [x] Dedicated development scene, globe/surface previews, maps and diagnostics.
- [x] Explicit validated system/preset saves and exports.
- [x] EditMode regressions and live editor verification.

### Authoring expansion checkpoint

- [x] Seeded random planets and moons with physical parameters, atmosphere choices, terrain and biomes.
- [x] Property-specific hover descriptions and correct contextual units.
- [x] Biome themes, random biome sets and individual random biome insertion with Undo/Redo.
- [x] Animated system/parent orbit preview using the game's Kepler model and draft parameters.
- [x] Generation, tooltip coverage, orbit and editor UI verification.

Verification results are recorded in [TestReports/body-lab.md](TestReports/body-lab.md). Windows editor verification is performed locally; Linux editor operation remains untested.
