# 08 · Science (SCI)

**Have:** nothing. The slice has no science system.

**KSP 1.0 had:**
- science subjects: experiment × body × situation × biome;
- about ten instruments plus crew and EVA reports and surface samples;
- diminishing returns;
- data you can transmit (for part of its value) or recover (for all of it);
- a research lab that processes data over time;
- the R&D archive;
- the **Science** game mode.

TAP builds the same structure with its own instruments, values and texts.

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

| Experiment | Source | Works in | Transmit | Base | Notes | EA |
|---|---|---|---|---|---|---|
| Crew Observation | any crewed pod | all | 100% | 6 | biome when landed, splashed or flying low | EA1 |
| EVA Field Notes | rocketeer on EVA | all but flying | 100% | 8 | biome when landed or in space low | EA1 |
| Surface Sample | rocketeer on the ground | landed, splashed | 25% | 30 | career: research facility level 2 | EA1 |
| Thermal Probe | sci_thermo | landed, splashed, flying low, space low | 50% | 9 | | EA1 |
| Pressure Gauge | sci_baro | landed, splashed, flying low/high (atmosphere only) | 50% | 11 | | EA1 |
| Bio-Culture Canister | sci_bio | all | 30% | 12 | single use until a scientist or the lab resets it | EA1 |
| Materials Exposure Rack | sci_matrack | all | 30% | 24 | single use until reset | EA1 |
| Seismic Array | sci_seismic | landed | 45% | 22 | | EA2 |
| Air Sampler | sci_airsampler | flying low/high, landed in atmosphere | 40% | 18 | | EA2 |
| Gravimeter | sci_gravimeter | landed, space low/high | 40% | 20 | biome when landed or in space low | EA3 |
| Recovery | recovering a vessel at Tellus | flight record | — | 5–60 | by the furthest situation reached (flew by, orbited, landed on X) | EA1 |
| Landmark Survey | at a landmark | landed | 100% | 40 | SYS-19 | EA5 |

---

### SCI-01 · Science data model
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** FND-05, FND-02

- **Subjects**: experiment × body × situation × biome where relevant. Each has:
  - value = base × body/situation multiplier;
  - a cap;
  - collected so far.
- **Diminishing returns**: each repeat earns a falling share until the cap. A transmitted result counts at its
  transmit percentage.
- Data results are payloads with a size and a subject.
- The science archive is saved with the game (FND-05).

**Done when**
- [ ] Unit tests cover value, repeats, caps and transmit shares.
- [ ] The archive round-trips through save and load.

### SCI-02 · Situations and biome detection
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-02, SCI-01

- Situation classification per body from the boundaries in `system.json`.
- Biome lookup at the vessel's position (FND-02). A HUD line shows "Luma · Highlands · Landed" and a notice when the
  biome changes.

**Done when**
- [ ] Driving across a biome border changes the readout.
- [ ] Every body's boundaries are covered by a test.

### SCI-03 · Experiment module and results dialog
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** SCI-02

- One configurable module for every instrument:
  - the situations where it works, and whether biome matters;
  - whether it needs an atmosphere;
  - electricity use and run time;
  - whether it can be rerun, or is single use until reset;
  - data size and transmit percentage.
- **Results dialog**:
  - result text (SCI-04);
  - value if recovered vs if transmitted;
  - Keep, Transmit, Discard, Reset.
  - A queue when several experiments finish at once.
- Right-click on a part or action groups to run it. The rocketeer runs EVA experiments.

**Done when**
- [ ] Every EA1 experiment runs where it should and explains why when it can't.
- [ ] The dialog values match the model (unit test).

### SCI-04 · Result texts
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** SCI-03, FND-10

- Short, flavourful observations per experiment × body, with extra variants for notable biomes and situations. That
  is roughly 12 experiments × 17 bodies × 2–3 texts ≈ 500 lines. Generic fallbacks cover the rest.
- Claude drafts in TAP's voice and you review. Batches follow the bodies in each milestone. Everything goes in the
  string tables.

**Done when**
- [ ] Every experiment on every EA1 body has at least 2 texts.
- [ ] Nothing is repeated verbatim across bodies.
- [ ] Your review is done.

### SCI-05 · Transmission and antennas
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** FLT-02, PRT-02

- An antenna module with packet size, packet interval and electricity per packet. A transmission queue per vessel,
  with progress, pause and cancel.
- Transmitted results credit their transmit share.
- No relay network or signal delay (as in KSP 1.0; a comms network is post-1.0, see
  [21-out-of-scope.md](21-out-of-scope.md)). Every antenna reaches home from anywhere, faster with bigger dishes.

**Done when**
- [ ] Transmitting drains electricity at the configured rate.
- [ ] A dead battery pauses the queue.
- [ ] Credit arrives on completion.

### SCI-06 · Recovery science and the recovery dialog
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-01

- On recovery at Tellus:
  - credit every stored result in full;
  - the recovery experiment for the flight record;
  - in career, the refund and experience lines too (CAR-02, CRW-04).
- A clear summary dialog.

**Done when**
- [ ] Recovering the `lunar` capsule credits its samples, notes and the "returned from the surface of Luma" record.

### SCI-07 · Storing and carrying data
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-03, CRW-08

- Crewed pods and the lab store results. A rocketeer can collect data from instruments and store it in a pod.
- Transfer between docked vessels. No duplicates of the same subject in one container.

**Done when**
- [ ] Data collected on an EVA on Pruina rides home in the pod and is credited on recovery.

### SCI-08 · Research lab
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** SCI-07, PRT-06

- The lab part:
  - resets single-use instruments;
  - takes in data once per subject;
  - converts it into research points over time: faster with scientists, with a bonus in orbit of or landed on other
    bodies, and uses electricity.
- Capacity limits; points are transmitted or recovered.
- It keeps working on rails, simulated over the warp.

**Done when**
- [ ] A lab station in Luma orbit produces the designed science per day with two scientists.
- [ ] A 100-day warp credits it correctly.

### SCI-09 · Science archive in R&D
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-01, HUB-02

- Browse everything collected by body, experiment, situation and biome, with percentages of each cap and filters
  such as "not yet done here".

**Done when**
- [ ] The archive lists every collected subject.
- [ ] "Not yet done" shows what is left on Luma.

### SCI-10 · Science mode
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** FND-06, TEC-02, SCI-01

- New-game type Science: the research tree and science, with no funds, contracts or facility upgrades.
- Start with the tier-0 parts. The mission guide suggests early goals (reach space, orbit, Luma).

**Done when**
- [ ] A new science game starts, research unlocks parts, and the save round-trips.
- [ ] You play from launch to a Luma landing in science mode.

### SCI-11 · Science balance
**Milestone** EA1 · **Claude** 2 d · **You** 3 d · **Needs** SCI-10, TEC-01

- Tune base values, body multipliers and caps against research costs, so that tiers open at the intended moments:
  - orbit tech after the first space flights;
  - Luma landers after orbit science;
  - interplanetary parts after Luma and Pruina.
- Put your target hours per tier on a pacing sheet.

**Done when**
- [ ] A scripted "science career" simulation meets the pacing sheet within ±25%.
- [ ] Your playtest confirms it.

### SCI-12 · Science autotests
**Milestone** EA1 · **Claude** 2 d · **You** — · **Needs** SCI-03, QA-02

- Missions that run experiments in each situation near Tellus and on Luma, transmit and recover. They check the
  credited values against the model, including diminishing returns.

**Done when**
- [ ] The tests pass nightly.
- [ ] Changing a base value makes them fail with a clear message.
