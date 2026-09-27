# 10 · Career (CAR)

**Have:** the slice's mission guide for the Luma mission (`MissionTracker`, milestones in the save). There are no
funds, reputation, contracts, facility levels or strategies.

**KSP 1.0's career had:**
- funds, reputation and science;
- contracts from Mission Control (generated, with advances, rewards, penalties and deadlines);
- world-first milestones;
- facility upgrades with gameplay limits;
- strategies at the Administration building;
- vessel recovery refunds;
- difficulty options.

TAP keeps that loop and uses its own contract types, names, strategies and numbers. Trade routes (defined later,
[20-multi-system-and-trade.md](20-multi-system-and-trade.md)) will build on this economy.

## Facilities and levels (draft, CAR-03 finalises)

| Facility | Level 1 | Level 2 | Level 3 |
|---|---|---|---|
| Assembly Building | 30 parts | 255 parts | unlimited |
| Launch Pad | 20 t, 25 m tall | 150 t, 45 m | unlimited |
| Tracking Station | current orbit only, no untracked objects | 2 patched conics, track 2 asteroids | all conics, track all |
| Mission Control | 2 active contracts, no manoeuvre nodes | 7 contracts, manoeuvre nodes | unlimited |
| Astronaut Complex | 5 crew, EVA only on Tellus, no flags | 12 crew, EVA anywhere, flags | unlimited |
| R&D | tiers 0–3 | tiers 0–6, surface samples | all tiers |
| Administration | 1 strategy, 25% commitment | 3 strategies, 50% | 5 strategies, 75% |

Sandbox and science mode have every facility at level 3.

## Contract types

| Type | Example | Parameters | EA |
|---|---|---|---|
| **Milestone expedition** | "Orbit Pruina", "Return from Rubra's surface" | body, situation, return | EA3 |
| **Part test** | "Test the Wasp LV-300 in flight over Tellus, 12–24 km, 400–900 m/s" | part, situation, altitude, speed band | EA3 |
| **Satellite placement** | "Place a probe with an antenna and power in orbit: Ap 2,800 km ±5%, i 45° ±2°" | orbit window, required parts, uncrewed | EA3 |
| **Field survey** | "Take pressure readings at 3 sites on Luma" | waypoints (NAV-08), experiment, situation | EA3 |
| **Rescue** | "Bring home Dale Voss, stranded in Luma orbit" | a stranded rocketeer (CRW-05) | EA3 |
| **Recovery** | "Recover the probe core from Pruina's surface" | object, location | EA3 |
| **Tourism** | "Take 3 tourists to Luma orbit and back" | tourists, destinations | EA3 |
| **Science collection** | "Return seismic data from Vigil" | experiment, body, situation | EA3 |
| **Station** | "Build a station in Luma orbit: 6 crew, docking port, power, lab optional" | capacity, parts, orbit | EA4 |
| **Outpost** | "Establish an outpost on Rubra: 4 crew, power, near 12.4°N 55.1°E" | capacity, parts, location | EA4 |
| **Resources** | "Mine 800 units of ore on Pruina" / "Deliver 2 t of propellant to the Luma station" | resource, amount, place | EA4 |
| **Asteroid** | "Capture asteroid TAP-2811 and bring it into Tellus orbit" | object, final orbit | EA4 |
| **Landmark** | "Photograph the arch in Rubra's rift" | landmark (SYS-19) | EA5 |

---

### CAR-01 · Currencies and the ledger
**Milestone** EA3 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-06

- Funds, reputation and science with a transaction ledger (amount, reason, date), saved.
- Difficulty multipliers (CAR-09). A top bar in the hub, assembly building, R&D and flight.
- Reputation has a soft cap (gains shrink near the top) and decays slowly without achievements (tunable).

**Done when**
- [ ] Every change goes through the ledger with a reason.
- [ ] The multipliers apply.
- [ ] A test covers the soft cap.

### CAR-02 · Costs, launch and recovery
**Milestone** EA3 · **Claude** 3 d · **You** 1 d · **Needs** CAR-01, PRT-01

- Costs:
  - every part has a cost and every resource a unit price;
  - a launch costs the craft's value, and you can't launch without the funds.
- **Recovery**:
  - a refund of the vessel's value × a distance factor from the launch complex (100% on the pad, falling with
    distance around Tellus);
  - resources left count at their price;
  - crew are recovered (experience, CRW-04) and science credited (SCI-06).
- The recovery dialog lists everything.

**Done when**
- [ ] Launching deducts the exact craft cost.
- [ ] Recovering on the pad refunds 100%, and 500 km away the designed share.
- [ ] Unit tests.

### CAR-03 · Facility levels, limits and upgrades
**Milestone** EA3 · **Claude** 3 d · **You** 1 d · **Needs** CAR-01, HUB-02

- The level table above as data. Upgrade costs rise per level. Upgrades happen from the hub (HUB-04 shows them).
- Each limit is enforced where it applies:

  | Limit | Where it is enforced |
  |---|---|
  | Part count | Engineer's Report (ASM-08) |
  | Launch mass and height | launch |
  | Conics depth | map (NAV-02) |
  | Manoeuvre nodes | map |
  | Contracts | Mission Control |
  | EVA and flags | EVA |
  | Research tiers | R&D (TEC-03) |
  | Strategies | Administration |

**Done when**
- [ ] Each limit has a test.
- [ ] Every blocked action tells you which facility level lifts it.

### CAR-04 · Contract system core
**Milestone** EA3 · **Claude** 5 d · **You** 1 d · **Needs** FND-14, CAR-01

- **Contract templates** in data. A contract has:
  - a title and description template;
  - a parameter tree (all, any or ordered sub-goals), with each parameter able to check:
    situation or body, orbit parameters within tolerances, parts present, crew count or specific crew, time held,
    waypoint reached, science collected, vessel delivered, resource amount;
  - an advance, reward and penalty in funds, reputation and science;
  - a deadline and an expiry for the offer.
- **Generation**:
  - weighted by progression (what you have achieved and researched) and by prestige tiers (trivial, significant,
    exceptional);
  - never offers the impossible (for example a part test for an unresearched part).
- **Lifecycle**: offered → accepted → active → completed, failed or expired. It is event-driven (FND-14),
  evaluated also on rails (orbit parameters) and saved.

**Done when**
- [ ] Unit tests cover each parameter type and the lifecycle.
- [ ] A generated set of 1,000 offers contains no impossible or duplicate contracts.

### CAR-05 · Contract types
**Milestone** EA3 · **Claude** 6 d · **You** 2 d · **Needs** CAR-04, NAV-08, CRW-05

- The EA3 rows of the table above as templates:
  - generation rules;
  - text variants, written by Claude and reviewed by you;
  - reward curves by body and difficulty.
- The station, outpost, resources and asteroid types arrive with EA4 (RES, FLT-10) and landmarks with EA5.

**Done when**
- [ ] Each type is completed at least once by an autotest (CAR-12).
- [ ] Descriptions read naturally, with no template artefacts.

### CAR-06 · Mission Control screen
**Milestone** EA3 · **Claude** 3 d · **You** 1 d · **Needs** CAR-04, HUB-02

- Tabs for offered, active and archive. Accept or decline. Details show each parameter with a live tick, plus
  rewards, deadline and advance.
- An **objectives panel** in flight lists the active contracts' parameters that apply to the current vessel (the same
  panel as FND-14).

**Done when**
- [ ] Every action works.
- [ ] Parameters tick live in flight.
- [ ] The slot limit follows the facility level.

### CAR-07 · World firsts and progression
**Milestone** EA3 · **Claude** 2 d · **You** 1 d · **Needs** FND-14, CAR-01

- Firsts:
  - first launch, space, orbit, EVA, docking;
  - per body: flyby, orbit, landing, flag, return;
  - records for altitude, speed and distance, crew duration.
- Each pays funds, reputation and science once, with a notice. The log becomes the agency history in the hub, and
  feeds the achievements (REL-03).
- In sandbox and science it still logs, with no pay.

**Done when**
- [ ] The `lunar` mission records all its firsts.
- [ ] The payouts match the table.

### CAR-08 · Strategies
**Milestone** EA3 · **Claude** 3 d · **You** 1 d · **Needs** CAR-01, CAR-03

TAP's own set, each with a commitment slider (share of income converted) and a setup cost:

| Strategy | Effect |
|---|---|
| **Public Outreach** | a share of funds income becomes reputation |
| **Grants Office** | a share of science income becomes funds |
| **Research Partnerships** | a share of funds income becomes science |
| **Heritage Fund** | reputation gains also pay funds; reputation losses cost more |
| **Recovery Crews** | recovery refunds up, launch costs slightly up |
| **Frugal Launches** | part prices down, contract rewards down |
| **Bold Frontiers** | exploration contracts pay more and penalise more |
| **Test Pilot Program** | part-test contracts pay more, and pilots gain experience faster |

- The Administration screen, and limits by facility level.

**Done when**
- [ ] Each strategy's effect has a test.
- [ ] Cancelling one follows the rules.

### CAR-09 · Difficulty presets and custom options
**Milestone** EA3 · **Claude** 2 d · **You** 0.5 d · **Needs** CAR-01

- Presets Easy, Normal, Hard and Custom. The options:
  - funds, science and reputation multipliers;
  - contract reward and penalty multipliers;
  - reentry heating;
  - crash tolerance;
  - allow revert and quickload;
  - lost crew respawn;
  - indestructible facilities;
  - starting funds;
  - SAS always available (sandbox-style).
- Shown in the new-game dialog (UX-02) and changeable later where it is fair.

**Done when**
- [ ] Every option has an effect covered by a test.
- [ ] Presets load their values.

### CAR-10 · Career economy balance
**Milestone** EA3 · **Claude** 2 d · **You** 4 d · **Needs** CAR-05, CAR-08, PRT-11

- An economy model (a spreadsheet or simulation) of income vs costs per phase, with a target pace:
  - first orbit in session 1;
  - Luma landing within about 5 hours;
  - Rubra within about 15;
  - the Magnus moons within about 40.
- Contract rewards, part costs, facility upgrade costs and recovery factors are tuned to it. Several full
  playthroughs by you, and later by Early Access players (telemetry D-10).

**Done when**
- [ ] Your playthrough reaches each target within ±30%.
- [ ] No bankruptcy spiral: you can always take a cheap contract.

### CAR-11 · Career start
**Milestone** EA3 · **Claude** 1 d · **You** 0.5 d · **Needs** CAR-05, UX-02

- Starting funds, tier-0 parts, all facilities at level 1.
- The first two contracts are always "Launch a rocket" and "Reach space".
- A guided welcome in the hub (UX-05).

**Done when**
- [ ] A new career starts correctly on every difficulty.

### CAR-12 · Career autotests
**Milestone** EA3 · **Claude** 3 d · **You** 0.5 d · **Needs** CAR-05, QA-02

- Scripted careers that accept and complete one contract of each type (part test, satellite placement, survey,
  rescue, tourism, science, milestones). They check payouts, funds balance and the state after save and load.

**Done when**
- [ ] The tests pass nightly.
