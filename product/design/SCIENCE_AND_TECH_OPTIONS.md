# Science, technology and progression: options for career mode

**Status:** superseded for the research half by [SCIENCE_MODE.md](SCIENCE_MODE.md) (agreed 2026-10-16: science points,
prototype groups with test missions, full CommNet, labs). Kept as background. Written 2026-10-10.
When you choose, I turn the choice into tasks and rewrite [08-science](../plan/08-science.md),
[09-tech-tree](../plan/09-tech-tree.md) and the research parts of [10-career](../plan/10-career.md).

**Your direction (2026-10-10):**
- Two modes: **sandbox** and **career**. You are not sure a KSP-style science-only mode is needed.
- Timed R&D is attractive, especially for engine tests, but it can get repetitive, and the game warps up to
  100,000×, which makes any plain timer meaningless.
- The old KSP loop was not bad, but it repeated.
- Science could be automatic, but you must process it and bring it home (or transmit it, which needs infrastructure
  and takes time).
- The whole thing must run from the first launch to **interstellar missions**.

---

## 1. At a glance

| # | Option | In one line | Tech comes from | Time enters as | Rough cost (Claude days) |
|---|---|---|---|---|---|
| 1 | **Classic, de-grinded** | KSP's science points, but automatic, once per subject, with depth upgrades | spending Points | priced R&D days (optional) | ~46 |
| 2 | **Milestone Programmes** | The tree is a campaign: world firsts unlock tech | completing milestones | industrialisation delay (optional) | ~30 |
| 3 | **Data Pipeline** | Instruments make physical data; you carry or transmit it to labs that turn it into Insight | Insight in five fields | bandwidth and lab throughput | ~62 |
| 4 | **Engineering Programmes** | Design, prototype, flight-test, certify: you earn parts by proving them | passing test objectives | design days with payroll and rush | ~55 |
| 5 | **Discovery** | Fog of war: questions and discoveries reveal the tree and the map | answering questions, eureka triggers | observatory sweep rate | ~60 |

The baseline in the current plan (SCI-01…12 and TEC-01…04) is about 38 Claude days and 17 of yours. The costs above
are my rough estimates (±40%) of the whole research system, baseline pieces included. Firm numbers come after you
choose.

Six **layers** (section 5) can be added to any option: doctrines, payroll and rush, partners, specialists,
landmarks, heritage.

**My recommendation** is a mix: Option 2 as the spine (pacing, onboarding, no grind), Option 4 for hardware
(engine tests and so on), a light Option 5 (a journal of open questions) as the repetition killer for science,
a light Option 3 (automatic data, returned or transmitted) as the transport, and layers L1 and L2. Section 7 shows
how to build it in stages so each stage is playable and nothing is thrown away.

---

## 2. The problem

### 2.1 What the plan has today

[08-science.md](../plan/08-science.md), [09-tech-tree.md](../plan/09-tech-tree.md) and
[10-career.md](../plan/10-career.md) rebuild KSP 1.0's loop:
- 10 experiments × body × situation × biome, with diminishing returns and a transmit percentage;
- Science points buy nodes of a 48-node tree (tiers 0–9, costs 0 to 1,600);
- funds, reputation and contracts sit beside it;
- a lab converts data to points over time;
- modes: sandbox, science, career.

It works, and it is what you called "not bad but repetitive". Nothing in it reaches beyond the home system.

### 2.2 Why plain timers fail at 100,000×

At the top warp rate one game day passes in 0.86 real seconds.

| Game time | Real time at 100,000× |
|---|---|
| 30 days | 26 s |
| 1 Tellus year (110 days) | 95 s |
| Transfer to Magnus (286 days) | 4 min |
| 1 year | 5.3 min |
| 10 years | 53 min |

So "research takes 30 days" is a 26-second chore, not a decision. Timers only matter when something else is
attached to the time. There are four kinds of time in a game like this:

| Kind | Example | Does warp break it? |
|---|---|---|
| **Free** | a lab or drill that just runs | No, if its output is limited by an input (data, ore) and not by the clock |
| **Priced** | payroll, upkeep, life support | No: warping costs something |
| **Windowed** | transfer windows, contract deadlines | No: warping costs an opportunity |
| **Bare** | a progress bar that gates an unlock | **Yes**: the player presses `.` and waits |

**Rule 1: every timer must be input-bound, priced or windowed. Bare timers do not exist.**

TAP already has plenty of windowed time. These come from the draft table in
[02-solar-system.md](../plan/02-solar-system.md) (GM of Astraea 1.2·10¹⁸, as in `system.json`); SYS-01 will
regenerate exact values:

| Destination | One-way Hohmann transfer | Window repeats every |
|---|---|---|
| Ignis | ≈ 32 d | ≈ 35 d |
| Caligo | ≈ 43 d | ≈ 160 d |
| Rubra | ≈ 77 d | ≈ 240 d |
| Rupes | ≈ 150 d | ≈ 137 d |
| Magnus | ≈ 286 d | ≈ 121 d |
| Remota | ≈ 394 d | ≈ 117 d |

A 60–120 day programme therefore fits inside one window cycle: "the new engine is ready before the Rubra window"
is a real planning question, and it is the natural size for R&D days.

### 2.3 Why KSP science repeats

1. **Every subject pays**, so the best play is to farm everything everywhere.
2. **Manual clicking**: the same five clicks per biome.
3. **One currency**: all science is equal, so nothing tells you where to go next.
4. **The tree is a shopping list**: what you buy has no link to what you learned.
5. **Diminishing returns** punish repeating, but they do not give a reason to go somewhere *new*.

**Rule 2: unlocks come from doing something new (a place, a capability, a problem solved), never from repeating.**

### 2.4 Design rules (shared by all options)

1. **No bare timers** (above).
2. **First times pay.** Repeats pay little or nothing. New depth (a better instrument) is the only reason to return.
3. **Science is automatic; logistics is the game.** Instruments run on their own when conditions match. The
   decisions are what to carry, where to go, how to get the data home.
4. **Problems before solutions.** What you meet in space should tell you what you need to research.
5. **Never idle.** There is always a flight to do while a project runs: parallel slots, background processes.
6. **Nothing unlocks from a menu alone.** A flight, a test or a discovery is always in the chain.
7. **Everything is data.** Subjects, nodes, objectives, questions live in JSON (FND-04 validates them), so mods and
   new systems add content without code.
8. **The same system reaches the stars.** No special mechanic is bolted on for interstellar; it is the last era of
   the same loop.

---

## 3. Shared foundation (the same for every option)

### 3.1 Modes

| | Sandbox | Career |
|---|---|---|
| Parts | all | unlocked by the chosen option |
| Funds, contracts, facility levels | off | on, with presets |
| Instruments | run and fill the archive and handbook, nothing is gated | run and pay |
| Discovery (option 5) | everything revealed, with a switch | on |
| Time warp | free | free, but priced time applies (payroll) |

**Career presets** replace the separate Science mode:
- **Research only**: no funds, no contracts, no payroll. This is what KSP called Science mode, so the plan's
  SCI-10 stops being a mode and becomes a preset (a few lines in CAR-09). It ships first, in EA1, so the research
  half is playable before funds and contracts (EA3).
- **Standard**: funds, contracts, payroll.
- **Hard**: payroll rates up, lower refunds, heritage failures (layer L6).

### 3.2 Eras

Eight eras give the campaign a spine and give each option the same pacing targets. Hours come from CAR-10's targets
(Luma about 5 h, Rubra about 15 h, Magnus moons about 40 h); the rest is my interpolation. "Tiers" refer to the
draft tree in 09-tech-tree.md.

| Era | Name | Goal | Bodies | Tiers | Target hours | Content ships in |
|---|---|---|---|---|---|---|
| 1 | **Ground Truth** | first launch, space, orbit, recovery | Tellus | 0–1 | 0–2 | EA1 |
| 2 | **Near Space** | orbit tools, Luma flyby, landing, return | Luma | 2–3 | 2–5 | EA1 |
| 3 | **Neighbours** | docking, first station, Pruina, Rubra, Vigil | Pruina, Rubra, Vigil | 4–5 | 5–15 | EA1 |
| 4 | **Heavy Steps** | heavy lift, rovers, nuclear stages, the belt | Caligo, Mica, Rupes | 6 | 15–28 | EA2 |
| 5 | **Outer System** | ion drives, habitats, the giant and its moons | Magnus and moons | 7 | 28–45 | EA2 |
| 6 | **Living off the Land** | ISRU, asteroids, outposts, the hot and cold extremes | Ignis, Remota, asteroids | 8–9 | 45–65 | EA4 |
| 7 | **Precursor** | deep-space array, interstellar drives, precursor probes | the whole system | new, about 15–25 nodes | 65–85 | X1 |
| 8 | **Interstellar** | first arrival at another star | other systems | new | 85+ | X1 |

Eras 7 and 8 do not exist in the tree today. Whatever you choose adds roughly 15–25 nodes for them.

### 3.3 The interstellar ladder

The travel mechanism is a decision for X1 ([20-multi-system-and-trade.md](../plan/20-multi-system-and-trade.md)).
This ladder works with real flight (engines and years of warp) and with a gate or jump drive, and every option
below maps onto it.

| Stage | What happens | What it needs |
|---|---|---|
| 1 · **Look** | A deep-space observatory array resolves nearby stars in four grades: point, orbit, atmosphere, biosignature | a new facility and a huge array part, built and powered |
| 2 · **Reach** | The agency chooses a drive family (see L1) and builds it | the interstellar tech nodes |
| 3 · **Prove** | A proving flight inside the system: a round trip to Remota, a year of closed-loop life support in orbit, autonomous flight with no ground control | tests (option 4), milestones (option 2) |
| 4 · **Send** | A precursor probe goes first; its data arrives years later. Then the crewed or colony ship | a probe, a long mission, light-lag handling |
| 5 · **Arrive** | The new system starts its own first-time subjects, discoveries and questions | X1 content, X2 trade |

**Drive families** (a natural fork, see L1):
- **Fusion torch**: needs helium-3, which TAP can scoop from Magnus's deep atmosphere. It uses the resource
  system and gives the giant a reason to be visited again.
- **Beamed sail**: needs a huge laser array and a lot of power, best near the star (Ignis orbit). A construction
  project, not a ship.
- **Ark**: slow and cheap in propellant, needs closed-loop life support and crew rotation: the survival route.

**Warp for interstellar:** at 100,000× a 10-year trip is 53 minutes of watching. X1 will need either an extra warp
tier (1,000,000× makes it 5 minutes) or compressed distances. Whichever it is, a long cruise needs events on board
(consumables, rotations, faults, onboard research), otherwise it is waiting.

### 3.4 Instruments the plan does not have yet

The plan's 10 experiments stop at gravimeter (Tier 7). Eras 5–8 need new instruments so that science keeps
evolving: magnetometer, radiation detector, spectrometer, particle detector and, for the observatory, a telescope.
Each fits the same module as the existing ones (SCI-03). In options 1, 3 and 5 they are also the *depth upgrades*.

### 3.5 What every option needs from the code

- **One background-process service** for everything that runs on rails: labs, bureau projects, scanners, drills,
  converters, transmissions. It integrates analytically over a warp step, so none of them gets its own warp bug.
  RES-03/04 and SCI-08 already need this; this makes it one piece.
- **Events (FND-14)** as the single source of "something happened": altitude, orbit, situation, EVA, dock, part
  state, subject recorded. Contracts (CAR-04), milestones, test objectives and discoveries all read the same events
  with the same parameter types.
- **A subject registry in data**: experiment × body × situation × biome (× depth). Validated at load.
- **The ledger (CAR-01)** takes an open list of currencies, so Insight, Data or any later good fits.
- **Saves** gain sections for whatever the option adds (FND-05), with migrations.

---

## 4. The options

Each option has the same shape: the loop, the mechanics, a walk from start to interstellar, how it handles warp,
whether it will become repetitive, what it costs, and a sample evening.

---

### Option 1 · Classic, de-grinded

**Pitch.** Keep KSP's structure (instruments, science points, a tree to buy) and remove what made it repeat. The
safest and cheapest option.

**The loop.** Build a vessel with instruments for the target. They run by themselves. Bring the findings home or
transmit them. Spend the points on nodes.

**Mechanics**
- **First findings only.** Every subject (experiment × body × situation × biome) pays its full value *once*. There
  is no farming curve to optimise, only places you have not been.
- **Depth levels.** A subject has three depths: *Survey*, *Detailed*, *Definitive*. Each depth needs a better
  instrument grade, unlocked by tree nodes ("Thermal Probe II" upgrades every thermal probe, with no new part).
  Returning to an old place is worth it only when your instruments improved, so tech and exploration feed each
  other.
- **Auto-run.** Every instrument has an auto toggle (default on). When the vessel enters a matching situation the
  finding is logged and a one-line toast appears: "Pressure profile · Rubra · Rift · Flying low". No dialog.
- **Return vs transmit.** Kept from the plan: recovering pays 100%, transmitting pays the instrument's share
  (25–100%). The best data still has to come home, and samples are 100% recover-only.
- **Finite pool.** The points available in the world add up to about 140% of the tree's cost. You cannot get stuck
  and you cannot grind past the tree. Completionists can chase the remaining 40% for lore and funds.
- **Tech purchase.** Science points plus (in career) funds for entry cost, as in the plan. Optional: a project time
  in game days (2 days at Tier 1 up to 160 at Tier 9), priced by payroll (L2) so it is not bare.

**Start to finish**
| Phase | What you do | What you unlock |
|---|---|---|
| Start (E1–2) | Probe-and-pod flights over Tellus and Luma; first findings are cheap and come fast | Tiers 0–3, first instruments |
| Middle (E3–4) | Pruina, Rubra, Caligo: each body is a pool of fresh subjects; depth-2 instruments reopen Luma | Tiers 4–6 |
| Late (E5–6) | Magnus moons, belt, Ignis, Remota; new instruments (magnetometer, radiation) give new subject families | Tiers 7–9 |
| Interstellar (E7–8) | Observatory produces *Stellar survey* subjects; precursor probes return enormous first-visit subjects | Era 7–8 nodes, costs far above Tier 9 |

**Time warp.** Findings, transmissions and labs run on rails. Project timers (if used) are priced. Nothing is gated by
a bare timer.

**Repetition check.** Medium. First-times and depth levels remove farming. The remaining risk is Era 4–6: dozens of
subjects per body can still feel like a checklist. Keep the number of subjects per body small (about 12–25) and make
each one worth a visible step in the tree.

**Cost.** Baseline plus about 8 Claude days (auto-run, depth levels, finite pool, project slots) and about 3 of
yours (texts for depths). Reuses SCI-01…12 and TEC-01…04 almost unchanged.

**A sample evening.** You launch a probe with a thermal probe, a barometer and a bio canister, all on auto. Over
Rubra's rift it logs the pressure profile by itself while you fly the landing. You land, step out and take a surface
sample. In the lab at home the new Detailed grade of the barometer opens three old subjects on Luma. You research
"Barometer II" and plan a quick Luma flight on the way to the Rubra window.

---

### Option 2 · Milestone Programmes

**Pitch.** The tech tree is a space-programme campaign. Each era is a **Programme** with a director's brief and 5–8
**milestones**. Completing milestones unlocks specific nodes. Science instruments still exist but feed *Insights*
(optional upgrades), not the main path. Nearly free of grind.

**The loop.** Read the Programme board. Pick a milestone you can reach with your current parts. Do it. The nodes it
unlocks open the next ones.

**Mechanics**
- A **milestone** is a world first or a capability, defined from FND-14 events (CAR-07 already plans these):
  reach space, orbit, EVA, dock, flyby / orbit / land / return from a body, 30 days in orbit, build a 6-crew station,
  drive 10 km on a rover, make propellant from ore.
- A **node** lists 1–3 triggers. Example (data):
  ```
  node: vacuum_propulsion
  unlockedBy: [ first_orbit, any( recovered_from_space, eva_in_space ) ]
  industrialise: 6 d   (priced by L2, optional)
  ```
- Each era ends with a **flagship**: the hardest milestone (era 2: crew on Luma and home). It opens the next
  Programme, raises the funding and gives a funds and reputation payout.
- **Side milestones** are optional and pay funds, reputation or Insights (cosmetic upgrades, extra instrument depth).
- Parts become buildable after **industrialisation** (a few days of priced time) or immediately, as you choose.
- Within an era milestones can be done in any order, and most have several ways to be satisfied.

**Start to finish**
| Phase | What you do | What you unlock |
|---|---|---|
| Start (E1–2) | "Launch", "Reach space", "Orbit", "Recover from orbit", "Flyby Luma", "Land on Luma and return" | Tiers 0–3 as milestones fall |
| Middle (E3–4) | "Dock", "Station with 4 crew", "Land on Rubra", "Rover 10 km on Vigil", "Splash on Caligo" | Tiers 4–6 |
| Late (E5–6) | "Aerocapture at Magnus", "Land on Gravis", "Refuel a lander with ISRU", "Capture an asteroid", "Orbit Remota" | Tiers 7–9 |
| Interstellar (E7–8) | "Build the Deep-Space Array", "Round trip to Remota on the new drive", "Year in closed-loop orbit", "Precursor flyby", "Arrive" | Era 7–8 nodes |

**Time warp.** Not an issue: no timers except the optional priced industrialisation.

**Repetition check.** Low repetition, but low freedom too. Every player does the same flagships in the same order.
Replay value is the weakest of all the options. Layers L1 (doctrines) and L5 (landmarks) help a lot.

**Cost.** About 30 Claude days: CAR-07 grows into the trigger system, a programme board replaces the tree screen,
science shrinks to SCI-01…04, 06, 09. About 14 of yours for briefs and milestone texts.

**A sample evening.** The board shows "Reach space", "Orbit" and "Recover from orbit". You build a Kestrel with a
heat shield for the third and take the first two on the same flight. Three nodes appear. The second Programme brief
arrives: "Luma is next."

---

### Option 3 · Data Pipeline

**Pitch.** The science loop is *logistics*. Instruments make **Data**, a physical or streamed good with size and
mass. Data is carried home or transmitted, then **processed** in a lab into **Insight**, which pays for tech. The
infrastructure you build decides how fast you learn.

**The loop.** Observe (automatic) → carry or transmit → process → spend Insight.

**Mechanics**
- **Datasets.** Each finding is a dataset with a subject, a size in megabits (transmission) and a kind:
  - *Streamed data* (spectra, telemetry, images): stored in **Data Vaults** (a part with a capacity and mass);
    can be transmitted or carried.
  - *Samples* (rock, ice, bio cultures): physical, with a mass, carried home or to an orbital lab. They cannot be
    transmitted, but a sample is worth about 4× the data from the same site. An onboard spectrometer reduces a
    sample to data at 10%.
- **Transmission.** Antennas have a bandwidth (Mbit/s) and a power draw. Link strength drops with the square of the
  distance, so a small antenna at Magnus takes days to send what a dish sends in hours. The agency's **Deep Space
  Network** is a facility with levels: L1 covers Tellus and Luma, L2 the inner system out to Rubra, L3 to Magnus,
  L4 the whole system, L5 interstellar. Beyond its level, transmission is not possible (carry instead). No relay
  occlusion before 1.0 (D-22); relays and blackouts come with the comms network after 1.0.
- **Labs** process datasets into Insight, once per dataset. They have a throughput (Insight per lab-day), need
  electricity and benefit from scientists. Locations: the home lab (free, slow, with upgrade levels), orbital labs
  (faster, microgravity bonus), outpost labs (local-sample bonus), and a ship's lab (autonomy: needed when light-lag
  makes home too far).
- **Five Insight fields** (draft; they map onto the plan's 10 experiments and the new instruments of 3.4):

  | Field | Fed by | Feeds nodes such as |
  |---|---|---|
  | Atmosphere and heat | thermal probe, barometer, air sampler | heat shields, aerobraking, parachutes |
  | Geology | surface sample, seismic, gravimeter | landing gear, drills, rovers |
  | Life | bio canister, crew observation | habitation, life support |
  | Materials and radiation | materials rack, radiation detector | shielding, solar and nuclear parts |
  | Space physics | magnetometer, plasma detector, telescope | ion drives, comms, interstellar |

  Nodes need *specific* Insight (for example "Ion Propulsion: 60 Space physics + 30 Materials"), so you need a
  balanced diet of data. A hundred barometer readings do not buy an ion drive.
- **Warp-proof.** Labs run on rails, but they can only process what you brought. Warp cannot manufacture data.

**Start to finish**
| Phase | What you do | What you unlock |
|---|---|---|
| Start (E1–2) | Recover capsules full of data; the home lab processes it overnight (warp) | Tiers 0–3 |
| Middle (E3–4) | Build the first orbital lab; transmit with the L2 DSN; samples return from Pruina and Rubra | Tiers 4–6 |
| Late (E5–6) | Magnus data is huge: L3 DSN, bigger dishes, outpost labs at the moons | Tiers 7–9 |
| Interstellar (E7–8) | Deep-Space Array, L5 DSN; precursor data streams for years; the ship carries its own lab | Era 7–8 nodes |

**Time warp.** All rate-based and input-bound. A transmission of 4,000 Mbit at 2 Mbit/s takes 33 minutes; at
Magnus it takes days. Warp finishes it; it never skips it.

**Repetition check.** Good. The decision is *which data kind you need next* and *how to get it home*, which changes
with every era. The remaining risk is the click-level "go there, wait" for each dataset. Auto-run removes the clicks.

**Cost.** About 62 Claude days (data model, vaults, DSN, labs on rails, fields), about 10 of yours (Insight
balance, texts). It builds on SCI-05, 07, 08.

**A sample evening.** The Rubra orbiter has filled two vaults. A low-gain antenna would take four days to send them,
and Rubra's rift sample is on the lander. You launch the lander, dock at the orbital lab, process the sample
(Geology Insight), and spend it on Landing II while the Magnus probe's weekly packets trickle in.

---

### Option 4 · Engineering Programmes

**Pitch.** You do not *buy* an engine, you *qualify* it. Every important part is a **programme**: concept, prototype,
test, certify. Flights you were going to fly anyway complete the tests. Closest to what you described as exciting.

**The loop.** Start a programme. Wait for the prototype (time priced by payroll, not bare). Fly it past the test
objectives, preferably several on one flight. The part is certified and can go on crewed craft.

**Mechanics**
- A **programme** has four stages:
  1. **Concept**: funds plus *design days* (2 days at Tier 1, 8 at Tier 3, 25 at Tier 5, 70 at Tier 7, 160 at Tier 9).
     Slots equal the R&D level (1, 2, 3), and hired engineers shorten it (L4).
  2. **Prototype**: pay the prototype cost. The part appears in the assembly building with a *Prototype* badge. A
     craft with a prototype launches as a **test flight**: no crew unless you accept the risk on Easy.
  3. **Test objectives** (2–3 per keystone node; none for the first tiers). Validated by telemetry events, on any
     flight, at any time. You do not accept a contract. When a flight meets an objective a toast says so.
  4. **Certification**: all objectives passed. Pay the part's entry cost. It is a normal part from now on.
- **Keystone nodes** (about 30 of 80: engines, heat shields, life support, drives, big structures) have tests. The
  rest (struts, fins, tanks) take design time only. That keeps tests special.
- **Objective archetypes** (eight, with parameters per node):

  | Archetype | Example |
  |---|---|
  | Static fire | "Run the Wasp for 20 s on the pad" (Era 1–2 only) |
  | Flight envelope | "Fly above 40 km faster than 900 m/s" |
  | Vacuum burn | "Burn the Lantern NTR for 90 s above 250 km, with 2 restarts" |
  | Re-entry | "Survive 2.5 MW/m² heat flux" |
  | Landing | "Touch down on Gravis at under 4 m/s" |
  | Environment | "Operate in Magnus's radiation belt for 3 days", "Caligo splashdown", "Ignis orbit at perihelion" |
  | Endurance | "Thrust for 30 days (cumulative) in space", "30 days crewed in orbit" |
  | Integration | "Dock with a S1 port", "Run on a craft with 150 parts" |

- **Anti-repetition** (the part you worried about):
  - Tests move from "fly it" (Era 1–2) to *environments* (Era 4–6), which are places you must go anyway.
  - **Bundling** is the game: one Rubra flight can finish a landing test, a dust test and an endurance test for
    three programmes. Planning a flight that qualifies four things for a Δv budget is a design puzzle.
  - Only keystones get tests; there are about 30 tests in total, hand-written, not generated.
  - Objectives are visible per programme, so you plan, rather than grind.
- **Rush.** Pay ×2 funds for ×0.5 design time. Money and time are exchangeable, so time is a *decision*.

**Start to finish**
| Phase | What you do | What you unlock |
|---|---|---|
| Start (E1–2) | Short programmes (days), a few static fires, "fly above 40 km" | Tiers 0–3 |
| Middle (E3–4) | Programmes of weeks: a restartable vacuum burn at Luma, the first re-entry test at Rubra | Tiers 4–6 |
| Late (E5–6) | Programmes of months: radiation belt endurance, 30-day thrust (ion), ISRU fuel qualification | Tiers 7–9 |
| Interstellar (E7–8) | Mega-programmes (several test flights, infrastructure you build): fusion torch round trip to Remota, hibernation pods, the sail's laser array | Era 7–8 nodes |

**Time warp.** Design days are priced (payroll) and windowed (the next Rubra window). The player always has a flight
to fly during the wait. Payroll never takes funds below 0: the bureau stalls instead.

**Repetition check.** Good if bundling works and tests stay environmental. The weak spots are Tiers 2–4 (if every
node got "burn it over Tellus" it would be a chore) and the hand-writing load (about 30 objectives).

**Cost.** About 55 Claude days. Reuses CAR-04's parameter types for objectives, FND-14 events, and the R&D slots.
New: programme state machine, prototype flag in the assembly building and launch, programme board. About 8 of yours.

**A sample evening.** The Lantern NTR prototype arrived on day 81. You have an Era-4 probe already going to Caligo
for an environment test; you add the Lantern as the transfer stage and plan a 90-second vacuum burn with two restarts
on the way. Two programmes complete in the same flight. The third (a heat shield) needs the Caligo re-entry itself.

---

### Option 5 · Discovery

**Pitch.** The universe, and the tree, are hidden. You do not buy a shield; you *encounter* heat, and the agency
works out what it needs. Observation reveals bodies, observation reveals problems, problems reveal programmes.

**The loop.** Observe (telescope, probes). Pick a **question**. Go and answer it. The answer reveals a node and the
next questions.

**Mechanics**
- **Observatory** (a facility with levels). It sweeps the sky at a rate in game days and reveals bodies in grades:
  L1 *position* (a pin on the map), L2 *size and orbit* (map and transfer planner work), L3 *atmosphere and
  composition*. A visit gives biomes and the detailed science. Early on you know Tellus, Luma and the star. The rest
  is points of light. Warp helps the sweep, but it does not give you the rest: that takes flights.
- **Open Questions journal.** About 60–80 authored questions, each a mini-mission with a text payoff. Examples:
  - E2 *How thin is Luma's old crust?* (seismic + gravimeter at two biomes). Reveals the Seismic nodes.
  - E3 *Is there water under Pruina's flats?* (seismic + thermal + sample at the Great Flats and the Poles). Reveals
    the drilling branch.
  - E3 *What makes Rubra's sunsets blue?* (air sampler at dusk). Needs the right time of day: a timing puzzle on an
    18-hour day.
  - E4 *How deep is Caligo's brine?* (a sounding splashdown). Reveals pressure hulls.
  - E5 *Where does Magnus's radiation come from?* (radiation detector at three heights). Reveals shielding.
  - E6 *Do Remota's streaks come from vents?* (the frozen-geyser landmark, SYS-19). Reveals cryogenic handling.
  - E7 *Are there planets around the nearest star?* (observatory grades 2–4).
- **Eureka triggers.** Nodes are hidden until a trigger fires: first hot entry reveals Heat Shield II, first
  aerocapture reveals Aerobraking, first EVA in low gravity reveals Jetpack upgrades.
- **Silhouettes.** The tree's *shape* is visible with "???" nodes, so you know there is something ahead and how
  many steps away it is.
- **Spending.** Once revealed, a node is paid with funds (and optional design days, L2). Science does not need its
  own currency.
- **Soft-lock proof.** A validator proves every trigger and question is satisfiable with the parts that are
  unlocked when it appears.

**Start to finish**
| Phase | What you do | What you unlock |
|---|---|---|
| Start (E1–2) | Tellus and Luma only; the observatory's first light shows two more pins | Tiers 0–3; first questions |
| Middle (E3–4) | Pruina and Rubra turn from points to worlds; questions steer you | Tiers 4–6 |
| Late (E5–6) | Magnus's grade-3 spectrum shows what is in the atmosphere, and the helium-3 question opens | Tiers 7–9 |
| Interstellar (E7–8) | The observatory array resolves neighbouring stars; each grade is a decision about where to go | Era 7–8 nodes |

**Time warp.** Sweeps are rate-based. Warping through the sky survey reveals new objects slowly (a cap per year), so
waiting has a benefit but never the whole answer. Missions are never replaced.

**Repetition check.** The best on a first playthrough: every question is unique. The weaker side is replay (the
questions are the same on the second run) and the writing load.

**Cost.** About 60 Claude days (observatory, "unknown bodies" in the map and tracking station, journal, triggers,
validator) and about 12–15 of yours for 60–80 question texts and trigger lines. The "unknown" state touches NAV
(map, tracking station) and SYS (body pages), so it is the broadest change.

**A sample evening.** You open the journal: "What makes Rubra's sunsets blue?" You need the air sampler on the
orbiter and a descent at dusk. Rubra's day is 18 hours, so you plan the burn. It works, the reveal text appears, and
two new nodes turn from "???" into "Dust filters" and "Dust-rated bearings".

---

## 5. Layers (add to any option)

| Layer | What it adds | Cost | Notes |
|---|---|---|---|
| **L1 · Doctrines** | Three **forks** where you choose a specialisation. Taking the other path later costs 1.6× and is delayed (cross-licensing), never locked | ~4 d | Fork A (end of Era 3): heavy lift vs nuclear vs electric. Fork B (end of Era 5): solar-electric vs fission. Fork C (Era 7): fusion torch vs beamed sail vs ark. Fork C changes the shape of the missions and the interstellar content, which gives replay value |
| **L2 · Payroll and rush** | Active projects cost funds per day. Warp shows a cost preview and stops when reserves run out. Pay ×2 to halve a project's time. Funds never go below 0: projects stall instead | ~3 d | What turns design days from bare into priced. No bankruptcy spiral (CAR-10) |
| **L3 · Partners** | Part families belong to fictional manufacturers. Their standing (0–5) gives discounts, early prototypes and test contracts that match your programmes | ~8 d | Reuses the contract generator. Adds flavour, also adds KSP's contract repetition, so keep it small |
| **L4 · Specialists** | Crew traits drive R&D: scientists speed labs, engineers shorten design days, pilots speed flight-test certification. The roster is the limit | ~3 d | Makes the Astronaut Complex matter |
| **L5 · Landmarks** | One-off discoveries (SYS-19): the glassy impact fields on Ignis, the arch in Rubra's rift. Each pays a large, unique reward and unlocks something | ~3 d | Also feeds option 5's questions |
| **L6 · Heritage** | Certified parts gain a flight count. Low-heritage parts have a small failure chance on crewed flights | ~8 d | Hard preset only. Realism and risk, but adds failure systems that D-14 does not cover |

---

## 6. Comparison

Ratings are my judgement, 5 is best.

| | 1 Classic | 2 Milestones | 3 Data | 4 Engineering | 5 Discovery |
|---|---|---|---|---|---|
| Fights repetition | 3 | 4 | 4 | 4 | 5 |
| Survives 100,000× warp | 4 | 5 | 5 | 4 | 4 |
| Build cost (5 = cheap) | 5 | 4 | 2 | 3 | 2 |
| Writing load (5 = light) | 4 | 3 | 4 | 3 | 1 |
| Fits the current plan | 5 | 4 | 4 | 3 | 2 |
| Freedom and replay | 4 | 2 | 4 | 3 | 3 |
| Fits interstellar | 3 | 3 | 5 | 5 | 5 |
| Easy to learn | 5 | 5 | 3 | 4 | 3 |
| Fits "engine tests" | 1 | 2 | 1 | 5 | 2 |
| Makes the infrastructure matter | 1 | 2 | 5 | 3 | 3 |

How they pair:
- **2 + 4** is the leanest strong combination: a campaign spine with qualification flights inside it.
- **3 + 5** is the "scientific" pair: data and questions. Rich, content-heavy.
- **1** alone is the safe fallback and works as the base for any of the others.

---

## 7. Recommended mix and staged delivery

**Mix M** keeps what each option does best:

| Part | From | What it does |
|---|---|---|
| Spine | Option 2 | Eras, Programmes, flagship milestones, era pacing and onboarding |
| Hardware tech | Option 4 | Programmes with design days, rush, and test objectives on keystone nodes only |
| Science | Option 1 core + Option 5 light | Automatic instruments, first-findings only, depth upgrades, and an **Open Questions** journal of about 40 questions |
| Transport | Option 3 light | Data is returned or transmitted; the DSN grows by level; samples need to be brought home; labs handle samples only |
| Layers | L1 (three forks), L2 (payroll and rush), L4, L5 | Variety, priced time, a reason to hire crew |

Staged, so every stage is playable on its own and nothing is thrown away:

| Stage | Ships in | Contents |
|---|---|---|
| A | EA1 | Career preset **Research only**: automatic instruments, first-findings subjects with depth, tree opened by milestones (option 2 for Eras 1–3). No funds |
| B | EA2–EA3 | Funds, contracts, payroll, rush, engineering programmes with test objectives on keystone nodes, DSN levels, R&D slots, doctrine forks A and B |
| C | EA4 | Questions journal, labs for samples, observatory, ISRU and asteroid milestones, fork C prepared |
| D | X1 | The interstellar ladder (3.3): array, precursors, proving flights, arrival |

If you want the cheapest path: **Option 1 + L2** now (about 50 Claude days) and decide on the rest after Early Access
feedback. Everything in this document layers on top of it.

---

## 8. Impact on the current plan

| Task | What changes |
|---|---|
| FND-06 modes | Sandbox and Career only. Science mode becomes the "Research only" preset |
| FND-14 events | Becomes critical: milestones, objectives, questions and discoveries all read it |
| SCI-01 data model | Subjects gain depth and a finite pool; no diminishing-returns curve |
| SCI-03 module | Auto-run toggle, toast, no dialog by default |
| SCI-05 transmission | Bandwidth, DSN level, distance falloff (options 3, mix) |
| SCI-08 lab | Processes samples and, in option 3, datasets, on the background-process service |
| SCI-10 Science mode | Removed as a mode; becomes a preset (CAR-09) |
| SCI-11 / TEC-04 balance | Rewritten around the chosen currency or objectives |
| TEC-01…03 tree and R&D screen | Nodes gain triggers, programme stages, test objectives, slots; the screen becomes a programme board |
| CAR-03 facilities | R&D level sets slots and tiers; new Observatory and Deep Space Network facilities |
| CAR-04 contracts | Parameter types reused for objectives and milestones |
| CAR-07 world firsts | Become the milestone triggers |
| HUB-02 screens | Programme board, Observatory, DSN, Questions |
| New | Background-process service, instrument grades, Era 7–8 nodes, interstellar array |

---

## 9. Questions for you

My recommendation is in italics after each.

1. **Modes:** sandbox and career only, with Science mode as the "Research only" career preset? *Yes: it costs
   almost nothing and removes a mode to balance.*
2. **R&D time:** keep it, but only priced (payroll and rush) and windowed? *Yes. Bare timers are never used.*
3. **Prototype testing (option 4):** are you happy that a prototype part cannot fly crewed until it is certified?
   *Yes, with an Easy-preset override.*
4. **Hidden tree and unknown bodies (option 5):** is a hidden map acceptable, or should bodies be visible from the
   start and only their *details* be discovered? *Visible bodies, hidden details and hidden nodes (silhouettes). It
   avoids touching the map and tracking station.*
5. **Data logistics (option 3):** DSN levels without occlusion before 1.0, relays after (D-22)? *Yes.*
6. **Interstellar travel (X1):** real flight with higher warp tiers, a gate, or both? And distances: real, or
   compressed? *Real flight with a 1,000,000× tier and compressed distances; a gate can be an end-game node later.*
7. **Schedule:** should EA1 ship the **Research only** preset (stage A) so research is playable before funds?
   *Yes.*
8. **Writing:** about 40 Open Questions and about 30 test objectives are hand-written. Is that load acceptable,
   or should one of them be smaller? *Start with 15 questions and 12 objectives, and grow by era.*

Tell me what you choose (a number, a mix, "take the recommendation"), and I will draft the tasks, rewrite the three
plan files and update `tasks.csv`.
