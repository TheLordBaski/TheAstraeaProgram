using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using TAP.Core;
using UnityEngine;

namespace TAP.EditorTools
{
    public enum BodyLabBiomeTheme { Temperate, Arid, Frozen, Volcanic, Cratered, Random }

    /// <summary>Reproducible authoring starting points, exported as ordinary body/terrain data.</summary>
    public static class BodyLabRandomizer
    {
        private static double Between(System.Random r, double min, double max) => min + (max - min) * r.NextDouble();
        private static double LogRange(System.Random r, double min, double max) => Math.Exp(Between(r, Math.Log(min), Math.Log(max)));
        private static JObject Noise(System.Random r, string type, double frequency, int octaves) => new JObject
        {
            ["type"] = type, ["seed"] = r.Next(1, 1000000), ["frequency"] = frequency, ["octaves"] = octaves,
            ["offset"] = new JArray(Between(r, -10, 10), Between(r, -10, 10), Between(r, -10, 10)), ["gain"] = 0.5
        };

        public static JObject Generate(CelestialSystem system, string id, bool moon, int seed, string parentId = null)
        {
            var r = new System.Random(seed);
            var parents = system.Bodies.Where(b => moon ? !b.IsStar && b.Def.type != "moon" : b.Parent == null).ToArray();
            if (parents.Length == 0) throw new FormatException("A random moon needs an existing planet to orbit. Save a planet first.");
            var parent = parents.FirstOrDefault(b => b.Id == parentId) ?? parents[r.Next(parents.Length)];
            if (moon && parent.Parent != null && parent.SOIRadius < parent.Radius * 10)
                throw new FormatException("This parent's sphere of influence is too small for the generated moon. Choose another planet.");
            double radius = moon ? LogRange(r, parent.Radius * 0.035, parent.Radius * 0.25) : LogRange(r, 250000, 4000000);
            double density = Between(r, 1800, 6500);
            double maxMass = parent.Mass * (moon ? 0.025 : 0.0001);
            radius = Math.Min(radius, Math.Pow(maxMass / (density * 4 * Math.PI / 3), 1.0 / 3));
            double gm = MathD.G * density * 4 * Math.PI / 3 * radius * radius * radius;
            double eccentricity = Between(r, 0, moon ? 0.09 : 0.16);
            double minAxis = (parent.Radius * (moon ? 3 : 8) + radius) / (1 - eccentricity);
            minAxis = Math.Max(minAxis, radius * 1.5 / Math.Pow(gm / parent.GM, 0.4));
            double maxAxis;
            if (moon)
                maxAxis = double.IsInfinity(parent.SOIRadius) ? minAxis * 12 : parent.SOIRadius * 0.35 / (1 + eccentricity);
            else
            {
                double outer = parent.Children.Where(b => b.Orbit != null).Select(b => b.Orbit.ApoapsisRadius).DefaultIfEmpty(minAxis).Max();
                minAxis = Math.Max(minAxis, outer * 1.35 / (1 - eccentricity));
                maxAxis = minAxis * 2.5;
            }
            if (maxAxis <= minAxis) throw new FormatException("There is no room for this moon inside the parent's sphere of influence.");
            double axis = LogRange(r, minAxis, maxAxis);
            var theme = (BodyLabBiomeTheme)r.Next(5);
            bool ocean = theme == BodyLabBiomeTheme.Temperate;
            bool atmosphere = ocean || r.NextDouble() < (moon ? 0.22 : 0.75);
            string[] starts = { "Aster", "Vela", "Nere", "Cori", "Sola", "Orin", "Thal", "Eris" };
            string[] ends = { "is", "on", "ara", "eus", "ia", "os", "um", "ene" };
            var body = new JObject
            {
                ["id"] = id, ["displayName"] = starts[r.Next(starts.Length)] + ends[r.Next(ends.Length)],
                ["description"] = $"Generated {theme.ToString().ToLowerInvariant()} {(moon ? "moon" : "planet")} (authoring seed {seed}).",
                ["type"] = moon ? "moon" : "planet", ["parent"] = parent.Id,
                ["radius"] = radius, ["gm"] = gm, ["rotationPeriod"] = LogRange(r, 10000, 200000),
                ["tidallyLocked"] = moon && r.NextDouble() < 0.7, ["initialRotationDeg"] = Between(r, 0, 360), ["hasOcean"] = ocean,
                ["orbit"] = new JObject
                {
                    ["semiMajorAxis"] = axis, ["eccentricity"] = eccentricity, ["inclinationDeg"] = Between(r, 0, moon ? 25 : 12),
                    ["lanDeg"] = Between(r, 0, 360), ["argPeDeg"] = Between(r, 0, 360), ["meanAnomalyAtEpochDeg"] = Between(r, 0, 360), ["epoch"] = 0
                }
            };
            if (atmosphere)
            {
                double temperature = theme == BodyLabBiomeTheme.Frozen ? Between(r, 110, 210) : theme == BodyLabBiomeTheme.Volcanic ? Between(r, 450, 800) : Between(r, 260, 390);
                double molarMass = Between(r, 0.02, 0.044);
                double scale = Math.Min(radius * 0.025, Math.Max(100, 8.314462618 * temperature / (molarMass * gm / (radius * radius))));
                double height = Math.Min(radius * 0.25, scale * Between(r, 8, 12));
                Color sky = Color.HSVToRGB((float)r.NextDouble(), 0.5f, 0.85f);
                Color horizon = Color.Lerp(sky, Color.white, 0.65f);
                body["atmosphere"] = new JObject
                {
                    ["height"] = height, ["seaLevelPressure"] = LogRange(r, moon ? 100 : 1500, 2000000), ["scaleHeight"] = scale,
                    ["molarMass"] = molarMass, ["adiabaticIndex"] = Between(r, 1.25, 1.5),
                    ["temperatureCurve"] = new JArray(new JArray(0, temperature), new JArray(height * 0.2, temperature * 0.7), new JArray(height * 0.7, temperature * 0.76), new JArray(height, temperature * 0.55)),
                    ["skyColor"] = Rgb(sky), ["horizonColor"] = Rgb(horizon)
                };
            }
            double relief = radius * Between(r, 0.004, 0.012);
            var layers = new JArray(
                new JObject { ["from"] = "relief", ["amplitude"] = relief, ["add"] = ocean ? -relief * 0.2 : relief * 0.2 },
                new JObject { ["noise"] = Noise(r, "ridged", Between(r, 5, 12), 4), ["power"] = 2, ["amplitude"] = relief * 0.7 });
            if (theme == BodyLabBiomeTheme.Cratered)
                layers.Add(new JObject { ["craters"] = new JObject { ["seed"] = r.Next(1000000), ["sizes"] = new JArray(
                    new JObject { ["cell"] = radius * 0.2, ["chance"] = 0.5, ["depth"] = 0.12 },
                    new JObject { ["cell"] = radius * 0.07, ["chance"] = 0.45, ["depth"] = 0.16 }), ["erosion"] = r.NextDouble() * 0.6 } });
            if (theme == BodyLabBiomeTheme.Arid)
                layers.Add(new JObject { ["dunes"] = new JObject { ["wavelength"] = radius * 0.01, ["height"] = relief * 0.05, ["axisLat"] = Between(r, -60, 60), ["axisLon"] = Between(r, -180, 180) }, ["detail"] = true });
            if (theme == BodyLabBiomeTheme.Volcanic)
                layers.Add(new JObject { ["canyons"] = new JObject { ["noise"] = Noise(r, "fbm", 8, 3), ["width"] = 0.05, ["depth"] = relief * 0.5 } });
            body["terrain"] = new JObject
            {
                ["seed"] = seed, ["description"] = "Generated inline terrain; edit freely or export as a shared preset.",
                ["minHeight"] = -Math.Max(relief * 5, theme == BodyLabBiomeTheme.Cratered ? radius * 0.15 : 0), ["maxHeight"] = relief * 5, ["seaLevel"] = 0,
                ["fields"] = new JArray(new JObject { ["id"] = "relief", ["noise"] = Noise(r, "fbm", Between(r, 1.5, 4), 4) }),
                ["layers"] = layers, ["shading"] = new JObject { ["noise"] = Noise(r, "fbm", 35, 2), ["amount"] = 0.08, ["slopeDarkening"] = 0.25 }
            };
            body = ApplyBiomes(body, theme, r.Next(), vary: true);
            var colour = Parse((string)body["terrain"]["biomes"].Last["color"]);
            body["mapColor"] = Rgb(colour);
            double atmoHeight = (double?)body["atmosphere"]?["height"] ?? radius * 0.08;
            body["warpAltitudeLimits"] = new JArray(0, atmoHeight, atmoHeight, atmoHeight, Math.Max(atmoHeight, radius * 0.2), radius * 0.4, radius * 0.8, radius * 1.6);
            return body;
        }

        private static JArray Rgb(Color c) => new JArray((double)c.r, (double)c.g, (double)c.b);
        private static Color Parse(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }
        private static string Vary(System.Random r, string hex, bool vary)
        {
            if (!vary) return hex;
            Color.RGBToHSV(Parse(hex), out float h, out float s, out float v);
            return "#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(Mathf.Repeat(h + (float)Between(r, -0.045, 0.045), 1), Mathf.Clamp01(s + (float)Between(r, -0.15, 0.15)), Mathf.Clamp(v + (float)Between(r, -0.15, 0.12), 0.15f, 0.95f)));
        }
        private static JObject Rule(string source, string shape, JToken value) => new JObject { ["of"] = source, [shape] = value };
        private static JObject Biome(string id, string name, string color, params JObject[] terms)
        {
            var b = new JObject { ["id"] = id, ["name"] = name, ["color"] = color, ["material"] = 0 };
            if (terms.Length > 0) b["when"] = new JArray(terms);
            return b;
        }

        /// <summary>Only biome and colour overrides change; terrain geometry and shared presets remain intact.</summary>
        public static JObject ApplyBiomes(JObject draft, BodyLabBiomeTheme theme, int seed, JObject effective = null, bool vary = false)
        {
            var r = new System.Random(seed);
            bool random = theme == BodyLabBiomeTheme.Random || vary;
            if (theme == BodyLabBiomeTheme.Random) theme = (BodyLabBiomeTheme)r.Next(5);
            string[][] palettes = {
                new[] { "#C8E1DE", "#747879", "#9A926C", "#47824B" },
                new[] { "#DBD0B5", "#856854", "#B77E4B", "#CBA361" },
                new[] { "#E5F4FA", "#677E94", "#A3BCCB", "#B8D5DF" },
                new[] { "#999487", "#423D43", "#923E2D", "#63544C" },
                new[] { "#C4C1B7", "#757278", "#989288", "#56565D" }
            };
            string[] names = { "Grasslands", "Dune Plains", "Ice Plains", "Basalt Plains", "Crater Basins" };
            var colours = palettes[(int)theme].Select(c => Vary(r, c, random)).ToArray();
            var body = (JObject)draft.DeepClone();
            var resolved = effective ?? draft;
            var terrain = body["terrain"] as JObject;
            if (terrain == null) { terrain = new JObject(); body["terrain"] = terrain; }
            double maxHeight = (double?)resolved["terrain"]?["maxHeight"] ?? 5000;
            bool ocean = (bool?)resolved["hasOcean"] ?? false;
            var biomes = new JArray();
            if (ocean)
            {
                biomes.Add(Biome("deepSea", "Deep Sea", Vary(r, "#123665", random), Rule("height", "below", -Math.Max(10, maxHeight * 0.08))));
                biomes.Add(Biome("shallowSea", "Shallow Sea", Vary(r, "#247A96", random), Rule("height", "below", 0)));
            }
            biomes.Add(Biome("polar", "Polar Caps", colours[0], Rule("absLatitude", "atLeast", random ? Between(r, 50, 78) : 65)));
            biomes.Add(Biome("cliffs", "Cliffs", colours[1], Rule("slope", "atLeast", random ? Between(r, 20, 40) : 28)));
            biomes.Add(Biome("highlands", "Highlands", colours[2], Rule("height", "atLeast", maxHeight * (random ? Between(r, 0.08, 0.22) : 0.15))));
            biomes.Add(Biome("plains", names[(int)theme], colours[3]));
            terrain["biomes"] = biomes; terrain["colors"] = ColorsFor(biomes);
            return body;
        }

        /// <summary>Inserts a conditional biome before the existing rules, preserving their fallback and geometry.</summary>
        public static JObject AddBiome(JObject draft, JObject effective, int seed)
        {
            var r = new System.Random(seed);
            var body = (JObject)draft.DeepClone();
            var terrain = body["terrain"] as JObject;
            if (terrain == null) { terrain = new JObject(); body["terrain"] = terrain; }
            var biomes = effective["terrain"]?["biomes"] is JArray existing ? (JArray)existing.DeepClone() : new JArray(Biome("surface", "Surface", "#808080"));
            if (biomes.Count == 0) biomes.Add(Biome("surface", "Surface", "#808080"));
            string id = "region"; for (int n = 2; biomes.Any(b => (string)b["id"] == id); n++) id = "region" + n;
            bool latitude = r.Next(2) == 0;
            double lower = latitude ? Between(r, -65, 45) : Between(r, -180, 110);
            double width = latitude ? Between(r, 10, 35) : Between(r, 20, 60);
            string colour = "#" + ColorUtility.ToHtmlStringRGB(Color.HSVToRGB((float)r.NextDouble(), (float)Between(r, 0.25, 0.65), (float)Between(r, 0.45, 0.85)));
            var biome = Biome(id, "Random Region " + (biomes.Count + 1), colour, Rule(latitude ? "latitude" : "longitude", "range", new JArray(lower, lower + width)));
            biomes.Insert(0, biome); terrain["biomes"] = biomes;
            var colors = effective["terrain"]?["colors"] is JArray painted ? (JArray)painted.DeepClone() : new JArray(new JObject { ["color"] = "#808080" });
            colors.Add(new JObject { ["color"] = colour, ["mask"] = biome["when"].DeepClone() }); terrain["colors"] = colors;
            return body;
        }

        private static JArray ColorsFor(JArray biomes)
        {
            var colors = new JArray();
            foreach (var biome in biomes.Reverse())
            {
                var rule = new JObject { ["color"] = biome["color"].DeepClone() };
                if (biome["when"] != null) rule["mask"] = biome["when"].DeepClone();
                colors.Add(rule);
            }
            return colors;
        }
    }
}
