# 09 · Research tree and R&D (TEC)

**Have:** nothing. Every part is available, as in sandbox.

**KSP 1.0 had:**
- a technology tree of dozens of nodes in tiers, paid with science;
- parts unlocked by node;
- in career, a price to buy each unlocked part (entry cost);
- R&D facility levels that cap the tiers.

TAP's tree is its own, built around the catalogue in [05-parts.md](05-parts.md) and the difficulty ladder of the
home system.

## Draft tree (TEC-01 finalises it)

Tiers are shown with their cost in science per node. Branches read left to right: **Rockets**, **Control**,
**Power & Comms**, **Structure**, **Survival**, **Science**, **Exploration**.

| Tier (cost) | Nodes and the parts they unlock |
|---|---|
| 0 (start) | **First Steps**: pod_kestrel, tank_s1_short, tank_s1_med, eng_hornet, srb_pogo, dec_s1, chute_main, fin_fs1, nose_s1, clamp · crew observation and EVA field notes |
| 1 (5) | **Rocketry I**: srb_kodiak, tank_s1_long · **Instruments I**: sci_thermo, sci_bio · **Staging**: dec_radial, sep_s1 |
| 2 (15) | **Vacuum Propulsion**: eng_ember, tank_s1_xl · **Stability**: fin_fs2, rw_s1 · **Recovery**: shield_s1, chute_drogue, chute_radial · **Instruments II**: sci_baro, sci_matrack |
| 3 (40) | **Rocketry II**: eng_wasp, srb_spit · **Landing I**: leg_ls2, leg_ls1, pod_mayfly1, ladder_fixed · **Power I**: battery_radial, battery_s1, solar_fixed, light_spot · **Probes I**: probe_finch, probe_sparrow, antenna_whip, battery_s0 · **Miniaturisation**: tank_s0_mini, tank_s0_short, tank_s0_long, adapter_s1_s0, eng_spark, eng_gnat, dec_s0, nose_s0, rw_s0, chute_s0, shield_s0 |
| 4 (90) | **Heavy Rocketry**: tank_s2_short, tank_s2_med, tank_s2_long, adapter_s2_s1, eng_goliath, dec_s2, sep_s2, nose_s2 · **Heavy Recovery**: shield_s2, chute_s2 · **Attitude Control**: rcs_quad, rcs_linear, mono_s1, mono_radial, mono_s0, rw_radial, rw_s2 · **Docking I**: dock_s1 · **Probes II**: probe_wren, antenna_dish, solar_fold · **Fairings**: fairing_s1, fairing_s2 · **Side Engines**: eng_cricket |
| 5 (160) | **Heavy Landing**: leg_ls5, pod_mayfly2 · **Command Modules**: pod_albatross, probe_starling, seat_perch · **Power II**: solar_wing, battery_s2, light_flood, ladder_tele · **Structures I**: truss_short, truss_long, strut_cube, plate_s1, plate_s2, adapter_struct_s2_s1 · **Instruments III**: sci_seismic, sci_airsampler · **Upper Stages**: eng_albacore, tank_s2_xl, eng_centipede, tank_radial |
| 6 (300) | **Very Heavy Rocketry**: tank_s3_short, tank_s3_med, tank_s3_long, adapter_s3_s2, eng_titan, dec_s3, sep_s3, nose_s3, fairing_s3, shield_s3, rw_s3, plate_s3, srb_behemoth, srb_bison · **Aerodynamic Control**: canard_ctl, fin_ctl, fin_tail_s2, airbrake · **Rovers**: wheel_s, wheel_m, wheel_l, rover_chassis · **Nuclear Propulsion**: eng_lantern, hydro_s1, hydro_s2 · **Heat Management I**: radiator_panel, radiator_fold · **Docking II**: dock_s0, dock_s2, sep_s0, rcs_duo, dec_radial_heavy · **Clusters**: coupler_bi_s2, coupler_tri_s2, bay_s1, bay_s2, chute_drogue_s2, leg_ls9 |
| 7 (550) | **Ion Propulsion**: eng_glimmer, xenon_s0, xenon_radial · **Advanced Probes**: probe_owl, antenna_highgain, mono_s2 · **Advanced Science**: sci_gravimeter · **Advanced Power**: solar_tracker, fuelcell · **Heavy Lifters**: eng_leviathan, coupler_quad_s3 · **Space Habitation**: cabin_bunk, cabin_hitch, cupola_lookout, dock_s1_inline · **Advanced Structures**: strut, fuel_line |
| 8 (1000) | **Research Outposts**: lab_quill · **Resource Utilisation**: drill_s, drill_l, isru_s, isru_l, scanner_orbital, scanner_narrow, sensor_surface, ore_s1, ore_s2 · **Asteroid Handling**: grabber · **Heat Management II**: radiator_large, fuelcell_array · **Radioisotope Power**: rtg |
| 9 (1600) | **Experimental Propulsion**: eng_wedge, eng_vernier · **Experimental Aerodynamics**: gridfin, shield_inflatable |

48 nodes. Every catalogue part appears exactly once (checked by script against the catalogue). Tier 9 is optional "late" content
for long games.

---

### TEC-01 · Research tree design
**Milestone** EA1 · **Claude** 2 d · **You** 2 d · **Needs** PRT-01, SYS-01

- Finalise nodes, prerequisites (any or all of the parents), costs, and positions in the tree view.
- Every part in exactly one node. Parts from later milestones sit in nodes that stay hidden until their milestone
  ships.
- Check the ladder: which body each tier can reach with the parts it unlocks (reference craft from ASM-11).
- Names and short descriptions per node.

**Done when**
- [ ] `tech.json` passes validation (FND-04).
- [ ] A test proves every part is placed once.
- [ ] The ladder table ("tier N reaches body X") is in this file.

### TEC-02 · Tech state and unlocking
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** TEC-01, FND-06

- Research a node when its prerequisites are met and you have the science.
- Parts become available. In career they must also be purchased (entry cost, funds) before use.
- Queries for the assembly building (ASM-01), craft loading (locked parts named) and contracts (part tests only
  offer researched parts).

**Done when**
- [ ] A new science game has only tier 0.
- [ ] Researching Rocketry I adds its parts.
- [ ] Career purchase deducts funds once.

### TEC-03 · R&D facility screen
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** TEC-02, HUB-02

- A pannable, zoomable tree with state colours: researched, available, locked. Hover shows prerequisites.
- The node panel shows its parts with the info panel (PRT-09), a Research button, and career purchase buttons.
- The science balance is shown. In career, tiers above the facility level are capped (CAR-03).
- The science archive tab (SCI-09) lives here too.

**Done when**
- [ ] Every node can be researched from the screen.
- [ ] Capped tiers explain why.
- [ ] The screen stays at 60 fps with every node visible.

### TEC-04 · Tree balance
**Milestone** EA1 · **Claude** 1 d · **You** 2 d · **Needs** TEC-01, SCI-11

- Tune costs with the science pacing (SCI-11) so that each tier opens roughly at the intended point of a campaign,
  and no tier leaves a dead end (you can always earn the next science).

**Done when**
- [ ] The pacing simulation and a full science playthrough to Rubra (you) match the targets within ±25%.
