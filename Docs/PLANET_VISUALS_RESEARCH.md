# Planet visuals: research

Date: 2026-09-30. Research completed before approval; implementation was subsequently approved and delivered. See [implementation and verification](D:/Dev/KSP/Docs/PLANET_VISUALS_IMPLEMENTATION.md).

## Recommendation

Keep Unity 6.5 / URP 17.5 and build a planet visual system around the existing terrain and distance handling. Improve surface materials and orbital detail, replace the approximate atmosphere with scattering, add inexpensive spherical cloud layers, then deliver actual volumetric clouds as a switchable High/Ultra option.

The user's reference image establishes an artistic target: detailed cloud formations, soft cloud shadows, a thin blue atmospheric limb, warm light near the terminator, and a readable spacecraft. It does not establish which mods produced that image or a measurable performance baseline. Mod popularity was not measured.

The requested machine is an RTX 3070 laptop GPU, an unspecified AMD Ryzen CPU, and 64 GB of system RAM, targeting 60 FPS. Use 1920x1080 for the initial benchmark; native resolution, GPU power limit and VRAM capacity still need recording. Performance numbers in the plan are targets, not measured results.

## Baseline at the research stage

These findings were checked against the checkout at the start of research. Existing screenshots were inspected as historical evidence. Unity CLI discovered a running KSP editor with Pipeline installed, but its server was then unreachable, so no fresh screenshot or runtime benchmark was obtained in the research pass. Live rendered checks were subsequently completed during implementation; performance benchmarking remains deferred.

| Area | Current implementation | Visual consequence |
|---|---|---|
| Engine | Unity 6000.5.7f1; URP 17.5.0 | New work needs to target this version, including Render Graph. |
| Ground | Cube-sphere quadtree, 32 subdivisions per chunk, asynchronous generation, skirts | A useful streaming foundation already exists. Blanket increases to mesh density would spend CPU and memory without providing material detail. |
| Surface shading | Vertex colours plus one 256x256 grayscale noise texture, sampled with triplanar projection | There are no biome-specific albedo/normal/roughness sets in the terrain shader. This is a major source of the soft, uniform appearance. |
| Detail distance | Fine noise fades by 450 m, coarse noise by 9 km | Orbital views receive little of that detail. |
| Distant surface | Six coarsest terrain faces, each generated at resolution 32, using the terrain shader | Orbital and distant colour detail is limited by vertex sampling rather than a detailed planet material. |
| Map view | Baked or asynchronously generated 1024x512 surface maps | Map texture resolution is low, but these maps are not currently the flight terrain's albedo texture. Increasing them alone will not fix flight visuals. |
| Atmosphere | Additive analytic shell for the limb; separate procedural sky and exponential fog below | This approximates glow and altitude, rather than consistently modelling wavelength-dependent scattering and extinction. |
| Ocean | Water flag in vertex alpha, a fixed specular exponent and radial surface normals | Little wave detail, roughness variation or coastline richness. |
| Clouds | No cloud renderer found | Missing weather coverage, cloud relief, shadows and fly-through depth. |
| PC rendering | Render scale 1, HDR on, MSAA setting 4, SSAO feature present | A reduced render scale is not the identified cause in the saved PC asset. Runtime output still needs profiling. |
| Cameras | Far base camera at 1:100,000, flight overlay camera; separate map camera at 1:1,000 | Camera roles, depth units and effect ownership must be explicit. |

Local evidence:

- [Engine version](D:/Dev/KSP/ProjectSettings/ProjectVersion.txt) and [packages](D:/Dev/KSP/Packages/manifest.json).
- [Terrain shading](D:/Dev/KSP/Assets/TAP/Shaders/Terrain.shader), [texture generation](D:/Dev/KSP/Assets/TAP/Scripts/Editor/AssetBuilder.cs), and [terrain material](D:/Dev/KSP/Assets/TAP/Resources/Materials/Terrain.mat).
- [Terrain LOD](D:/Dev/KSP/Assets/TAP/Scripts/Simulation/Planet/PlanetTerrain.cs), [chunk data](D:/Dev/KSP/Assets/TAP/Scripts/Simulation/Planet/CubeSphere.cs), and [scaled space](D:/Dev/KSP/Assets/TAP/Scripts/Game/Flight/ScaledSpace.cs).
- [Atmosphere shell](D:/Dev/KSP/Assets/TAP/Shaders/AtmosphereShell.shader), [sky controller](D:/Dev/KSP/Assets/TAP/Scripts/Game/Flight/SkyController.cs), and [map view](D:/Dev/KSP/Assets/TAP/Scripts/Game/Map/MapView.cs).
- [PC pipeline](D:/Dev/KSP/Assets/Settings/PC_RPAsset.asset), [renderer](D:/Dev/KSP/Assets/Settings/PC_Renderer.asset), and [documented limitations](D:/Dev/KSP/Docs/FEATURES_AND_LIMITATIONS.md).

### Why a larger planet texture alone is insufficient

Tellus has a 600 km radius. At its equator, a 1024-pixel-wide global map represents about 3.68 km per pixel; 4096 represents about 920 m, and 8192 about 460 m. These are useful orbital scales. Landing and EVA need tiled material detail measured in metres, blended with kilometre-scale colour patterns and the original terrain.

The existing map generator also bakes a fixed hillshade into colour. A flight albedo bake should separate colour from height-derived normals, so terrain is shaded by the actual sun rather than carrying a permanent artificial light direction. The hillshaded diagnostic map can remain a separate product.

## What EVE contributes

The linked original EVE code separates a spherical 2D cloud mesh from nearby cloud volumes. The layer uses global coverage, detail modulation and cloud shadows; its volume implementation creates local cloud particles from quads. It also switches between local and scaled representations. These are valuable architectural ideas, although the KSP-specific APIs and legacy projector/shader plumbing require replacement in TAP. [EVE Clouds2D](https://github.com/rbray89/EnvironmentalVisualEnhancements/blob/master/Atmosphere/Clouds2D.cs), [CloudsVolume](https://github.com/rbray89/EnvironmentalVisualEnhancements/blob/master/Atmosphere/CloudsVolume.cs), [VolumeSection](https://github.com/rbray89/EnvironmentalVisualEnhancements/blob/master/Atmosphere/VolumeSection.cs), [example configuration](https://github.com/rbray89/EnvironmentalVisualEnhancements/blob/master/ContentConfigs/GameData/BoulderCo/Atmosphere/clouds.cfg).

This original implementation is not a ready-made modern raymarched planetary cloud renderer. EVE provides cloud/layer ideas; surface materials, atmospheric scattering and ocean improvements need their own work. Scatterer is a separate KSP atmospheric-scattering project. Its published plugin code is GPLv3 and its repository says newer shaders are not included. Treat it as a visual reference rather than the proposed dependency. [Scatterer repository](https://github.com/LGhassen/Scatterer).

EVE's README states MIT for most of the project and GPL for its DDS loader. The plan does not require copying that loader or distributing KSP content. Any source actually reused later must retain its required notices. [EVE license statement](https://github.com/rbray89/EnvironmentalVisualEnhancements).

## Options for clouds and atmosphere

| Approach | Strength | Cost or limitation | Decision |
|---|---|---|---|
| Direct EVE port | Spherical layers and distance switching are relevant | KSP integration, old rendering plumbing, particle volumes; does not supply the whole desired upgrade | Use as reference. |
| Layered clouds in URP | Good coverage, animation and orbital silhouette at a modest cost | Flat appearance during close fly-through | Include as a usable lower-quality mode. |
| Existing URP volumetric-cloud port | Public raymarched-cloud implementation to study | Compatibility, planet radius, camera history and shadows need adaptation | Study or selectively adapt; no blind package installation. |
| TAP Render Graph cloud renderer | Explicit support for TAP camera roles, radius, origin shifts and time warp | More integration and validation work | Recommended Ultra implementation. |
| Move the whole project to HDRP | Built-in volumetric clouds and richer sky/water features | Converts shaders/materials and changes camera composition; does not automatically solve planetary scale or surface content | Do not migrate for this task. |

Unity's current feature comparison lists built-in volumetric clouds in HDRP and not URP. It also documents that standard URP TAA is incompatible with camera stacking and MSAA. Cloud history therefore needs a dedicated reconstruction path rather than simply enabling camera TAA. [Unity 6.5 feature comparison](https://docs.unity.com/en-us/engine/6000.5/manual/render-pipelines/choose-a-render-pipeline/feature-comparison).

The public jiaozi158 URP cloud project ports HDRP rendering, advertises Unity 2022.2 / URP 14 or newer, and is MIT licensed. Its README notes unfinished custom cloud-map overrides, no orthographic support, directional-light cookie replacement for shadows, and a separate sky package for custom planet centre/radius. The inspected renderer source uses legacy pass plumbing and does not expose a `RecordRenderGraph` override. Its default radius is Earth-sized. Compatibility with TAP's exact Unity 6.5 setup has not been tested. [Project requirements](https://github.com/jiaozi158/UnityVolumetricCloudsURP), [renderer source](https://github.com/jiaozi158/UnityVolumetricCloudsURP/blob/main/Assets/VolumetricClouds/VolumetricCloudsURP.cs).

The installed URP source marks RenderGraphSettings obsolete from 6000.4 and returns false for compatibility mode. Implementing the new feature through Render Graph is a project requirement, rather than relying on a legacy fallback. [Installed settings source](D:/Dev/KSP/Library/PackageCache/com.unity.render-pipelines.universal@73b4c4ff130e/Runtime/Settings/RenderGraphSettings.cs).

For atmosphere, Bruneton's documented implementation addresses arbitrary planets and precomputed scattering; Hillaire's reference project accompanies a scalable sky/atmosphere paper. Both are more appropriate foundations for consistent ground-to-space lighting than extending an additive glow. The recommended implementation uses compact transmittance and scattering lookup tables with explicit planet parameters. [Bruneton reference](https://ebruneton.github.io/precomputed_atmospheric_scattering/), [Hillaire reference implementation](https://github.com/sebh/UnrealEngineSkyAtmosphere).

Guerrilla's Nubis research is useful for cloud authoring, animation, atmosphere integration and optimization. Its published timings describe its engine and hardware and cannot be carried over as TAP performance estimates. [Nubis research](https://www.guerrilla-games.com/read/nubis-authoring-real-time-volumetric-cloudscapes-with-the-decima-engine).

## Content and performance implications

Use original planet coverage generated from TAP's terrain and visual seeds, plus a small selected library of tiled PBR materials. Poly Haven's assets are CC0 and available for commercial use. Record individual asset sources when selecting them after approval; their example renders and website content are separate from the asset license. [Poly Haven license](https://polyhaven.com/license).

A practical cloud density model combines a planet-wide weather map, a spherical altitude profile, low-frequency 3D shape noise and higher-frequency erosion. Wind advances from simulation time. The same coverage drives the low-cost layer, the volume and cloud shadows, making mode changes recognizable and stable.

Half-width/half-height raymarching computes one quarter of the output pixels. Depth-aware reconstruction and temporal history can restore detail, but introduce risks around rocket silhouettes, rapid camera motion, moving clouds, origin changes and high warp. Those need specific acceptance tests. Empty-space skipping, early termination and limited shadow sampling should precede raising sample counts.

System RAM does not replace GPU memory. As an illustrative texture calculation, one 8192x4096 RGBA8 map uses about 171 MiB with a complete mip chain; an 8-bit-per-pixel block-compressed map uses about 43 MiB. A three-map orbital material is therefore roughly 128 MiB compressed, before other assets and runtime buffers. Actual GPU allocation must be measured, including temporary uploads and texture-array residency. The proposed visual system aims for less than 1 GiB of additional resident GPU resources on Ultra.

## What remains unverified

- Current player CPU/GPU frame times and actual laptop thermal/power behaviour.
- The exact supported pass ordering and depth handling for the current stacked renderer.
- Final appearance and numerical stability of a cloud renderer at Tellus's scale.
- The best sample counts, texture resolutions and memory budget on the target GPU.
- Native display resolution and installed VRAM capacity.

These are explicit implementation checkpoints in [the proposed plan](D:/Dev/KSP/Docs/PLANET_VISUALS_PLAN.md). No rendering code, materials, textures, scene settings or packages were changed during research.
