# 20 · Multiple star systems and trade routes (planned; details to be defined)

**Direction (you, 2026-09-27):**
- TAP will have **more than one planetary system**, not only the home system.
- It will have **trade routes**. You will define them later.

This goes beyond KSP 1.0, so it is planned as expansions after 1.0: **X1** for multiple systems and **X2** for trade,
unless you decide otherwise. The rest of the plan is shaped so neither needs a rewrite.

## What the rest of the plan already does for this

| Task | Why it matters here |
|---|---|
| FND-15 | Galaxy file, system ids everywhere, one simulation per system, no single-star assumptions; a test system proves it |
| FND-01, FND-03 | Star, heliocentric orbits, scaled space and sky are per system, so a second system is data |
| FND-02 | Terrain and biomes are data, so new worlds need no code |
| FND-05 | Saves carry system ids and migrate |
| RES-01, RES-04 | Resources and converter recipes are an open list (future goods) |
| CAR-01 | The ledger can take trade income and costs |
| NAV-06 | The tracking station groups vessels by system |
| MOD-01 | Content packs can later add whole systems |
| QA-02 | Missions are parameterised by body and system |

## Questions to answer before the design

**Multiple systems**
1. How do vessels get from one system to another?
   - (a) Real interstellar flight: very high-Isp engines, burns lasting months, travel measured in years at high warp.
   - (b) Game technology (a gate or jump drive): fast, but it needs rules and infrastructure.
   - (c) No travel: separate campaigns per system.
2. Distances and trip length. What happens to crew on long trips (hibernation, rotation)?
3. How many systems, and what makes each special: binary stars, a hot giant, a rogue planet, a ringed ice giant…
4. How do new systems relate to the career: outposts, colonies, new agencies?
5. One clock for the whole galaxy? (Recommended: yes, and no relativity.)

**Trade routes**
1. What is traded: raw resources (ore, propellant, metals, ice), manufactured goods, research, crew?
2. Between what: only your bases and stations, or also markets or other agencies?
3. How is a route created?
   - Fly a delivery once and then automate it (it repeats on rails).
   - Design a route in a planner (vessel, schedule, cargo).
   - Abstract routes with no vessels.
4. Do automated shipments exist as vessels you can see and lose, or are they abstract with a risk?
5. The economy: prices, supply and demand, profit that funds the agency, colonies that consume goods?
6. Interstellar trade, or trade within a system only?

## Placeholder tasks

Estimates follow your design decisions.

### MSY-01 · Multi-system design
**Milestone** X1 · **Claude** 1 d · **You** 3 d · **Needs** R1

- Answer the multi-system questions and write `product/design/MULTI_SYSTEM.md`, then break it into tasks in this
  file.

**Done when**
- [ ] The design is approved and tasks with estimates replace the placeholders below.

### MSY-02 · Galaxy map and system switching
**Milestone** X1 · **Claude** TBD · **You** TBD · **Needs** MSY-01, FND-15

- A galaxy view listing systems. Vessels, bases and the tracking station per system. Switching the active system
  in the map and the tracking station.

**Done when**
- [ ] Defined by MSY-01.

### MSY-03 · The second system
**Milestone** X1 · **Claude** TBD · **You** TBD · **Needs** MSY-01

- Its star, worlds, biomes, science values, landmarks and visuals, built with the same tools as the home system.

**Done when**
- [ ] Defined by MSY-01.

### MSY-04 · Travel between systems
**Milestone** X1 · **Claude** TBD · **You** TBD · **Needs** MSY-01

- The chosen travel mechanism: engines or gates, long-transit simulation, arrival, crew handling and time warp at
  interstellar scale.

**Done when**
- [ ] Defined by MSY-01.

### TRD-01 · Trade design
**Milestone** X2 · **Claude** 1 d · **You** 3 d · **Needs** R1

- Answer the trade questions and write `product/design/TRADE.md`, then break it into tasks in this file.

**Done when**
- [ ] The design is approved and tasks with estimates replace the placeholders below.

### TRD-02 · Outposts and stations as economic nodes
**Milestone** X2 · **Claude** TBD · **You** TBD · **Needs** TRD-01, RES-04

- Production and consumption at bases and stations: storage, and what each node makes and needs.

**Done when**
- [ ] Defined by TRD-01.

### TRD-03 · Routes: creation and automation
**Milestone** X2 · **Claude** TBD · **You** TBD · **Needs** TRD-01

- How a route is created and then runs on its own on rails, with failures and reports.

**Done when**
- [ ] Defined by TRD-01.

### TRD-04 · Goods, prices and markets
**Milestone** X2 · **Claude** TBD · **You** TBD · **Needs** TRD-01, CAR-10

- The goods list, prices, supply and demand, and income into the agency ledger.

**Done when**
- [ ] Defined by TRD-01.

### TRD-05 · Trade interface
**Milestone** X2 · **Claude** TBD · **You** TBD · **Needs** TRD-03

- A route planner, a network view and a route ledger.

**Done when**
- [ ] Defined by TRD-01.
