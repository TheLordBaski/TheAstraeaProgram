# The Astraea Program — Space Centre models

Seven original facilities with modern industrial architecture: aerospace white, neutral cast concrete, charcoal steel, smoked glazing and brushed stainless steel. Color is concentrated in safety markings and small signal-red fittings. The VAB now includes a hollow, furnished high bay; the other six facilities remain exterior assets.

## VAB interior

The current VAB keeps the pass-4 thin exterior edges and replaces its solid high bay with separate thin walls, a roof, a floor and a real doorway. Both sliding door leaves are parked open and remain independently selectable. The previous exterior-only VAB is preserved in `Revisions/VAB_Before_Interior.zip`.

- Six levels of perimeter service galleries, rear connecting walks, steel brackets, guardrails and toe boards.
- A connected 24-flight stair tower with 178.6 mm risers, 371.4 mm going and 1.65 m wide treads, plus a static freight lift cage and landing gates.
- Roof trusses and a twin-girder bridge crane. Its trolley, ropes and hook are parked over the east service area.
- Four stored rocket engines with open nozzles, cooling lines, injectors and maintenance dollies; two horizontal tank sections on transport cradles; two split fairing shells and two adapter barrels.
- Storage racks, transit cases, workbenches, monitors, tool cases, fire stations, cable trays, utility risers and lighting fixtures.

The centre has a flat floor with painted guides and **no rocket, raised platform, scaffolding or hanging hook**. Its reserved clear volume is **24 m wide × 30 m deep × 67.9 m high**, beginning 90 mm above the finished floor. A rocket at the origin can extend about 68 m above the floor; the door opening is 25 m wide × 59.8 m high, so a craft taller than the door cannot roll out upright.

Use **`Assets/TAP/Art/SpaceCenter/Prefabs/VAB_AssemblySpace.prefab`** for rocket assembly at world `(0,0,0)`. Its `RocketAssemblyOrigin` marker is at that point on the finished floor. The existing `01_VehicleAssembly.prefab` retains the apron-centre origin for campus placement. Both reference the same FBX and shared URP material, with simplified static colliders, exact gallery/stair/door collision and a separate neutral light rig. Environment geometry uses the project's Terrain layer, not the selectable EditorParts layer.

`Assets/TAP/Art/SpaceCenter/Scenes/VAB_Interior_Preview.unity` provides an isolated placement scene with a camera. `Previews/VAB_Interior_Assembly.png`, `VAB_Interior_Galleries.png` and `VAB_Interior_Workshop.png` show the actual enclosed interior. The `.blend`, `.glb`, FBX, OBJ and ArmorPaint project all contain the furnished VAB. The complete model has 132,020 triangles, one shared surface material and separate gallery meshes for culling.

`vab-interior-manifest.json` documents the assembly origin, clear bounds, collider primitives and door offsets in Blender coordinates (Z up). `vab-interior-geometry-check.json` checks the saved mesh vertices, UVs, triangle areas and clear volume. `vab-interior-unity-check.json` records live Unity import, material remapping, physics clearance and floor raycast checks. Reinstall through **TAP → Space Centre → Install VAB Interior**, or batch method `TAP.EditorTools.VabInteriorInstaller.Install`.

The authored VAB is now loaded by the space-centre hub, assembly screen and flight launch-site builder. Runtime prefabs live under `Assets/TAP/Resources/SpaceCenter/` and reference the same FBX meshes and material as the authoring prefabs. The assembly camera clips the near side of the building when zoomed outside the clear workspace so tall rockets remain visible. The existing 60 m craft-height setting is retained. Crane, lift and doors remain static; no NavMesh or LOD chain is included.

`vab-game-compile-check.json` records compilation against Unity's generated project references. `VabIntegrationTests` covers the resource prefab, assembly origin, floor/central collision and camera framing; `VabSceneTests` covers the hub's clickable facility and a starter rocket in the assembly screen. A compiler check alone does not mean these tests or built-game performance checks have run. See `vab-game-integration-check.json` for the current validation status.

The launch complex now has a broad braced tower, a hoist carriage, two catch-style truss arms with hydraulic supports, an upper quick-disconnect arm and an elevated annular launch mount. This is an original static concept informed by contemporary launch infrastructure, not a dimensionally accurate SpaceX replica.

The previous version is preserved locally in `Revisions/Before_Modern_Industrial.zip`. Revision ZIPs and transient logs/caches are excluded from version control; current editable sources and exports are included.

Latest VAB revision (pass 4): external corner columns and doorway portals are removed completely. The envelope uses flush folded-metal corners, 35 mm cladding ribs, 100 mm roof coping, 5 mm edge bevels and thin door guides. Office fascia overhangs and raised plinth bands are removed. See `Previews/VAB_Pass4_Comparison.png`; the camera, lighting and palette are held fixed. `vab-pass4-check.json` records export checks and unchanged hashes for 96 protected files belonging to the other facilities and shared surfaces. Live Unity import remains pending.

VAB pass 3 changes only its corner flashings, roof coping, door perimeter and office trim. The thick raked entry frames and oversized office feet have been removed. The VAB's massing, palette, camera and lighting are retained for direct comparison in `Previews/VAB_Pass3_Comparison.png`. Its previous version is in `Revisions/VAB_Before_Pass3.zip`; `vab-pass3-scope-check.json` records unchanged hashes for the six other facilities and shared surface files.

Pass 3 passed FBX round-trip, GLB triangle/palette checks and static Unity prefab GUID/material checks. The live Unity re-import remains unverified because the task's Editor did not finish starting its Pipeline interface. `vab-pass3-check.json` and `package-check.json` record this pending check; the earlier six facility import results remain valid.

## Open the finished work

- `Previews/SpaceCentre_ContactSheet.png` — all seven facilities.
- `Previews/00_SpaceCentre_Campus.png` — assembled, landscaped campus.
- `SpaceCentre_Campus.blend` — complete editable campus with lighting and camera.
- `Source/01_VehicleAssembly.blend` through `Source/07_MissionControl.blend` — separate facilities at their local ground origin, with packed textures and preview cameras.
- `Source/*.arm` — editable ArmorPaint projects, each with a PBR fill layer and a trim-touchup layer. Keep the adjacent `Textures` folder when moving the projects.
- `Source/*.glb` — portable models with embedded textures.
- `Source/*_Paint.obj` — OBJ meshes with atlas UVs for the paint projects.

## Facilities

| Model | Modeled features |
| --- | --- |
| 01 Vehicle Assembly Building | Thin exterior edges, open sliding doors, hollow high bay, six perimeter service levels, connected stairs, freight lift, bridge crane, rocket parts, storage and workshop equipment |
| 02 Launch Complex | Heavy trussed catch-style tower, paired truss arms, capture rails, hydraulic supports, hoist carriage, QD arm, annular launch mount, concrete anchor blocks, open flame trench, crawler ramps and deluge pumps |
| 03 Science Centre | Deep concrete facade frames, twin laboratory wings, glazed connecting gallery, observatory dome, rooftop photovoltaics and service manifolds |
| 04 Tracking Station | Framed control block, dark equipment plinths, three double-skinned parabolic reflectors, panel seams, rear ribs, feed supports, azimuth mounts and cable trenches |
| 05 Training Facility | Raked school and simulation portals, solid bases, single-pitch buoyancy hall canopy, centrifuge and gondolas, training capsule and EVA practice frame |
| 06 Testing Facility | Anchored propulsion test frame, deep workshop portal, engine bell, service platform, switchback stairs, flame deflector, capped blast walls and instrumentation bunker |
| 07 Mission Control | Concrete tower buttresses, cantilevered observation roof, facade fins, recessed operations frontage, aerials, entry canopy, flag court and monument |

Service vehicles, electrical cabinets, roof fans, drainpipes, handrails, stairs and safety markings provide smaller-scale detail. These are static geometry.

## Unity

Assets are installed under `Assets/TAP/Art/SpaceCenter/`:

- `Models/` — seven FBXs, authored in metres.
- `Materials/SpaceCenter_PBR.mat` — shared URP Lit material.
- `Textures/` — BaseColor, tangent-space Normal and packed MOS maps.
- `Prefabs/01_…` through `07_…` — individual facility prefabs.
- `Prefabs/00_SpaceCentre_Facilities.prefab` — all seven facilities in their campus arrangement. Ground and roads are supplied in the Blender campus scene.

Drag a prefab into a scene. Its root is the centre of its ground apron. Models are separated into meaningful component meshes, so equipment can be selected and edited independently.

The VAB replaces its procedural predecessors in the hub, assembly editor and launch-site scenery. The six other facilities remain supplied as placement assets. Regenerate the VAB's runtime prefabs with **TAP → Space Centre → Install Runtime VAB Prefabs** after editing its authored prefabs; a full VAB install also updates them.

## Surfaces and editing

The shared 2048 × 2048 trim atlas contains sixteen padded material tiles. BaseColor, Roughness, Metallic, Normal, AO, ORM and Unity MOS textures are in `Textures/`. AO is a neutral white map; geometric lighting supplies the shadows in the preview renders. MOS is R=metallic, G=occlusion, B=white, A=smoothness. ORM is R=occlusion, G=roughness, B=metallic.

The nine primary architectural surfaces use near-neutral RGB colors rather than pastel hues. White, steel and concrete remain distinct through roughness, metallic response and tonal value. Glazing is dark blue-black. Preview lighting uses a neutral white environment and a higher-contrast display transform; no NASA LUT or global hue filter is applied.

UVs intentionally reuse surface tiles. Painting a tile affects its repeated uses; local, unique facade painting requires a unique unwrap and bake first. The ArmorPaint touchup layer is explicitly labeled to make this clear. Geometry supplies the door ribs, railings, window frames, pipe fittings and other visible relief.

## Validation

- `asset-manifest.json` — source mesh counts, dimensions, origins and campus offsets.
- `export-check.json` — seven FBX re-imports; UV presence, finite vertex coordinates, material slots, triangle counts and source/export bounds checked.
- `unity-check.json` — seven imported Unity models; URP material remaps, metre-scale bounds and prefab creation checked in the running Editor.
- Every facility was rendered and visually inspected. Rear views of training and testing are supplied separately.
- All seven ArmorPaint projects were created and saved successfully through the installed ArmorPaint executable; per-project logs are in `Source/`.
- `package-check.json` also verifies the neutral architectural palette, exact BaseColor pixel parity inside every GLB, current texture timestamps and byte-identical Unity texture copies.

The other six facilities are exterior assets without authored collision or interiors. The VAB includes the furnished interior and collision described above. Frame rate, animation and flight-clearance behavior have not been tested in a built game.

## Rebuild

Run `python Source/make_surfaces.py`, then launch Blender with `--factory-startup --background --python Source/build_space_center.py`. Run `python Source/package_assets.py` for individual sources, renders and ArmorPaint projects. Render and check the campus with `--python Source/render_and_check.py -- campus`. Install the material and prefabs using the Unity CLI `eval_file` command on `Source/InstallSpaceCenter.cs`.

For the VAB-only pass, `Source/iterate_vab.py` rebuilds only that facility and replaces its campus collection. `Source/render_vab_pass3.py` retains the comparison camera; `Source/InstallVabPass3.cs` targets only its Unity prefab.

Rebuilding overwrites generated meshes, textures, renders and prefabs. The packaging script refreshes an ArmorPaint project when its OBJ is newer; preserve any manual painting before rebuilding.

Design references: [SpaceX launch infrastructure and flight updates](https://www.spacex.com/updates/), [Spaceport America](https://www.spaceportamerica.com/business/event-venue/) and [GMB Mission Operations Control Center](https://www.gmbnet.com/projects/mission-launch-command-center/). These inform structural massing, material separation and the catch-style tower; all facility geometry was authored for TAP.
