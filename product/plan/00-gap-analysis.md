# 00 · Gap analysis: KSP 1.0 vs TAP today

What KSP had at its 1.0 release (April 2015), what TAP's vertical slice has (verified in the build, see
[Docs/FEATURES_AND_LIMITATIONS.md](../../Docs/FEATURES_AND_LIMITATIONS.md)), and which tasks close each gap.

**Status:** **Have**, **Partial** (the base exists), **Missing**, **No** (out of scope by your decision) or
**Beyond** (not in KSP 1.0 but planned).

## Game modes and meta

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Sandbox mode | sandbox game with saves | Have | — |
| Science mode | — | Missing | FND-06, SCI-01…12, TEC-01…04 |
| Career mode (funds, reputation, contracts, facilities, strategies) | the Luma mission guide only | Missing | CAR-01…12, HUB-04, CRW-02…05 |
| Difficulty options | — | Missing | CAR-09 |
| Training (tutorials) and scenarios | a mission guide with hints for the Luma mission | Partial | UX-05, UX-06 |
| Multiple saves, quicksave/quickload, revert | have; no save browser | Partial | FND-05, UX-02 |
| Settings (graphics, audio, controls) | — | Missing | FND-08, FND-09, UX-03 |
| Key rebinding, joystick | fixed keys | Missing | FND-09 |
| Mods: configs and plugins | data is JSON; FBX part replacement in the editor | Partial | MOD-01…04 |

## Space center

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Hub scene with clickable facilities | menu → assembly building directly | Missing | FND-07, HUB-01, HUB-02 |
| Vehicle assembly building | have (set aside, re-root, symmetry, staging, readouts) | Have | ASM-01…11 improve it |
| Spaceplane hangar, runway | — | No (D-15) | — |
| Launch pad | have | Have | FLT-09 (clamps) |
| Tracking station | — | Missing | NAV-06 |
| Mission Control | — | Missing | CAR-06 |
| Astronaut Complex | — | Missing | CRW-02 |
| R&D | — | Missing | TEC-03, SCI-09 |
| Administration (strategies) | — | Missing | CAR-08 |
| Facility upgrades with limits | — | Missing | CAR-03, HUB-04, ART-06 |
| Destructible facilities | — | Missing | HUB-05 (D-07) |

## Worlds

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| A star as a body; planets on heliocentric orbits | the sun is a fixed light direction; Tellus is the root | Missing | FND-01, FND-03, SYS-02 |
| 7 planets and 9 moons | Tellus and Luma | Partial (2 of 16) | SYS-01, SYS-03…15 |
| Atmospheres (5 bodies) and oceans (3) | Tellus's atmosphere and ocean | Partial | SYS-04, SYS-07, SYS-10, SYS-11, ART-08 |
| Biomes on every body | — | Missing | FND-02, SCI-02 |
| Asteroids | — | Missing | RES-05, RES-06 |
| Easter eggs | — | Beyond (as landmarks with science) | SYS-19 |
| Several star systems | — | Beyond (your direction) | FND-15, MSY-01…04 |

## Parts

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| A broad rocket catalogue in 4 sizes | 36 parts, 3 sizes | Partial | PRT-01, PRT-12a/b/c (168 at 1.0) |
| Liquid engines by role, solid boosters, RCS | have (4 engines, 2 solid, 1 RCS) | Partial | PRT-12a/b |
| Nuclear, ion and aerospike engines | — | Missing | FLT-18, PRT-12b/c |
| Solar panels, RTG, fuel cells | batteries only | Missing | FLT-02, PRT-12a/b/c |
| Antennas and transmitting | — | Missing | SCI-05 |
| Science instruments | — | Missing | SCI-03, PRT-07 |
| Docking ports in several sizes | one size | Partial | FLT-10 |
| Landing legs, parachutes, heat shields | have | Have | more sizes in PRT-12a/b |
| Rover wheels | — | Missing | FLT-08 |
| Ladders, lights | — | Missing | CRW-06, PRT-02 |
| Procedural fairings, service bays | — | Missing | PRT-04, FLT-05 |
| Struts and fuel lines | — | Missing | FLT-16, PRT-05 |
| Launch clamps | implicit pad hold | Partial | FLT-09 |
| Grabbing unit (asteroids) | — | Missing | FLT-11 |
| Drills, converters, scanners, ore tanks | — | Missing | RES-01…04 |
| Radiators | — | Missing | FLT-04 |
| Crew cabins, cupola, science lab, external seat | — | Missing | PRT-06, SCI-08, CRW-09 |
| Control surfaces, airbrakes | fins only | Partial | FLT-17 (rocket aero only, D-15) |
| Wings, jet engines, intakes, landing gear | — | No (D-15) | — |
| Tweakables (fuel levels, thrust limiter, chute settings) | — | Missing | FLT-15, ASM-02 |
| Action groups | a few fixed keys (legs, SAS, RCS) | Partial | FLT-07, ASM-03 |

## Flight and physics

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Rigid-body physics with part joints | single rigid body with analytic joint loads (no wobble) | Different by design (D-14) | FLT-16 |
| New aerodynamics (drag, lift, occlusion) | per-part drag with occlusion, Mach, fin and body lift | Have | FLT-05, FLT-17 |
| Heat: skin, conduction, radiators | heating and ablation | Partial | FLT-04 |
| Structural failures, crashes, splashdowns | have (verified) | Have | — |
| Fuel flow rules, crossfeed, resource transfer | stack flow, decouplers block | Partial | FLT-06 |
| Electricity generation and use | a charge resource only | Missing | FLT-02, FLT-03 |
| Time warp (rails and physics), no drift | have | Have | FLT-13 |
| Floating origin, large-distance precision | have (for Tellus/Luma) | Partial | FND-01, FLT-13 |
| Part count performance | fine at 51 parts | Partial | FND-13, FLT-14 |

## Navigation

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Map view with orbits, Ap/Pe, encounters | have (two bodies) | Partial | NAV-01, NAV-02 |
| Manoeuvre nodes | have (KSP-style interaction) | Have | NAV-04 |
| Patched conics across SOIs | generic code, two bodies | Partial | NAV-02 |
| Targets, closest approach | have | Have | NAV-03 (AN/DN, helpers) |
| Navball with all markers, SAS modes | have | Have | FLT-03 (by pilot or probe level) |
| Waypoints | — | Missing | NAV-08 |
| Vessel types and renaming | — | Missing | NAV-07 |
| Transfer planning help | — | Beyond | NAV-05 |
| Alarm clock | — | Beyond (KSP added it later) | NAV-10 |

## Crew and EVA

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Named crew roster | have (names, status, flights) | Partial | CRW-01 |
| Traits and experience levels | — | Missing | CRW-03, CRW-04 |
| Hiring, tourists, rescues | — | Missing | CRW-02, CRW-05 |
| Walk, jump, jetpack, flag, board | have (verified) | Have | — |
| EVA reports, surface samples | — | Missing | CRW-08 |
| Ladders and climbing | — | Missing | CRW-06 |
| Swimming, ragdoll | — | Missing | CRW-07 |
| Crew portraits and interior views | — | Missing | CRW-10, CRW-11 (D-04) |
| Male and female characters with animations | one procedural model | Partial | ART-05 |

## Science and research

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Experiments × situations × biomes, diminishing returns | — | Missing | SCI-01…03 |
| Transmit vs recover | — | Missing | SCI-05, SCI-06 |
| Science lab | — | Missing | SCI-08 |
| Research tree | — | Missing | TEC-01…04 |

## Presentation

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Sound effects and music | none | Missing | AUD-01…08 |
| Hand-made art for parts, buildings, characters | procedural (pipeline for hand-made parts exists) | Partial | ART-01…13 |
| Effects (plumes, reentry, explosions) | have | Have | ART-10 |
| In-game encyclopedia | tooltips and F1 control guide | Partial | UX-07 |
| Notifications and log | flight log lines | Partial | UX-08 |

## Release

| KSP 1.0 | TAP now | Status | Tasks |
|---|---|---|---|
| Windows, macOS, Linux | Windows build | Partial | FND-11, REL-07 (Linux); macOS later (D-19) |
| Store presence, patches, community | public repository | Missing | REL-01…09 |
