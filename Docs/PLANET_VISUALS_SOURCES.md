# Planet visuals: source data and credits

The source images that the editor bakes read stay on the developer's disk in `Art/PlanetVisuals/` and are not
versioned. The repository holds what the game needs, as binary images or small text manifests:

- Baked maps: PNG surface, normal, mask, weather and cloud normals.
- Sky lookups: EXR.
- Ground materials: `Assets/TAP/Art/PlanetMaterials/`.
- Satellite detail tiles: `Assets/TAP/Art/SatelliteDetail/`.
- `.texarray` and `.cloudnoise` manifests: Unity's importers build the texture arrays and 3D cloud noise from them into the import cache. No texture data is versioned as asset text. To bake again
on another machine, recreate `Art/PlanetVisuals/` from the tables below. Without it, *TAP > Planet visuals > Bake all*
and *Bake weather only* stop with an error instead of replacing the committed maps with plain colours.

## Clouds: NASA Blue Marble cloud composite

NASA Goddard Space Flight Center; cloud imagery by Reto Stöckli, enhanced by Robert Simmon. Historical imagery, not
live weather.

| File in `Art/PlanetVisuals/Clouds/` | Source | SHA256 |
| --- | --- | --- |
| `cloud_combined_8192.tif` (8192x4096) | https://eoimages.gsfc.nasa.gov/images/imagerecords/57000/57747/cloud_combined_8192.tif | `D137775D8966AB8D443FD5126DC6E7AD72072BC1ED50555C5818D221735DAF0F` |
| `cloud-shapes.png` | Lossless PNG conversion of the TIFF (same dimensions); the weather bake reads it | `92A2A01D47E8EF4C7AD2D05F1A1899FED2044EAD35A7D0EE5BBD46A7170F44A5` |

Tellus (`WeatherMode` Global) uses it as a whole-planet cloud map. The seed rotates it in longitude, and polar haze and ice
are thinned out. Context: https://science.nasa.gov/blogs/earth-matters/2011/10/06/crafting-the-blue-marble/

## Land texture: NASA Blue Marble: Next Generation (July 2004, base land surface)

Reto Stöckli, NASA Goddard Space Flight Center (MODIS). Downloaded 2026-10-01 with the user's approval. Base map
page: https://science.nasa.gov/earth/earth-observatory/blue-marble-next-generation/base-map/

| File in `Art/PlanetVisuals/BlueMarble/` | Size | SHA256 |
| --- | --- | --- |
| `world.200407.3x21600x10800.jpg` (2 km/px) | 21,125,326 | `dea8b4dc8a4f93f5f8bce0c8c85a508a178e7901e9ed8e6bf86e6ce7ef6d61e2` |
| `world.200407.3x21600x21600.C1.jpg` (500 m/px, 0–90° E, 0–90° N) | 77,295,488 | `18c4b869510f63bb2261182e26e5c072faee7bc6553842bf9d52175da539e956` |
| `world.200407.3x21600x21600.B2.jpg` (500 m/px, 90° W–0°, 0–90° S) | 40,104,616 | `ba7f8e4b9ef4858e14d74b5a7d69be142524f6838775ed0ec00296ca26c89e99` |

All three are at `https://assets.science.nasa.gov/content/dam/science/esd/eo/images/bmng/bmng-base/july/<file>`.

Tellus keeps its own geography. Only landscape *texture* is taken, per biome class. `BlueMarble/Exemplars/` holds
twelve 1024 px tiles, two per class.

Each tile is made as follows:
1. Cut the crop from the 500 m tile and resample it to 1024 px.
2. Make it seamless with Moisan's periodic-plus-smooth decomposition.
3. High-pass it in linear colour: divide by its own Gaussian blur (σ 110 px, wrap-around), clamp the ratio to 0.35–2.4, and multiply by the tile mean.

| Tile | Region (lon, lat) | Source tile | Crop x, y, size (px) |
| --- | --- | --- | --- |
| `forest_0` | Siberian taiga 72–80° E, 58–62° N | C1 | 400, 0, 960 |
| `forest_1` | Amazon 70–62° W, 8–3° S | B2 | 820, 120, 1024 |
| `grass_0` | Sudan savanna 26–34° E, 7–11° N | C1 | 0, 0, 960 |
| `grass_1` | Ukraine 32–40° E, 48–51° N | C1 | 120, 0, 720 |
| `steppe_0` | Kazakh steppe 62–70° E, 46–49° N | C1 | 500, 0, 720 |
| `steppe_1` | Sahel 14–22° E, 12–15° N | C1 | 300, 0, 720 |
| `scrub_0` | Cerrado 50–44° W, 16–12° S | B2 | 0, 0, 960 |
| `scrub_1` | Iranian plateau 53–61° E, 30–33° N | C1 | 700, 0, 720 |
| `sand_0` | Arabian sand 46–54° E, 18–21° N | C1 | 300, 0, 720 |
| `sand_1` | Sahara 20–28° E, 23–27° N | C1 | 0, 0, 960 |
| `rock_0` | Atacama 70–67° W, 25–20° S | B2 | 0, 100, 720 |
| `rock_1` | Iranian plateau 53–61° E, 30–33° N | C1 | 0, 0, 720 |

Region crops are taken first from the tile, at 240 px per degree, with the offsets above applied inside the region.

NASA's usage guidance: its imagery is generally not subject to copyright in the United States and should be
credited (https://www.nasa.gov/nasa-brand-center/images-and-media/). No NASA identifiers are used and no endorsement
is claimed.

## Ground materials: Poly Haven (CC0-1.0)

2K diffuse, `nor_gl` normal and `arm` maps of
[sparse_grass](https://polyhaven.com/a/sparse_grass),
[forest_ground_04](https://polyhaven.com/a/forest_ground_04),
[coast_sand_01](https://polyhaven.com/a/coast_sand_01),
[rocky_terrain_02](https://polyhaven.com/a/rocky_terrain_02),
[snow_02](https://polyhaven.com/a/snow_02) and
[moon_01](https://polyhaven.com/a/moon_01).
They are versioned in `Assets/TAP/Art/PlanetMaterials/`. Download URLs and MD5 hashes were recorded with the local
download script (`Art/PlanetVisuals/sources.json`). License: https://polyhaven.com/license

No Kitten Space Agency, EVE or Scatterer assets or code are used.
