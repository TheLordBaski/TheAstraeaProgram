using System;
using System.Collections.Generic;
using TAP.Core;
using UnityEditor;

namespace TAP.EditorTools
{
    /// <summary>Contextual property help: identically named parameters can have different units and meanings.</summary>
    public static class BodyLabHelp
    {
        private static readonly Dictionary<string, string> Descriptions = new Dictionary<string, string>();
        static BodyLabHelp()
        {
            Add<BodyDefinition>(
                "id|Unique body ID used by parent, launch-site and save references. Renaming updates references when saved.",
                "displayName|The name displayed to players and in the lab.", "description|Descriptive text for this body.",
                "type|Body category. A star emits light and has no terrain; planets, moons, dwarfs and gas giants use the terrain model.",
                "parent|ID of the body being orbited. Empty means the system's root; each child needs an orbit.",
                "radius|Datum radius in metres. Terrain heights are measured above it; oceans stand at datum height zero.",
                "gm|Gravitational parameter G × mass in m³/s². Increasing it raises surface gravity and orbital speeds. Mass is GM / G.",
                "rotationPeriod|Sidereal spin period in seconds. Zero uses the runtime default; ignored when tidally locked.",
                "tidallyLocked|Sets the spin period to the orbital period so the same hemisphere faces the parent on a circular orbit.",
                "initialRotationDeg|Body rotation about its north axis at UT zero, in degrees.",
                "orbit|Kepler orbit relative to the parent's centre. Changing it updates the system orbit preview.",
                "atmosphere|Pressure, temperature and atmospheric appearance. Disable it for an airless body.",
                "terrain|Procedural surface definition or shared preset, with height layers, colours and biome rules.",
                "hasOcean|Adds an ocean at datum height zero where terrain is below sea level.",
                "warpAltitudeLimits|Altitude thresholds in metres for each rails-warp level, in the game's warp-level order.",
                "mapColor|RGB colour used for the body's map marker and star preview. Channels range from zero to one.",
                "luminosity|Stars: radiated power in watts, controlling solar flux at other bodies.",
                "surfaceTemperature|Stars: surface temperature in Kelvin, stored for star colour/glare.");
            Add<OrbitDefinition>(
                "semiMajorAxis|Size of the ellipse in metres from the parent's centre. Periapsis = a(1−e), apoapsis = a(1+e).",
                "eccentricity|Ellipse shape: zero is circular; values approaching one are stretched. Body orbits require 0 ≤ e < 1.",
                "inclinationDeg|Tilt of the orbital plane relative to the system's equatorial plane, in degrees.",
                "lanDeg|Longitude of ascending node: turns the line where the orbit crosses the equatorial plane, in degrees.",
                "argPeDeg|Argument of periapsis: turns the closest approach within the orbital plane, in degrees.",
                "meanAnomalyAtEpochDeg|Orbital phase at the epoch in degrees. Mean anomaly advances uniformly in time.",
                "epoch|Reference universal time in seconds for the orbital phase.");
            Add<AtmosphereDefinition>(
                "height|Top of the atmosphere above datum radius in metres; pressure and density end here.",
                "seaLevelPressure|Pressure at height zero, in pascals. Higher pressure also raises density for the same temperature.",
                "scaleHeight|Exponential pressure falloff distance in metres. Larger values keep pressure high farther above the surface.",
                "molarMass|Average molecular mass in kg/mol, used by the ideal-gas density and sound-speed models.",
                "adiabaticIndex|Ratio of specific heats, used to calculate the speed of sound. Typical diatomic gas is about 1.4.",
                "temperatureCurve|Rows [altitude in metres, temperature in Kelvin]. Altitudes must increase; temperature is interpolated between rows.",
                "skyColor|RGB sky/atmosphere colour, each channel zero to one.", "horizonColor|RGB colour used near the horizon, each channel zero to one.");
            Add<TerrainDefinition>(
                "preset|Shared Resources/Data/Terrain/<name>.json. Objects merge into it; arrays replace whole inherited arrays.",
                "description|Description of the terrain definition, preserved in JSON.", "seed|Base seed for repeatable terrain. Individual noise and crater seeds are added to it.",
                "minHeight|Conservative lowest surface height in metres. Used for LOD bounds and safety; set below every generated depression.",
                "maxHeight|Conservative highest surface height in metres. Used for LOD bounds and safety; set above every generated peak.",
                "seaLevel|Height subtracted from the combined layers, in metres. Raising it lowers the land relative to the ocean at height zero.",
                "fields|Ordered named values reused by layers, colour masks and biome rules. References normally read earlier fields.",
                "layers|Ordered height operations. Each needs one source; shaping, amplitude, masks and blend are applied in sequence.",
                "flatAreas|Ground pulled onto planes for pads or bases, optionally fitted to the existing land.",
                "places|Named geographic landmarks that terms can measure distances to with near.",
                "shore|Field whose zero line is the coast; enables shore-distance terms.",
                "colors|Surface colour paint rules, applied in order. Later matching rules paint over earlier ones.",
                "shading|Brightness variation from noise and slope, applied over the surface paint.",
                "biomes|Ordered region rules. The first whose conditions all reach 0.5 wins; put an unconditional fallback last.");
            Add<NoiseDef>(
                "type|fbm: rolling signed noise; ridged: sharp crests in 0–1; billow: rounded lumps; perlin: one octave.",
                "seed|Seed offset added to the terrain seed. Changes this pattern without changing every terrain pattern.",
                "frequency|Features per radian of arc. Higher values produce smaller, more frequent features on the sphere.",
                "offset|Three coordinates translating the noise sample, separating patterns that share a seed.",
                "octaves|Number of noise detail levels. More octaves add finer detail and cost more generation time.",
                "lacunarity|Frequency multiplier per octave. Default is 2, or 2.1 for ridged noise.",
                "gain|Amplitude multiplier per octave. Lower values weaken fine detail.",
                "amount|For jitter noise: displacement magnitude in the units of the term being jittered.");
            Add<TerrainFieldDef>(
                "id|Unique field name referenced by later fields, layers and terms.", "note|Author note with no effect on generation.",
                "noise|Noise-pattern source for this field.", "from|ID of another field used as the source.",
                "sum|Weighted sum of other fields. Each row names a field and its weight.",
                "term|Term used as the field source. Field terms cannot read height, slope or shore because those are evaluated later.",
                "scale|Multiplier applied to the source value before add and spots.", "add|Value added after multiplying by scale.",
                "spots|Local Gaussian bumps, dents or pulls around geographic points.",
                "smooth|[low, high] smoothstep mapping into 0–1, applied after spots.", "linear|[low, high] clamped straight ramp into 0–1, applied after spots.");
            Add<WeightDef>("of|ID of the field to include in this weighted sum.", "weight|Multiplier for this field; negative values subtract it.");
            Add<SpotDef>("note|Author note with no effect on generation.", "lat|Centre latitude in degrees north.", "lon|Centre longitude in degrees east.",
                "radius|Gaussian falloff distance in metres along the surface.", "amount|Amount added at the centre, fading with distance; negative values make dents.",
                "toward|When enabled, pull the field towards this target instead of adding amount.", "strength|Multiplier controlling how strongly the field is pulled towards its target.");
            Add<TerrainLayerDef>(
                "note|Author note with no effect on generation.", "noise|Noise-pattern source for this height layer.", "from|ID of a field used as this layer's source.",
                "constant|Fixed source value, before amplitude and add.", "craters|Seeded crater depressions, rims and optional output fields.",
                "rift|Long valley following geographic path points.", "canyons|Branching valleys following a noise pattern's zero lines.",
                "terraces|Steps the height accumulated by earlier layers.", "dunes|Parallel dune crests around the selected wind axis.",
                "ellipsoid|Three semi-axes [x, y, z] in metres; y is the spin axis. Produces an elongated/flattened surface relative to radius.",
                "smooth|[low, high] smoothstep shaping into 0–1 before abs and power.", "linear|[low, high] clamped ramp into 0–1 before abs and power.",
                "abs|Use the absolute shaped source before raising it to power.", "power|Exponent shaping the source, before amplitude. Larger values narrow positive peaks.",
                "amplitude|Multiplier for the shaped source. For dimensionless noise this converts it into metres; geometric sources already return metres.",
                "add|Height offset in metres added after amplitude.", "mask|Terms multiply into the layer's influence. Height can read prior layers; slope and shore are unavailable here.",
                "blend|add sums with the prior height; max raises only lower land; min lowers only higher land.",
                "detail|Marks small-scale relief to exclude when calculating coast distances.");
            Add<TermDef>(
                "note|Author note with no effect on generation.",
                "of|A field ID or built-in height (m), slope (degrees), latitude/absLatitude/longitude (degrees), or shore (m, land positive).",
                "near|ID of a place or flat area. The source becomes surface distance to it in metres.", "sum|Sum of nested terms, which can each be shaped and scaled.",
                "jitter|Noise displacement added to the source before shaping; amount uses the source's units.",
                "smooth|[low, high] smoothstep into 0–1.", "linear|[low, high] clamped straight ramp into 0–1.",
                "range|[minimum, maximum): one inside, zero outside. Either end may be null for an open bound.",
                "below|One below this threshold, zero above it. The threshold uses the source's units.", "atLeast|One at or above this threshold, zero below it.",
                "invert|Replace the shaped result with one minus that result.", "scale|Multiplier applied after shaping and inversion.",
                "remap|[low, high] output range for the shaped value: zero maps to low, one to high.");
            Add<CraterSizeDef>("cell|Grid cell size in metres; at most one crater is placed in each cell.", "chance|Probability from zero to one that a cell contains a crater.", "depth|Crater depth as a fraction of its radius.");
            Add<CraterFieldDef>(
                "seed|Seed offset for crater placement and ages.", "sizes|Crater cell sizes, probabilities and depth ratios; several scales can coexist.",
                "radius|[minimum, maximum] crater radius as a fraction of its grid cell, not metres.", "rim|Rim height as a fraction of crater depth.",
                "rimInside|Rim width towards the crater interior, in crater radii.", "rimOutside|Rim width outside the crater, in crater radii.",
                "floor|Depth fraction below which the crater floor is flattened.", "reach|Extent of crater influence including rim and rays, in crater radii.",
                "search|Neighbour search distance in grid cells. Zero automatically searches everything within reach.",
                "depthScale|Multiplier on every crater depth.", "fresh|Fraction from zero to one of young craters that can have bright rays.",
                "rays|Brightness of rays from young craters.", "raysFrom|Distance where rays start, in crater radii.", "erosion|Zero keeps sharp rims; one slumps older craters into softer depressions.",
                "raysField|Optional output field ID for ray brightness.", "floorField|Optional output field ID for large crater floors.",
                "floorMinRadius|Minimum crater radius in metres contributing to floorField.", "freshField|Optional output field ID for young large craters and their surroundings.",
                "freshMinRadius|Minimum crater radius in metres contributing to freshField.");
            Add<RiftDef>("path|At least two [latitude, longitude] points in degrees, joined by great-circle segments.", "width|Rift width in metres.",
                "depth|Valley depth in metres.", "floor|Fraction of rift width occupied by the flat floor.", "shoulders|Raised shoulder height as a fraction of valley depth.",
                "wobble|Noise bending the rift edges; amount is in metres.", "field|Optional output field ID: one on the floor, fading up the walls.");
            Add<CanyonDef>("noise|Noise pattern; valleys follow its zero lines.", "width|Half-width in noise-value units. Land where |noise| is below this becomes canyon.",
                "depth|Canyon depth in metres.", "sharpness|Wall-shape exponent. One makes a round trough; higher values make narrower V-shaped valleys.");
            Add<TerraceDef>("step|Vertical spacing between terrace levels in metres.", "sharpness|Zero keeps the original relief; one creates flat treads and steep risers.");
            Add<DuneDef>("wavelength|Distance between successive crests in metres.", "height|Dune height in metres.", "axisLat|Wind-axis latitude in degrees; dune crests form circles around this axis.",
                "axisLon|Wind-axis longitude in degrees.", "crest|Crest's fraction of a wave, controlling the gentle face and steep slip face.", "warp|Noise distorting the dune crests.");
            Add<FlatAreaDef>("id|Unique flat-area ID, which launch sites and near terms can reference.", "name|Displayed site name.", "note|Author note with no effect on generation.",
                "lat|Centre latitude in degrees north.", "lon|Centre longitude in degrees east.", "height|Plane height at its centre in metres above datum.",
                "radius|Radius in metres of ground pulled fully onto the plane.", "blend|Distance in metres beyond radius where the plane fades into the surrounding land.",
                "outerRadius|Optional wider smoothing radius in metres.", "outerRelief|Remaining relief fraction near the inner blend edge; rises to full relief at outerRadius.",
                "slope|Plane tilt in degrees when normal and automatic fitting are absent.", "slopeAzimuth|Direction of downhill slope in degrees; east is 90.",
                "normal|Three components of the body-fixed plane normal; overrides slope/azimuth.", "fit|Fit the plane's height and tilt to the underlying terrain.",
                "level|When fitting, estimate height while keeping the plane level.", "maxSlope|Maximum allowed fitted tilt in degrees.");
            Add<PlaceDef>("id|Unique landmark ID referenced by near terms.", "name|Displayed landmark name.", "note|Author note with no effect on generation.", "lat|Latitude in degrees north.", "lon|Longitude in degrees east.");
            Add<ShoreDef>("field|ID of a field whose zero line is the coast and whose positive values are land.");
            Add<ColorRuleDef>("note|Author note with no effect on generation.", "color|Surface paint colour in #RRGGBB; later rules paint over earlier ones.", "mask|Terms multiply into this paint rule's influence. No terms paints everywhere.");
            Add<ShadingDef>("noise|Noise pattern varying brightness over the painted colour.", "amount|Brightness-noise strength in the formula 1 + noise × amount.", "slopeDarkening|Darkening strength multiplied by 1 − cos(slope); steep faces become darker.");
            Add<BiomeDef>("id|Unique biome ID stored by location/biome queries.", "name|Displayed biome name.", "note|Author note with no effect on generation.",
                "color|Colour of the biome map overlay in #RRGGBB. Surface paint is edited separately under Colors.",
                "material|Stored surface-material index for ART-07a; material-set rendering is not yet implemented by the game.",
                "when|All terms must reach at least 0.5. The first matching biome wins; no conditions makes a fallback taking all remaining land.");
        }

        private static void Add<T>(params string[] entries)
        {
            foreach (var entry in entries)
            {
                int split = entry.IndexOf('|');
                Descriptions.Add(typeof(T).Name + "." + entry.Substring(0, split), entry.Substring(split + 1));
            }
        }
        public static string Describe(Type owner, string name) => Descriptions.TryGetValue(owner.Name + "." + name, out string help) ? help : null;

        public static string Label(Type owner, string name)
        {
            string unit = "";
            if (owner == typeof(OrbitDefinition)) unit = name == "semiMajorAxis" ? "m" : name == "epoch" ? "s" : name.EndsWith("Deg", StringComparison.Ordinal) ? "°" : "";
            else if (owner == typeof(BodyDefinition)) unit = name == "radius" ? "m" : name == "gm" ? "m³/s²" : name == "rotationPeriod" ? "s" : name == "initialRotationDeg" ? "°" : name == "luminosity" ? "W" : name == "surfaceTemperature" ? "K" : "";
            else if (owner == typeof(AtmosphereDefinition)) unit = name == "height" || name == "scaleHeight" ? "m" : name == "seaLevelPressure" ? "Pa" : name == "molarMass" ? "kg/mol" : "";
            else if (owner == typeof(TerrainDefinition)) unit = name == "minHeight" || name == "maxHeight" || name == "seaLevel" ? "m" : "";
            else if (owner == typeof(FlatAreaDef)) unit = name == "radius" || name == "blend" || name == "height" || name == "outerRadius" ? "m" : name == "lat" || name == "lon" || name == "slope" || name == "slopeAzimuth" || name == "maxSlope" ? "°" : "";
            else if (owner == typeof(SpotDef) || owner == typeof(PlaceDef)) unit = name == "lat" || name == "lon" ? "°" : name == "radius" ? "m" : "";
            else if (owner == typeof(RiftDef)) unit = name == "width" || name == "depth" ? "m" : "";
            else if (owner == typeof(CanyonDef)) unit = name == "depth" ? "m" : "";
            else if (owner == typeof(CraterSizeDef)) unit = name == "cell" ? "m" : "";
            else if (owner == typeof(CraterFieldDef)) unit = name == "floorMinRadius" || name == "freshMinRadius" ? "m" : "";
            else if (owner == typeof(TerraceDef)) unit = name == "step" ? "m" : "";
            else if (owner == typeof(DuneDef)) unit = name == "wavelength" || name == "height" ? "m" : name == "axisLat" || name == "axisLon" ? "°" : "";
            else if (owner == typeof(TerrainLayerDef)) unit = name == "add" ? "m" : "";
            return ObjectNames.NicifyVariableName(name) + (unit.Length == 0 ? "" : " (" + unit + ")");
        }
    }
}
