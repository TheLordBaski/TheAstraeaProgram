# 12 · Resources, ISRU and asteroids (RES)

**Have:** nothing. Resources exist only as propellants in tanks.

**KSP 1.0 had:**
- **Resources and ISRU**: an **ore** resource spread over every body, orbital and surface scanners with a map
  overlay, drills, converters (ore → fuels), heat management and an engineer bonus.
- **Asteroids**: generated in size classes, found in the tracking station, captured with the grabbing unit, and
  mined.

TAP builds the same loop. It also shapes the resource model so later **trade routes** can move goods between bases,
stations and systems ([20-multi-system-and-trade.md](20-multi-system-and-trade.md)).

---

### RES-01 · Resource distribution
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** FND-02

- Resources are defined in data. They start as **Ore**; the list stays open for later goods: water ice, metals,
  volatiles (D-17).
- Per body and biome: a concentration field (seeded noise within ranges per biome). Some bodies have none
  (Tellus's oceans, the gas giant). The same for every player, set by the save's seed.
- Queries: concentration at a latitude/longitude, and average per biome (for contracts and scanners). The
  "resource abundance" difficulty option scales everything.

**Done when**
- [ ] Every EA2 body has a field.
- [ ] Unit tests cover determinism and ranges.
- [ ] A debug overlay shows it.

### RES-02 · Scanning and overlays
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** RES-01, NAV-01

- **Survey scanner**: in a low polar orbit, scans the body and reveals a coarse map of ore and biomes.
- **Narrow-band scanner**: detailed concentration below the vessel in low orbit.
- **Surface sensor**: exact value at the landing site.
- Map overlays: resource and biome textures on the body, with a legend, per body after scanning.

**Done when**
- [ ] Scanning Pruina from a polar orbit reveals its map.
- [ ] The overlay matches the surface sensor within 5%.

### RES-03 · Drills
**Milestone** EA4 · **Claude** 3 d · **You** 0.5 d · **Needs** RES-01, PRT-03, FLT-04

- Deploy on the ground (it needs to touch terrain) or on an asteroid grabbed with the grabbing unit.
- Extraction rate × concentration × engineer bonus. Uses electricity and makes heat (FLT-04 radiators).
- Fills ore tanks, and keeps running on rails (simulated over the warp with the heat and power balance).

**Done when**
- [ ] A Pruina miner fills 400 ore in the designed time.
- [ ] Running without cooling throttles it down.
- [ ] A 30-day warp credits the right amount.

### RES-04 · Converters
**Milestone** EA4 · **Claude** 2 d · **You** 0.5 d · **Needs** RES-03, FLT-01

- Recipes: ore → Propellant, ore → Monopropellant, and ore → Hydrogen if D-01 adds Hydrogen.
- Uses electricity, makes heat, and an engineer bonus applies. Converters run on rails.
- Recipes are data, ready for new goods later.

**Done when**
- [ ] A lander refuels itself on Pruina and returns to Tellus orbit (autotest RES-07).

### RES-05 · Asteroids
**Milestone** EA4 · **Claude** 4 d · **You** 1 d · **Needs** NAV-06, SYS-09

- **Spawning** on two paths: orbits that encounter Tellus's SOI in the coming weeks, and objects in the belt (SYS-09).
- **Size classes** A–E (from about 10 t to over 1,000 t). A procedural rocky mesh from a seed. Mass and ore content
  come from the class.
- **Tracking**: new ones appear in the tracking station as unknown objects. Tracking them keeps them; untracked ones
  despawn after a while. Tracking needs the facility level (CAR-03). Names such as "TAP-2811 AC".

**Done when**
- [ ] Asteroids keep appearing over a 200-day warp.
- [ ] Tracking stops their despawn.
- [ ] Their encounters show on the map.

### RES-06 · Capturing and moving asteroids
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** RES-05, FLT-11

- Grab an asteroid with the grabbing unit (FLT-11). The combined vessel handles its huge mass (thrust over the right
  centre of mass, stable joints).
- Redirect burns, parking orbits, mining it (RES-03), and asteroid contracts (CAR-05).

**Done when**
- [ ] Autotest: capture a class-B asteroid and park it in a high Tellus orbit.
- [ ] Mining it reduces its mass.

### RES-07 · ISRU and asteroid autotests
**Milestone** EA4 · **Claude** 2 d · **You** — · **Needs** RES-04, RES-06, QA-02

- Missions that check the loop:
  - scan Pruina;
  - land, drill, convert, refuel;
  - fly home;
  - capture an asteroid.

**Done when**
- [ ] The tests pass nightly, and the amounts match the rates within 2%.
