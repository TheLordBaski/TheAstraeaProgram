using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TAP.Core;

namespace TAP.EditorTools
{
    /// <summary>Authoring errors checked before compiling a preview or writing a system file.</summary>
    public static class BodyLabValidation
    {
        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        private static void Require(bool valid, string message) { if (!valid) throw new FormatException(message); }
        private static void Positive(double value, string path) => Require(Finite(value) && value > 0, path + " must be positive and finite.");

        public static void CheckSystem(JObject system)
        {
            Require(system["bodies"] is JArray, "The system must have a bodies list.");
            var bodies = ((JArray)system["bodies"]).Cast<JObject>().ToArray();
            Require(bodies.Length > 0, "The system needs a root body.");
            var ids = new HashSet<string>();
            foreach (var b in bodies)
            {
                string id = (string)b["id"];
                Require(!string.IsNullOrWhiteSpace(id) && !id.Contains('/') && !id.Contains('\\'), "A body needs a non-empty id without slashes.");
                Require(ids.Add(id), "Duplicate body id: " + id);
            }
            Require(bodies.Count(b => string.IsNullOrEmpty((string)b["parent"])) == 1, "A system must have exactly one root body.");
            foreach (var b in bodies)
            {
                var visited = new HashSet<string>();
                var current = b;
                while (!string.IsNullOrEmpty((string)current["parent"]))
                {
                    string id = (string)current["id"], parent = (string)current["parent"];
                    Require(visited.Add(id), "Parent cycle at " + id);
                    Require(ids.Contains(parent), id + ": unknown parent " + parent);
                    Require(current["orbit"] is JObject, id + ": a child body needs an orbit.");
                    current = bodies.First(x => (string)x["id"] == parent);
                }
            }
            string siteBody = (string)system["launchSite"]?["body"];
            if (siteBody != null) Require(ids.Contains(siteBody), "The launch site references an unknown body: " + siteBody);
        }

        public static void CheckBody(JObject json)
        {
            foreach (var v in json.Descendants().OfType<JValue>())
                if (v.Type == JTokenType.Float) Require(Finite((double)v), v.Path + " must be finite.");
            var b = json.ToObject<BodyDefinition>();
            string p = b.id + ": ";
            Positive(b.radius, p + "radius");
            Positive(b.gm, p + "GM");
            Require(new[] { "planet", "moon", "dwarf", "gasgiant", "star" }.Contains(b.type), p + "unknown body type.");
            Require(b.rotationPeriod >= 0, p + "rotation period cannot be negative.");
            ColorArray(b.mapColor, p + "mapColor");
            if (b.orbit != null)
            {
                Positive(b.orbit.semiMajorAxis, p + "semi-major axis");
                Require(b.orbit.eccentricity >= 0 && b.orbit.eccentricity < 1, p + "a celestial body's orbit needs 0 ≤ eccentricity < 1.");
            }
            if (b.warpAltitudeLimits != null) Require(b.warpAltitudeLimits.All(x => x >= 0), p + "warp altitude limits cannot be negative.");
            if (b.atmosphere != null)
            {
                var a = b.atmosphere;
                Positive(a.height, p + "atmosphere height");
                Positive(a.scaleHeight, p + "scale height");
                Positive(a.molarMass, p + "molar mass");
                Positive(a.adiabaticIndex, p + "adiabatic index");
                Require(a.seaLevelPressure >= 0, p + "pressure cannot be negative.");
                ColorArray(a.skyColor, p + "skyColor");
                ColorArray(a.horizonColor, p + "horizonColor");
                double previous = double.NegativeInfinity;
                if (a.temperatureCurve != null)
                    foreach (var row in a.temperatureCurve)
                    {
                        Require(row != null && row.Length == 2, p + "temperature rows need [altitude m, temperature K].");
                        Require(row[0] > previous && row[1] > 0, p + "temperature altitudes must increase and Kelvin temperatures must be positive.");
                        previous = row[0];
                    }
            }
            if (b.type == "star") return;
            var t = b.terrain ?? new TerrainDefinition();
            Require(t.minHeight < t.maxHeight && b.radius + t.minHeight > 0, p + "terrain bounds must increase and stay outside the body's centre.");
            foreach (var obj in (json["terrain"] as JObject)?.DescendantsAndSelf().OfType<JObject>() ?? Enumerable.Empty<JObject>())
            {
                if (obj["type"] != null && obj["frequency"] != null)
                {
                    double octaves = (double?)obj["octaves"] ?? 1;
                    Require(octaves >= 1 && octaves <= 16, obj.Path + ": use 1–16 noise octaves.");
                    Positive((double)obj["frequency"], obj.Path + ".frequency");
                    var offset = obj["offset"] as JArray;
                    Require(offset == null || offset.Count == 3, obj.Path + ": noise offset needs three values.");
                }
            }
            if (t.fields != null)
                foreach (var f in t.fields)
                    Require(Count(f.noise, f.from, f.sum, f.term) == 1, p + "field " + f.id + " needs exactly one source.");
            if (t.layers != null)
                foreach (var l in t.layers)
                {
                    Require(Count(l.noise, l.from, l.constant, l.craters, l.rift, l.canyons, l.terraces, l.dunes, l.ellipsoid) == 1, p + "each layer needs exactly one source.");
                    Require(new[] { "add", "min", "max" }.Contains(l.blend), p + "unknown layer blend.");
                    if (l.ellipsoid != null) Require(l.ellipsoid.Length == 3 && l.ellipsoid.All(x => x > 0), p + "ellipsoid needs three positive semi-axes.");
                    if (l.dunes != null) Positive(l.dunes.wavelength, p + "dune wavelength");
                    if (l.terraces != null) Positive(l.terraces.step, p + "terrace step");
                    if (l.rift != null)
                    {
                        Positive(l.rift.width, p + "rift width");
                        Require(l.rift.path != null && l.rift.path.Count >= 2 && l.rift.path.All(x => x != null && x.Length == 2), p + "rift path needs at least two [latitude, longitude] points.");
                    }
                    if (l.craters?.sizes != null)
                        foreach (var s in l.craters.sizes)
                        {
                            Positive(s.cell, p + "crater cell size");
                            Require(s.chance >= 0 && s.chance <= 1 && s.depth >= 0, p + "crater chance needs 0–1 and depth cannot be negative.");
                        }
                }
            if (t.flatAreas != null)
                foreach (var a in t.flatAreas)
                {
                    Positive(a.radius, p + "flat area radius");
                    Require(a.blend >= 0 && a.maxSlope >= 0 && a.maxSlope < 90, p + "flat area blend/maximum slope is invalid.");
                    Require(a.normal == null || a.normal.Length == 3 && a.normal.Any(x => x != 0), p + "flat area normal needs three values and cannot be zero.");
                }
            if (t.fields != null)
                foreach (var f in t.fields)
                    if (f.spots != null) foreach (var s in f.spots) Positive(s.radius, p + "spot radius");
            if (t.biomes != null)
            {
                var ids = new HashSet<string>();
                foreach (var biome in t.biomes) Require(!string.IsNullOrEmpty(biome.id) && ids.Add(biome.id), p + "biome ids must be non-empty and unique.");
            }
        }

        private static int Count(params object[] sources) => sources.Count(x => x != null && (!(x is string s) || s.Length > 0));
        private static void ColorArray(float[] values, string path) =>
            Require(values != null && values.Length == 3 && values.All(x => x >= 0 && x <= 1), path + " needs three colour channels in 0–1.");
    }
}
