# 15 · Art and visuals (ART)

**Have:**
- procedural parts in the shared `Part_*` materials;
- a procedural rocketeer and procedural buildings;
- cube-sphere terrain with LOD for two bodies;
- a sky dome, the Tellus atmosphere shell;
- exhaust plumes, reentry plasma, explosions.

**The part model pipeline works:** export as FBX/OBJ, edit in Blender, and an FBX in `Resources/PartModels`
replaces a part ([Docs/PART_MODELS.md](../../Docs/PART_MODELS.md)).

This workstream is mostly **your** time (you plan to model parts yourself), so it is the longest lane in the
schedule. The plan hand-models the "hero" parts players look at most, and improves the procedural generators for the
rest.

---

### ART-01 · Art direction and style guide
**Milestone** M1 · **Claude** 0.5 d · **You** 2 d · **Needs** —

- The look: clean, readable, "stylised real hardware". Silhouettes first: every part recognisable in a thumbnail.
- The palette of the shared materials. Texel density, polygon budgets per part size, detail through bevels and
  panel lines rather than noise.
- Decals (agency flag, stencils). Reference sheets for each manufacturer's design language (PRT-01).

**Done when**
- [ ] `Docs/ART_STYLE.md` has rules and example renders.
- [ ] Two sample parts are made to the guide.

### ART-02 · Pipeline v2: textures, LODs, validation, Blender add-on
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** ART-01

- Hand-made models can carry textures: albedo, normal, metal/smoothness masks, on URP Lit or a TAP part shader with
  the shared palette as tint. Optional LODs.
- An import check on every FBX in `PartModels`:
  - polygon budget;
  - required rig names;
  - scale and size against the part's collider;
  - materials named for the shared slots or textured.
  Problems show in the console and in *TAP → Validate Content*.
- **A Blender add-on**: *Import TAP part* and *Export to game* buttons with the right settings (tested in PART_MODELS)
  and the right file name, so no settings mistakes are possible.
- Part icons regenerate after a model changes.

**Done when**
- [ ] A textured model with a missing `Bell` is rejected with a clear message.
- [ ] The add-on round-trips a part with one click each way.

### ART-03a · Hero parts, batch 1 (hand-made)
**Milestone** EA1 · **Claude** 0.5 d · **You** 12 d · **Needs** ART-02

- About 45 hero parts over five batches, roughly one day per part including texturing, modelled by you through the
  pipeline. Simple shapes (tanks, adapters, plates) stay procedural (ART-04).
- **Batch 1**, 12 parts: Kestrel and Albatross pods, Mayfly-1 lander can, Wren and Finch probes, Hornet, Ember,
  Goliath and Wasp engines, LS-2 legs, Canopy-16 chute, HS-125 shield.

**Done when**
- [ ] The batch passes the ART-02 checks and is shown in the EA1 release notes.

### ART-03b · Hero parts, batch 2
**Milestone** EA2 · **Claude** 0.5 d · **You** 12 d · **Needs** ART-03a

- 12 parts: Titan and Albacore, Lantern nuclear engine, Glimmer ion drive, rover wheels and chassis, Clasp docking
  ports, SP-8 solar wing, Starling and Owl probes, Mayfly-2.

**Done when**
- [ ] The batch passes the ART-02 checks and is in the EA2 notes.

### ART-03c · Hero parts, batch 3
**Milestone** EA3 · **Claude** 0.5 d · **You** 6 d · **Needs** ART-03b

- 6 parts: Leviathan, Wedge aerospike, Sunfield array, Hearth RTG, fuel cell, Gravimeter.

**Done when**
- [ ] The batch passes the ART-02 checks.

### ART-03d · Hero parts, batch 4
**Milestone** EA4 · **Claude** 0.5 d · **You** 10 d · **Needs** ART-03c

- 10 parts: Lookout cupola, Quill lab, crew cabins, drills, converters, survey scanner, grabbing unit.

**Done when**
- [ ] The batch passes the ART-02 checks.

### ART-03e · Hero parts, batch 5
**Milestone** EA5 · **Claude** 0.5 d · **You** 5 d · **Needs** ART-03d

- 5 parts: the remaining parts players look at most, chosen from Early Access feedback.

**Done when**
- [ ] About 45 hand-made parts are in the game.

### ART-04 · Procedural part upgrade
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** ART-01

- The generators get bevels, panel lines, weld seams, stringers on tanks, stencil decals and small details (bolts,
  vents), all from data.
- Tanks, adapters, decouplers and structure look finished without hand modelling.

**Done when**
- [ ] Side-by-side renders show the new procedural tank next to a hero engine without a style clash (your judgement).

### ART-05 · Rocketeers (characters)
**Milestone** EA5 · **Claude** 2 d · **You** 6 d · **Needs** D-05

- Two body types, suits for inside and EVA, helmet and visor, faces with expressions for portraits (CRW-10).
- A rig and animations: idle, walk, run, jump, fall/ragdoll, ladder climb, swim, jetpack float, flag plant, sample
  collection, sit, board.
- Sourcing per D-05. The recommendation is to commission the model and rig from a character artist and use licensed
  motion capture for the animations. The 6 days are your direction and integration; external cost is extra.

**Done when**
- [ ] The new rocketeer replaces the procedural one in every EVA feature.
- [ ] The animations blend with no foot sliding at walking speed.

### ART-06 · Space center buildings
**Milestone** EA3 · **Claude** 1 d · **You** 10 d · **Needs** HUB-01, ART-01

- Seven facilities × 3 levels from a modular kit (walls, roofs, dishes, trusses, doors) to keep it manageable, plus
  the launch pad's three levels, ruins states (HUB-05) and props: roads, lights, fences, fuel tanks.

**Done when**
- [ ] Every facility has 3 levels and a ruin.
- [ ] The hub stays within its frame budget.

### ART-07a · Planet surfaces v2 (EA1 worlds)
**Milestone** EA1 · **Claude** 3 d · **You** 3.5 d · **Needs** FND-02

2026-09-30: Tellus/Luma visual upgrade approved and underway; see `Docs/PLANET_VISUALS_IMPLEMENTATION.md`. Visual rocks in this scope do not add colliders. Other EA1 worlds and measured scatter cost remain pending.

- Terrain shader v2:
  - triplanar detail textures per biome;
  - rock on slopes, snow and ice by height and latitude;
  - distance blending to avoid tiling.
- Mid-range and ground detail follow the Gaea 2 visual-biome tasks ART-14 through ART-21 below. These replace the
  earlier coarse mid-range checklist; see [TERRAIN_LOD_RESEARCH.md](../../Docs/TERRAIN_LOD_RESEARCH.md).
- Scatter: rocks and boulders with colliders near vessels, density per biome.
- Oceans v2: waves, shore foam, a per-body liquid colour.
- Texture sets for Tellus, Luma, Pruina, Rubra and Vigil.

**Done when**
- [ ] Landing screenshots on the five worlds read as distinct places.
- [ ] Scatter costs under 1 ms.

## Visual biomes: Gaea 2 authoring and KSP 2 texture tiers

2026-10-23: user selected **Gaea 2 instead of World Creator**, with four visual channels for grass, sand, rock and
ice/snow. Existing named gameplay biomes remain intact. Planning only: these tasks do not authorize installing an
MCP server, buying software, or changing terrain/rendering now. Estimates are TBD until the first tile and render
comparison establish the authoring and GPU costs; ART-07a's earlier estimate predates this breakdown.

The three tiers are the existing orbital appearance, authored regional terrain normals, and procedural ground
materials with distance resampling. Each visual channel can reference up to four reusable ground-material sets;
four visual biomes do not imply sixteen unique texture sets or sampling all of them at every pixel. Water remains
separate from the four land channels. Gaea source graphs and exported height/normal maps are authoring assets;
Gaea and an MCP server are not player-runtime dependencies.

This pass adds shading detail. Exported heights provide an authoritative source for normals and material masks,
but do not displace the terrain mesh or colliders. Actual terrain displacement is a separate decision requiring
matching CPU height queries, collision, launch-site flattening and gameplay-biome preservation.

### ART-14 · Prove the Gaea 2 authoring and export workflow
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** FND-02

- Record the installed Gaea build, licence edition and available export/automation features. Read-only inspection
  found Gaea and Build Swarm **2.2.9.0** in `C:/Program Files/QuadSpinner/Gaea 2`; the licence is not verified.
  Current online documentation includes newer 2.3 features, so verify every required feature against this install.
- Create one small source graph, make the heightfield seamless, derive normals from that final heightfield, and
  export matching height/normal maps. Distinguish a repeatable tile from a tiled build of a larger world.
- Start with a graph that builds in the UI. Evaluate exposed parameters and a UI-generated Build Swarm command
  only when supported by the installed edition. Keep manual export available.
- Evaluate community MCP candidates only if they improve this workflow: inspect source, verify graph/version
  compatibility on a disposable copy, then require a successful build/export before treating one as usable.
  A CLI/manual workflow is sufficient; custom MCP development is not a prerequisite.

**Done when**
- [ ] One saved `.terrain` graph produces a seamless height map and a matching, correctly oriented normal map.
- [ ] Graph version, physical width/height range, output formats and paths, seed and reproducible build steps are recorded.
- [ ] The supported manual/CLI/MCP route and edition limits are recorded with actual build evidence; an untested MCP is not reported as working.

### ART-15 · Correct terrain normal blending and record the baseline
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** FND-02

- Record Tellus ground, low-flight, regional and orbital views with saved camera positions, lighting, quality and
  render settings. Record GPU terrain time and texture memory on the measurement machine.
- Correct triplanar normal reorientation in `Terrain.shader`. Preserve the underlying surface normal when all
  sampled detail normals are flat, including negative projection axes.
- Trace the current filtered material-index decoding and retain a focused reproducer for an unrelated material
  appearing between two IDs. ART-16 replaces this encoding for the new visual-biome path.

**Done when**
- [ ] A focused shader check preserves flat normals and verifies known bumps on all projection axes and blend regions.
- [ ] Matched baseline/corrected captures and measurements exist, without changing body geography or named biomes.

### ART-16 · Add four-channel visual biome maps and authoring controls
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-15

- Add a separate per-body RGBA weight map and four visual definitions for grass, sand, rock and ice/snow; keep
  `BiomeAt`, named IDs, diagnostic gameplay maps and science-region data unchanged.
- Generate an initial map from existing appearance rules, then support map import/export and per-channel/combined
  previews in Celestial Body Lab. Preserve its draft, Undo/Redo and explicit-save workflow.
- Treat weights as linear data with documented normalization and an all-zero fallback. Check longitude wrapping,
  poles, filtered boundaries and mip levels. Keep existing ocean/depth information separate.
- Extend visual bake keys and material bindings so changes rebuild the appropriate assets. Keep other bodies on
  their current rendering path until they have validated visual profiles.

**Done when**
- [ ] Each channel and mixed boundaries render predictably; grass/ice boundaries cannot decode an unrelated sand ID.
- [ ] Import/export, channel order, fallback and filtering have focused checks; invalid source data gives a clear error.
- [ ] Existing named-biome fingerprints and terrain-height fingerprints still match, including the launch complex.

### ART-17 · Author the first four seamless regional tiles in Gaea 2
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-14

- Author reusable source graphs for rolling/eroded grassland, sand undulations/dunes, fractured/eroded rock, and
  ice/snow formations. Start near **2 km per repeat**, with the KSP 2 reference range of 1-3 km as an art guide.
- Keep height data at least 16-bit; record physical dimensions and height range so exported normals have a known
  slope scale. Derive normals after making the heightfield seamless and verify repeat-edge derivatives too.
- Export only the maps required by the shader; the regional layer does not need a separate albedo/gloss set.
- Check each tile repeated in a grid and on a curved surface, including mip filtering and oblique lighting.

**Done when**
- [ ] All four graphs and matching height/normal exports are reproducible and their physical scale is documented.
- [ ] Repeated height and normal maps have no visible edge seams; the user approves the four regional landform styles.

### ART-18 · Blend regional terrain normals across visual biomes
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-16, ART-17

- Import the Gaea regional maps through existing visual profiles/texture arrays and blend them with the RGBA
  weights using corrected body-fixed triplanar normal projection.
- Combine regional and underlying terrain normals without flattening slopes, shifting the orbital palette or
  swimming during body rotation, camera movement or floating-origin changes.
- Expose per-channel repeat distance and normal strength. Add regional-only, weights-only and normals diagnostic
  views so the contribution can be inspected independently of ground materials.
- Avoid paying for channels with negligible contribution; measure the added texture sampling and memory cost.

**Done when**
- [ ] One Tellus test area shows distinct regional structure and smooth mixed-biome boundaries from several directions.
- [ ] Geometry, collision, launch-site flattening and named-biome results remain unchanged.
- [ ] Regional-only captures and measured GPU/memory deltas are recorded against ART-15.

### ART-19 · Add procedural ground materials within each visual biome
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-18

- Reuse the existing ground material library first. Support up to four material slots per visual biome, sharing
  sets where appropriate: albedo, normal, roughness, height and AO as actually needed.
- Place materials using stable height, slope and peak/cavity inputs. Separate placement inputs from view-dependent
  lighting normals and changing mesh LOD so material boundaries do not shift with the camera.
- Use height-aware blending for close boundaries, such as sand around rocks and snow over exposed ground.
  Preserve physical texture scale and validate normal orientation and packed-map conventions.

**Done when**
- [ ] Close grass/sand/rock/ice regions and their transitions are visually approved in the Tellus test area.
- [ ] A fixed ground point keeps its material placement through LOD changes and camera-distance changes.
- [ ] Focused blending/placement checks pass and the actual material sampling cost is recorded.

### ART-20 · Connect ground, regional and orbital texture scales
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-19

- Sample ground materials at multiple fixed, body-anchored scales and blend by camera-to-surface distance;
  use the talk's at-least-four scales as the target, with thresholds tuned from captures rather than copied blindly.
- Fade close height blending towards smoother alpha blending farther away. The reference's 50-100 m example is
  a starting point for evaluation, not a fixed threshold for every camera angle or body.
- Tune the overlap with regional Gaea normals and the existing orbital appearance together. Preserve broad colour
  and geographic landmarks while progressively exposing detail during descent.
- Test ground-level horizon views, overhead descent/ascent and sideways flight; inspect swimming, tiling, seams,
  mip shimmer and visible texture-scale switches.

**Done when**
- [ ] A continuous ground-to-orbit-and-back capture shows no obvious swimming or abrupt material/scale transitions.
- [ ] Matched close, low-flight, regional and orbital captures are approved by the user.
- [ ] GPU cost is measured across quality presets with a practical fallback on lower settings.

### ART-21 · Validate the Tellus slice and gate the remaining-body rollout
**Milestone** EA1 · **Claude** TBD · **You** TBD · **Needs** ART-20

- Review one representative Tellus area covering the four visual families before extending authoring to the full
  planet. Include coast, steep rock, snow/ice and the launch-complex surroundings in the final Tellus review.
- Run focused shader/import/bake checks, existing biome and terrain fingerprints, launch-site/collision checks,
  map overlays and scaled/local handover checks. Keep saved terrain and named regions unchanged.
- Record actual CPU/GPU time and texture memory against the baseline; agree the acceptable budget before rollout.
- After Tellus acceptance, apply the pipeline to Luma and the remaining EA1 bodies under ART-07a, then EA2 bodies
  under ART-07b with body-appropriate tile content and profiles. Gas giants do not need land-surface tiles.

**Done when**
- [ ] The user approves the Tellus result at all three scales and measurements meet the agreed budget.
- [ ] Gameplay-biome/terrain preservation checks and visual pipeline checks pass with evidence linked from the report.
- [ ] ART-07a/ART-07b have a measured authoring/rendering baseline for their remaining rollout; no unreviewed body is marked complete.

### Research and tool references

- [KSP 2 three-tier authoring](https://www.youtube.com/watch?v=kvytgzvqlgQ&t=1200s),
  [World Creator regional tiles](https://www.youtube.com/watch?v=kvytgzvqlgQ&t=1477s),
  [ground material sets](https://www.youtube.com/watch?v=kvytgzvqlgQ&t=1620s),
  [distance resampling](https://www.youtube.com/watch?v=kvytgzvqlgQ&t=1762s).
- Gaea [Seamless](https://docs.gaea.app/reference/nodes/utility/seamless.html),
  [Normals](https://docs.gaea.app/reference/nodes/derive/normals.html),
  [documented automation](https://docs.gaea.app/developers/automation/index.html),
  [current edition comparison](https://www.quadspinner.com/Order). Verify availability on the installed 2.2.9 build;
  the current store lists limited automation for Professional and an automation workflow for Enterprise.
- Community candidates, **not installed or validated**:
  [JulianIrigoyen/gaea-mcp](https://github.com/JulianIrigoyen/gaea-mcp) automates graph plumbing and builds;
  [nghia-peirth/gaea-mcp](https://github.com/nghia-peirth/gaea-mcp) advertises node-graph editing and builds.
  Neither is an official QuadSpinner MCP. Do not assume either can author finished terrain without Gaea validation.

### ART-07b · Planet surfaces (EA2 worlds)
**Milestone** EA2 · **Claude** 1 d · **You** 3.5 d · **Needs** ART-07a

- Texture sets and scatter rules for the other eleven worlds, including Caligo's brine and Maris's teal seas.

**Done when**
- [ ] Every world has its own look in landing screenshots.

### ART-08a · Atmospheric scattering (Tellus, Rubra)
**Milestone** EA1 · **Claude** 2.5 d · **You** 1 d · **Needs** FND-03

2026-09-30: Tellus LUT scattering and switchable cloud layers/volumes advanced into current work by user approval. Rubra tuning remains future work. Performance benchmarking is deferred at the user's request.

- An atmospheric scattering shader with correct colours from orbit and from the ground: Tellus blue, Rubra salmon
  with blue sunsets and dust haze. Clouds are an option (D-16).

**Done when**
- [ ] You approve sky screenshots from the ground and from orbit for Tellus and Rubra.

### ART-08b · Atmospheric scattering (Caligo, Maris, Magnus)
**Milestone** EA2 · **Claude** 0.5 d · **You** 1 d · **Needs** ART-08a

- The same shader tuned for Caligo's violet haze, Maris's air and Magnus's deep atmosphere.

**Done when**
- [ ] You approve sky screenshots for the three bodies.

### ART-09 · Star, gas giant, rings and sky
**Milestone** EA2 · **Claude** 3 d · **You** 2 d · **Needs** FND-03

- The star: surface, corona, flare.
- Magnus: banded clouds with slow flow and storms.
- Rings: texture, transparency, shadows on the planet and the planet's shadow on the rings.
- A starfield and galaxy background that later systems (FND-15) can reuse with different skies.

**Done when**
- [ ] Magnus from Maris's surface is a showpiece screenshot for the store page.

### ART-10 · Effects v2
**Milestone** EA2 · **Claude** 3 d · **You** 2 d · **Needs** FLT-18

- Plumes per engine family: kerosene-like, hydrogen-clear, solid-smoky, nuclear, ion, aerospike. Expansion in vacuum.
- Solid booster smoke trails, landing dust on regolith, reentry plasma v2, explosions and debris v2, jetpack puffs,
  heat glow on parts, and lights.

**Done when**
- [ ] Each engine family is recognisable in a screenshot.
- [ ] Effects stay within the frame budget at 30 engines.

### ART-11 · Visual clarity pass
**Milestone** EA1 · **Claude** 2 d · **You** 2 d · **Needs** UX-04, ART-07a

- **Lighting**: auto-exposure that keeps both a sunlit capsule and the night side readable, and ambient light in
  space so shadowed parts aren't pure black.
- **Highlights**: an outline for the selected part, target vessel and docking port.
- **Terrain readability**: height shading and slope contrast so you can judge a landing.
- Done together with the HUD pass (UX-04). Before and after screenshots.

**Done when**
- [ ] A panel of five players picks the "after" version as clearer in at least 4 of 5 comparisons.
- [ ] Your approval.

### ART-12 · Store and marketing art
**Milestone** EA1 · **Claude** 1 d · **You** 3 d · **Needs** ART-11

- The TAP logo and wordmark.
- Steam capsules: header, small, main, vertical, hero, library, logo; loading screens (UX-12); a key-art render.
- Screenshots from scripted camera setups (the autotest scenes).

**Done when**
- [ ] Every Steam asset is at the required size (REL-04).
- [ ] You pick the key art.

### ART-13 · Interface icons and flags
**Milestone** EA1 · **Claude** 1 d · **You** 2 d · **Needs** UX-01

- Icons for:
  - part categories and resources;
  - vessel types and facilities;
  - contract types and crew traits;
  - experiments, alarms, notifications.
- About 12 agency flags (UX-11). A consistent line style.

**Done when**
- [ ] Every icon slot in the interface is filled.
- [ ] Icons stay readable at 24 px.
