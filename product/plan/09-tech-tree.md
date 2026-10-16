# 09 · Research tree and R&D (TEC)

**Have:** nothing. Every part is available, as in sandbox.

**Design:** [SCIENCE_MODE.md](../design/SCIENCE_MODE.md), agreed 2026-10-16:
- science points buy nodes when their prerequisites are met;
- some nodes are **prototype groups** (★ below): researching one gives its **lead part** as an unreliable prototype,
  and the rest of the node stays locked;
- each group has its own **test mission**, written for that group only; the prototype itself must fly it, on any
  flight, without a contract; passing it certifies the group and unlocks every part in the node;
- until certified, a prototype can fail in a way you can recover from (TEC-07); normal parts never fail;
- the Comms branch holds the ground network's levels (Ground Network I–IV) next to the antennas.

KSP 1.0 had a tree of dozens of nodes in tiers paid with science, parts unlocked by node, a purchase price per part in
career, and R&D levels that capped the tiers. TAP keeps the tiers and adds the prototype groups.

## Draft tree (TEC-01 finalises it)

Tiers are shown with their cost in science per node. Branches read left to right: **Rockets**, **Control**,
**Power & Comms**, **Structure**, **Survival**, **Science**, **Exploration**. ★ marks a prototype group, with its
lead part first.

| Tier (cost) | Nodes and the parts they unlock |
|---|---|
| 0 (start) | **First Steps**: pod_kestrel, tank_s1_short, tank_s1_med, eng_hornet, srb_pogo, dec_s1, chute_main, fin_fs1, nose_s1, clamp · crew observation and EVA field notes · Ground Network I |
| 1 (5) | **Rocketry I**: srb_kodiak, tank_s1_long · **Instruments I**: sci_thermo, sci_bio · **Staging**: dec_radial, sep_s1 |
| 2 (15) | ★ **Vacuum Propulsion**: eng_ember, tank_s1_xl · **Stability**: fin_fs2, rw_s1 · ★ **Recovery**: shield_s1, chute_drogue, chute_radial · **Instruments II**: sci_baro, sci_matrack |
| 3 (40) | **Rocketry II**: eng_wasp, srb_spit · ★ **Landing I**: leg_ls2, leg_ls1, pod_mayfly1, ladder_fixed · **Power I**: battery_radial, battery_s1, solar_fixed, light_spot · **Probes I**: probe_finch, probe_sparrow, antenna_whip, battery_s0 · **Miniaturisation**: tank_s0_mini, tank_s0_short, tank_s0_long, adapter_s1_s0, eng_spark, eng_gnat, dec_s0, nose_s0, rw_s0, chute_s0, shield_s0 |
| 4 (90) | ★ **Heavy Rocketry**: eng_goliath, tank_s2_short, tank_s2_med, tank_s2_long, adapter_s2_s1, dec_s2, sep_s2, nose_s2 · ★ **Heavy Recovery**: shield_s2, chute_s2 · **Attitude Control**: rcs_quad, rcs_linear, mono_s1, mono_radial, mono_s0, rw_radial, rw_s2 · ★ **Docking I**: dock_s1 · **Probes II**: probe_wren, antenna_dish, solar_fold · ★ **Comms I**: antenna_relay · Ground Network II · **Fairings**: fairing_s1, fairing_s2 · **Side Engines**: eng_cricket |
| 5 (160) | **Heavy Landing**: leg_ls5, pod_mayfly2 · **Command Modules**: pod_albatross, probe_starling, seat_perch · **Power II**: solar_wing, battery_s2, light_flood, ladder_tele · **Structures I**: truss_short, truss_long, strut_cube, plate_s1, plate_s2, adapter_struct_s2_s1 · **Instruments III**: sci_seismic, sci_airsampler · ★ **Field Lab**: lab_nib · **Upper Stages**: eng_albacore, tank_s2_xl, eng_centipede, tank_radial |
| 6 (300) | **Very Heavy Rocketry**: tank_s3_short, tank_s3_med, tank_s3_long, adapter_s3_s2, eng_titan, dec_s3, sep_s3, nose_s3, fairing_s3, shield_s3, rw_s3, plate_s3, srb_behemoth, srb_bison · **Aerodynamic Control**: canard_ctl, fin_ctl, fin_tail_s2, airbrake · ★ **Rovers**: wheel_m, wheel_s, wheel_l, rover_chassis · ★ **Nuclear Propulsion**: eng_lantern, hydro_s1, hydro_s2 · **Heat Management I**: radiator_panel, radiator_fold · **Docking II**: dock_s0, dock_s2, sep_s0, rcs_duo, dec_radial_heavy · **Clusters**: coupler_bi_s2, coupler_tri_s2, bay_s1, bay_s2, chute_drogue_s2, leg_ls9 · **Comms II**: Ground Network III |
| 7 (550) | ★ **Ion Propulsion**: eng_glimmer, xenon_s0, xenon_radial · **Advanced Probes**: probe_owl, antenna_highgain, mono_s2 · ★ **Deep Space Comms**: antenna_relay_deep · Ground Network IV · **Advanced Science**: sci_gravimeter · **Advanced Power**: solar_tracker, fuelcell · **Heavy Lifters**: eng_leviathan, coupler_quad_s3 · **Space Habitation**: cabin_bunk, cabin_hitch, cupola_lookout, dock_s1_inline · **Advanced Structures**: strut, fuel_line |
| 8 (1000) | **Research Outposts**: lab_quill · ★ **Resource Utilisation**: isru_s, isru_l, drill_s, drill_l, scanner_orbital, scanner_narrow, sensor_surface, ore_s1, ore_s2 · **Asteroid Handling**: grabber · **Heat Management II**: radiator_large, fuelcell_array · **Radioisotope Power**: rtg |
| 9 (1600) | ★ **Experimental Propulsion**: eng_wedge, eng_vernier · ★ **Experimental Aerodynamics**: shield_inflatable, gridfin |

51 nodes, 15 of them prototype groups (the list below, chosen with you). Every catalogue part appears exactly once (checked by script against
`parts.json`). Ground Network levels are nodes or parts of nodes with no part of their own. Tier 9 is optional "late"
content for long games.

## Prototype groups and their test missions (chosen with you, 2026-10-16)

The prototype (the lead part) must do the test itself. Tests can be bundled on one flight. TEC-06 turns each line into
objectives and texts.

| Group | Lead part | Test mission | Tier |
|---|---|---|---|
| Vacuum Propulsion | eng_ember | Ignite the Ember twice above Tellus's atmosphere | 2 |
| Recovery | shield_s1 | Re-enter Tellus faster than 1,800 m/s from a suborbital arc, and land | 2 |
| Landing I | leg_ls2 | Land on Tellus under 3 m/s on engines, with no parachutes | 3 |
| Heavy Rocketry | eng_goliath | Put a 15 t payload into Tellus orbit | 4 |
| Heavy Recovery | shield_s2 | Coming back from Luma, aerobrake into Tellus orbit with no engine burn, then land | 4 |
| Docking I | dock_s1 | Dock two vessels in Tellus orbit | 4 |
| Comms I | antenna_relay | Relay a probe's signal home while it is behind Luma | 4 |
| Field Lab | lab_nib | Analyse a sample while landed on another body | 5 |
| Rovers | wheel_m | Drive 2 km on another body | 6 |
| Nuclear Propulsion | eng_lantern | Leave Tellus orbit on the Lantern and reach another planet's sphere of influence | 6 |
| Ion Propulsion | eng_glimmer | Change velocity by 1,000 m/s in total beyond Rubra's orbit (ion thrusts only in physics warp, FLT-18) | 7 |
| Deep Space Comms | antenna_relay_deep | Relay a signal home from Magnus | 7 |
| Resource Utilisation | isru_s | Refuel a lander on another body with fuel made there, and fly it to orbit | 8 |
| Experimental Propulsion | eng_wedge | Fire the Wedge while on an escape trajectory from Astraea | 9 |
| Experimental Aerodynamics | shield_inflatable | Aerocapture at Rubra | 9 |

---

### TEC-01 · Research tree design
**Milestone** EA1 · **Claude** 2 d · **You** 2 d · **Needs** PRT-01, SYS-01

- Finalise nodes, prerequisites (any or all of the parents), costs, and positions in the tree view.
- Mark the prototype groups and their lead parts; the Ground Network levels sit in the Comms branch.
- Every part in exactly one node. Parts from later milestones sit in nodes that stay hidden until their milestone
  ships.
- Check the ladder: which body each tier can reach with the parts it unlocks (reference craft from ASM-11).
- Names and short descriptions per node.

**Done when**
- [ ] `tech.json` passes validation (FND-04).
- [ ] A test proves every part is placed once and every prototype group's lead part is in its own node.
- [ ] The ladder table ("tier N reaches body X") is in this file.

### TEC-02 · Tech state and unlocking
**Milestone** EA1 · **Claude** 2.5 d · **You** 0.5 d · **Needs** TEC-01, FND-06

- Research a node when its prerequisites are met and you have the science.
- A normal node makes its parts available. A prototype group makes its lead part available as a **prototype** (a
  badge in the assembly building, failures from TEC-07) and keeps the rest locked until the group is **certified**.
- Certification turns the prototype into a normal part and unlocks the rest of the node.
- The Ground Network level comes from the researched Comms nodes (SCI-13 reads it).
- Queries for the assembly building (ASM-01), craft loading (locked parts named) and contracts (only researched
  parts). In career, parts must also be purchased (entry cost, funds) before use.

**Done when**
- [ ] A new science game has only tier 0.
- [ ] Researching Vacuum Propulsion gives eng_ember as a prototype and tank_s1_xl stays locked; certifying unlocks it.
- [ ] Tech state round-trips through save and load.

### TEC-03 · R&D facility screen
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** TEC-02, HUB-02

- A pannable, zoomable tree with state colours: researched, prototype, certified, available, locked. Hover shows
  prerequisites.
- The node panel shows its parts with the info panel (PRT-09) and a Research button. For a prototype group: the test
  mission, each objective's progress, and which parts the certification unlocks.
- The science balance and the Ground Network level are shown. In career, tiers above the facility level are capped
  (CAR-03), and parts have purchase buttons.
- The science archive tab (SCI-09) lives here too.

**Done when**
- [ ] Every node can be researched from the screen.
- [ ] A prototype group shows its objectives and their progress.
- [ ] The screen stays at 60 fps with every node visible.

### TEC-04 · Tree balance
**Milestone** EA1 · **Claude** 1 d · **You** 2 d · **Needs** TEC-01, SCI-11

- Tune costs with the science pacing (SCI-11) so that each tier opens roughly at the intended point of a campaign,
  and no tier leaves a dead end (you can always earn the next science).
- The test missions fit the pacing: a group's test can be flown with the parts available when it is researched.
- The science bonus for a passed test (tentative, about 10% of the node's cost): keep it, tune it or set it to 0.

**Done when**
- [ ] The pacing simulation and a full science playthrough to Rubra (you) match the targets within ±25%.

### TEC-05 · Prototype test missions
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** TEC-02, FND-14

- A test mission is an objective set (FND-14's `ObjectiveTracker`) bound to a prototype group. It is active from the
  moment the group is researched, on every flight, with no contract to accept.
- New conditions, each usable by later contracts too (CAR-04):
  - **by the prototype**: the condition counts only for the group's lead part on that vessel;
  - engine burn time (continuous or in total), ignitions and restarts;
  - re-entry and aerocapture (survived, speed at entry, from which orbit or body, no engine burn in between);
  - landed or splashed speed, distance driven;
  - docked; payload mass in orbit;
  - a relay carrying another vessel's signal (SCI-13); a sample analysed (SCI-08); a resource produced (RES-04);
  - trajectory: escaping a body or the star, reaching another body's sphere of influence;
  - total velocity change by the prototype in a region (beyond a body's orbit).
- Totals that grow over days (ion thrust) add up on rails as well.
- Certification pays a small science bonus, set per group in data (tentative, about 10% of the node's cost; TEC-04).
- A toast for each objective met and for the certification; progress is saved. Several tests can finish on one
  flight.

**Done when**
- [ ] Unit tests for each new condition.
- [ ] The Ember test (two ignitions above the atmosphere) passes in a scripted flight and certifies Vacuum Propulsion.
- [ ] One flight finishes two tests (an engine on the way out, a shield on the way back).

### TEC-06 · Test missions for every prototype group
**Milestone** EA1 → EA4 · **Claude** 2 d · **You** 3 d · **Needs** TEC-05, TEC-01, ASM-11

- One test per group, hand-written: objectives, a title, a one-line brief and a certification text, in the string
  tables, from the table above (groups and tests chosen with you on 2026-10-16).
- Each milestone ships the tests of its groups.
- A validator proves each test can be done with the parts available when its group can be researched (reference
  craft from ASM-11), so no group can lock the tree.

**Done when**
- [ ] Every prototype group of the milestone has a test, and the validator passes.
- [ ] No two tests share the same objectives.
- [ ] Your review of the texts is done.

### TEC-07 · Prototype failures
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** TEC-02, FLT-18

- Until certified, a prototype has a chance to fail each time it is used (ignition, deploy, entry), about 5%, set per
  group in data. Science and career only; sandbox never fails.
- Failures can be recovered from:

  | Part kind | Failure | Recovery |
  |---|---|---|
  | Engine | does not ignite, or shuts down early in a burn | ignite again |
  | Heat shield | ablator burns faster | larger margin, or a shallower entry |
  | Landing leg | does not lock on deploy | retract and deploy again |
  | Docking port | latch does not catch | back off and dock again |
  | Wheel | motor stalls | restart |
  | Drill / converter | jams | restart |
  | Antenna / lab | does not deploy, or pauses | deploy or start again |

- A toast names the failure and the way out. A failure never counts against the test.
- The random draws come from the save's seed, so a test can force a failure.
- Certified and normal parts never fail. This is the only failure system in the game.

**Done when**
- [ ] Over 10,000 simulated ignitions the failure rate is 5% ± 0.5%.
- [ ] An autotest with the rate forced to 100% shows the failure, then (rate 0) the retry completes the test.
- [ ] A certified part never fails.
