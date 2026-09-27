# 02 · The home system (SYS)

KSP 1.0 shipped a star with 7 planets and 9 moons. Each world was a different challenge: a hot inner planet that is
expensive to reach, a thick-atmosphere world that is easy to land on and very hard to leave, a small icy moon with
flat plains, a desert planet with thin air, a gas giant with a family of moons. TAP has Tellus and Luma. This
workstream builds the rest of the **home system**. Further systems come later
([20-multi-system-and-trade.md](20-multi-system-and-trade.md)), so every body is plain data on the FND-02 and FND-15
frameworks.

**Design rules**
- **Scale**: stay at the slice's small scale (Tellus: 600 km radius, 9.81 m/s², 70 km atmosphere, 6 h day), so
  missions are minutes, not hours.
- **Roles**: every world teaches something new: aerobraking, thin-air landing, extreme Δv, dense-atmosphere ascent,
  low-gravity EVA, gas-giant capture.
- **Originality**: names, shapes, colours and features are TAP's own. The table is a *starting point* for SYS-01;
  its numbers produce sensible SOIs and a Δv ladder, and are tuned by the SYS-17 autotests.
- **Differentiators from KSP**: Magnus has rings, Rubra has blue sunsets and dust, Caligo has seas of dense brine, and
  landmarks exist to be discovered (SYS-19).

## Proposed bodies (working names, decision D-02)

| Body | Role | Orbits | Radius | Surface g | Atmosphere | Orbit (a · e · i) | Rotation | EA |
|---|---|---|---|---|---|---|---|---|
| **Astraea** (star) | Light, heat, the gravity well everything orbits | — | 250,000 km | — (GM 1.2·10¹⁸) | — | — | 20 d | EA1 |
| **Ignis** | Scorched inner planet; hardest Δv to reach and land | Astraea | 260 km | 2.9 | none | 5.4 Gm · 0.20 · 7.0° | 51 Tellus days | EA2 |
| **Caligo** | Thick haze and brine seas; easy to land on, very hard to leave | Astraea | 720 km | 15.8 | 5 atm, 90 km | 9.9 Gm · 0.01 · 2.1° | 22 h | EA2 |
| · **Mica** | Tiny captured rock; EVA-hop gravity | Caligo | 13 km | 0.05 | none | 31,000 km · 0.35 · 12° | 7 h | EA2 |
| **Tellus** (home) | Home, launch complex, oceans | Astraea | 600 km | 9.81 | 1 atm, 70 km | 14.0 Gm · 0 · 0° | 6 h | done |
| · **Luma** | First landing; cratered, locked | Tellus | 220 km | 1.6 | none | 13,000 km · 0 · 0° | locked | done |
| · **Pruina** | Small icy moon; wide frozen flats, inclined orbit | Tellus | 70 km | 0.45 | none | 45,000 km · 0.02 · 6.5° | 11 h | EA1 |
| **Rubra** | Red desert, thin air (big chutes), polar ice, rifts | Astraea | 340 km | 2.9 | 0.07 atm, 45 km | 21.0 Gm · 0.05 · 0.1° | 18 h | EA1 |
| · **Vigil** | Large close moon of Rubra, locked | Rubra | 140 km | 1.1 | none | 3,300 km · 0.03 · 0.2° | locked | EA1 |
| **Rupes** | Dwarf planet in the belt; deep rifts and terraces | Astraea | 150 km | 1.1 | none | 41 Gm · 0.14 · 5° | 34 h | EA2 |
| **Magnus** | Ringed gas giant; no surface, crush depth, aerocapture | Astraea | 6,200 km | 8.0 | deep, 200 km | 70 Gm · 0.05 · 1.3° | 10 h | EA2 |
| · **Maris** | Ocean moon with islands and air | Magnus | 520 km | 7.8 | 0.8 atm, 55 km | 28,000 km · 0 · 0° | locked | EA2 |
| · **Gelida** | Cracked ice shell, ridges | Magnus | 300 km | 2.3 | none | 44,000 km · 0 · 0° | locked | EA2 |
| · **Gravis** | Big airless, heavy gravity: the landing exam | Magnus | 600 km | 8.0 | none | 70,000 km · 0 · 0.03° | locked | EA2 |
| · **Lapis** | Captured rock, inclined | Magnus | 65 km | 0.6 | none | 130,000 km · 0.24 · 15° | 15 h | EA2 |
| · **Granum** | Tiny, elongated seed shape, far out | Magnus | 44 km | 0.35 | none | 180,000 km · 0.17 · 4.3° | locked | EA2 |
| **Remota** | Outer icy dwarf with a very eccentric orbit | Astraea | 210 km | 1.7 | none | 90 Gm · 0.26 · 6.2° | 5 h | EA2 |

Sphere of influence with these numbers:

| Body | SOI |
|---|---|
| Tellus | ≈ 86,000 km |
| Rubra | ≈ 50,000 km |
| Caligo | ≈ 85,000 km |
| Magnus | ≈ 2.6 Gm |
| Maris | ≈ 3,800 km |
| Gravis | ≈ 10,800 km |
| Ignis | ≈ 10,600 km |
| Rupes | ≈ 34,000 km |
| Remota | ≈ 118,000 km |

Every moon sits well inside its parent's sphere of influence. Asteroids and the belt's visuals are RES-05 and
SYS-09.

---

### SYS-01 · Final system design, names and Δv budget
**Milestone** M1 · **Claude** 1 d · **You** 2 d · **Needs** —

- Settle names and short lore (D-02), the parameters, colours and the one-line identity of each world.
- Generate from the data: SOIs, periods, transfer windows (phase angles), a Δv map from Tellus to every orbit and
  surface.
- Check that the difficulty ladder rises in the intended order: Luma → Pruina → Rubra/Vigil → Caligo/Mica →
  Magnus moons → Ignis → Remota.
- Update `Docs/PLANETARY_SYSTEM.md` and list which worlds are in EA1.

**Done when**
- [ ] The table is final (or changed deliberately).
- [ ] A script generates the Δv map from `system.json`.
- [ ] The EA1 set is fixed.

### SYS-02 · The star: light, heat and danger
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-01, FND-03

- Solar flux falls off with the square of distance and drives solar panels (FLT-02) and heating (FLT-04). The HUD
  shows the flux at the vessel.
- Flying into the star destroys the vessel: skin heating beyond limits, then the photosphere.
- The star is a map target, a science subject (in space high and low; no landing) and appears in the handbook.

**Done when**
- [ ] A solar panel gives about 4 times the output at half Tellus's distance.
- [ ] A probe sent to 300 Mm of the star burns up.
- [ ] Science in orbit of the star works.

### SYS-03 · Pruina, the icy moon
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** FND-02

- Terrain: very flat frozen lowlands (easy landings), frosted hills, a few soft craters. Pale mint-white ice.
- Biomes, at least 7 with original names: Great Flats, Lesser Flats, Frost Hills, Highlands, Slopes, Poles, Midlands.
- An orbit inclined 6.5°, which teaches plane changes. Low gravity: an EVA jump reaches about 20 m.
- Warp limits and science multipliers (SCI-01).

**Done when**
- [ ] The body is defined in data only.
- [ ] Autotest `pruina-landing`: from Tellus, plane change, landing, EVA, return. It passes.

### SYS-04 · Rubra, the red desert
**Milestone** EA1 · **Claude** 3 d · **You** 1.5 d · **Needs** FND-02, FLT-05

- Terrain: highlands, lowland plains, a long rift valley, dune fields, polar ice caps, impact craters.
- A thin atmosphere: 0.07 atm, 45 km, with its own temperature curve. A salmon sky with blue sunsets and dust haze.
- Landing needs aerobraking plus large parachutes or a powered touchdown. The terminal velocity under the S2 main
  chute must be survivable (tune `chute_s2`).
- At least 8 biomes; science multipliers.

**Done when**
- [ ] Autotest `rubra-landing`: transfer window, aerobrake into orbit, landing under chutes plus engine, EVA, flag.
      It passes in the build.
- [ ] Screenshots of the sky at noon and at sunset.

### SYS-05 · Vigil, Rubra's moon
**Milestone** EA1 · **Claude** 1.5 d · **You** 0.5 d · **Needs** SYS-04

- A tidally locked, grey-brown moon with cratered highlands, smooth plains facing Rubra, and a big impact basin. At
  least 6 biomes.

**Done when**
- [ ] Autotest `vigil-landing` passes.
- [ ] Rubra hangs in Vigil's sky in the right place.

### SYS-06 · Ignis, the scorched inner planet
**Milestone** EA2 · **Claude** 2 d · **You** 1 d · **Needs** FND-02, SYS-02

- Terrain: cracked highlands, dark glassy lava plains (visual only), cooler polar craters.
- Very high solar flux, so panels are strong but heat is a problem (FLT-04). A slow rotation, 51 Tellus days.
- The highest Δv target in the system. The handbook warns about it.

**Done when**
- [ ] Autotest `ignis-orbit` passes. An unshielded probe parked for days does not overheat unless it is too close
      to the star (thermal balance checked).

### SYS-07 · Caligo, the hazy sea world
**Milestone** EA2 · **Claude** 3 d · **You** 1.5 d · **Needs** FND-02, FLT-04, FLT-05

- A dense atmosphere: 5 atm, 90 km, heavier gas (higher molar mass), purple-teal haze, strong entry heating.
- Seas of dense brine (1.2× water density). Buoyancy uses a per-body liquid density.
- Lowlands, highlands, shores and sea biomes (at least 8).
- Leaving Caligo takes a big, aerodynamic launcher. Build a reference craft that proves it can be done with 1.0
  parts.

**Done when**
- [ ] Autotest `caligo-landing` (splashdown) passes.
- [ ] Autotest `caligo-ascent`: the reference craft reaches orbit from sea level. It passes.

### SYS-08 · Mica, Caligo's pebble
**Milestone** EA2 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-02

- 13 km radius at 0.05 m/s². Surface amplitude large compared with the radius, so it looks lumpy. At least 3 biomes.
- Landing and EVA must stay stable: nothing bounces into orbit by accident, and the EVA jetpack alone can reach
  orbit, deliberately.

**Done when**
- [ ] A lander settles.
- [ ] A rocketeer can jetpack to orbit and back.

### SYS-09 · Rupes and the asteroid belt
**Milestone** EA2 · **Claude** 2 d · **You** 1 d · **Needs** FND-02, FND-03

- Rupes: deep rift canyons and terraces. At least 6 biomes.
- The belt, between 35 and 48 Gm: instanced rocks in scaled space and on the map, with no collisions. It is also a
  spawn region for trackable asteroids (RES-05).

**Done when**
- [ ] Autotest `rupes-orbit` passes.
- [ ] The belt is visible on the map without hurting frame rate (FND-13 budget).

### SYS-10 · Magnus, the ringed giant
**Milestone** EA2 · **Claude** 3 d · **You** 1.5 d · **Needs** FND-03, FLT-04, FLT-05

- No surface. A 200 km atmosphere with pressure rising steeply. Vessels are crushed below a crush depth (pressure
  or temperature limit) with a clear message.
- A banded shader with slow flow and storms. Rings: visual, lit, casting a shadow on the planet; no collisions.
- Aerocapture from interplanetary speeds is possible with a heat shield. Warp limits; science situations (flying
  high/low, space high/low).

**Done when**
- [ ] Autotest `magnus-aerocapture`: arrive on a hyperbolic path, aerocapture into orbit. It passes.
- [ ] A dive below the crush depth is fatal and says why.

### SYS-11 · Maris, the ocean moon
**Milestone** EA2 · **Claude** 2.5 d · **You** 1 d · **Needs** FND-02, SYS-10

- Mostly ocean with island chains and one small continent. 0.8 atm and 55 km of air, so parachutes and splashdowns
  work.
- Biomes include shores, islands, shallows and deep sea (at least 7).

**Done when**
- [ ] Autotest `maris-landing` (parachute landing on an island, or splashdown) passes.

### SYS-12 · Gelida, the cracked ice moon
**Milestone** EA2 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-02

- Ice shell with long ridges and crack networks, bright and smooth. At least 5 biomes.

**Done when**
- [ ] Autotest `gelida-landing` passes.

### SYS-13 · Gravis, the landing exam
**Milestone** EA2 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-02

- Big and airless, 8 m/s² of gravity: a Tellus-strength landing with no atmosphere to help. Cratered highlands and
  dark plains. At least 6 biomes.

**Done when**
- [ ] Autotest `gravis-landing` passes with a reference lander.
- [ ] The handbook states the Δv needed.

### SYS-14 · Lapis and Granum, the small moons of Magnus
**Milestone** EA2 · **Claude** 1.5 d · **You** 0.5 d · **Needs** FND-02

- Lapis: a captured rock on an inclined, eccentric orbit.
- Granum: small, far out and elongated. It needs an ellipsoid base shape in FND-02, not only a sphere.
- At least 3 biomes each.

**Done when**
- [ ] Both have autotest flybys and landings.
- [ ] Granum's collider matches its elongated shape.

### SYS-15 · Remota, the far ice dwarf
**Milestone** EA2 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-02

- Bright ice with dark streaks and a fast 5 h rotation.
- A very eccentric orbit, so arrival times and costs vary.
- At least 5 biomes.

**Done when**
- [ ] Autotest `remota-orbit` passes.

### SYS-16 · Body data tests
**Milestone** EA1 · **Claude** 1 d · **You** — · **Needs** FND-02

EditMode tests over `system.json` (and later every system file). They check that:
- SOIs are nested and every moon fits inside its parent;
- periods match Kepler's law;
- tidal locks match the orbital period;
- warp limits sit above terrain and atmosphere;
- atmosphere curves are monotonic;
- every surface point has a biome;
- science multipliers are set for every situation.

**Done when**
- [ ] The tests run in CI and fail on a bad edit.

### SYS-17 · Autotest missions to every body
**Milestone** EA2 · **Claude** 5 d · **You** 1 d · **Needs** QA-02, NAV-05

- Teach the scripted pilot the missing steps:
  - interplanetary transfers (window search, ejection burn, mid-course correction);
  - capture by burn or aerobraking;
  - landings with chutes on atmospheric worlds;
  - a return where the design allows.
- One mission per body (flyby, orbit, landing), run nightly (FND-11). They double as proof that the worlds can be
  played with 1.0 parts.

**Done when**
- [ ] Every body in the table has a passing mission in the nightly report.
- [ ] The Δv actually used is logged, and it matches the Δv map within 10%.

### SYS-18 · Δv map and body pages
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** SYS-01, UX-07

- The Δv map drawn from data, as a handbook page and a docs image.
- One page per body: facts, biomes discovered, science collected. It fills in as you explore.

**Done when**
- [ ] The handbook shows the map.
- [ ] A body's page updates after a landing.

### SYS-19 · Landmarks and points of interest
**Milestone** EA5 · **Claude** 2 d · **You** 2 d · **Needs** SYS-17, SCI-03, CAR-05

- Two to four per major body, original to TAP. Examples:
  - the wreck of a lost early probe on Luma;
  - glassy impact fields on Ignis;
  - frozen geysers on Remota;
  - a natural arch in Rubra's rift.
- Finding one gives discovery science, can be the goal of exploration contracts, and marks it in the handbook.

**Done when**
- [ ] Every landmark can be found, has science attached, and at least three contracts target landmarks.
