# 08 · Science (SCI)

**Have:** nothing. The slice has no science system. `GameMode.Science` and the `ScienceCollected` event exist (FND-06,
FND-14) and nothing uses them yet.

**Design:** [SCIENCE_MODE.md](../design/SCIENCE_MODE.md), agreed 2026-10-16. It replaces the KSP 1.0 loop this file
used to plan. In short:
- instruments record findings by themselves, and each subject pays science points **once**, the first time;
- data pays only when it gets home: a **reading** over a live link of the comms network, or carried home; a
  **sample** only carried, or analysed in a lab first;
- a **comms network** (CommNet) with ground stations, antennas, relays and line of sight; a probe without signal has
  partial control;
- **labs** analyse samples into readings and reset single-use instruments, nothing else;
- science points buy tree nodes ([09-tech-tree.md](09-tech-tree.md)), where prototype groups are certified by test
  missions.

Rules: no bare timers (everything that takes time depends on an input), and first findings only.

## Situations

| Situation | Where | Biome matters for |
|---|---|---|
| Landed | on the ground | all surface experiments |
| Splashed | in a liquid | all surface experiments |
| Flying low | inside the atmosphere, below the body's low/high boundary (Tellus 18 km) | crew observation, pressure, air |
| Flying high | inside the atmosphere above that boundary | — |
| Space low | below the body's low-space boundary (Tellus 250 km, Luma 60 km…) | field notes, gravimeter |
| Space high | above that boundary, inside the SOI | — |

Each body sets its boundaries and a multiplier per situation (SCI-01), and SYS-16 tests them.

## Instruments and experiments (base values are starting points for SCI-11)

| Experiment | Source | Works in | Kind | Base | Notes | EA |
|---|---|---|---|---|---|---|
| Crew Observation | any crewed pod | all | reading | 6 | biome when landed, splashed or flying low | EA1 |
| EVA Field Notes | rocketeer on EVA | all but flying | reading | 8 | biome when landed or in space low | EA1 |
| Surface Sample | rocketeer on the ground ("Take sample") | landed, splashed | sample | 30 | the one manual act | EA1 |
| Thermal Probe | sci_thermo | landed, splashed, flying low, space low | reading | 9 | | EA1 |
| Pressure Gauge | sci_baro | landed, splashed, flying low/high (atmosphere only) | reading | 11 | | EA1 |
| Bio-Culture Canister | sci_bio | all | sample | 12 | single use until a lab resets it | EA1 |
| Materials Exposure Rack | sci_matrack | all | sample | 24 | single use until a lab resets it | EA1 |
| Seismic Array | sci_seismic | landed | reading | 22 | | EA2 |
| Air Sampler | sci_airsampler | flying low/high, landed in atmosphere | sample | 18 | | EA2 |
| Gravimeter | sci_gravimeter | landed, space low/high | reading | 20 | biome when landed or in space low | EA3 |
| Recovery | recovering a vessel at Tellus | flight record | — | 5–60 | furthest situation reached (flew by, orbited, landed on X), once per record | EA1 |
| Landmark Survey | at a landmark | landed | reading | 40 | SYS-19 | EA5 |

A reading can be transmitted at full value over a live link. A sample cannot be transmitted; it pays when recovered,
or a lab turns it into a reading. Later instruments (magnetometer, radiation detector, spectrometer, telescope) are
readings.

## Comms network

| Piece | What it is |
|---|---|
| Ground stations | three on Tellus, turning with it; their power comes from the tree (Ground Network I–IV, TEC-01) |
| Antennas | power (range), bandwidth, electricity per Mbit, and a type: *direct* or *relay* |
| Range | `√(powerA × powerB)`; strength falls from 100% to 0% at maximum range; bandwidth scales with strength |
| Line of sight | a body, as a sphere, blocks a link; atmospheres do not |
| Path home | a chain of links to a ground station; the weakest link sets the strength |
| Delay | none in the home system; light-lag comes with interstellar flight (X1) |

| Antenna | Type | Reach with the matching ground level (SCI-14 sets the numbers) | EA |
|---|---|---|---|
| antenna_whip | direct | Tellus orbit and Luma | EA1 |
| antenna_dish | direct | the inner planets | EA1 |
| antenna_relay | relay | the inner planets; passes other vessels' signals on | EA1 |
| antenna_highgain | direct | the whole system | EA2 |
| antenna_relay_deep | relay | the whole system; passes other vessels' signals on | EA2 |

---

### SCI-01 · Science data model
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** FND-05, FND-02

- **Subjects**: experiment × body × situation × biome where relevant. Each has a value (`base × body/situation
  multiplier`) and is either found or not. First findings only: no diminishing returns, no caps, no partial credit.
- **Findings** are a reading (size in Mbit) or a sample (mass), with their subject. A finding pays when it reaches
  home (transmitted, recovered or analysed and transmitted) and only if its subject has not been found before.
- A finding that would pay nothing (the subject is already found) is still recorded for the archive, marked as known.
- The science archive and the balance are saved with the game (FND-05).

**Done when**
- [ ] Unit tests cover value, first-finding-only, readings and samples.
- [ ] The archive round-trips through save and load.

### SCI-02 · Situations and biome detection
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-02, SCI-01

- Situation classification per body from the boundaries in `system.json`.
- Biome lookup at the vessel's position (FND-02). A HUD line shows "Luma · Highlands · Landed" and a notice when the
  biome changes.

**Done when**
- [ ] Driving across a biome border changes the readout.
- [ ] Every body's boundaries are covered by a test.

### SCI-03 · Instrument module and automatic recording
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** SCI-02, FND-14

- One configurable module for every instrument:
  - the situations where it works, and whether biome matters;
  - whether it needs an atmosphere;
  - electricity use;
  - single use (until a lab resets it) or not;
  - the kind (reading or sample) and its size or mass.
- **Automatic recording**: an auto toggle per instrument, on by default. When the vessel enters a matching situation
  and biome, the instrument records the finding and a one-line toast appears ("Pressure profile · Rubra · Rift ·
  Flying low"). No dialog. A finding of a subject already known shows a quieter toast.
- When an instrument can't record (no electricity, wrong situation, already used), the part's menu says why.
- Each recording raises `ScienceCollected` (FND-14). Readings go to the transmission queue (SCI-05) or stay stored;
  samples stay in the part.
- The part's menu lists what it holds: Transmit, Discard, and Reset for single-use instruments when a lab is in reach.

**Done when**
- [ ] Every EA1 instrument records where it should, by itself, and explains why when it can't.
- [ ] Turning auto off stops the recording; a manual "Record now" works.

### SCI-04 · Result texts
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** SCI-03, FND-10

- Short, flavourful observations per experiment × body, with extra variants for notable biomes and situations. That
  is roughly 12 experiments × 22 bodies × 2–3 texts ≈ 600 lines. Generic fallbacks cover the rest.
- They appear in the archive (SCI-09) and when you open a toast. Claude drafts in TAP's voice and you review. Batches
  follow the bodies in each milestone. Everything goes in the string tables.

**Done when**
- [ ] Every experiment on every EA1 body has at least 2 texts.
- [ ] Nothing is repeated verbatim across bodies.
- [ ] Your review is done.

### SCI-05 · Transmission queue and background processes
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** SCI-01, SCI-13, FLT-02

- **Background-process service**: one place where everything that runs on rails is integrated over a warp step
  (transmissions here; labs in SCI-08; drills and converters in RES-03/04 reuse it). Each process says what it
  consumes and produces; the service stops it when an input runs out.
- **Transmission queue** per vessel: readings wait in order, with progress, pause and cancel.
  - Rate = the link's bandwidth (SCI-13); electricity per Mbit from the antenna.
  - Pauses when the link is lost or the battery is empty, resumes by itself. Nothing is lost.
  - A completed reading pays 100% (SCI-01).
- Warp finishes a transmission at the same rate, only while the link exists.

**Done when**
- [ ] A reading of 40 Mbit at 2 Mbit/s takes 20 s, in physics and over warp.
- [ ] A moon passing in front pauses the queue and it resumes afterwards (unit test with a scripted occlusion).
- [ ] A dead battery pauses the queue.

### SCI-06 · Recovery science and the recovery dialog
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-01

- On recovery at Tellus:
  - credit every stored reading and sample whose subject is new;
  - the flight record (furthest situation reached), once per record;
  - in career, the refund and experience lines too (CAR-02, CRW-04).
- A clear summary dialog: new findings with their value, already-known ones greyed.

**Done when**
- [ ] Recovering the `lunar` capsule credits its samples, notes and the "returned from the surface of Luma" record.
- [ ] Recovering the same findings twice pays nothing the second time.

### SCI-07 · Storing and carrying data and samples
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-03, CRW-08

- Readings stay in the instrument or a crewed pod until transmitted. Samples stay in the part that holds them and add
  their mass.
- A rocketeer moves readings and samples between parts on EVA and stores them in a pod or a lab.
- Transfer between docked vessels. One container holds one copy of a subject.

**Done when**
- [ ] A sample taken on an EVA on Pruina rides home in the pod and is credited on recovery.
- [ ] A reading moved to a docked vessel with a link is transmitted from there.

### SCI-08 · Labs
**Milestone** EA2 · **Claude** 3 d · **You** 1 d · **Needs** SCI-05, SCI-07, PRT-06

- Two lab parts: **lab_nib** (S1, 1 seat, one sample at a time, EA2) and **lab_quill** (S2, 2 seats, two samples at
  once, EA4).
- **Analyse samples**: a sample put into the lab becomes a reading of the same subject at full value, ready to
  transmit. Each takes lab hours, fewer with more crew aboard, and electricity. It runs on the background-process
  service (SCI-05) and only on samples the lab holds, so warp cannot create science.
- **Reset single-use instruments** on its own vessel or a docked one (bio canister, materials rack).
- Nothing else: no points over time, no value bonus.

**Done when**
- [ ] A Nib lab in Rubra orbit analyses a Rubra surface sample and the reading is transmitted home.
- [ ] A 100-day warp analyses exactly the samples aboard, then stops.
- [ ] A bio canister reset by the lab records again at a new site.

### SCI-09 · Science archive in R&D
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-01, HUB-02

- Browse everything found by body, experiment, situation and biome, with how it came home (transmitted, recovered,
  analysed), its text, and filters such as "not yet found here".

**Done when**
- [ ] The archive lists every found subject.
- [ ] "Not yet found" shows what is left on Luma.

### SCI-10 · Science mode
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** FND-06, TEC-02, SCI-01, SCI-13

- New-game type Science: the research tree and science, with no funds, contracts, facility upgrades or crew wages.
- Start: tier 0 parts, Ground Network I, zero science. The mission guide suggests early goals (reach space, orbit,
  Luma).
- Sandbox uses the same science (the archive fills, nothing is gated), never fails a part, and has a new-game switch
  for CommNet, on by default.

**Done when**
- [ ] A new science game starts, research unlocks parts, and the save round-trips.
- [ ] You play from launch to a Luma landing in science mode.

### SCI-11 · Science balance
**Milestone** EA1 · **Claude** 2 d · **You** 3 d · **Needs** SCI-10, TEC-01

- Tune base values and body multipliers against node costs, so that tiers open at the intended moments:
  - orbit tech after the first space flights;
  - Luma landers after orbit science;
  - interplanetary parts after Luma and Pruina.
- The points available in the home system add up to about 140% of the tree's cost, so you cannot get stuck and cannot
  grind past the tree.
- Put your target hours per tier on a pacing sheet.

**Done when**
- [ ] A scripted "science career" simulation meets the pacing sheet within ±25%.
- [ ] A script proves the 140% pool from the subject list.
- [ ] Your playtest confirms it.

### SCI-12 · Science and comms autotests
**Milestone** EA1 · **Claude** 3 d · **You** — · **Needs** SCI-03, SCI-15, QA-02

- Missions that record findings in each situation near Tellus and on Luma, transmit over a link, lose the link behind
  Luma and get it back through a relay, and recover. They check the credited values against the model, including
  that repeats pay nothing.
- A probe on Luma's far side has partial control without a relay and full control with one.

**Done when**
- [ ] The tests pass nightly.
- [ ] Changing a base value or an antenna's power makes them fail with a clear message.

### SCI-13 · Comms network: links and paths
**Milestone** EA1 · **Claude** 5 d · **You** 0.5 d · **Needs** FND-01, PRT-03

- **Ground stations** as data on the home body (three, spread in longitude), turning with it. Their power comes from
  the Ground Network level (TEC-02).
- **Antenna module**: power, bandwidth, electricity per Mbit, direct or relay. A deployable antenna works only when
  extended (PRT-03).
- **Links**: range `√(powerA × powerB)`, a strength curve from 100% to 0% at range, line of sight against every body
  as a sphere (atmospheres do not block).
- **Graph**: vessels and ground stations; relay antennas forward, direct ones do not. Each vessel's path home is the
  strongest chain; its weakest link sets the strength and bandwidth.
- Recomputed a few times per second in flight and at each warp step on rails, for every vessel, within the
  performance budget (FND-13).

**Done when**
- [ ] Unit tests: the range formula, sphere occlusion, a path through two relays, weakest-link strength.
- [ ] A probe behind Luma has no link; a relay in Luma orbit gives it one.
- [ ] 100 vessels and 20 relays update in under 1 ms per frame.

### SCI-14 · Antenna and relay parts
**Milestone** EA1 → EA2 · **Claude** 1.5 d · **You** 1 d · **Needs** SCI-13, SYS-01, PRT-02

- Module data for the five antennas in the table above, including the two relay parts the science design added
  (antenna_relay in EA1, antenna_relay_deep in EA2).
- The power of each antenna and of each Ground Network level, so that the reach table above holds for the real
  distances of the home system (a script prints it per body at its nearest and farthest).
- Models: antenna_relay uses the dish shape; antenna_relay_deep the high-gain shape (PRT-08) at its own size.

**Done when**
- [ ] The reach script matches the table at every Ground Network level.
- [ ] Each antenna is flown by an autotest.

### SCI-15 · Probe control without signal
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-13, FLT-03

- A probe-controlled vessel with no path home has **partial control**: SAS hold modes and staging work, the throttle
  can only go down, no manual steering. Crewed vessels (and a pilot on an external seat) always have full control.
- A clear "No signal" state on the HUD and in the vessel list; control comes back by itself when the link returns.
- Sandbox with CommNet off: always full control.

**Done when**
- [ ] Losing the link mid-burn lets the throttle go to zero but not up again; SAS keeps holding.
- [ ] A crewed lander behind Luma flies normally.

### SCI-16 · Comms overlay and signal readout
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** SCI-13, NAV-01, NAV-06

- Map view and tracking station: an overlay draws active links coloured by strength, and the path home of the
  focused vessel. Ground stations show on Tellus.
- The vessel panel shows "Connected · 74% · via Luma Relay 2" or "No signal", and the queue progress.
- In the assembly building, the Engineer's Report (ASM-08) shows the craft's antenna range and whether it relays.

**Done when**
- [ ] The overlay matches the graph in a scripted scene with 3 relays.
- [ ] The vessel panel updates when a moon blocks the link.
