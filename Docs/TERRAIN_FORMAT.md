# Terrain and biomes as data

Every body's surface is data. The heights, colours, biomes and flattened sites all come from a JSON definition
(FND-02). A new world needs no code: add a body to a system file and give it a `terrain` block.

## Where it lives

- **In a body.** A body in `Resources/Data/system.json` (or any system file) has a `terrain` object. It can hold the
  whole definition, like Testrock in `system_debug.json` does.
- **As a preset.** It can name one with `"preset": "tellus"`, which loads `Resources/Data/Terrain/tellus.json`. Entries
  the body gives itself are laid over the preset: nested objects merge, and lists are replaced whole.

  The debug system's Testworld is Tellus with another seed and its own launch site:
  ```json
  "terrain": { "preset": "tellus", "seed": 777,
               "flatAreas": [ { "id": "launchComplex", "name": "Test Range", "lat": 0, "lon": -72.6, "height": 70, "radius": 700, "blend": 2600 } ] }
  ```
- **Units:**
  - Heights are metres above the body's radius, which is sea level.
  - Latitudes are geographic (north positive) and longitudes east positive, in degrees.
  - Distances are in metres.
- **Notes:** every entry may carry a `"note"` for the reader.

## How a height is made

1. **Fields** are named values at the point. They are worked out first, in order, and layers, colours and biomes can
   all read them.
2. **Layers** are added up into the height, in order. Each one takes a source, shapes it, scales it and masks it.
3. **Flat areas** pull the ground onto a plane around a site: the launch complex, or the ground under a base.

Colours are then painted over each other in order. The first biome whose conditions all hold names the place.

## Top level

| Entry | Meaning |
|---|---|
| `seed` | Base seed. Every noise adds its own `seed` to it |
| `minHeight`, `maxHeight` | The lowest and highest heights the layers reach. They set the terrain's level-of-detail bounds and time-warp safety; a test checks them |
| `seaLevel` | Where the sea stands in the layers' heights. Raise it to flood more (bodies with `hasOcean`) |
| `fields`, `layers`, `flatAreas`, `colors`, `shading`, `biomes` | See below |
| `places` | Named points, `{ "id", "name", "lat", "lon" }`, that terms can measure distances to (`near`) |
| `shore` | `{ "field": "continent" }`: the field whose zero line is the coast, for distances to the shore |

## Noise

`{ "type": "fbm", "seed": 17, "frequency": 0.95, "offset": [2.5, 8.1, 3.3], "octaves": 4, "gain": 0.5 }`

| Entry | Meaning |
|---|---|
| `type` | `fbm` (rolling, about −1…1), `ridged` (sharp crests, 0…1), `billow` (rounded lumps) or `perlin` (one octave) |
| `frequency` | Features per radian of arc, so the same value gives the same pattern on a big or small body |
| `octaves`, `lacunarity`, `gain` | Octaves of detail. Each octave is `lacunarity` times finer (2, or 2.1 for ridged) and `gain` times weaker |
| `offset` | Moves the pattern, so two noises with the same seed differ |
| `amount` | Only when the noise is a `jitter` (below): how far it moves the value |

## Fields

A field has an `id` and one source:

- `noise`: a noise pattern.
- `from`: another field.
- `sum`: a weighted sum, `[ { "of": "rays" }, { "of": "maria", "weight": -1 } ]`.
- `term`: any term (below) that doesn't need the height, for example the latitude.

The value then goes through, in order:
- `scale` and `add`;
- `spots`: round patches where the value is raised (`amount`) or pulled towards a value (`toward`, `strength`), with a
  gaussian of `radius` metres around `lat`/`lon`;
- optionally `smooth: [a, b]` (smoothstep) or `linear: [a, b]` (a clamped ramp).

A field can read fields listed before it, and the outputs of layers (craters, rifts). One that reads a layer's output is
worked out after the layers, so no layer may depend on it.

## Layers

Each layer has exactly one source:

| Source | Value |
|---|---|
| `noise` | A noise pattern |
| `from` | A field |
| `constant` | A number |
| `ellipsoid: [x, y, z]` | Semi-axes in metres (y is the spin axis) for an elongated or flattened body. The value is the ellipsoid's surface measured from the radius |
| `craters` | A crater field (below) |
| `rift` | A long valley along a path |
| `canyons` | A branching canyon network |
| `terraces` | Steps the height made so far into terraces |
| `dunes` | Parallel dune crests |

Then, in order:
- `smooth` or `linear` shaping, `abs`, `power`;
- `amplitude` (a multiplier) and `add`;
- `mask`: where the layer applies, 0 to 1. Several terms multiply;
- `blend` against the height so far: `add` (default), `max` or `min`. With `max`, polar ice shelves lift the polar seas
  to 30 m but leave higher land alone;
- `detail: true` marks small-scale layers that are left out when measuring distances to the shore.

- **Craters.** `sizes` is a list of `{ "cell", "chance", "depth" }`: one crater at most per grid cell of `cell` metres,
  present with `chance`, with a depth of `depth` × its radius. The other entries:

  | Entry | Meaning |
  |---|---|
  | `radius: [0.14, 0.44]` | Crater radius as a fraction of the cell |
  | `rim`, `rimInside`, `rimOutside` | Rim height and width |
  | `floor` | The floor is flat below this fraction of the depth |
  | `reach` | How far a crater reaches, in radii |
  | `fresh` | Share of young craters, which have bright `rays` |
  | `erosion` | 0 to 1: old craters slump into soft, rimless dimples |
  | `raysField` | Output: ray brightness |
  | `floorField` | Output: 1 on the floors of craters of at least `floorMinRadius` |
  | `freshField` | Output: 1 around young craters of at least `freshMinRadius` |

- **Rift.** `path` is a list of `[lat, lon]` points joined by great-circle segments. The other entries are `width`,
  `depth`, `floor` (the share of the width that is flat), `shoulders` (raised edges) and `wobble` (a noise, in metres).
  `field` is an output: 1 on the rift floor.
- **Canyons.** A `noise` pattern, cut where `|noise| < width`, `depth` metres deep. `sharpness` sets the shape: 1 is
  rounded, higher is V-shaped.
- **Terraces.** Steps of `step` metres. `sharpness` runs from 0 (soft steps) to 1 (flat treads with vertical risers).
- **Dunes.** Crests run around an axis at `axisLat`/`axisLon` (like circles of latitude around it), `wavelength` apart
  and `height` high. `crest` places the ridge in the wave (lower is a steeper slip face), and `warp` is a noise measured
  in wavelengths.

## Terms (masks, colours and biomes)

A term reads a value, optionally shapes it, and gives a number.

- **Source:**
  - `of`: a field id, or `height`, `slope` (degrees), `latitude`, `absLatitude`, `longitude` or `shore` (metres to the
    coast, positive on land);
  - `near`: metres to a flat area or place;
  - `sum`: a list of terms, added.
- **`jitter`:** a noise with an `amount` that moves the value before shaping, which gives ice caps and borders ragged
  edges.
- **Shape (one of):**
  - `smooth: [a, b]` or `linear: [a, b]`: 0 at a, 1 at b. Reversed edges count down;
  - `range: [min, max]`: 1 inside, else 0. Either end may be `null`;
  - `below: x`, `atLeast: x`: 1 or 0.
- **Then:** `invert` (1 − v), `scale`, and `remap: [a, b]` (a + (b − a)·v).

Where terms are read:
- **Layer masks** can't read `slope` or `shore`, which come from the finished height.
- **A biome's `when`** holds when every term is at least 0.5.
- **A colour's `mask`** blends the colour in by the product of its terms.

## Colours and shading

`colors` is a list of `{ "color": "#RRGGBB", "mask": … }`. Each one is painted over the previous ones by its mask;
the first usually has no mask and is the base.

`shading` varies the brightness: `1 + noise × amount − slopeDarkening × (1 − cos slope)`.

## Biomes

`biomes` is an ordered list of `{ "id", "name", "color", "material", "when" }`.

- **Matching:** the first biome whose conditions all hold names the place. The last one should have no `when`, so it
  takes everything left.
- **`color`:** shown on the biome map (developer window → *Biome map*, and *TAP → Terrain → Export Maps*).
- **`material`:** the surface material family used by the Tellus/Luma visual bake and slope-blended ground shader.
  Visual assets are separate from terrain/collision data; re-bake after authoring changes using *TAP → Planet visuals → Bake all*.
- **Natural ground only:** biomes read the ground without its flat areas, so a base never changes one.

In code: `CelestialBody.BiomeAt(lat, lon)`, or `Terrain.BiomeAt(direction)`.

## Flat areas (the launch complex, bases)

`{ "id", "name", "lat", "lon", "height", "radius", "blend" }` pulls the ground onto a plane: exactly flat within
`radius`, easing back into the land over `blend` metres. The plane can tilt with the land, since buildings stand on
foundations that make up the difference.

| Entry | Meaning |
|---|---|
| `slope`, `slopeAzimuth` | A tilt in degrees, and the compass direction it slopes down towards |
| `normal` | The plane's body-fixed normal, as saves store it |
| `fit: true` | Fit height and tilt to the ground under the area, up to `maxSlope` (20° by default) |
| `level: true` | Fit only the height |
| `outerRadius`, `outerRelief` | Also soften the land further out |

A launch site names its flat area (`"flatArea": "launchComplex"` in `launchSite`) and takes its position and height
from it.

Bases will add flat areas while the game runs:
- `LayeredTerrain.Flatten(id, direction, radius, blend, level, maxSlope)` makes one;
- `RemoveFlatArea(id)` takes it away;
- `FlatArea.ToDef()` gives the definition to save.

The chunks and colliders under an area are made again at once, and the old meshes show until the new ones are ready. A
landed vessel whose ground moved while it was away is set back on the ground when it loads. The developer window's
*Terrain* row tries this out (*Flatten ground for a base*).

## Determinism

Heights and biomes are the same bits on every platform:
- the terrain uses only plain arithmetic and its own exp, log, sin, cos and atan (`DetMath`), never System.Math;
- the noise tables come from a copy of .NET's seeded random generator.

`Assets/TAP/Tests/EditMode/TerrainFingerprint.txt` holds heights and biomes at 300 fixed points per body. A unit test
compares them bit for bit, and a program built from the same source with .NET 9 gives the same bits on Windows and on
Linux (see the FND-02 issue).

**After changing terrain data on purpose, record it again: *TAP → Terrain → Record Fingerprint*.** The test says so
when the data changed.

## Checking a new world

1. **Export the maps:** *TAP → Terrain → Export Maps* writes `Screenshots/Terrain/<system>_<body>_surface.png` and
   `…_biomes.png`, with each biome's share of the surface.
2. **Run the tests:** EditMode `TerrainTests` checks every body for these problems:
   - heights outside their bounds;
   - rules that never match;
   - bad data, which gives a clear message such as *Terrain of luma: layer 3 reads the field "nowhere", which is not
     defined*.
3. **Check the data:** *TAP → Validate Content* checks every system file and preset with the game's rules
   (also run when the game loads a system), naming the file, line, body and field of each problem
   ([CONTENT_VALIDATION.md](CONTENT_VALIDATION.md)).
4. **Look at it in the game:** in the editor, JSON edits need a reimport (right-click the file → *Reimport*) before
   the game sees them.
