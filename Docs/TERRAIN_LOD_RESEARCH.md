# Terrain LOD research: PQS, BurstPQS and KSP 2's scaled and local space

2026-10-17. Research only: no code changed. Sources are listed at the end.

## Short answer

We already have a PQS. `PlanetTerrain` (cube-sphere quadtree), `LayeredTerrain` (the terrain definition, our version of the
PQSMod chain) and `ScaledSpace` (the far view) do what KSP's PQS and scaled space do. KSP's PQS code is not ours to use, and
BurstPQS is a set of patches to KSP's own `PQ`/`PQSMod` classes, not a library we could drop in. What we take from both is
their ideas.

- Keep generating terrain geometry on the CPU: physics, landing, altitude and determinism need the heights there anyway.
- Make generation faster the way BurstPQS did: evaluate the terrain a whole chunk at a time in Burst jobs, with no garbage.
- Make the change from orbit to ground look seamless the way KSP 2 aimed to: no LOD pops, and texture detail at every
  distance.
- Measure first. Our benchmarks never stream terrain, so we do not know yet where the time goes.

## What PQS is (KSP 1)

- Each body has a `PQS` ("procedural quad sphere"): a cube of six root quads, split into a quadtree by camera distance.
  Every quad is the same small vertex grid, so near quads have small triangles and far quads large ones.
- A vertex's height is the sum of an ordered chain of **PQSMods**: heightmaps, simplex/ridged noise, Voronoi craters,
  decals and flattened areas (KSC), vertex colours and land classes. A mod hooks `OnVertexBuildHeight`, `OnVertexBuild`
  and `OnMeshBuild`, so the chain runs once per vertex, as managed virtual calls.
- Quads near the vessel get mesh colliders.
- Far away, a separate low-resolution **scaled space** mesh with baked textures stands in, and fades to the PQS as you
  descend.
- Stock KSP builds quads on the main thread. That is the terrain stutter BurstPQS exists to remove.

## What KSP 2 changed

From the GDC 2023 talk "Houston, we have a planet" and Developer Insights #12 and #18.

**Two representations, one look.** Scaled space (a low-resolution mesh seen from afar) and local space (the PQS terrain you
land on) were kept from KSP 1. Local space dithers in as you approach, with artist-set altitude fade ranges, and a little of
the scaled-space albedo stays visible in local space to tie the two together. Their rule was "what you see is what you get":
a green crater seen from orbit is a green crater on the ground. Local-space texturing reads the scaled-space textures (an
RGBA mask for four biomes per body), and detail generated on the ground can be baked back up into scaled space.

**Their authoring order was the reverse of ours.** They painted the scaled-space heightmap and biome mask first (Substance
Painter, after ZBrush proved too destructive to iterate on), then dressed the ground to match. Moving between the two views
and correcting colours was a constant back-and-forth.

**Three texture tiers.**
1. Scaled space: the painted heightmap and biome mask.
2. Large scale: one tiling normal/height texture per biome, 1 to 3 km across (made in World Creator), showing the biome's
   signature landform: mountains, dunes, rolling hills, craters. They dropped its albedo and gloss to save memory.
3. Small scale: four texture sets per biome (albedo, normal, gloss, height, AO), placed by height, slope and peak rules,
   with scatter on top.

**Hiding tiling cheaply.**
- Height blending between materials looks right up close but shows its repetition from 50 to 100 m up, so above a set
  altitude they switch to plain alpha blending.
- *Distance resampling:* the small textures' UV scale steps through at least four scales, blended by distance from the
  camera. Blending by altitude alone made the textures swim under you. The distance comes from the depth buffer, so it
  costs almost nothing, and it gives a free macro texture at mid range.

**Heights.** 16-bit heightmaps stretched over a 600 km radius gave stair-stepped hills. They smoothed between vertices
(bilinear, then a Gaussian blur) instead of shipping 16K heightmaps.

**Physics and ocean.** Physics quads are built at high detail, so a landed vessel sits on the ground you see. The ocean uses
the same tessellation. Coastlines still jump between LODs. They hid it by colouring the terrain under the water to match the
shore at every LOD, and said the problem is not solved.

**PQS+ (the KSP 2 version).** Built on multiple threads, with vertex data generated in compute shaders and never read back,
drawn by procedural draw calls. Quads are frustum-culled on the CPU and positioned relative to the floating origin. Their own
profiling (Developer Insights #18) named PQS and its shaders a major cost. At GDC they said they had hit its limits and were
moving to a new system: a concurrent binary tree for meshing, S2 cells for addressing, and the mesh split into chunks with
separate shaders, so a body could have more than four biomes.

**Lighting the whole system.**
- The star's light follows an artist-tuned falloff curve, with per-body overrides when you enter a sphere of influence.
- Spherical harmonics carry planetshine, and simple circle-overlap maths dims it in eclipses.
- Bounce light comes from a cube map of the always-present scaled-space scene, rendered at the camera: a low mip for
  diffuse, a sharp one for ocean reflections.
- The atmosphere follows Nishita, baked into textures, with colour overrides per body.

## What BurstPQS does

BurstPQS is a KSP 1 mod (MIT licence) that keeps PQS but rebuilds how quads are made.

- **A whole quad at a time.** Each PQSMod loops over all the quad's vertices in one call (arrays of heights, directions and
  UVs), instead of the per-vertex callback chain. Mods are ported as small job structs.
- **Burst.** The build jobs and Burst ports of every noise module are compiled by Burst. The author reports quad builds
  about 6 times faster from Burst alone, and frames with no terrain change about twice as fast.
- **Jobs and native buffers.** Quads build in parallel on Unity's job system: heights, then vertices, then the mesh. Meshes
  are filled with `SetVertexBufferData` from native buffers, with no managed copies.
- **Colliders off the main thread.** `Physics.BakeMesh` runs in an `IJobParallelFor`, and the collider is assigned at the
  next physics step.
- **Better split decisions.** The split decision runs as a job. It splits around the camera *and every loaded vessel*, so
  ships do not fall through, and it looks ahead along the camera's velocity, so swinging or diving does not outrun the
  terrain. It measures straight-line distance, which works better on small or lumpy bodies.
- **No dependence on the floating origin.** Quad positions are computed in doubles, independent of the floating origin, so
  vessels no longer jump when they load.
- **Maps from the terrain.** A texture exporter bakes the terrain to height, colour and normal maps (equirectangular or cube
  map, with a GPU BC7 encoder), and a bicubic (Mitchell–Netravali) heightmap mod samples heightmaps smoothly.

## Where we stand

What we already have, with the PQS idea it matches:

| Ours | Where | PQS equivalent |
|---|---|---|
| Equi-angular cube-sphere quadtree, 32×32 chunks, skirts against cracks | `PlanetTerrain`, `CubeSphere` | PQS quads (ours has less stretching than a plain normalised cube) |
| A node splits only when all four children have meshes, so there are no holes | `PlanetTerrain.Visit` | none |
| Chunks generated on 1 to 6 worker threads; main thread uploads at most 12 meshes and 6 ms per frame | `TerrainWorkers`, `BuildCompleted` | BurstPQS's background builds |
| Terrain as JSON fields and layers, evaluated in doubles, the same bits on every platform (fingerprint test) | `LayeredTerrain`, `TERRAIN_FORMAT.md` | the PQSMod chain, without heightmap stair steps |
| Flat areas for the launch complex and bases | `LayeredTerrain.Flatten` | MapDecal / FlattenArea |
| Separate collision chunks at full detail around every loaded vessel, cooked with `Physics.BakeMesh` in a job | `PlanetManager` | QuadMeshColliders, BurstPQS's baker |
| Floating origin and Krakensbane | `FlightSim` | the same |
| Far view in camera-relative doubles; terrain takes over at 6 radii, where the quadtree shows only the six root chunks the far view also draws | `ScaledSpace` | scaled space and its fade |
| Orbital albedo, normals and masks baked from the terrain definition; the terrain shader reads them | `PlanetVisualBaker`, `Terrain.shader` | KSP 2's "what you see is what you get", true by construction |

The gaps:

1. **Terrain streaming is not measured.** Every benchmark launches from the pad, where the terrain barely changes. The
   `Planets` step costs 0.43 to 0.59 ms per frame on the main thread (`PERFORMANCE.md`). Worker time per chunk, and how
   long the ground under you takes to reach full detail, are unknown.
2. **The work queue is first in, first out.** `TerrainWorkers` is a `BlockingCollection` over a `ConcurrentQueue`, so a far
   chunk requested earlier is built before a near one. Nothing looks ahead along the camera's velocity, and chunks outside
   the view frustum get the same priority. Our raw threads also compete with Unity's own job workers.
3. **The terrain is evaluated one sample at a time.** `LayeredTerrain.Sample` interprets the definition for every sample in
   managed code. This is the per-vertex PQSMod chain that BurstPQS replaced.
4. **Every chunk allocates.** About ten managed arrays per chunk (directions, heights, fields, positions, normals, colours,
   two UV sets, `ChunkData`): roughly 100 KB per 32×32 chunk, which the GC must collect later. The main thread then copies
   them into the mesh with `SetVertices`, `SetColors` and so on.
5. **LOD changes are instant.** A parent swaps to its four children in one frame, which shows as popping on descent. The
   ocean is flattened into the same mesh, so the coastline pops too.
6. **Texture detail has a gap at mid range.** Near ground materials fade out within 1.6 km of the camera
   (`PLANET_VISUALS_KSA_DIRECTION.md`); beyond that only the orbital maps remain. This is the range KSP 2 filled with
   large-scale tiles and distance resampling, and the "detail must grow on approach" goal.

## Recommended plan

Each step can ship on its own. The steps are plan tasks:

| Step | Task | Milestone |
|---|---|---|
| 1. Measure | FND-16, done | M1 |
| 2. Schedule better | FND-20 | EA2, only once the queue fills up |
| 3. Remove the garbage | FND-17, done | EA1 |
| 4. Evaluate in Burst | FND-19 | EA1, after FND-17 |
| 5. Morph between LODs | FND-18 | EA1 |
| 6. Fill the mid range | part of ART-07a | EA1 |
| 7. Later | none yet | — |

The measurements of step 1 changed the order. The workers are mostly idle, so better scheduling can wait. Garbage
collection is the cost that shows, so removing the garbage comes first.

1. **Measure.** Add two benchmark scenarios: `descent` (from orbit to the surface of Tellus) and `low-pass` (1 km up at
   about 2 km/s). Record:
   - worker milliseconds per chunk and the queue length;
   - the time until `PlanetTerrain.Complete` and until the chunk under the camera reaches full level;
   - mesh uploads per frame, `GC.Alloc` and GC collections.

   Proposed budgets: terrain never takes more than 2 ms of a main-thread frame, and the ground under the camera reaches
   full detail within 1 s.

   Done on 2026-10-18: the first numbers are in `PERFORMANCE.md` ("Baseline: 2026-10-18"). Both budgets hold on both paths;
   the workers are mostly idle, and the garbage collections triple while streaming.
2. **Schedule better (small change).**
   - Replace the queue with a priority queue keyed by distance or screen error, re-ranked each frame.
   - Rank chunks in the frustum first, and request chunks around where the camera will be 1 to 2 s ahead.
   - Drop stale requests before they run.
   - Move the work to Unity's job system to stop competing with its workers.
3. **Remove the garbage.** Pool native buffers per worker. Fill meshes with `Mesh.AllocateWritableMeshData` in the job and
   apply them with `ApplyAndDisposeWritableMeshData` on the main thread. Keep one native index buffer per resolution,
   handedness and skirt setting.

   Done on 2026-10-18 with a smaller change: a pool of managed chunk buffers and per-thread scratch, not native buffers. Streaming now
   collects garbage no more often than the pad (`descent` 89 → 7 collections in 10 s), and its longest frame fell from 15.6 to 6.2 ms.
   Native buffers move to FND-19 (`PERFORMANCE.md`, "after FND-17").
4. **Evaluate in Burst, a chunk at a time.**
   - Compile each body's terrain definition once into a flat list of operations (native arrays of plain structs).
   - In a Burst job, run each operation over all of a chunk's samples before the next one. The JSON format stays as it is.
   - Port `DetMath` and the noise to Burst with `FloatMode.Strict`, which allows no reassociation and no fused
     multiply-add.
   - The terrain fingerprint must stay bit for bit identical: that test is the gate. Keep the managed path as the
     reference and as the fallback.
   - The same evaluator then serves colliders, altitude queries and map exports.

   *Risk:* Unity does not promise Burst results are the same on every CPU (`FloatMode.Deterministic` is reserved for
   future use). Strict mode, our own maths and the fingerprint on each platform are how we check it.
5. **Morph between LODs (as in CDLOD).** Store each vertex's height as the parent chunk would give it in a spare vertex
   channel. In the vertex shader, blend from that to the real height over the last part of the split distance. This
   removes pops on descent and most coastline jumps, and costs one float per vertex. Collisions are unaffected: they use
   their own full-detail grid.
6. **Fill the mid range with texture (KSP 2's tiers).**
   - Give each biome one large-scale tiling normal/height texture, 1 to 3 km across.
   - Step the near textures' UV scale with distance from the camera (not altitude).
   - Use height blending up close and alpha blending beyond about 100 m.
7. **Later.** Bounce light from a small cube map of the far view rendered at the camera (KSP 2's trick), planetshine from
   bright neighbours, and eclipse dimming of that light.

Not recommended:

- **Porting KSP's PQS or BurstPQS code.** Both are built on KSP's own classes, and PQS belongs to KSP's owners.
- **Geometry generated only on the GPU (PQS+).** We would still need the same heights on the CPU for physics, landing,
  altitude and determinism, and KSP 2's own profiling flagged this path.
- **Painting scaled space first.** Our orbital maps are baked from the terrain definition, so orbit and ground agree
  without the back-and-forth KSP 2 described. Keep the definition as the single source.
- **A rewrite on a concurrent binary tree or S2 cells.** Our quadtree's (face, level, i, j) addressing already gives
  S2-like cells. Revisit only if we move tessellation to the GPU.

## Sources

- [Houston, we have a planet: The spherical terrain of Kerbal Space Program 2](https://youtu.be/kvytgzvqlgQ) (Unity at GDC
  2023), read from the video's automatic transcript.
- [KSP 2 Developer Insights #12: Planet Tech](https://www.kerbalspaceprogram.com/news/dev-diaries-developer-insights-12-planet-tech)
  and the [Developer Insights #18 discussion](https://forum.kerbalspaceprogram.com/topic/214806-developer-insights-18-graphics-of-early-access-ksp2).
- [BurstPQS](https://github.com/Phantomical/BurstPQS): README, `docs/guide.md`, `CHANGELOG.md`, `BatchPQS.cs`,
  `Jobs/BuildQuadJob.cs`, `QuadColliderBaker.cs` (read 2026-10-17, last pushed 2026-10-14).
- Our code: `PlanetTerrain.cs`, `CubeSphere.cs`, `PlanetManager.cs`, `ScaledSpace.cs`, `LayeredTerrain.cs`; docs
  `PERFORMANCE.md`, `TERRAIN_FORMAT.md`, `PLANET_VISUALS_KSA_DIRECTION.md`.
