# Planet visual refinement: Kitten Space Agency references

2026-10-01. Implementation refinement within the approved planet visual upgrade. Visual acceptance and controlled laptop performance measurements remain pending.

## Research and comparison

The supplied views show broad cloud banks alongside fine broken formations and translucent wisps, a muted blue ocean under aerial haze, soft thin atmospheric edges, and terrain relief that follows the light. Those are visual observations; screenshots alone cannot identify the renderer used for a particular frame.

KSA's official [July 2026 development summary](https://ahwoo.com/news/7350020/kitten-space-agency/kitten-space-agency-teaser-revealed) explicitly describes NASA-data normal maps for 2D clouds. Its [year-one recap](https://ahwoo.com/news/6309887/kitten-space-agency/development-recap-year-one) describes custom planet geometry and procedural terrain work. The [BRUTAL overview](https://ahwoo.com/brutal) describes camera-relative rendering with simulation in doubles and rendering in floats. These establish useful approaches, not an exact recipe or a claim of equivalent performance in Unity.

Our current renderer already supports camera-relative planet rendering, filtered clouds in map/distant views, nearby volumetrics, cloud shadows, orbital terrain normals and atmospheric scattering. Inspection identified three gaps:

1. The weather map was assembled entirely from noise and curved envelopes. The envelopes gave different sizes, but their interiors had similar noise structure.
2. The distant layer used optical coverage and a fixed lighting factor, without cloud-top relief normals.
3. The generated orbital albedo retained strong biome colours; the ocean highlight had no water Fresnel factor. Fine ground materials fade out within 1.6 km of the camera, so they do not solve orbital surface appearance.

Tellus has a 600 km radius, consistent with the user's Kerbal-size Earth direction. Cloud formation scale is judged against that radius and the camera footprint. Matching the references at the same absolute altitude would not match curvature. Existing body scale and gameplay geography are preserved. Atmosphere and clouds are the priority; rain and other weather effects are deferred.

## Work and review order

- [x] Record matched globe, orbit and horizon baseline captures.
- [x] Obtain and record an 8K NASA cloud-only source, independent of KSA assets.
- [x] Add source-assisted regional cloud baking with source digest in cache keys. Keep bank/patch/front counts, seed, coverage and density authorable, with procedural fallback when no source is selected.
- [x] Bake cloud-top relief normals and use them for sunlight on distant layers. Keep one shared optical coverage map for layered clouds, near volumes and terrain shadows.
- [x] Expose surface saturation/tint, terrain normal strength and ocean colour blend in body profiles; correct ocean highlight reflectance.
- [x] Tune the Tellus profile in actual captures and verify all graphics presets, distant filtering and the volume transition.
- [x] Record final before/after comparisons, compilation and focused contract checks, and restore the user's original session/preferences.

This pass retains the existing near-volume sample budgets and distance blend. Distant cloud shading adds one filtered normal-map sample. High/Ultra texture clouds add two existing 3D-noise samples only when fine optical detail is resolved, fading them away with pixel footprint. The large source image is bake-only; only the resulting 4K weather and normal maps are shipped. Unity reports 22,392,432 bytes of runtime object memory for each BC7 map in this Editor session. This is not a measured resident VRAM value; the theoretical BC7 texture payload with mips is approximately 10.67 MiB per map. CPU/GPU profiler diagnostics remain authorized; a controlled 60 FPS benchmark awaits PC availability.

## Implemented tuning and checks

The profile now combines NASA-source regional cloud shapes with baked lighting normals. Large banks, small patches and thin fronts remain controlled by the existing counts/seed/coverage; the source adds varied interiors rather than importing Earth's geography onto Tellus. Cloud relief is an artistic height approximation, not measured cloud altitude. The resolved layer detail reuses the near volume's erosion threshold to break up opacity into billows, with a faint floor and continuous fade to the baked map. Tellus's **Cloud Layer Detail** is 0.85; this detail is confined to weather regions and is enabled on High/Ultra.

Tellus's visual Rayleigh scale height is 3.6 km, with coefficients adjusted to preserve the previous vertical optical depth. Mie height is 1 km. Ocean colours and surface saturation are editable; water specular reflection now includes a Fresnel factor. Optional regional material variation on High/Ultra uses the existing ground texture arrays, without changing geometry. A GPU probe caught and corrected double sRGB-to-linear conversion in the new tint/ocean material binding.

Select [tellus.asset](D:/Dev/KSP/Assets/TAP/Resources/PlanetVisuals/tellus.asset) outside Play mode. Cloud counts, source path and **Cloud Relief** require **TAP > Planet visuals > Bake weather only**. **Cloud Layer Detail**, surface tint/saturation, normal strength and ocean colour are rendering controls; reopen Play mode to review saved edits. Scattering coefficient/height edits require the new **Bake atmosphere only** command. This small bake preserves weather and surface assets.

- [Focused tests](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/ksa_editor_tests.json): 9/9 passed, including source digest and cloud relief cache invalidation.
- [GPU colour check](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/ksa_colour.json): maximum linear channel error 1.49e-8.
- [Rendered checks](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/ksa_runtime.json): identical repeated distant frames and distant Layers/Volumetric frames; mean channel difference 0.0000897 across the 120 km volume boundary over a 2 m camera move. All four presets rendered. Atmosphere/cloud, terrain and plume shaders have zero compiler messages.
- Near-new/full Luma centre luminance is 0.0/0.601 with Sun disc/flare disabled to isolate body shading. Tellus behind the camera requests no atmosphere pass in the Luma map pose.
- [Before/after gallery](D:/Dev/KSP/Screenshots/PlanetFeedback/KsaDirection/GALLERY.md): three globe angles, 80 km nadir and 300 km horizon, matching UT, pose, FOV, resolution and Ultra/Layers settings. Actual camera renders omit the overlay HUD and do not establish graphical-player performance.
- [Restoration](D:/Dev/KSP/Docs/TestReports/PlanetVisuals/ksa_restored.json): original Sandbox session, stopped Flight scene, Low/Layers/scatter settings, save root and Profiler state restored. No new Windows player build was made for this pass; the existing executable predates these refinements.

## Limits of this visual pass

This is a step toward the reference appearance, not a claim of full KSA parity. The close 80 km nadir Layers capture now resolves broken billows but still exposes soft global surface detail and approximate cloud lighting. More detailed regional lighting/art needs further visual review and hardware validation. A surface tint cannot create the regional geology visible in the Mars reference. Larger geological changes require terrain authoring. New planetary bodies and KSA spacecraft/HUD reproduction are outside this pass. The airless Luma retains its physical phase lighting.

Review requires actual rendered images with fixed UT, lighting, pose, FOV, resolution and graphics mode. Tests establish rendering contracts, not visual acceptance. Source provenance is in [PLANET_VISUALS_SOURCES.md](PLANET_VISUALS_SOURCES.md).
