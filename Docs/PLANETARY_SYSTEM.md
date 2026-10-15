# The home system

All values come from `Assets/TAP/Resources/Data/system.json` (data-driven: edit the file to change the
world). Derived values below are computed from those numbers with G = 6.674×10⁻¹¹ m³ kg⁻¹ s⁻².

The system is one entry in `Resources/Data/galaxy.json`, the list of star systems. The home system is the only one
players see today: a star, seven planets, two dwarf planets and twelve moons, laid out like ours ([below](#the-whole-system)).
A small debug system (a star, "Testworld", "Testmoon" and "Testrock", a world defined in JSON alone)
exists for tests and can be played with
`-system debug` on the command line (or `PlayerPrefs "TAP.System" = "debug"` in the editor). More systems are planned
after 1.0 (`product/plan/20-multi-system-and-trade.md`).

## Astraea (the star)

| Property | Value |
|---|---|
| Radius | 250,000 km |
| Gravitational parameter μ | 1.2×10¹⁸ m³/s² |
| Luminosity | 3.35×10²⁴ W, which gives about 1,360 W/m² at Tellus's distance |
| Rotation | 432,000 s (20 Tellus days) |
| Surface | None: no terrain, no landing. It lights the system: the sun direction everywhere comes from its position |
| Time-warp limits | 5×/10× above 20 Mm, 50× above 40 Mm, 100× above 80 Mm, 1,000× above 200 Mm, 10,000× above 500 Mm, 100,000× above 1 Gm (heights above its surface) |
| Orbiting it | An orbit 60 Mm up has a period of 8.7 h and a speed of 62 km/s. The developer window can put a vessel there (*Teleport*, Astraea), and the map draws the orbit round the star. Nothing hovers closer than 10 Mm. Flying into the surface destroys the vessel |

## Tellus (home world)

| Property | Value |
|---|---|
| Radius | 600 km |
| Gravitational parameter μ | 3.5316×10¹² m³/s² |
| Mass | 5.29×10²² kg |
| Surface gravity | 9.81 m/s² (1.00 g) |
| Sidereal rotation | 21,551 s, prograde: a solar day (noon to noon) of exactly 6 h (21,600 s); equatorial surface speed 174.9 m/s. Turned 84.6° at UT 0, so the launch complex starts the game in the same place and time of day as before it moved |
| Atmosphere | 70 km thick, 101.325 kPa at sea level, scale height 5.6 km, N₂/O₂ (M = 28.96 g/mol, γ = 1.4) |
| Temperature profile | 288 K at sea level → 217 K (11–20 km) → 271 K (47–51 km) → 215 K at 70 km |
| Ocean | Yes (sea level = radius); splashdowns float |
| Terrain | Data (`Resources/Data/Terrain/tellus.json`, seed 4242, [format](TERRAIN_FORMAT.md)): continents, ridged mountain ranges with snow, woodland and steppe regions, beaches, polar ice; −3.6 km to +6.5 km; a flattened launch site |
| Biomes | Astraea Launch Complex, Polar Ice 6%, Shallows 10%, Deep Sea 30%, Snowcaps 0.2%, Coast 2%, Mountains 18%, Uplands 15%, Woodlands 7%, Steppe 5.5%, Grasslands 6% (shares of the surface) |
| Escape velocity (surface) | 3.43 km/s |
| Circular orbit at 80 km | 2,279 m/s, period 31.2 min |
| Orbit around Astraea | Circular, semi-major axis 14.0 Gm, inclination 0° (no seasons), mean anomaly 235° at UT 0 (the sun then shines from the same direction as the fixed sun of the first versions) |
| Year | 9.50×10⁶ s = 439.87 Tellus days (6 h days); calendar years have 439 or 440 days |
| Orbital speed | 9.26 km/s |
| Sphere of influence | 85,831 km (beyond it a vessel orbits the star) |
| Time-warp altitude limits | 5×/10×/50× above 70 km, 100× above 120 km, 1,000× above 240 km, 10,000× above 480 km, 100,000× above 960 km |

**Launch site**: Astraea Launch Complex at 0° N, 84.6° W on a coastal plain, the open sea 38 km to the east; pad deck
71.2 m above sea level, 700 m flattened radius blending into the land over 2.6 km, hills calm within 30 km. Launching
east gains the 174.9 m/s rotation speed and flies out over water. The assembly building stands 420 m west of the pad.
(Until FND-02 the site was at 0° E, on a bump raised in a mountainous region.)

**Clock and calendar** (`Calendar`): the HUD clock starts at Y1 D001 00:00:00 at UT 0. A day is Tellus's solar day,
6 h from noon to noon, so the time of day always follows the sun at the launch site. A year is Tellus's orbit, 439.87
days: calendar years have 439 or 440 days and each new year begins within half a day of the orbital anniversary.
Durations shown in the game (mission time, time to apoapsis, burn times) count the same 6-hour days.

## Luma (moon)

| Property | Value |
|---|---|
| Radius | 220 km |
| Gravitational parameter μ | 7.744×10¹⁰ m³/s² |
| Mass | 1.16×10²¹ kg |
| Surface gravity | 1.60 m/s² (0.163 g) |
| Rotation | Tidally locked (one turn per orbit) |
| Atmosphere | None |
| Terrain | Data (`Resources/Data/Terrain/luma.json`, seed 777): cratered highlands, dark flooded maria, craters from 18 km basins down to 20 m pits, young craters with bright rays; ±4.5 km |
| Biomes | North Pole 1.7%, South Pole 1.7%, Great Craters 1.8% (the floors of about 40 basins of 10 km radius or more), Bright Craters 2.2% (about 30 young rayed craters), Maria 15%, Highlands 30%, Foothills 48% |
| Orbit around Tellus | Circular, semi-major axis 13,000 km, inclination 0°, mean anomaly 60° at UT 0 |
| Orbital period | 156,700 s ≈ 43.5 h (7.3 Tellus days) |
| Orbital speed | 521 m/s |
| Sphere of influence radius | 2,821 km (Laplace: a·(m/M)^0.4) |
| Escape velocity (surface) | 839 m/s |
| Circular orbit at 20 km | 568 m/s, period 44 min |
| Time-warp altitude limits | 5×/10× above 6 km, 50× above 8 km, 100× above 12 km, 1,000× above 30 km, 10,000× above 60 km, 100,000× above 120 km |

## The whole system

Every world is data in `Resources/Data/system.json` and `Resources/Data/Terrain/<id>.json`. Values below are computed from
those files (periods in Tellus days of 6 h, years of 439.87 days).

| Body | Orbits | Type | Radius | g | Air | Orbit (a, e, i) | Period | SOI |
|---|---|---|---|---|---|---|---|---|
| **Ignis** | Astraea | planet | 260 km | 2.90 | none | 5.4 Gm · 0.20 · 7.0° | 105.4 days | 10.41 Mm |
| **Caligo** | Astraea | planet | 720 km | 15.80 | 5 atm, 90 km | 9.9 Gm · 0.01 · 2.1° | 261.6 days | 84.98 Mm |
| **Mica** | Caligo | moon | 13 km | 0.05 | none | 31 Mm · 0.35 · 12.0° | 17.5 days | 125 km |
| **Tellus** | Astraea | planet | 600 km | 9.81 | 1 atm, 70 km | 14 Gm · 0.00 · 0.0° | 1.0 years | 85.83 Mm |
| **Luma (locked)** | Tellus | moon | 220 km | 1.60 | none | 13 Mm · 0.00 · 0.0° | 7.3 days | 2.821 Mm |
| **Pruina** | Tellus | moon | 70 km | 0.45 | none | 45 Mm · 0.02 · 6.5° | 46.7 days | 2.352 Mm |
| **Rubra** | Astraea | planet | 340 km | 2.90 | 0.07 atm, 45 km | 21 Gm · 0.05 · 0.1° | 1.8 years | 50.2 Mm |
| **Vigil (locked)** | Rubra | moon | 140 km | 1.10 | none | 3.3 Mm · 0.03 · 0.2° | 3.0 days | 1.101 Mm |
| **Rupes** | Astraea | dwarf planet | 150 km | 1.10 | none | 41 Gm · 0.14 · 5.0° | 5.0 years | 34.56 Mm |
| **Magnus** | Astraea | giant | 6,200 km | 8.00 | 15 bar at the datum, 200 km | 70 Gm · 0.05 · 1.3° | 11.2 years | 2.56 Gm |
| **Maris (locked)** | Magnus | moon | 520 km | 7.80 | 0.8 atm, 55 km | 28 Mm · 0.00 · 0.0° | 2.5 days | 3.816 Mm |
| **Gelida (locked)** | Magnus | moon | 300 km | 2.30 | none | 44 Mm · 0.00 · 0.0° | 4.8 days | 2.37 Mm |
| **Gravis (locked)** | Magnus | moon | 600 km | 8.00 | none | 70 Mm · 0.00 · 0.0° | 9.7 days | 10.81 Mm |
| **Lapis** | Magnus | moon | 65 km | 0.60 | none | 125 Mm · 0.20 · 15.0° | 23.2 days | 1.157 Mm |
| **Granum (locked)** | Magnus | moon | 44 km | 0.35 | none | 190 Mm · 0.12 · 4.3° | 43.4 days | 1.038 Mm |
| **Anulus (rings)** | Astraea | giant | 5,200 km | 7.00 | 14 bar at the datum, 220 km | 128 Gm · 0.06 · 2.5° | 27.6 years | 3.86 Gm |
| **Fons (locked)** | Anulus | moon | 160 km | 0.90 | none | 62 Mm · 0.00 · 0.4° | 10.3 days | 1.685 Mm |
| **Pica (locked)** | Anulus | moon | 240 km | 1.40 | none | 480 Mm · 0.03 · 14.0° | 222.3 days | 21.53 Mm |
| **Glacies** | Astraea | giant | 2,500 km | 5.20 | 10 bar at the datum, 160 km | 235 Gm · 0.04 · 0.8° | 68.8 years | 3.5 Gm |
| **Vagus (locked, retrograde)** | Glacies | moon | 180 km | 0.80 | none | 350 Mm · 0.00 · 156.0° | 334.1 days | 20.17 Mm |
| **Remota** | Astraea | dwarf planet | 210 km | 1.70 | none | 345 Gm · 0.26 · 11.0° | 122.3 years | 453 Mm |


The design follows ours: a hot airless inner world, a thick-air sea world, Tellus, a red desert, a belt dwarf, two
giants and an ice giant, and a far, eccentric ice dwarf. Orbit sizes keep the ratios of ours at about 14 Gm = 1 AU
(Magnus is out where Jupiter is, Anulus where Saturn is). Magnus's three big moons sit in a 1:2:4 resonance (periods
14.7 h, 29.1 h and 58.3 h), like Io, Europa and Ganymede. Every moon, with its sphere of influence, fits inside its parent's
sphere; no two sibling spheres ever meet (checked by `PlanetarySystemTests`).

### What each world is for

| World | Identity |
|---|---|
| **Ignis** | The scorched inner world, 51 days to turn: cracked dark highlands, glassy lava plains, long cliffs, frost in the polar craters. Hardest to reach. |
| **Caligo** | Five atmospheres of purple-teal haze over seas of brine (1.2 times the density of water): easy to land on, hard to leave. |
| **Mica** | A 13 km pebble on an eccentric orbit round Caligo: the gravity (0.05 m/s²) of a jetpack hop. |
| **Pruina** | Tellus's small icy moon on a 6.5° orbit: very flat frozen lowlands for gentle landings. |
| **Rubra** | The red desert: southern highlands, northern dust plains, a rift valley, three volcanoes, dark dunes, polar caps. Thin salmon sky (0.07 atm). |
| **Vigil** | Rubra's big locked moon; its near side, a smooth dark plain, always faces Rubra. |
| **Rupes** | The dwarf planet of the belt: two giant rift canyons, terraced plateaus, a few bright salt spots. |
| **Magnus** | The gas giant (see below), with five moons: **Maris** (ocean world with air and one small continent), **Gelida** (cracked ice), **Gravis** (big, dark, Tellus-strength gravity and no air: the landing exam), **Lapis** (captured blue rock) and **Granum** (a seed-shaped, 2:1 elongated rock). |
| **Anulus** | The ringed giant (see below), with **Fons** (a white ice moon with four "tiger stripe" fractures) and **Pica** (one hemisphere coal-dark, one bright, a ridge round the equator, an orbit tilted 14°). |
| **Glacies** | The ice giant: cyan haze, a dark oval storm. Its moon **Vagus** orbits the wrong way round (a captured, locked, retrograde moon with cantaloupe terrain). |
| **Remota** | The far ice dwarf, a = 345 Gm and e = 0.26: a bright plain of frozen nitrogen, dark equatorial streaks, ice mountains. |

### Gas giants (`"type": "gasgiant"`)

A giant has no ground: the datum radius is the level in the clouds where the air has `atmosphere.seaLevelPressure`
(15 bar on Magnus), and the air thins above it exponentially. The cloud tops are painted by the terrain file's colour
rules, as on any world, on a surface with no relief: bands from a jittered latitude, storms from `spots` (the Great Storm
of Magnus), fine turbulence from noise. A vessel is crushed once the static pressure passes `crushPressure` (6 bar on
Magnus, 13.7 km above the datum): it is destroyed with the message *crushed by the pressure of 6.0 bar in Magnus's
clouds*, long before it could reach the datum's collision shell. Unloaded vessels are lost in the upper air as on any
world. Only a thin haze (12% of the air column) is drawn over the cloud tops. Aerocapture works as at any atmosphere: a
heat shield slows a probe in the upper air (200 km up on Magnus, where the pressure is a few pascals).

| Giant | Datum pressure | Crush pressure | Crush depth | Air top |
|---|---|---|---|---|
| Magnus | 15 bar | 6 bar | 13.7 km | 200 km |
| Anulus | 14 bar | 5.6 bar | 14.7 km | 220 km |
| Glacies | 10 bar | 4 bar | 11.0 km | 160 km |

### Rings

A body's `rings` list is a set of annular bands, from the body outwards: `inner` and `outer` radius (m from the centre),
`opacity` at normal incidence (0 is a gap), a `color`, how strongly narrow `ringlets` vary it and the `softEdge`. A gap is
a space between bands. Rings are visual: no collisions. They are lit by the star from either side (through the thin parts
when seen from the dark side), dimmed in the planet's shadow, and shade the planet in turn (a dark band across the
cloud tops). The ring plane can be tilted from the equator with `ringTiltDeg` and `ringTiltAzimuthDeg`: bodies spin about
one axis only, so the tilt is what gives the rings their seasons. Anulus's rings run from 5,770 km to 12,210 km:
D (faint), C (dark, translucent), B (bright, dense), a wide gap, A in two parts and a thin F ribbon. They are tilted 26.7°,
so the star climbs above the ring plane and sinks below it once per Anulus year (27.6 years). Moons orbit outside the rings.

### Moons and locked rotation

A moon with `tidallyLocked` turns once per orbit. Its `initialRotationDeg` is set so that longitude 0 faces the parent
when the game starts (Luma, the first moon, predates this). A retrograde locked moon (Vagus, 156°) turns the other
way: its `RotationPeriod` is negative. Bodies spin about one axis, so a locked moon on an inclined orbit librates in
longitude by about the inclination squared.

### Biomes

- **Ignis**: Cold Traps, Glass Plains, Rayed Craters, Great Basins, Scarps, Cracked Highlands, Foothills
- **Caligo**: Polar Slush, Shallows, Deep Brine, Salt Peaks, Salt Flats, Violet Ranges, Rust Highlands, Brine Fens, Salt Pans, Crusted Lowlands
- **Mica**: Tips, Hollows, Craters, Pale Patches, Flanks
- **Tellus**: Astraea Launch Complex, Polar Ice, Shallows, Deep Sea, Snowcaps, Coast, Mountains, Uplands, Woodlands, Steppe, Grasslands
- **Luma**: North Pole, South Pole, Great Craters, Bright Craters, Maria, Highlands, Foothills
- **Pruina**: Poles, Lesser Flats, Great Flats, Frost Hills, Slopes, Highlands, Midlands
- **Rubra**: Polar Caps, Rift Valley, Great Volcanoes, Dune Fields, Crater Floors, Escarpments, Dust Plains, Cratered Highlands
- **Vigil**: Rubra Plain, Great Basin, Fresh Craters, Crater Floors, Slopes, Far Highlands, Foothills
- **Rupes**: Bright Spots, Rift Floors, Crater Floors, Scarps, Terraced Plateaus, Dark Plains
- **Magnus**: Great Storm, Polar Hood, Equatorial Zone, Equatorial Belts, Tropical Zone, Temperate Bands
- **Maris**: Ice Shelf, Reefs, Shallows, Deep Sea, Beaches, Highlands, Island Hills, Lowland Isles
- **Gelida**: Frost Poles, Crack Fields, Stained Terrain, Ridged Plains, Crater Floors, Smooth Ice
- **Gravis**: North Pole, South Pole, Great Craters, Bright Craters, Dark Plains, Highlands, Foothills
- **Lapis**: Veined Ridges, Impact Basins, Boulder Fields, Blue Plains
- **Granum**: Seed Tips, Groove, Craters, Flanks
- **Anulus**: White Storm, Northern Cap, Polar Haze, Equatorial Band, Temperate Bands
- **Fons**: Tiger Stripes, Northern Frost, South Polar Terrain, Cratered North, Smooth Plains
- **Pica**: Polar Caps, Equatorial Ridge, Bright Crater Floors, Dark Hemisphere, Crater Floors, Bright Hemisphere
- **Glacies**: Dark Oval, Polar Clouds, Equatorial Haze, Mid-Latitude Bands
- **Vagus**: Dark Streaks, Southern Cap, Smooth Plains, Impact Pits, Uplands, Cantaloupe Terrain
- **Remota**: Polar Caps, Bright Plain, Ice Mountains, Crater Floors, Dark Belt, Cratered Uplands


### Checking the system

- `PlanetarySystemTests` (EditMode): Kepler periods, Laplace spheres, nesting, no sibling overlap, locks, warp limits above
  the air and ground, atmospheres, gas giants, rings, biomes, the star orbit.
- Autotest `star` (editor): a probe in circular orbits 60 Mm and 400 Mm above Astraea flies in physics and on rails
  (50× and 1,000×) and stays on its Kepler orbit to the metre; the map shows the orbit.
- Autotest `giant` (editor): a capsule diving into Magnus is crushed at 6 bar, 13.8 km above the datum.
- Autotest `tour` (editor): pictures of every body from a few distances (and the map), written to
  `autotest_shots/tour_*.png` beside the report. `PlayerPrefs "TAP.TourBodies"` limits it to some bodies.

## Distances and travel

| Leg | Value |
|---|---|
| Tellus surface → Luma surface (centre distance) | 13,000 km (12,180 km surface to surface) |
| Hohmann transfer from an 80 km orbit | ≈ 860 m/s prograde, ≈ 8.3 h to reach Luma's orbit |

## Approximate Δv budget (vacuum Δv; ascent includes drag and gravity losses)

| Manoeuvre | Δv |
|---|---|
| Launch to 80 km orbit | ≈ 3,400 m/s |
| Trans-Luma injection | ≈ 860 m/s |
| Luma orbit insertion (to ~30 km) | ≈ 250–320 m/s |
| Descent and landing | ≈ 600–700 m/s |
| Ascent to low Luma orbit | ≈ 600–650 m/s |
| Trans-Tellus injection | ≈ 250–300 m/s |
| Reentry and landing | 0 (aerobraking, heat shield, parachute) |
| **Total** | **≈ 6,000–6,300 m/s + margin** |

The **Luma Pathfinder** starter rocket carries about 6,690 m/s of vacuum Δv (lift-off TWR 1.95).
