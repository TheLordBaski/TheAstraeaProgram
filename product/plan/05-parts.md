# 05 · Parts (PRT)

**Have:** 36 parts, plus the rocketeer and the flag. There are 3 command parts, 7 tanks, 4 engines, 2 solid
boosters, 4 coupling parts, 2 landing legs, 5 recovery parts, 7 utility parts and 2 aero parts. All are data in
`parts.json` with procedural models, and any of them can be replaced by a hand-made FBX
([Docs/PART_MODELS.md](../../Docs/PART_MODELS.md)).

**Goal for 1.0:** about 168 parts, a rocket catalogue as broad as KSP 1.0's, with 4 size classes:

| Class | Diameter |
|---|---|
| S0 | 0.625 m |
| S1 | 1.25 m |
| S2 | 2.5 m |
| S3 | 3.75 m |

There are no aircraft parts (no wings, jets, intakes or landing gear), but it does include the aero parts rockets
need: nose cones, fairings, fins, control fins, airbrakes and heat shields.

Names below are **working names** in the slice's style: birds for pods and probes, insects and animals for engines,
plain codes for structure. The final names come with PRT-01.

## Catalogue

`EA` is the milestone that adds the part. Existing parts are marked *have*.

**Command and crew**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| pod_kestrel | Kestrel-1 Command Pod | S1 | 1 seat, heat-shield ready | have |
| pod_albatross | Albatross-3 Command Pod | S2 | 3 seats | have |
| probe_wren | Wren Probe Core | S1 | probe | have |
| pod_mayfly1 | Mayfly-1 Lander Can | S1 | 1 seat, light, no shield | EA1 |
| probe_finch | Finch Micro Probe | S0 | stability only, tiny battery | EA1 |
| probe_sparrow | Sparrow Radial Probe | radial | stability only, very light | EA1 |
| pod_mayfly2 | Mayfly-2 Lander Can | S2 | 2 seats | EA2 |
| probe_starling | Starling Advanced Probe | S1 | all SAS modes | EA2 |
| probe_owl | Owl Heavy Probe | S2 | all SAS, strong wheel | EA2 |
| seat_perch | Perch External Seat | radial | a rocketeer flies from it | EA2 |
| cabin_bunk | Bunk Crew Cabin | S1 | 2 seats, no control | EA4 |
| cabin_hitch | Hitch Crew Module | S2 | 4 seats, no control | EA4 |
| cupola_lookout | Lookout Cupola | S2 | 1 seat, control, windows | EA4 |
| lab_quill | Quill Research Lab | S2 | 2 seats, science lab (SCI-08) | EA4 |

**Propellant and tanks** (the propellant model is decision D-01)

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| tank_s0_mini | Pip-50 Mini Tank | S0 | small tank | have |
| tank_s1_short / med / long | LT-100 / LT-200 / LT-400 | S1 | tanks | have |
| mono_s1, mono_radial | MP-150, MP-R Monoprop Tanks | S1 / radial | monopropellant | have |
| tank_s2_short / long | HT-800 / HT-3200 | S2 | tanks | have |
| adapter_s2_s1 | TA-21 Taper Adapter Tank | S2→S1 | adapter tank | have |
| tank_s0_short, tank_s0_long | Pip-25, Pip-100 | S0 | tanks | EA1 |
| tank_s1_xl | LT-800 | S1 | long tank | EA1 |
| tank_s2_med | HT-1600 | S2 | tank | EA1 |
| adapter_s1_s0 | TA-10 Taper Adapter Tank | S1→S0 | adapter tank | EA1 |
| mono_s0 | MP-25 Monoprop Tank | S0 | monopropellant | EA1 |
| tank_s2_xl | HT-6400 | S2 | long tank | EA2 |
| tank_s3_short / med / long | XT-3600 / XT-7200 / XT-14400 | S3 | tanks | EA2 |
| adapter_s3_s2 | TA-32 Taper Adapter Tank | S3→S2 | adapter tank | EA2 |
| coupler_bi_s2 | Twin Coupler | S2→2×S1 | clusters | EA2 |
| coupler_tri_s2 | Triple Coupler | S2→3×S1 | clusters | EA2 |
| tank_radial | Saddle Radial Tank | radial | side tank | EA2 |
| mono_s2 | MP-600 Monoprop Toroid | S2 | monopropellant | EA2 |
| xenon_s0, xenon_radial | XG-40 / XG-20 Xenon Tank | S0 / radial | ion propellant | EA2 |
| hydro_s1, hydro_s2 | HY-600 / HY-2400 Hydrogen Tank | S1 / S2 | nuclear propellant | EA2 |
| coupler_quad_s3 | Quad Coupler | S3→4×S1 | clusters | EA3 |

**Engines and boosters**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| eng_spark | Spark MX-18 | S0 | small engine | have |
| eng_hornet | Hornet LV-215 | S1 | lifter | have |
| eng_ember | Ember VX-60 | S1 | vacuum | have |
| eng_goliath | Goliath LV-650 | S2 | heavy lifter | have |
| srb_pogo, srb_kodiak | Pogo SR-230, Kodiak SR-300 | S1 | solid boosters | have |
| eng_gnat | Gnat Lander Engine | S0 | light, restartable, high TWR | EA1 |
| eng_cricket | Cricket Radial Engine | radial | side-mounted | EA1 |
| eng_wasp | Wasp LV-300 | S1 | high-thrust lifter | EA1 |
| srb_spit | Spit Separation Motor | radial | tiny solid, pushes stages apart | EA1 |
| eng_albacore | Albacore VX-400 | S2 | vacuum upper stage | EA2 |
| eng_centipede | Centipede Cluster | S2 | multi-nozzle lifter/lander | EA2 |
| eng_titan | Titan LV-2000 | S3 | heavy lifter | EA2 |
| eng_lantern | Lantern NTR | S1 | nuclear thermal, Hydrogen | EA2 |
| eng_glimmer | Glimmer Ion Drive | S0 | Xenon + electricity | EA2 |
| srb_bison | Bison Long Booster | S1 | long solid | EA2 |
| srb_behemoth | Behemoth Heavy Booster | S2 | heavy solid | EA2 |
| eng_leviathan | Leviathan VX-1500 | S3 | heavy vacuum | EA3 |
| eng_wedge | Wedge Aerospike | S1 | altitude-compensating | EA3 |
| eng_vernier | Vernier Thruster | radial | Propellant attitude thruster | EA3 |

**Control**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| rw_s1, rw_s2 | Gyro-S / Gyro-L | S1 / S2 | reaction wheels | have |
| rcs_quad | Quad-4 RCS Block | radial | RCS | have |
| rw_s0 | Gyro-XS | S0 | reaction wheel | EA1 |
| rw_radial | Gyro-R | radial | small reaction wheel | EA1 |
| rcs_linear | Linear RCS Port | radial | single-nozzle RCS | EA1 |
| rw_s3 | Gyro-XL | S3 | reaction wheel | EA2 |
| rcs_duo | Duo RCS Block | radial | small RCS | EA2 |

**Structure and coupling**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| dec_s1, dec_s2 | SD-1 Stack / SD-2 Heavy Stack Decoupler | S1 / S2 | decouplers | have |
| dec_radial | RD-1 Radial Decoupler | radial | decoupler | have |
| dock_s1 | Clasp-1 Docking Port | S1 | docking | have |
| nose_s1 | Aero Nose Cone | S1 | nose cone | have |
| dec_s0 | SD-0 Stack Decoupler | S0 | decoupler | EA1 |
| sep_s1, sep_s2 | SS-1 / SS-2 Separator | S1 / S2 | both halves separate | EA1 |
| nose_s0, nose_s2 | NC-0 / NC-2 Nose Cone | S0 / S2 | nose cones | EA1 |
| fairing_s1, fairing_s2 | PF-1 / PF-2 Fairing Base | S1 / S2 | procedural fairings (PRT-04) | EA1 |
| clamp | Anchor Launch Clamp | radial | holds the rocket on the pad (FLT-09) | EA1 |
| dec_s3, sep_s0, sep_s3 | SD-3, SS-0, SS-3 | S3 / S0 / S3 | decouplers and separators | EA2 |
| dec_radial_heavy | RD-2 Heavy Radial Decoupler | radial | big boosters | EA2 |
| dock_s0, dock_s2 | Clasp-0 / Clasp-2 Docking Port | S0 / S2 | docking sizes (FLT-10) | EA2 |
| truss_short, truss_long, strut_cube | TR-1, TR-3, Cube Node | — | station and lander structure | EA2 |
| plate_s1, plate_s2, plate_s3 | Structural Plate | S1–S3 | mounting | EA2 |
| adapter_struct_s2_s1 | Hollow Adapter | S2→S1 | light, no tank | EA2 |
| nose_s3 | NC-3 Nose Cone | S3 | nose cone | EA2 |
| fairing_s3 | PF-3 Fairing Base | S3 | procedural fairing | EA2 |
| bay_s1, bay_s2 | SB-1 / SB-2 Service Bay | S1 / S2 | enclosed bay with doors | EA2 |
| dock_s1_inline | Clasp-1S Shielded Docking Port | S1 | inline, closes | EA4 |
| strut, fuel_line | Strut Connector, Fuel Line | — | two-point parts (FLT-16) | EA4 |

**Power, heat, communications, lights and ladders**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| battery_radial, battery_s1 | Cell-100 Pack, Cell-400 Bank | radial / S1 | batteries | have |
| battery_s0 | Cell-50 | S0 | battery | EA1 |
| solar_fixed | SP-F Fixed Panel | radial | small solar, always on | EA1 |
| solar_fold | SP-2 Folding Panel | radial | deployable solar | EA1 |
| antenna_whip | Whip Antenna | radial | fast, low power, short | EA1 |
| antenna_dish | DS-1 Dish | radial | medium | EA1 |
| light_spot | Spotlight | radial | light | EA1 |
| ladder_fixed | Fixed Ladder | radial | EVA climbing | EA1 |
| battery_s2 | Cell-1600 Bank | S2 | battery | EA2 |
| solar_wing | SP-8 Solar Wing | radial | large deployable | EA2 |
| radiator_panel, radiator_fold | RP-1 Radiator, RF-3 Folding Radiator | radial | heat (FLT-04) | EA2 |
| antenna_highgain | HG-5 High-Gain Dish | radial | long range, slow deploy | EA2 |
| light_flood | Floodlight | radial | light | EA2 |
| ladder_tele | Telescopic Ladder | radial | extends | EA2 |
| solar_tracker | Sunfield Array | radial | huge, tracking | EA3 |
| rtg | Hearth RTG | radial | constant power, heavy | EA3 |
| fuelcell | FC-1 Fuel Cell | radial | Propellant → electricity | EA3 |
| fuelcell_array | FC-8 Fuel Cell Array | S1 | fuel cell | EA4 |
| radiator_large | RL-10 Active Radiator | radial | active radiator | EA4 |

**Recovery, descent and aero for rockets**

| Id | Working name | Size | Role | EA |
|---|---|---|---|---|
| chute_main, chute_drogue, chute_radial | Canopy-16, Drogue-6, Canopy-R | — | parachutes | have |
| shield_s1, shield_s2 | HS-125 / HS-250 Ablative Heat Shield | S1 / S2 | heat shields | have |
| leg_ls2, leg_ls5 | LS-2 / LS-5 Landing Strut | radial | legs | have |
| fin_fs1 | FS-1 Stabilizer Fin | radial | fin | have |
| chute_s0 | Canopy-6 | S0 | small chute | EA1 |
| chute_s2 | Canopy-32 | S2 | big main chute (Rubra) | EA1 |
| shield_s0 | HS-60 | S0 | heat shield | EA1 |
| leg_ls1 | LS-1 Mini Strut | radial | small leg | EA1 |
| fin_fs2 | FS-2 Swept Fin | radial | bigger fin | EA1 |
| chute_drogue_s2 | Drogue-24 | S2 | big drogue | EA2 |
| shield_s3 | HS-375 | S3 | heat shield | EA2 |
| leg_ls9 | LS-9 Heavy Strut | radial | heavy leg | EA2 |
| fin_tail_s2 | FT-2 Tail Fin | radial | big fin | EA2 |
| canard_ctl, fin_ctl | CF-1 Canard, CF-4 Control Fin | radial | actuated control surfaces (FLT-17) | EA2 |
| airbrake | AB-1 Airbrake | radial | deployable drag | EA2 |
| gridfin | GF-1 Grid Fin | radial | booster recovery (optional) | EA3 |
| shield_inflatable | Bloom Inflatable Shield | S1 stowed | big shield for heavy landers (optional) | EA4 |

**Science** (SCI-03)

| Id | Working name | Role | EA |
|---|---|---|---|
| sci_thermo | Thermal Probe | temperature | EA1 |
| sci_baro | Pressure Gauge | pressure | EA1 |
| sci_bio | Bio-Culture Canister | single-use exposure | EA1 |
| sci_matrack | Materials Exposure Rack | single-use exposure | EA1 |
| sci_seismic | Seismic Array | surface only | EA2 |
| sci_airsampler | Air Sampler | atmosphere composition | EA2 |
| sci_gravimeter | Gravimeter | gravity field | EA3 |

**Rover, resources and asteroids**

| Id | Working name | Role | EA |
|---|---|---|---|
| wheel_s, wheel_m, wheel_l | Rover Wheel S / M / L | rover wheels (FLT-08) | EA2 |
| rover_chassis | Rover Chassis | flat body with mounting points | EA2 |
| drill_s, drill_l | Drill S / L | surface and asteroid drills (RES-03) | EA4 |
| isru_s, isru_l | Converter S / L | ore → propellants (RES-04) | EA4 |
| scanner_orbital | Survey Scanner | maps ore from polar orbit (RES-02) | EA4 |
| scanner_narrow | Narrow-band Scanner | detailed ore below the vessel | EA4 |
| sensor_surface | Surface Sensor | ore at the landing spot | EA4 |
| ore_s1, ore_s2 | Ore Tank S1 / S2 | ore storage | EA4 |
| grabber | Grabbing Unit | grabs debris and asteroids (FLT-11) | EA4 |

**Counts**

| Milestone | New parts | Total |
|---|---|---|
| Have | — | 36 |
| EA1 | +40 | 76 |
| EA2 | +63 | 139 |
| EA3 | +9 | 148 |
| EA4 | +20 | 168 |

---

### PRT-01 · Catalogue spec, naming and stat framework
**Milestone** M1 · **Claude** 2 d · **You** 2 d · **Needs** D-01

- Conventions:
  - ids;
  - size classes and attach-node sizes;
  - fictional manufacturers, each with a personality that shows in the descriptions;
  - the naming pattern.
- **Stat templates per role**, the reference that PRT-11 balances against:
  - engine families: TWR and Isp bands;
  - tanks: dry-to-wet ratio per size;
  - structural strength by size;
  - cost per mass for each category;
  - entry costs for career.
- Turn this file's table into `parts.json` entries with template stats: data only. Models come from PRT-02 and
  PRT-08.
- Final names (D-02).

**Done when**
- [ ] Every catalogue entry exists in data with a milestone flag, hidden until its milestone.
- [ ] A test checks every part against its role template (for example, no lifter below TWR 1.2 at sea level).

### PRT-02 · Procedural models for the EA1 shapes
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** PRT-03

- New `PartModelFactory` types, each with the named rig transforms its module drives, and each exportable and
  replaceable like today's parts:
  - fixed and folding solar panels;
  - whip and dish antennas;
  - spotlight;
  - fixed ladder;
  - launch clamp with its tower;
  - fairing bases;
  - science instruments: box, boom, canister, rack;
  - lander can;
  - small probes;
  - separators;
  - the separation motor.

**Done when**
- [ ] All 40 EA1 parts have a readable procedural model and an icon.
- [ ] *Export All* includes them.

### PRT-03 · Deployables framework
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** —

- A generic module with states:

  | State | |
  |---|---|
  | retracted | |
  | extending | |
  | extended | |
  | retracting | |
  | broken | |

- It drives named transforms (rotation, translation, scale) along curves from data.
- It can break when deployed above a dynamic-pressure limit, or on impact.
- It feeds action groups and the part menu.
- Used by solar panels, antennas, radiators, bays, ladders, airbrakes, drills, scanners and the shielded docking port.
  Hand-made models work if they keep the transform names (PART_MODELS rules).

**Done when**
- [ ] A folding panel deploys and retracts, and breaks if deployed at 20 kPa.
- [ ] The same module drives an antenna and a bay door without code changes.

### PRT-04 · Procedural fairings
**Milestone** EA1 · **Claude** 5 d · **You** 1 d · **Needs** PRT-03

- In the assembly building, click on a fairing base to shape the fairing, or auto-fit around the payload. The profile
  has sections, with the widest and tapered parts.
- A generated mesh with a thickness; mass and cost from area.
- Enclosed parts are shielded from drag and heat (FLT-05).
- Deploying splits it into 2 or 3 shells that fly away as debris, with an optional ejection impulse.

**Done when**
- [ ] A probe inside an S2 fairing reaches orbit, the fairing deploys at 60 km, and the shells fall away without
      touching the payload.

### PRT-05 · Struts and fuel lines
**Milestone** EA4 · **Claude** 3 d · **You** 0.5 d · **Needs** FLT-16

- Two-point parts: place the anchor, then click the target part. A line or strut mesh is stretched between the two.
  Both ends must be on the same craft or detached group, and within a maximum length.
- The part goes away when either end is destroyed or decoupled. It works with symmetry.

**Done when**
- [ ] Struts placed ×4 in symmetry survive save, load and launch.
- [ ] A fuel line feeds its engine as in FLT-16.

### PRT-06 · Crew parts and crew transfer
**Milestone** EA2 · **Claude** 3 d · **You** 0.5 d · **Needs** CRW-01

- Seats and hatches for lander cans, cabins, the cupola and the lab. An external seat works with the rocketeer
  model: sit down and stand up.
- *Transfer crew* from the part menu to another crewed part of the same vessel, including across docking ports.
- EVA from any part with a free hatch.

**Done when**
- [ ] Moving a rocketeer from a station module to a docked lander works.
- [ ] A seat-flown rover is controllable.

### PRT-07 · Science instrument parts
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** SCI-03, PRT-02

- Part definitions and configuration of the experiment module for each instrument: which situations it works in,
  electricity use, whether it can be rerun, its size.

**Done when**
- [ ] Every EA1 instrument can run in its valid situations and refuses the others with a reason.

### PRT-08 · Procedural models for the EA2–EA4 shapes
**Milestone** EA2 · **Claude** 5 d · **You** 1 d · **Needs** PRT-02

- New shapes:
  - radiators;
  - solar wing and tracking array;
  - high-gain dish;
  - floodlight;
  - telescopic ladder;
  - service bays;
  - trusses, plates and cube node;
  - couplers;
  - S3 parts;
  - cluster engine, aerospike, nuclear and ion engines;
  - rover wheels and chassis;
  - control fins, canards, airbrake, grid fin;
  - crew cabins, cupola, lab;
  - drills, converters, scanners;
  - grabbing unit;
  - struts and fuel lines;
  - inflatable shield.

**Done when**
- [ ] Every part in the catalogue has a model (procedural or hand-made).

### PRT-09 · Part information and comparison
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- An info panel for any part:
  - mass (wet and dry) and cost;
  - thrust (sea level and vacuum) and Isp (sea level and vacuum);
  - gimbal and propellants;
  - electricity use and production;
  - capacities;
  - crash tolerance;
  - temperature limits;
  - science;
  - the research node it comes from.
- Compare two parts side by side.
- Used in the assembly building, R&D and the handbook.

**Done when**
- [ ] Hovering a part in the assembly building shows the panel.
- [ ] Every value agrees with the physics (a unit test compares against the modules).

### PRT-10 · Descriptions and manufacturer lore
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** PRT-01

- A description for every part in the voice of its manufacturer: short, useful and a little funny, the charm KSP is
  known for, but all TAP's own. Claude drafts, you edit. Batches follow the milestones.

**Done when**
- [ ] Every visible part has a reviewed description in the string tables (FND-10).

### PRT-11 · Part balance
**Milestone** EA2 · **Claude** 2 d · **You** 3 d · **Needs** PRT-01, SYS-17

- Tune against the templates and the Δv map:
  - every engine has a clear job (lifter, sustainer, vacuum, lander, efficient, powerful);
  - tank ratios make staging decisions interesting;
  - costs are ready for the career economy (CAR-10).
- Re-run the ascent comparisons (the `ascent` autotest) for each reference craft.

**Done when**
- [ ] Every reference craft reaches its target (orbit, Luma, Rubra…) with a 5 to 15% Δv margin.
- [ ] No engine is strictly worse than another in its size and role (checked by a script).

### PRT-12a · Content batch EA1 (40 parts)
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** PRT-01, PRT-02, PRT-07

- Data, stats, models, icons, descriptions and tech placement (TEC-01) for the EA1 rows. Each new part is used in
  at least one stock craft or autotest.

**Done when**
- [ ] All EA1 parts are visible, researchable in science mode and flown by at least one test.

### PRT-12b · Content batch EA2 (63 parts)
**Milestone** EA2 · **Claude** 4 d · **You** 3 d · **Needs** PRT-12a, PRT-08

- Same for the EA2 rows: S3, rovers, nuclear and ion, radiators, control fins, docking sizes, structure.

**Done when**
- [ ] All EA2 parts are visible and each is flown by at least one test.

### PRT-12c · Content batch EA3–EA4 (29 parts)
**Milestone** EA4 · **Claude** 3 d · **You** 2 d · **Needs** PRT-12b

- Same for the EA3 and EA4 rows: aerospike, big vacuum engine, RTG, fuel cells, crew modules, lab, ISRU, grabbing
  unit, struts and fuel lines.

**Done when**
- [ ] The catalogue is complete: 168 parts, all tested.
