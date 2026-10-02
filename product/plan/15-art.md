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
- Scatter: rocks and boulders with colliders near vessels, density per biome.
- Oceans v2: waves, shore foam, a per-body liquid colour.
- Texture sets for Tellus, Luma, Pruina, Rubra and Vigil.

**Done when**
- [ ] Landing screenshots on the five worlds read as distinct places.
- [ ] Scatter costs under 1 ms.

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
