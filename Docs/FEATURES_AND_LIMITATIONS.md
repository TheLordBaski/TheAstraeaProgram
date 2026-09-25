# Features, verification and limitations

Legend — **Verified**: exercised by an automated test or an observed run with measured results (evidence given).
**Implemented**: in the game and playable, but not covered by an automated check. **Untested**: implemented but never
exercised end to end. **Limitation**: simplified or missing compared with the specification or with KSP.

Evidence sources: EditMode unit tests (`Assets/TAP/Tests/EditMode`, 20 tests), automated missions flown by the
scripted pilot through player controls (reports in [TestReports](TestReports)), every one of them re-run in the final
**standalone Windows build** (`build_<mission>.md`, including the complete lunar mission in
[TestReports/build_lunar.md](TestReports/build_lunar.md)), and screenshots taken during development and play-testing
(several bugs below were first spotted by the user watching the game view).

## Acceptance: the complete mission

| Step | Editor run 17 | Standalone build (`TAP.exe`, 2026-09-25) |
|---|---|---|
| Build → launch (Luma Pathfinder starter, 80 t, 7,643 m/s) | liftoff, max Q 36.9 kPa | liftoff, max Q 37.0 kPa |
| Orbit | Pe 80.8 / Ap 82.9 km, 3,885 m/s left, 285 s after launch | Pe 80.9 / Ap 82.8 km, 287 s after launch |
| Plan and fly the transfer | node found by patched-conic search, 939 m/s; map shows the encounter | 870 m/s, encounter confirmed after the burn |
| Luma orbit | capture 36.2 × 37.3 km | capture 36.7 × 37.3 km |
| Landing | tilt 2.4°, legs 4/4, crew aboard | tilt 2.6°, legs 4/4, crew aboard |
| EVA, walk, jump, flag | 12 m in 6.9 s, jump apex 1.64 m, flag planted | 12 m in 7.1 s, 1.69 m, flag |
| Back to the spacecraft | boarded with the jetpack | boarded |
| Launch from Luma | Luma orbit Pe 22.3 km | Pe 22.3 km |
| Return home | 300 m/s burn, Tellus periapsis 32.6 km | 271 m/s, 35.3 km (planned 32.0 km) |
| Reentry | heat shield peak 1,011 K, 7.8 g | 799 K, 6.4 g |
| Parachute landing | touchdown 0.1 m/s with crew | splashdown 0.9 m/s with crew |
| Result | **PASSED 19/19** | **PASSED 19/19** |

An earlier standalone run (before the landing-site selection) landed on a 29° crater wall and failed the 20° landing
check: the scripted pilot now hovers over to the flattest spot within 60 m, as a player would.

## All automated missions

| Mission | What it flies | Editor | Final standalone build |
|---|---|---|---|
| `lunar` | the complete mission above | 19/19 | 19/19 |
| `orbit` | Meridian to orbit, drift checks, 2 orbits of warp, orbital EVA and boarding, deorbit, splashdown | 9/9 | 9/9 |
| `persistence` | quicksave in orbit → warp → quickload → compare → one more orbit | 10/10 | 10/10 |
| `suborbital` | Skylark to 170 km and back under the parachute | 3/3 | 3/3 |
| `failures` | joint overload, unshielded burn-up, torn parachute, 111 m/s impact and crew loss | 6/6 | 6/6 |
| `docking` | RCS approach from 9.8 m, capture, docked stack, undock, re-arm | 5/5 | 5/5 |
| EditMode unit tests | orbits, patched conics, attachment, staging, Δv, JSON, wheel notches, geographic latitude | 20/20 | — |

Reports: `Docs/TestReports/<mission>.md` (editor) and `build_<mission>.md` (standalone build). The editor runs were
made during development; the standalone column is the release check of the shipped build, run after the last change.

## World and scope

| Feature | Status | Evidence / notes |
|---|---|---|
| Sandbox with launch site, assembly building, flight view, map view | Verified | Main menu → assembly → launch → flight exercised; all autotests start from the launch pad |
| Tellus (atmosphere, ocean, rotation) and airless tidally locked Luma, data-driven | Verified | `system.json`; reference values in `PLANETARY_SYSTEM.md`; orbit and lunar autotests |
| Rockets only (no aircraft, wings, jets, runways, multiplayer, career, tech tree) | By design | Fins exist only as rocket stabilisers |

## Construction (assembly building)

| Feature | Status | Evidence / notes |
|---|---|---|
| Vertical construction around a root part, stack nodes with snapping | Verified | Automated placement: tank snapped to the pod's bottom node at exactly −2.88 m; unit test `StackAttach_MatesNodesExactly` |
| Surface (radial) attachment on the exact part profile | Verified | 4 fins placed at r = 0.625 m on the tank surface |
| Radial symmetry 1/2/3/4/6/8 and mirroring onto symmetry counterparts | Verified | ×4 fins in one symmetry group, 90° apart; unit test with ×3 at 120° |
| Angle snap (15° positions, 90° rotations), free rotation (5° with Shift) | Implemented | Keys W A S D Q E |
| Attachment feedback (green nodes, green/red highlight, placement hint text) | Implemented | Screenshots |
| Pick up / move subtrees, copy (Alt), delete, undo/redo | Implemented | |
| Set parts aside (KSP's detached parts): dropped in empty space they stay greyed out, are not part of the craft (not launched, not in the readouts), can be built on and attached again | Verified (scripted editor session) | Meridian: lower stage set aside (craft 6 parts, 8 set aside, info line in the report), picked up and re-attached (14 parts again, same staging) |
| Pick up the root (the whole craft), re-root on any stack-attached part (part menu → Make root part), put back with Esc | Verified (scripted editor session) | Re-rooted on the upper decoupler, set the capsule section aside, attached the whole craft under it: 14 parts, root = heat shield, staging, fin symmetry, 3,901 m/s and TWR 1.81 identical to the original |
| Configurable staging: automatic order, drag parts between stages, new stage gaps, reset | Implemented | Unit test `AutoStage_EnginesBelowFireFirst_ChuteLast` |
| Readouts: mass, dry mass, TWR, Δv per stage and total (sea level / vacuum / Luma), burn time, EC, monoprop, crew seats, size | Verified | Unit tests; the in-flight Δv readout matches the assembly figures (6,926 m/s = 1,126 + 1,425 + 2,013 + 2,362 for the old Pathfinder) |
| CoM / CoT / CoP markers and stability verdict; design warnings | Verified | Unit tests `DeltaV_DecouplerBlocksFuelFlow`, `DesignAnalysis_StarterRocketsAreSound` |
| Save / load craft files, starter craft library | Verified | Lossless JSON round trip unit test; the `ascent` test flies craft files |
| Tested starter rockets (suborbital, orbital, lunar) | Verified | Skylark: `suborbital` 3/3 (apex 170.6 km, chute armed at 6.2 km / 204 m/s, touchdown 0.0 m/s); Meridian: `orbit` 9/9 (orbit, 2 orbits of warp, orbital EVA, splashdown 0.9 m/s), `persistence` 10/10; Pathfinder: `lunar` (editor and build). The Skylark was redesigned after its first version peaked at only 35 km; the Pathfinder was sized by ascent comparison flights (see the checklist log) |
| Every part has a gameplay purpose | Implemented | 36 parts: 3 command, 7 tanks, 4 engines, 2 solid boosters, 3 decouplers, docking port, 2 legs, 3 parachutes, 2 heat shields, 2 batteries, 2 reaction wheels, RCS, 2 monoprop tanks, nose cone, fin |

## Flight physics

| Feature | Status | Evidence / notes |
|---|---|---|
| Gravity from the dominant body, thrust, mass/CoM/inertia from parts | Verified | Orbit and lunar autotests (editor and build) |
| Fuel flow through stacks, decouplers block crossfeed, per-part solid fuel | Verified | Staging and burn times in autotests; unit tests |
| Atmosphere (pressure, density, temperature profile), per-part drag with occlusion, Mach effects, fins and body lift | Verified | Max dynamic pressure 33–49 kPa across designs; a high-thrust design lofted and a sluggish one fell back, as physics predicts |
| Gimbal, reaction wheels, RCS with limited authority and resource use | Verified (gimbal, wheels) / Implemented (RCS) | SAS holds prograde through ascents; the lander steers with wheels and gimbal |
| Throttle, staging, engine restart; sea-level vs vacuum Isp and thrust | Verified | The lander's engine restarts for descent, lunar ascent and the return burn |
| Structural loads from simulated forces (tension, compression, shear, bending); weak joints break; broken parts become debris | Verified | Boosters tore off in shear before their mounts were strengthened; a lander that tipped over broke at the capsule joints; `failures` autotest. A rocket standing still on its engine bell broke at the capsule joints (213 kN·m of phantom bending): ground contact was split evenly over the points PhysX reported, often all on one side of the bell rim, and the mismatch landed on the top joint. Contacts now use each point's own impulse, and each joint's load is summed over the side without ground contact (else the lighter side); `failures` still passes 6/6 |
| Collisions / impact damage per part (a part feels its share of the velocity change: light bodies cannot wreck heavy ones), crew loss | Verified | Failed runs: impacts at 58–243 m/s destroyed the parts and killed the crew; `failures` autotest; a rocketeer who stepped out 27 m up on the pad died on landing (20.8 m/s) while the rocket stayed intact |
| Ground contact, landing legs (suspension, breakage), tipping | Verified | Landings with 4/4 legs at 2.4° and 7.4°; a tall lander tipped over on an 11° slope (logged 10° → 86°), which led to the wide lander |
| Heating with density, speed and orientation; flow shadowing; ablation; overheating destroys parts | Verified | Orbital reentry 672–742 K on the heat shield; lunar-return reentry 783–1,011 K on the heat shield (depends on the return periapsis); `failures` autotest: an unshielded capsule entering nose first at 4.5 km/s loses its parachute to heat (1,402 K > 1,400 K) with the pod at 1,599 K of its 1,600 K limit |
| Parachutes: arm → semi → full at 1 km, load and temperature limits, unsafe deployment tears the canopy; safe / risky / unsafe indicator | Verified | Touchdowns at 0.0–0.9 m/s; `failures` autotest (armed at 389 m/s in a 45° dive at 4.8 km: canopy torn at 330 m/s, 103 kN > 100 kN). The scripted pilot arms the chute only once the indicator reads safe, as a player should |
| Surface vs orbital velocity, sea/terrain altitude | Verified | HUD readouts, SAS speed modes |

## Orbital mechanics and map

| Feature | Status | Evidence / notes |
|---|---|---|
| Patched conics with SOI transitions (Tellus ↔ Luma) | Verified | Unit tests (incl. nodes several orbits ahead); lunar autotests |
| Predictions agree with physics | Verified | Burns executed to 0.15–0.30 m/s residual; post-burn encounters confirmed; return periapsis 32.6/36.7 km for a 32 km plan |
| Map view: bodies, vessels, debris, flags, orbit lines, Ap/Pe, encounters | Implemented | Screenshots |
| Readouts: Ap, Pe, inclination, period, times to apsides, SOI, encounter Pe, escape | Implemented | |
| Manoeuvre nodes: create, drag handles, fine steps, time shift, delete; burn Δv/duration/time-to-burn/remaining | Verified | Autotests plan and fly all burns through the node API used by the map UI |
| Map clicks as in KSP: the orbit pop-up (**Add maneuver node**, **Warp here**), dragging a node along its orbit, deleting a node in the map (right-click, Del, × button) | Verified (logic) / Implemented (mouse) | The pop-up, node creation, drag, delete and warp were exercised by calling the map UI in a play session: orbit picking lands on the clicked time exactly (0.0 s error at four points), warp here ran at 50× and stopped at the point. Real mouse input could not be injected into an unfocused game view |
| Targets (Luma or a vessel), closest approach and relative velocity | Implemented | |
| Docking ports: magnetic guidance in the last metres, capture within range / alignment / speed limits, merge into one vessel, undock with a push, re-arm only once clear | Verified | `docking` autotest 5/5: chaser flown in on RCS translation from 9.8 m, captured at 0.25 m/s and 0.3°, the stack held together, undocking pushed the craft apart at 0.34 m/s and the ports re-armed 3.5 m apart without re-docking |

## Controls, interface, camera

| Feature | Status | Evidence / notes |
|---|---|---|
| Pitch/yaw/roll, throttle, staging, SAS (hold, pro/retro, normal, radial, target, manoeuvre), RCS, legs | Verified | Autotests use the same control paths |
| Navball with prograde/retrograde, normal, radial, target, manoeuvre markers; speed modes | Verified | Screenshots |
| KSP axes: on the pad the rocket's right side faces east, so D tips it east and W/S pitch north/south; navball headings run clockwise | Verified | Holding D from the pad tipped the nose to heading 090°; the scripted pilot now pitches over with D (orbit and lunar missions). The world used to be mirror-imaged (east left of north) so the navball's heading ring ran backwards and its lettering was mirrored; north is now the −Y pole, which un-mirrors the view without changing any physics |
| HUD: altitude (sea/terrain), vertical/horizontal speed, Mach, Q, g-meter, throttle, resources, EC, crew, staging stack, Δv, warnings | Verified | Screenshots (editor and build) |
| Cameras: orbit (free), chase, locked; map camera | Implemented | Zoom: 25% per wheel notch in flight, 30% on the map, 22% in the assembly building, down to just above the focused body or ~10 m from a focused vessel (map markers no longer swallow the wheel). The wheel reports ±120 per notch in the editor but ±1 in a player build, so the build zoomed 120 times slower; readings are now converted to notches, and the zoom speed can be changed in the developer tools (saved) |
| Pause menu, quicksave/quickload, revert to launch, revert to assembly, recover, leave | Implemented / Verified (quickload) | `persistence` autotest |
| Control guide (F1), tooltips, mission guide with hints | Implemented | Screenshots |
| Developer tools (**Alt+F12** or **`**, or the pause menu), like KSP's cheat menu: infinite propellant and electricity, no crash damage, ignore heat, unbreakable joints (until the game is closed), refill tanks, teleport to a circular orbit (altitude, inclination) or land at a latitude/longitude on Tellus or Luma, zoom speed; the window can be dragged | Verified (editor play sessions) | Infinite propellant: tanks stayed at 36,000 / 25,600 kg through 9 s of full thrust, consumption resumed when switched off. No crash damage + unbreakable joints: the 51-part Pathfinder dropped onto the ground at ~45 m/s stayed whole. Orbits: Pe/Ap 100.0/100.0 km at Tellus, 30.0/30.0 km at Luma. Landing: the full Pathfinder stood on Luma (the tool moved it 12 m from an 11° slope to 1.5°) and beside the Tellus pad (0.0°), 51/51 parts after 30 s |

## Time warp and persistence

| Feature | Status | Evidence / notes |
|---|---|---|
| Rails warp up to 100,000× with altitude limits, physics warp 2–4× | Verified | Orbit autotest (2 orbits on rails), lunar coasts, 10 h and 24 h warps while landed on Luma, 1000× on the launch pad followed by a normal lift-off; leaving warp (also by stepping out on EVA) returns every vessel to physics |
| Accurate propagation, no drift, no jumps (double precision, floating origin, Krakensbane) | Verified | 60 s physics coast da = 0.000 m; 2 orbits on rails da = 0.000 m, de ≈ 1e−15 |
| Inactive vessels, debris and flags kept on rails | Implemented | |
| Save/load preserves everything | Verified | `persistence` autotest 10/10: after quicksave → warp → quickload the vessel is back at Δr 0.000 m / Δv 0.0000 m/s with the same parts, crew, resources, vessels and milestones, and one more orbit on rails ends 0.00 m from the prediction; `lunar-surface` reloads a landed lander standing on its legs |

## EVA

| Feature | Status | Evidence / notes |
|---|---|---|
| Exit / board with distance and speed limits, inherited velocity | Verified | Lunar mission (editor and build); `orbit` autotest: EVA in orbit (pushes off at 0.56 m/s), 7.1 m jetpack excursion, back to the hatch and boarded |
| Walk, run, jump, ground contact, low gravity, limited jetpack | Verified | Walk 12 m in 6.9 s, jumps 1.6–1.8 m at 0.16 g, jetpack climb back to the hatch |
| Flag planting | Verified | Lunar mission |
| States preserved on switching and saving | Implemented | |

## Presentation

| Feature | Status | Evidence / notes |
|---|---|---|
| Clean 3D: procedural parts, launch complex, assembly hangar, cube-sphere terrain with LOD, sky and horizon, atmosphere shell | Verified | Screenshots (editor and standalone build) |
| Planets visible from any distance | Verified | Above ~500 km the terrain used to vanish (the user's screenshots): the sky, drawn by the skybox pass after the terrain, painted over ground whose depth had reached the far-plane value. The sky is now a background dome drawn first; screenshots at 550 km and 3,000 km show the full planet |
| Sunlight, night sides, eclipses (nearby objects dark in a planet's shadow, planets still sunlit; map always sunlit) | Verified | Screenshots at Luma dawn and midnight (lander dark, map shows Luma and Tellus lit); fixed after the user reported planets going black |
| Exhaust plumes with pressure-dependent expansion, smoke, staging and decoupling effects, reentry plasma, heat glow, explosions, canopy shreds | Implemented | |
| Part models can be exported (FBX with hierarchy and pivots, OBJ + MTL, a reference sheet) and replaced by hand-made FBX models; collider, attach nodes and anchors stay data-driven | Verified | Blender 5.2 round trip on the Hornet engine: bell widened 30%, gold ring added; back in the game the unchanged body was identical, the bell kept its gimbal pivot, materials mapped to the shared ones ([PART_MODELS.md](PART_MODELS.md), `Screenshots/part_model_roundtrip.png`) |

## Known limitations

* Part, character and building models are procedural low-poly meshes generated in code. Any part's model can be
  exported and replaced with a hand-made FBX ([PART_MODELS.md](PART_MODELS.md)); characters and buildings cannot yet.
  There is no audio.
* Vessels are single rigid bodies with analytic joint loads: parts do not flex or wobble; failures happen at joints.
* Aerodynamics are per-part approximations (no CFD). A flat end face counts as fully exposed unless a part at least
  ~88% as wide covers it (use a tapered adapter under a narrower stack).
* Only one planet and one moon; the sun is a fixed light direction, not a body. Eclipses are decided for the camera
  position (one shadow test for all nearby objects).
* Docking was verified with two craft placed 12 m apart on the same orbit (`docking` test). A full rendezvous between
  separately launched craft has not been flown by the scripted pilot (the target readouts it would use exist).
* Crew have no g-force limits or experience; crew are assigned automatically. Tank fill levels cannot be edited.
* There are no ladders. A rocketeer who steps out high above the ground falls: the suit survives about 12 m/s, i.e.
  a drop of ~7 m on Tellus or ~45 m on Luma (use the jetpack to get down and back up).
* Reentry heating is forgiving at lunar-return speeds: an unshielded capsule flown blunt end first peaked at 1,053 K
  (pod limit 1,600 K) in a lunar-return entry, so the heat shield is essential only for faster or nose-first
  entries (the `failures` test uses 4.5 km/s nose first). The starter rockets carry shields anyway.
* The terrain is procedural noise with craters on Luma; there are no biomes or science. Luma's slopes can topple tall
  landers; the starter lander is built wide for that reason.
* Spent boosters tumble quickly after separation (debris only; they fall away cleanly).
* The scripted pilot is a test tool, not a player autopilot (players have SAS). Its transfer planner prefers a fast
  transfer (~940 m/s instead of the ~860 m/s Hohmann burn).
* The standalone player was built and verified on one Windows 11 machine only; other platforms were not built.
