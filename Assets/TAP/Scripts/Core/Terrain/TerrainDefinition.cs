using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TAP.Core
{
    // A body's surface as data (Resources/Data/Terrain/*.json, or inline in a system file). Heights are metres above the
    // body's datum radius, which is sea level. Latitudes are geographic: north positive. Every entry may carry a "note".
    //
    // A sample is built in three steps:
    //  1. fields: named values at the point (noise patterns and values derived from them), for layers, colours and biomes;
    //  2. layers: added up into the height in order, each shaped and masked;
    //  3. flat areas pull the ground to a set height around a site (the launch complex).
    // Colours are painted in order over each other, and the first biome whose conditions all hold names the place.

    [Serializable]
    public class TerrainDefinition
    {
        /// <summary>Name of a shared definition in Resources/Data/Terrain. Entries given here replace the preset's.</summary>
        public string preset;
        public string description;
        public int seed = 1;
        /// <summary>Lowest and highest heights the layers can reach (m): LOD bounds, warp safety. A test checks them.</summary>
        public double minHeight = -3000;
        public double maxHeight = 5000;
        /// <summary>Where the sea stands in the layers' heights (m). Heights are measured from it.</summary>
        public double seaLevel;
        public List<TerrainFieldDef> fields;
        public List<TerrainLayerDef> layers;
        public List<FlatAreaDef> flatAreas;
        /// <summary>Named points (craters, peaks, landmarks) that biome rules can measure distances to.</summary>
        public List<PlaceDef> places;
        /// <summary>For bodies with a sea: the field whose zero line is the coast, for distances to the shore.</summary>
        public ShoreDef shore;
        public List<ColorRuleDef> colors;
        public ShadingDef shading;
        public List<BiomeDef> biomes;
    }

    /// <summary>A noise pattern on the unit sphere. Frequency counts features per radian of arc.</summary>
    [Serializable]
    public class NoiseDef
    {
        /// <summary>"fbm" (rolling), "ridged" (sharp crests, 0–1), "billow" (rounded lumps) or "perlin" (one octave).</summary>
        public string type = "fbm";
        /// <summary>Added to the terrain's seed.</summary>
        public int seed;
        public double frequency = 1;
        public double[] offset;
        public int octaves = 1;
        /// <summary>Frequency step per octave; 2.1 for ridged, else 2.</summary>
        public double? lacunarity;
        /// <summary>Amplitude step per octave.</summary>
        public double gain = 0.5;
        /// <summary>Only for "jitter": how far the noise moves the value.</summary>
        public double amount = 1;
    }

    [Serializable]
    public class TerrainFieldDef
    {
        public string id;
        public string note;
        /// <summary>
        /// The source: a noise pattern, another field ("from"), a weighted sum of fields, or a term (for example the
        /// latitude, shaped; terms here cannot read the height, slope or shore, which come later).
        /// </summary>
        public NoiseDef noise;
        public string from;
        public List<WeightDef> sum;
        public TermDef term;
        public double scale = 1;
        public double add;
        /// <summary>Round bumps (or dents, with a negative amount) in the value around points.</summary>
        public List<SpotDef> spots;
        /// <summary>Optional shaping of the result: smoothstep between two values, or a clamped ramp.</summary>
        public double[] smooth;
        public double[] linear;
    }

    [Serializable]
    public class WeightDef
    {
        public string of;
        public double weight = 1;
    }

    /// <summary>
    /// A round patch where a field is raised or lowered by amount·w, or pulled towards a value (v + (toward − v)·w·strength),
    /// with the weight w = exp(−(d/radius)²).
    /// </summary>
    [Serializable]
    public class SpotDef
    {
        public string note;
        public double lat, lon;
        public double radius;
        public double amount;
        public double? toward;
        public double strength = 1;
    }

    [Serializable]
    public class TerrainLayerDef
    {
        public string note;
        // The source, one of:
        public NoiseDef noise;
        public string from;
        public double? constant;
        public CraterFieldDef craters;
        public RiftDef rift;
        public CanyonDef canyons;
        public TerraceDef terraces;
        public DuneDef dunes;
        /// <summary>
        /// Semi-axes [x, y, z] (m, y is the spin axis): an elongated or flattened body. The layer is the ellipsoid's surface
        /// measured from the body's radius.
        /// </summary>
        public double[] ellipsoid;
        // Shaping of the source value, in this order: smooth or linear, abs, power, then ·amplitude + add.
        public double[] smooth;
        public double[] linear;
        public bool abs;
        public double power = 1;
        public double amplitude = 1;
        public double add;
        /// <summary>Where the layer applies (0–1); several terms multiply.</summary>
        [JsonConverter(typeof(SingleOrListConverter<TermDef>))]
        public List<TermDef> mask;
        /// <summary>"add" (default), "max" or "min" against the height so far.</summary>
        public string blend = "add";
        /// <summary>Small-scale detail: left out when measuring distances to the shore.</summary>
        public bool detail;
    }

    /// <summary>
    /// A value at the point, shaped: the source ("of" a field, "height", "slope" in degrees, "latitude", "absLatitude",
    /// "longitude", "shore" in metres, land positive; or "near": metres to a place; or "sum" of terms), optionally moved
    /// by "jitter" noise, then shaped (smooth, linear, range, below, atLeast), inverted, scaled and remapped.
    /// </summary>
    [Serializable]
    public class TermDef
    {
        public string note;
        public string of;
        public string near;
        [JsonConverter(typeof(SingleOrListConverter<TermDef>))]
        public List<TermDef> sum;
        public NoiseDef jitter;
        public double[] smooth;
        public double[] linear;
        /// <summary>[min, max): 1 inside, else 0. Either end may be null.</summary>
        public double?[] range;
        public double? below;
        public double? atLeast;
        public bool invert;
        public double scale = 1;
        public double[] remap;
    }

    [Serializable]
    public class CraterSizeDef
    {
        /// <summary>Grid cell (m): at most one crater per cell, its centre anywhere inside.</summary>
        public double cell;
        /// <summary>Chance that a cell has a crater.</summary>
        public double chance;
        /// <summary>Depth as a fraction of the crater's radius.</summary>
        public double depth;
    }

    [Serializable]
    public class CraterFieldDef
    {
        public int seed;
        public List<CraterSizeDef> sizes;
        /// <summary>Radius range as a fraction of the cell.</summary>
        public double[] radius = { 0.14, 0.44 };
        /// <summary>Rim height as a fraction of the depth, and the rim's width inside and outside (in radii).</summary>
        public double rim = 0.28;
        public double rimInside = 0.2;
        public double rimOutside = 0.28;
        /// <summary>The floor is flat below this fraction of the depth.</summary>
        public double floor = 0.85;
        /// <summary>How far a crater reaches, in radii (rim and rays).</summary>
        public double reach = 2.4;
        /// <summary>How far to look for craters in neighbouring cells, in cells. 0 = everything within reach.</summary>
        public double search;
        public double depthScale = 1;
        /// <summary>Share of craters young enough to have bright rays, their brightness, and where rays start (radii).</summary>
        public double fresh = 0.28;
        public double rays = 0.8;
        public double raysFrom = 0.6;
        /// <summary>0: every crater sharp. 1: the oldest slump into soft, rimless dimples (age is random per crater).</summary>
        public double erosion;
        /// <summary>
        /// Outputs: the ray brightness; 1 on the floors of craters of at least floorMinRadius (m); and 1 in and around
        /// young (rayed) craters of at least freshMinRadius (m), fading out where their rays end.
        /// </summary>
        public string raysField;
        public string floorField;
        public double floorMinRadius;
        public string freshField;
        public double freshMinRadius;
    }

    /// <summary>A long valley along a path of [lat, lon] points (great-circle segments).</summary>
    [Serializable]
    public class RiftDef
    {
        public List<double[]> path;
        public double width = 20000;
        public double depth = 2000;
        /// <summary>Share of the width that is flat floor.</summary>
        public double floor = 0.4;
        /// <summary>Raised shoulders beside the rift, as a fraction of the depth.</summary>
        public double shoulders = 0.08;
        /// <summary>Noise that bends the rift's edges, in metres.</summary>
        public NoiseDef wobble;
        /// <summary>Output: a field that is 1 on the rift's floor, fading out up its walls.</summary>
        public string field;
    }

    /// <summary>A branching network of canyons along the zero lines of a noise pattern.</summary>
    [Serializable]
    public class CanyonDef
    {
        public NoiseDef noise;
        /// <summary>Half-width in noise units: |noise| below this is canyon.</summary>
        public double width = 0.03;
        public double depth = 800;
        /// <summary>Wall shape: 1 is a rounded trough, higher gives narrower, V-shaped canyons.</summary>
        public double sharpness = 2;
    }

    /// <summary>Steps the height so far into terraces.</summary>
    [Serializable]
    public class TerraceDef
    {
        public double step = 200;
        /// <summary>0: no steps; 1: flat treads with vertical risers.</summary>
        public double sharpness = 0.7;
    }

    /// <summary>Parallel dune crests across the wind, warped by noise.</summary>
    [Serializable]
    public class DuneDef
    {
        public double wavelength = 500;
        public double height = 20;
        /// <summary>Wind axis: dune crests run around it (a latitude-like circle), in geographic lat/lon of the axis.</summary>
        public double axisLat = 0;
        public double axisLon = 90;
        /// <summary>Where the crest sits in the wave: small = a gentle slope up and a steep slip face.</summary>
        public double crest = 0.75;
        public NoiseDef warp;
    }

    /// <summary>
    /// Ground pulled onto a plane around a point: the launch complex, or the ground under a base. The plane may tilt
    /// with the land; buildings stand on foundations that make up the difference.
    /// </summary>
    [Serializable]
    public class FlatAreaDef
    {
        public string id;
        public string name;
        public double lat, lon;
        /// <summary>Height of the plane at the centre (m).</summary>
        public double height;
        /// <summary>Radius of the flat area (m), and the distance over which it blends into the land.</summary>
        public double radius = 500;
        public double blend = 2000;
        /// <summary>Optional wider softening: out to outerRadius the land keeps only part of its relief (outerRelief at the
        /// blend's edge, rising to all of it).</summary>
        public double outerRadius;
        public double outerRelief = 1;
        /// <summary>Tilt of the plane (degrees) and the compass direction it slopes down towards (degrees, east = 90).</summary>
        public double slope;
        public double slopeAzimuth;
        /// <summary>The plane's body-fixed unit normal, as saves store it; overrides slope.</summary>
        public double[] normal;
        /// <summary>Fit the plane to the ground under the area instead: its height and tilt, or only its height (level).</summary>
        public bool fit;
        public bool level;
        /// <summary>With fit: the steepest tilt allowed (degrees).</summary>
        public double maxSlope = 20;
    }

    [Serializable]
    public class PlaceDef
    {
        public string id;
        public string name;
        public double lat, lon;
    }

    [Serializable]
    public class ShoreDef
    {
        /// <summary>The field that is 0 on the coast and positive on land.</summary>
        public string field;
    }

    [Serializable]
    public class ColorRuleDef
    {
        public string note;
        /// <summary>"#RRGGBB".</summary>
        public string color;
        [JsonConverter(typeof(SingleOrListConverter<TermDef>))]
        public List<TermDef> mask;
    }

    /// <summary>Brightness variation over the painted colour: 1 + noise·amount − slopeDarkening·(1 − cos slope).</summary>
    [Serializable]
    public class ShadingDef
    {
        public NoiseDef noise;
        public double amount;
        public double slopeDarkening;
    }

    [Serializable]
    public class BiomeDef
    {
        public string id;
        public string name;
        public string note;
        /// <summary>"#RRGGBB" for the biome map overlay.</summary>
        public string color = "#808080";
        /// <summary>Index of the surface material set used to render it (terrain shader v2, ART-07a).</summary>
        public int material;
        /// <summary>Conditions that must all hold (each term 0.5 or more). None: the biome takes everything left.</summary>
        [JsonConverter(typeof(SingleOrListConverter<TermDef>))]
        public List<TermDef> when;
    }

    /// <summary>Reads a single object or an array of them as a list, so a one-term mask needs no brackets.</summary>
    public sealed class SingleOrListConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(List<T>);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token.Type == JTokenType.Null) return null;
            if (token.Type == JTokenType.Array) return token.ToObject<List<T>>(serializer);
            return new List<T> { token.ToObject<T>(serializer) };
        }

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotSupportedException();
    }
}
