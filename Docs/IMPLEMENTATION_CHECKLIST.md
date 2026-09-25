# The Astraea Program (TAP) — Implementation Checklist

Durable tracking document for the rocket-sandbox demo (Unity 6.5 / 6000.5.7f1, URP 17.5).
Status legend: `[ ]` not started · `[~]` in progress · `[x]` implemented · `[V]` implemented **and verified** (test or observed run).

## 0. Project foundation
- [V] Unity 6.5 URP project at `D:\Dev\KSP`, trimmed package manifest, Pipeline package for live-editor automation
- [V] Folder + asmdef layout (Core / Parts / Simulation / Trajectory / Construction / Persistence / UI / Game / App / Editor / Tests)
- [V] Player settings (product name, fixed timestep 0.02 s, physics settings, layers)
- [V] Data files: `Resources/Data/system.json` (planets), `parts.json` (36 parts), starter craft JSON (`Resources/Craft`)
- [x] Docs: planetary system reference, controls, lunar walkthrough, features and limitations, test reports

## 1. Launch, steering, fuel consumption, staging, landing
- [V] Double-precision math (Vector3d), Kepler orbit (universal variables), body data — 8 EditMode tests
- [V] Universe clock (UT), floating origin + Krakensbane velocity frame, reference-body-centred frame
- [V] Planet renderer: cube-sphere quadtree LOD terrain with noise heights, colliders near vessels, launch-site flattening
- [V] Part database (data-driven), procedural part models, attach nodes
- [V] Vessel runtime: single rigidbody per vessel, mass/CoM/inertia from parts, resources & fuel flow
- [V] Engines (throttle, Isp vs pressure, gimbal, restart), SRBs, decouplers, staging (orbit autotest)
- [V] Flight input, SAS attitude hold, reaction wheels (autotest drives the same control inputs)
- [V] Aerodynamic drag per part with occlusion + Mach curve, fins, body normal force (max Q 39.9 kPa ascent)
- [V] Landing legs (raycast suspension), ground contact, impact damage — lunar landings (4/4 legs, tilt 0.1–2.4°), `failures` autotest (impact)
- [V] Flight HUD: altitude, vertical speed, speeds, throttle, fuel, EC, staging stack, navball (screenshot 2026-09-23)
- [V] Chase camera, launch pad, sky/atmosphere visuals, exhaust effects (screenshots)

## 2. Stable orbit, map view, orbital readouts, time warp
- [V] On-rails Kepler lock for coasting vessels (no drift: da = 0.000 m over 60 s physics coast)
- [V] Map view (scaled space), orbit lines, Ap/Pe markers, vessel/debris icons — screenshots (encounter, Luma orbit, map at Luma midnight)
- [x] Orbital readouts: Ap, Pe, inclination, period, time to Ap/Pe, SOI, escape
- [V] Time warp: rails warp + physics warp, safety limits (2 orbits on rails: da = 0.000 m, de ≈ 1e-15)
- [V] Persistent inactive vessels / debris on rails, loading/unloading by distance — parked ship stays loaded through the orbital EVA; landed lander reloads standing (`lunar-surface`)

## 3. Maneuver nodes, lunar transfers, encounters, lunar landing
- [V] Patched-conic predictor with SOI transitions (Tellus <-> Luma), encounter detection — unit tests
- [V] Maneuver nodes: create/edit/delete, prograde/normal/radial (circularisation node executed, residual 0.26 m/s)
- [x] Burn info: dv, burn time, time to burn, remaining dv vector, navball marker
- [x] SAS modes: prograde/retrograde/normal/anti/radial/target/maneuver (pro/retro/maneuver exercised by autotest)
- [x] Targeting (Luma or vessel): closest approach, relative velocity
- [V] Docking ports: capture, merge, undock, re-arm once clear — `docking` autotest 5/5
- [V] Luma terrain + landing (legs), surface/orbital readouts in Luma SOI — lunar autotest 19/19 (editor and standalone build)

## 4. EVA, boarding, flag planting, return missions
- [V] EVA exit from hatch with inherited velocity, walking/jumping/jetpack, low gravity — lunar autotest (walk 12 m, jump 1.6 m)
- [V] Boarding with distance + relative-speed limits, crew roster persistence — lunar autotest (jetpack back to the hatch)
- [V] Flag planting (persistent flag vessel) — lunar autotest
- [V] Vessel switching (`[` / `]`), control transfer, EVA in orbit — `orbit` autotest (out, 7 m jetpack excursion, boarded)

## 5. Heating, structural damage, reentry, parachutes
- [V] Two-node (skin/internal) thermal model, convective heating vs density/speed/orientation, radiation, conduction
- [V] Flow shadowing (heat shield protects parts behind it), ablation (orbital reentry: max skin 742 K on the shield)
- [V] Structural loads per joint (force/torque), overload breaks, excessive-g failures — `failures` autotest (244 kN > 180 kN)
- [V] Collision/impact damage per part, explosions, debris persistence — `failures` autotest (capsule destroyed at 111.6 m/s)
- [V] Parachutes: arm/semi/full deploy, pressure/speed/temperature limits, tearing, cut (splashdown 0.9 m/s)
- [x] Reentry visuals (plasma glow), damage feedback

## 6. Editor polish, saving, quicksave/load, tutorials, packaged build
- [V] Assembly editor: palette with thumbnails/search, stack/radial attach, symmetry (incl. counterpart mirroring),
      angle snap, rotate, pick up/move/copy, delete, undo/redo — automated placement test + screenshots
- [x] Staging editor (drag parts between stages / new stage gaps), readouts (mass, TWR, dv per stage, EC, crew, CoM/CoT/CoP, warnings)
- [V] Save/load craft, starter craft (suborbital, orbital, lunar) — all three flown end to end by autotests
- [V] Game save: quicksave/quickload (`persistence` autotest 10/10: state restored exactly), restart flight / return to VAB (implemented), persistence of everything
- [x] Main menu (continue / new / load / controls), pause menu, control guide (F1), tooltips, mission guide hints
- [V] Standalone Windows build, automated mission verification in build: all six missions (lunar, orbit, persistence,
      suborbital, failures, docking) pass in `Build/Windows/TAP.exe`

## Verification log
- 2026-09-23 — EditMode tests (OrbitTests, 8 tests): all pass. (2026-09-24: 18 tests incl. construction/staging/Δv/JSON, all pass.)
- 2026-09-23 — `orbit` autotest (Meridian Orbiter, editor play mode): **PASSED 6/6** — liftoff; max Q 39.9 kPa;
  stable orbit Pe 79.4 km / Ap 79.8 km (e 0.0003); physics coast da = 0.000 m / 60 s; rails warp 2 orbits
  da = 0.000 m; deorbit, reentry (max skin 742 K on HS-125, 9.3 g), chute armed → semi → full, splashdown 0.9 m/s with crew.
- 2026-09-23 — Assembly editor automated placement: pod root → tank on pod bottom node (y −2.88 m) → engine on tank
  bottom (y −5.88 m) → chute on pod top → 4 fins surface-attached with ×4 symmetry (r = 0.625 m, 90° apart, one
  symmetry group); auto staging engine=1 / chute=0; TWR 3.83; CoP 1.0 m below CoM (stable); launch from the editor
  loads the Flight scene with the craft on the pad.
- 2026-09-24 — Lunar autotest runs 1–8 exposed and fixed (each confirmed by instrumented reproductions):
  * Kodiak booster mounts rated below their own thrust (tore off in shear) → 700 kN / radial decoupler 750 kN.
  * Transfer stage TWR 0.41 could not finish orbit → Pathfinder redesigned (Hornet transfer engine, bigger lander
    tank, extra 4 t core tank: 6,929 m/s vacuum Δv, lift-off TWR 1.80).
  * Warp-to requests refused while engines spool down → autopilot warp helper waits and re-engages.
  * Separation impulses were discarded for vessels pinned to their analytic (Kepler) orbit → split vessels are
    unlocked for the separation step.
  * Separation impulses bypassed the recorded-force path, so the structural analysis blamed the resulting motion
    on the joints (phantom ~1,000 kN·m bending destroyed the whole stack) → impulses are applied as recorded
    one-step forces on the parts they act on.
  * Ground colliders moved kilometres in one step after a vessel switch (swept by continuous collision) and old
    vessel colliders lingered until end of frame → large moves teleport; vessels are deactivated before destroy.
  * Fast impacts could tunnel through terrain → continuous collision on ground chunks + below-terrain crash check.
- 2026-09-24 — Lunar run 11 (two-booster Pathfinder) reached a 36.5 × 37.1 km Luma orbit (TLI residual 0.29 m/s,
  mid-course correction 13.8 m/s, capture 323 m/s) but exposed:
  * Final descent throttle used the *unsigned* speed: once the lander stopped and began to rise, the rising speed read
    as "too fast", full throttle pushed it 2 km up and emptied the tanks → 58.7 m/s crash. Replaced with a hover
    controller that holds a signed sink rate and cancels sideways drift by leaning the thrust (≤ 30°).
  * Flight HUD stage Δv skipped the burn in progress (analysis started at the next stage to fire, e.g. the lander
    showed 0 m/s) → shared `Vessel.ComputeStageDeltaV` starts at the stage that is burning.
  * Lunar ascent tipped over with a raw pitch key, so the heading depended on the lander's roll after touchdown
    (possible polar orbit, unusable for the return) → steers east explicitly into Luma's orbital plane.
  * "Climb too flat" pitched straight up (radial out) with a low-thrust upper stage → proportional pitch above
    prograde limited by dynamic pressure (≈ 200 m/s saved on the Pathfinder ascent).
  * Transfer stage is always jettisoned before the descent (the legs are on the lander); descent coast on rails.
- 2026-09-24 — Ascent comparison flights (`-autotest ascent -craft <file>`), Δv left in an 83 km orbit:
  2 boosters 2,893 m/s; 4 boosters + short core and 6 boosters lofted (target apoapsis reached at 39–44 km) and
  ended eccentric after a 1,300–1,500 m/s circularisation; **4 boosters + two long core tanks + two long lander
  tanks: 4,364 m/s left, max Q 35 kPa** → adopted as the Luma Pathfinder (7,928 m/s vacuum, 91.3 t, TWR 1.75).
- 2026-09-24 — Lunar run 12 (new Pathfinder): orbit with 4,455 m/s left, then the planned trajectory showed no
  encounter although the transfer search had found one: a closed orbit was predicted for only one revolution, so a
  manoeuvre node more than one orbit ahead (here 2,047 s on a 1,914 s orbit) was never applied — also visible to
  players who move a node forward by whole orbits. The orbit now runs on to the next node; regression test
  `PlannedTrajectory_AppliesNodeSeveralOrbitsAhead` (EditMode tests: 18/18 pass).
- 2026-09-24 — Lunar runs 13–16 and resumed surface tests (`lunar-surface` resumes from a snapshot saved after the
  landing, so EVA → return can be iterated in minutes):
  * Run 14 hit Luma: the transfer search had picked an encounter on the way back down from a high apoapsis (~20 h
    coast), where a 0.15 m/s burn residual moved the Luma periapsis to −1.4 km → the search now prefers a quick
    outbound transfer (cost per second of coast and per m/s), and a periapsis correction inside Luma's sphere of
    influence trims it to 30 km before the capture burn.
  * EVA walk "0.0 m": distances were measured between world positions taken at different times, but the world frame
    follows the active vessel (floating origin) → measured relative to the lander / above the terrain.
  * Loading a saved landed vessel placed it relative to its own not-yet-placed rigidbody (the handle already pointed
    at the new vessel), with zero inertial velocity: the lander appeared ~2 m low and sank into the ground, and blew
    apart on lift-off (reported by the user from the game view). Fixed in `LoadHandle`; the ground under a vessel
    loaded on the surface is now built immediately and it never coasts on a free-fall arc while settling. Same
    root cause affected any non-active vessel loaded from a save. Quickload check tightened from 2 m to 0.5 m.
  * The redesigned lander (two long 1.25 m tanks, centre of mass ~7 m up) slowly tipped over on an 11° slope while
    the crew was outside (logged twice a second: 10° → 86° in 15 s) → squat 2.5 m lander (tapered adapter tank +
    2.5 m tank, legs spread ~5 m, centre of mass ~3.6 m, tips beyond ~26°), 3,476 m/s.
  * EVA pack now holds up to 10 kg of monopropellant (from the part data) drawn from the vessel and returned on
    boarding; RCS blocks moved off the hatch line.
  * Run 16: the heavier wide lander left the core stage at TWR 1.11 after booster separation; the climb went flat at
    16 km and the rocket fell back (the steering's pitch-up is limited by dynamic pressure). Ascent comparison with the
    wide lander: 4 boosters + long core 3,848 m/s left in orbit but sluggish (730 s to orbit); **4 boosters + long/short
    core + transfer stage 3,888 m/s left, clean 290 s ascent, max Q 37 kPa → adopted (80.0 t, 7,643 m/s, TWR 2.0)**.
- 2026-09-24 — Lunar run 17 (editor) **PASSED 19/19**: orbit 285 s after launch with 3,885 m/s left, transfer 939 m/s,
  capture 36.2 × 37.3 km, landing tilt 2.4° with 4/4 legs, EVA walk 12 m in 6.9 s, jump 1.64 m, flag, boarding, lunar
  ascent to Pe 22.3 km, return burn 300 m/s, Tellus periapsis 32.6 km, heat shield peak 1,011 K, 7.8 g, touchdown
  0.1 m/s with crew. Standalone Windows build, same mission: **PASSED 19/19** (splashdown 0.9 m/s, 944 K, 8.8 g).
- 2026-09-24 — Issues found by the user watching the game view and by follow-up tests:
  * "No light on Luma and Tellus": the eclipse logic switched the single sun off whenever the camera was in a planet's
    shadow, blacking out every planet in view (their day sides too). Now only nearby objects are eclipsed (URP light
    rendering layers on the sun); planets stay sunlit and their night sides are dark by their own shading.
  * Time warp while landed destroyed the lander ("crashed into the terrain at 16,643 m/s"): leaving rails read the
    landed vessel's state after making it dynamic (stale pre-warp velocity), and the terrain collision chunks, not
    moved during warp, were then *swept* through 199° of Luma's rotation in one physics step. Now the rails state is
    read first, ground is ensured and settled, and ground chunks teleport after any time gap or implausible jump
    (verified: 24 h and 10 h warps on Luma's surface, lander standing afterwards).
  * The staging stack overlapped the mission guide on shorter screens → it now fills the space below the guide.
  * The lunar ascent raises the landing legs after lift-off (retracting them first dropped the lander on its engine).
- 2026-09-24 — A second standalone run landed on a 29° crater wall (legs intact, but over the 20° check) → the
  scripted pilot now samples the collision surface on rings up to 60 m while hovering in and flies to the flattest
  spot (slope over the lander's footprint plus roughness). Also: a Luma-orbit snapshot + `lunar-landing` resume test.
  **Final standalone build run: PASSED 19/19** — landing site 1.0° slope 12 m away (5.8° below), tilt 0.1°, EVA walk
  12 m in 7.1 s, jump 1.66 m, flag, boarding, lunar ascent Pe 22.3 km, return burn 350 m/s, Tellus periapsis 31.3 km,
  heat shield 1,007 K, 7.8 g, splashdown 0.9 m/s with crew. Editor `lunar-surface` resume test: PASSED 12/12.
- 2026-09-24 — User report "still no light" (map view during the parachute descent at night): the map's planet
  spheres use the standard lit shader and inherited the vessel's eclipse, the dimming of a sun below the vessel's
  horizon and the night haze → the map is now treated as a view from space (planets always sunlit), and sunlight is
  no longer tinted by a sunset outside an atmosphere. Verified with the lander at Luma midnight: map shows Luma and
  Tellus lit (screenshots `Screenshots/map_midnight_fix2.png`, `map_tellus_fix.png`).
  Harness note: two test runners were found driving the editor at once (a stopped one had survived), which spoiled
  that batch's orbit/suborbital/failures/persistence results; the batch was re-run with a single runner.
- 2026-09-24 — Regression batch (editor) after the fixes below: `orbit` **9/9**, `persistence` **10/10**,
  `suborbital` **3/3**, `failures` **6/6**.
  * Skylark redesign: the first version (4.6 t, 1,780 m/s) peaked at only 35 km → finned Kodiak SRB at 80% thrust,
    9.2 t, 2,447 m/s, TWR 2.2: apex 170.6 km, parachute armed at 6.2 km / 204 m/s, touchdown 0.0 m/s.
  * A parachute armed at a fixed altitude after a steep fall tore at 1,110 m/s → the scripted pilot now arms it only
    once the staging stack's safety indicator (green / amber / red, same logic as the HUD) reads safe.
  * `failures` test made decisive: the "puller" rig got a fuel tank above its engine; the unshielded capsule
    survived a lunar-return entry blunt end first (1,053 K, pod limit 1,600 K — documented as a limitation) and a
    steeper one (1,477 K), so the heat test now enters nose first at 4.5 km/s (the parachute burns at 1,402 K); the
    unsafe-chute test deploys at 450 m/s at 5 km (canopy torn at 103 kN > 100 kN).
  * Quickload time check compares the clock gained with the real time since the reload (the old fixed 1.5 s bound
    failed on a slow editor frame: 2.54 s gained in 2.53 s).
  * **Orbital EVA bug:** leaving the ship during the last seconds of a time warp set the warp index to zero without
    leaving rails, so every loaded vessel stayed a frozen kinematic body; the parked ship's drawn position and its
    rails orbit drifted apart and 27 s later the loader dropped it as "2.7 km away" (the test then crashed on the
    destroyed hatch). EVA now stops warp properly (all vessels back in physics), the scripted pilot's warp helper
    returns only once the warp has run out, and leaving warp keeps a rocket that is still clamped to the pad
    kinematic (checked: 1000× warp on the pad, then a normal lift-off with all 51 parts).
  * After the deorbit burn the pilot cut the throttle and dropped the upper stage in the same frame; the stage's
    engine, still winding down (throttle response ~0.3 s), rammed it into the capsule at 6.8 m/s. The pilot now waits
    for the engines to stop before staging (the walkthrough now tells players the same), and impact messages name the part
    actually hit.
  * Stars were drawn as 10–16 px blobs that looked like snow around a vessel in orbit → 4096×2048 star texture with
    point-like stars (screenshot `Screenshots/luma_stars_sharp.png`).
  * Harness: the mission runner re-arms the test until the editor confirms it and verifies the mission started.
- 2026-09-24 — Docking verified (new `docking` test, 5/5): chaser and target start 12 m apart on one 100 km orbit,
  the pilot flies in on RCS translation only, the ports capture and merge the craft, the stack holds, undocking
  pushes them apart. Found and fixed on the way: (1) after undocking the ports re-armed on a 4 s timer while still
  0.5 m apart, so their magnets would pull the craft straight back together → ports now re-arm only once clear of
  other ports; (2) the magnets kept pulling however fast the craft closed (captures at 0.76–0.99 m/s against a
  1.0 m/s limit) → they now only help below 0.3 m/s; (3) the test pilot controlled the port-face velocity, which
  includes the craft's wobble, and the RCS blocks sat below the centre of mass → it now controls centre-of-mass
  velocities with the blocks at the centre of mass: 0.01 m off-axis, capture at 0.25 m/s and 0.3°.
- 2026-09-24 — HUD and collision fixes from build screenshots: the EVA panel covered the lower half of the navball
  (moved to the bottom left, sized to its text) and showed the jetpack as "9.29/5.00 kg" (now the suit's real
  capacity); heat warnings fired for any part above 72% of its limit in kelvin, so a rocketeer at a normal 290 K
  (limit 380 K) always showed one — warnings now measure the rise from room temperature. Impact damage used the raw
  relative speed, so a 100 kg rocketeer falling past the pad-held Pathfinder destroyed a solid booster at 10.9 m/s;
  a part now takes its share of the velocity change (mass-weighted; ground, pad and clamped vessels are immovable).
  Re-check: rocketeer lost on landing from 27 m (20.8 m/s), rocket intact with 51/51 parts.
- 2026-09-24 — **Final standalone build, every mission re-run after the last change: `lunar` 19/19, `orbit` 9/9,
  `persistence` 10/10, `suborbital` 3/3, `failures` 6/6, `docking` 5/5**; EditMode unit tests 18/18. Lunar run: the
  ground under the descending lander was a 30.5° slope, the pilot hovered 48 m to a 0.6° spot and landed at 1.3°;
  walk 12 m in 7.0 s, jump 1.87 m, flag, boarding, lunar orbit Pe 22.3 km, return burn 349 m/s, Tellus periapsis
  31.3 km, heat shield 825 K, 6.4 g, splashdown 0.9 m/s with crew. Reports: `Docs/TestReports/build_*.md`.
- 2026-09-25 — Second round of user feedback (from play-testing in the editor):
  * **Axes and navball**: in KSP, D (yaw right) tips a rocket east off the pad; here it was W, and the navball looked
    "rotated". Root cause: the world was mirror-imaged. The orbital maths is right-handed with bodies spinning about +Y,
    but Unity renders left-handed, so with +Y called north, east was on the *left* of north: the heading ring ran
    backwards (yaw right lowered the heading) and the navball lettering was mirrored. North is now the −Y pole (one
    helper, `Geo`), which un-mirrors everything seen without touching physics, orbits or saves' orbital states. The
    rocket stands on the pad with its right side east (belly north), so **D tips it east** and W/S pitch north/south; the
    scripted pilot's pitch-over now holds D. The map camera looks from the north so orbits run counter-clockwise.
  * **Planet vanished above ~500 km** (user screenshots at 427/513/550 km): reproduced at 550 km; not culling or a
    distance cut-off but depth: with a near plane of ~0.3 m the far terrain's depth reached the far-plane value and the
    skybox pass (drawn after opaque geometry) painted the sky over it — raising the near plane or disabling the skybox
    brought it back. The sky is now a background dome drawn first (no depth test); checked at 550 km and 3,000 km.
  * **Zoom**: flight camera 25% per wheel notch (was 12%), editor 22% (13%), map 30% (15%). The map could not zoom in
    towards the vessel because its marker icon (a tooltip target at the screen centre) counted as UI and swallowed the
    wheel; markers no longer block the wheel or clicks, and the map zooms down to the focused body's surface.
  * **Map nodes as in KSP**: clicking the orbit opens a pop-up (**Add maneuver node**, **Warp here**); a node is dragged
    along its orbit by its marker (the first node on the current orbit, later ones on the planned path between their
    neighbours); right-click, Delete or the × button deletes it. Orbit picking now interpolates along the drawn segments
    (0.0 s error at four points). Exercised by calling the map UI in a play session (the unfocused game view drops
    injected mouse input).
  * **Assembly building**: the root part can be picked up (the whole craft, put back with Esc or attached to a set-aside
    group), any stack-attached part can be made the root (part menu → Make root part), and parts dropped in empty space
    are **set aside** — greyed out, not part of the craft, saved with it, never launched; they can be built on, picked
    up and attached again. Checked in a scripted editor session on the Meridian (set aside / re-attach, re-root and
    attach the whole craft under the set-aside capsule: identical staging, symmetry, Δv and TWR).
