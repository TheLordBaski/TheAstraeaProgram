using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TAP.Core
{
    /// <summary>
    /// The rules of a star system file (system.json, system_debug.json) and of the terrain presets it uses: the shape of
    /// every value, the bodies' family tree, each body's physics, the launch site, and that every terrain builds. The
    /// game checks a system when it loads it; the Celestial Body Lab checks its drafts with the same rules.
    /// </summary>
    public static class SystemValidation
    {
        public static readonly string[] BodyTypes = { "star", "planet", "moon", "dwarf", "gasgiant" };
        private static readonly string[] Blends = { "add", "min", "max" };

        /// <summary>The file of a terrain preset, as problems name it.</summary>
        public static string PresetFile(string name) => "Data/Terrain/" + name + ".json";

        private static string BodyName(JToken body, int index) =>
            body is JObject o && o["id"] is JValue v && v.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)v)
                ? "body " + (string)v
                : "body #" + (index + 1);

        /// <summary>
        /// Checks a system file the way the game loads it. Returns the definition with its terrain presets resolved (the
        /// parts that could be read when there are errors), or null when the file can't be read at all.
        /// </summary>
        /// <param name="presets">Reads a terrain preset's JSON by name (null when there is none).</param>
        public static SystemDefinition Check(string file, string json, Func<string, string> presets, ContentReport report)
        {
            var token = ContentJson.Parse(json, file, report);
            if (token == null) return null;
            var system = new ContentEntry(report, file, "system", token);
            if (!(token is JObject root))
            {
                system.Error("", "an object with the system's bodies and launch site");
                return null;
            }
            ContentJson.CheckShape(root, typeof(SystemDefinition), system, "", new[] { "bodies", "launchSite" });
            var site = new ContentEntry(report, file, "launch site", root["launchSite"]);
            if (root["launchSite"] != null) ContentJson.CheckShape(root["launchSite"], typeof(LaunchSiteDefinition), site);
            if (!(root["bodies"] is JArray rawBodies))
            {
                system.Error("bodies", "a list of the system's bodies");
                return null;
            }

            // Each body: its own JSON, then the presets its terrain is built on, each checked once in its own file.
            var parsedPresets = new Dictionary<string, JObject>();
            var entries = new List<ContentEntry>();
            var resolvedBodies = new List<JObject>();
            for (int i = 0; i < rawBodies.Count; i++)
            {
                var raw = rawBodies[i];
                var e = new ContentEntry(report, file, BodyName(raw, i), raw);
                entries.Add(e);
                if (!(raw is JObject body))
                {
                    e.Error("", "an object describing the body");
                    resolvedBodies.Add(null);
                    continue;
                }
                ContentJson.CheckShape(body, typeof(BodyDefinition), e);
                var resolved = (JObject)body.DeepClone();
                if (resolved["terrain"] is JObject terrain)
                    resolved["terrain"] = Resolve(terrain, 0, e, "terrain.preset", presets, parsedPresets, report);
                resolvedBodies.Add(resolved);
            }

            CheckGraph(root, entries, system, site);
            CheckSun(system, root);

            var def = ContentJson.ToObject<SystemDefinition>(root);
            def.bodies = new List<BodyDefinition>();
            var resolvedEntries = new List<ContentEntry>();
            for (int i = 0; i < rawBodies.Count; i++)
            {
                if (resolvedBodies[i] == null) continue;
                // Rules read the resolved body; a problem points at the body's own JSON, else at the preset holding the value.
                var sources = new List<ContentEntry.Source> { new ContentEntry.Source(file, rawBodies[i]) };
                foreach (var (name, preset) in PresetChain(rawBodies[i]["terrain"] as JObject, parsedPresets))
                    sources.Add(new ContentEntry.Source(PresetFile(name), preset, "terrain"));
                var r = new ContentEntry(report, entries[i].Name, resolvedBodies[i], sources.ToArray());
                var b = ContentJson.ToObject<BodyDefinition>(resolvedBodies[i]);
                if (b == null) continue;
                CheckBody(r, b);
                def.bodies.Add(b);
                resolvedEntries.Add(r);
            }
            CheckLaunchSite(site, def);
            if (!report.HasErrors) CheckTerrainsBuild(def, resolvedEntries);
            return def;
        }

        // ------------------------------------------------------------------ presets

        /// <summary>
        /// A terrain laid over its preset, the preset first laid over its own (as the game does). A preset that is missing
        /// is reported where it is named: the body's terrain, or the preset that names it.
        /// </summary>
        private static JObject Resolve(JObject terrain, int depth, ContentEntry owner, string field, Func<string, string> presets,
            Dictionary<string, JObject> parsed, ContentReport report)
        {
            string name = (string)terrain["preset"];
            if (string.IsNullOrEmpty(name)) return terrain;
            if (depth > 4)
            {
                owner.Error(field, "presets nested at most 4 deep", "a longer chain");
                return terrain;
            }
            var preset = Preset(name, presets, parsed, report);
            if (preset == null)
            {
                // A preset that exists but can't be read is reported in its own file.
                if (!parsed.ContainsKey(name))
                    owner.Error(field, "the name of a terrain preset (a file in Resources/Data/Terrain)", $"\"{name}\"");
                return terrain;
            }
            var presetEntry = new ContentEntry(report, PresetFile(name), "terrain preset " + name, preset);
            var basis = Resolve((JObject)preset.DeepClone(), depth + 1, presetEntry, "preset", presets, parsed, report);
            return CelestialSystem.MergePreset(basis, terrain);
        }

        /// <summary>A preset's JSON, parsed and shape-checked the first time it is used; null when missing or unreadable.</summary>
        private static JObject Preset(string name, Func<string, string> presets, Dictionary<string, JObject> parsed, ContentReport report)
        {
            if (parsed.TryGetValue(name, out var cached)) return cached;
            string text = presets?.Invoke(name);
            JObject json = null;
            if (text != null)
            {
                string file = PresetFile(name);
                var token = ContentJson.Parse(text, file, report);
                var e = new ContentEntry(report, file, "terrain preset " + name, token);
                if (token is JObject o)
                {
                    ContentJson.CheckShape(o, typeof(TerrainDefinition), e);
                    json = o;
                }
                else if (token != null) e.Error("", "an object describing a terrain");
                parsed[name] = json;
            }
            return json;
        }

        /// <summary>The presets a body's terrain is built on, nearest first.</summary>
        private static IEnumerable<(string name, JObject json)> PresetChain(JObject terrain, Dictionary<string, JObject> parsed)
        {
            string name = (string)terrain?["preset"];
            for (int depth = 0; depth < 6 && !string.IsNullOrEmpty(name) && parsed.TryGetValue(name, out var p) && p != null; depth++)
            {
                yield return (name, p);
                name = (string)p["preset"];
            }
        }

        // ------------------------------------------------------------------ the system

        /// <summary>
        /// The bodies' family tree: ids, one root, parents that exist without loops, an orbit for every body with a
        /// parent, and a launch site on a body of the system. For a system JSON on its own (the Body Lab's drafts).
        /// </summary>
        public static void CheckGraph(JObject system, ContentReport report, string file = null)
        {
            var sys = new ContentEntry(report, file, "system", system);
            var site = new ContentEntry(report, file, "launch site", system["launchSite"]);
            if (!(system["bodies"] is JArray bodies))
            {
                sys.Error("bodies", "a list of the system's bodies");
                return;
            }
            var entries = new List<ContentEntry>();
            for (int i = 0; i < bodies.Count; i++) entries.Add(new ContentEntry(report, file, BodyName(bodies[i], i), bodies[i]));
            CheckGraph(system, entries, sys, site);
        }

        private static void CheckGraph(JObject system, List<ContentEntry> bodies, ContentEntry sys, ContentEntry site)
        {
            if (bodies.Count == 0)
            {
                sys.Error("bodies", "a list of at least one body", "an empty list");
                return;
            }
            var byId = new Dictionary<string, int>();
            for (int i = 0; i < bodies.Count; i++)
            {
                var e = bodies[i];
                string id = (string)(e.Json as JObject)?["id"];
                if (!e.Check(!string.IsNullOrWhiteSpace(id) && id.IndexOf('/') < 0 && id.IndexOf('\\') < 0, "id",
                        "an id without slashes", hint: "a body's id names it in saves and in other files")) continue;
                if (byId.TryGetValue(id, out int first))
                    e.Error("id", "an id no other body has", $"\"{id}\"", $"body #{first + 1} has it too");
                else byId[id] = i;
            }
            var roots = new List<string>();
            for (int i = 0; i < bodies.Count; i++)
            {
                var b = bodies[i].Json as JObject;
                if (b == null) continue;
                string parent = (string)b["parent"];
                if (string.IsNullOrEmpty(parent))
                {
                    roots.Add((string)b["id"] ?? "#" + (i + 1));
                    continue;
                }
                var e = bodies[i];
                if (!byId.ContainsKey(parent))
                {
                    e.Error("parent", "the id of another body in this system (" + string.Join(", ", byId.Keys) + ")");
                    continue;
                }
                // Follow the parents up: they must reach the root without coming back.
                var seen = new List<string> { (string)b["id"] };
                for (string p = parent; !string.IsNullOrEmpty(p) && byId.TryGetValue(p, out int pi); p = (string)(bodies[pi].Json as JObject)?["parent"])
                {
                    if (seen.Contains(p))
                    {
                        seen.Add(p);
                        e.Error("parent", "a chain of parents that ends at the root", "a loop: " + string.Join(" → ", seen));
                        break;
                    }
                    seen.Add(p);
                }
                e.Check(b["orbit"] is JObject, "orbit", "an orbit around " + parent, hint: "every body with a parent circles it");
            }
            if (roots.Count != 1)
                sys.Error("bodies", "exactly one body without a parent (the root, usually the star)",
                    roots.Count == 0 ? "none" : roots.Count + ": " + string.Join(", ", roots));

            // A system without a launch site is one rockets only fly to.
            if (!(system["launchSite"] is JObject s)) return;
            string siteBody = (string)s["body"] ?? new LaunchSiteDefinition().body;
            if (!byId.TryGetValue(siteBody, out int sb))
                site.Error("body", "the id of a body in this system (" + string.Join(", ", byId.Keys) + ")", $"\"{siteBody}\"");
            else if ((string)(bodies[sb].Json as JObject)?["type"] == "star")
                site.Error("body", "a body with a surface", $"\"{siteBody}\", a star");
        }

        private static void CheckSun(ContentEntry sys, JObject root)
        {
            var sun = ContentJson.ToObject<SunDefinition>(root["sun"] ?? new JObject());
            if (sun == null) return;
            sys.Vector(sun.direction, 3, "sun.direction", direction: true);
            sys.NotNegative(sun.intensity, "sun.intensity");
            sys.Color(sun.color, "sun.color", error: false);
        }

        // ------------------------------------------------------------------ a body

        /// <summary>The rules of one body whose terrain presets are resolved (the entry's JSON).</summary>
        public static void CheckBody(ContentEntry e, BodyDefinition b)
        {
            if (e.Json is JContainer json)
                foreach (var v in json.Descendants().OfType<JValue>())
                    if (v.Type == JTokenType.Float && (double.IsNaN((double)v) || double.IsInfinity((double)v)))
                        e.Error(Relative(v, e.Json), "a finite number");
            e.OneOf(b.type, "type", BodyTypes);
            e.Positive(b.radius, "radius");
            e.Positive(b.gm, "gm", "the standard gravitational parameter GM, m³/s²");
            e.NotNegative(b.rotationPeriod, "rotationPeriod", "seconds; 0 turns it once in 86,400 s");
            e.Color(b.mapColor, "mapColor");
            if (b.orbit != null)
            {
                e.Positive(b.orbit.semiMajorAxis, "orbit.semiMajorAxis");
                e.Check(b.orbit.eccentricity >= 0 && b.orbit.eccentricity < 1, "orbit.eccentricity",
                    "a number from 0 up to but not including 1", hint: "a body's orbit is closed");
            }
            if (b.warpAltitudeLimits != null)
                for (int i = 0; i < b.warpAltitudeLimits.Length; i++) e.NotNegative(b.warpAltitudeLimits[i], $"warpAltitudeLimits[{i}]");
            if (b.atmosphere != null) CheckAtmosphere(e, b.atmosphere);
            if (b.type == "star")
            {
                e.Positive(b.luminosity, "luminosity", "a star's radiated power, W");
                e.Positive(b.surfaceTemperature, "surfaceTemperature", "kelvin");
                return;
            }
            CheckTerrain(e, b.terrain ?? new TerrainDefinition(), b.radius);
        }

        private static void CheckAtmosphere(ContentEntry e, AtmosphereDefinition a)
        {
            e.Positive(a.height, "atmosphere.height");
            e.NotNegative(a.seaLevelPressure, "atmosphere.seaLevelPressure");
            e.Positive(a.scaleHeight, "atmosphere.scaleHeight");
            e.Positive(a.molarMass, "atmosphere.molarMass", "kg/mol");
            e.Positive(a.adiabaticIndex, "atmosphere.adiabaticIndex");
            e.Color(a.skyColor, "atmosphere.skyColor");
            e.Color(a.horizonColor, "atmosphere.horizonColor");
            if (a.temperatureCurve == null) return;
            double previous = double.NegativeInfinity;
            for (int i = 0; i < a.temperatureCurve.Length; i++)
            {
                string f = $"atmosphere.temperatureCurve[{i}]";
                var row = a.temperatureCurve[i];
                if (!e.Vector(row, 2, f)) continue;
                e.Check(row[0] > previous, f + "[0]", "altitudes that increase from row to row",
                    found: ContentEntry.Number(row[0]) + " after " + ContentEntry.Number(previous), hint: "each row is [altitude m, temperature K]");
                e.Positive(row[1], f + "[1]", "kelvin");
                previous = row[0];
            }
        }

        private static void CheckTerrain(ContentEntry e, TerrainDefinition t, double radius)
        {
            e.Check(t.minHeight < t.maxHeight, "terrain.maxHeight", "a height above terrain.minHeight (" + ContentEntry.Number(t.minHeight) + ")");
            e.Check(radius + t.minHeight > 0, "terrain.minHeight", "a height above minus the radius (" + ContentEntry.Number(-radius) + ")",
                hint: "the lowest ground must stay outside the body's centre");

            // Noise wherever it is used: in fields, layers, canyons, shading and jitter.
            if (e.Json?["terrain"] is JObject terrain)
                foreach (var noise in terrain.DescendantsAndSelf().OfType<JObject>().Where(o => o["type"] != null && o["frequency"] != null))
                {
                    string p = Relative(noise, e.Json);
                    var octaves = noise["octaves"];
                    if (octaves != null && (octaves.Type == JTokenType.Integer || octaves.Type == JTokenType.Float))
                        e.Check((double)octaves >= 1 && (double)octaves <= 16 && (double)octaves % 1 == 0, p + ".octaves", "a whole number from 1 to 16");
                    var freq = noise["frequency"];
                    if (freq.Type == JTokenType.Integer || freq.Type == JTokenType.Float) e.Positive((double)freq, p + ".frequency");
                    if (noise["offset"] is JToken off && off.Type != JTokenType.Null)
                        e.Check(off is JArray a && a.Count == 3, p + ".offset", "a list of 3 numbers");
                }

            if (t.fields != null)
                for (int i = 0; i < t.fields.Count; i++)
                {
                    var f = t.fields[i];
                    string p = $"terrain.fields[{i}]";
                    if (f == null) continue;
                    Sources(e, p, "noise, from, sum or term", ("noise", f.noise), ("from", f.from), ("sum", f.sum), ("term", f.term));
                    if (f.spots != null)
                        for (int k = 0; k < f.spots.Count; k++) e.Positive(f.spots[k]?.radius ?? 0, $"{p}.spots[{k}].radius", "metres");
                }

            if (t.layers != null)
                for (int i = 0; i < t.layers.Count; i++)
                {
                    var l = t.layers[i];
                    string p = $"terrain.layers[{i}]";
                    if (l == null) continue;
                    Sources(e, p, "noise, from, constant, craters, rift, canyons, terraces, dunes or ellipsoid",
                        ("noise", l.noise), ("from", l.from), ("constant", l.constant), ("craters", l.craters), ("rift", l.rift),
                        ("canyons", l.canyons), ("terraces", l.terraces), ("dunes", l.dunes), ("ellipsoid", l.ellipsoid));
                    e.OneOf(l.blend, p + ".blend", Blends);
                    if (l.ellipsoid != null)
                        e.Check(l.ellipsoid.Length == 3 && l.ellipsoid.All(x => x > 0), p + ".ellipsoid", "3 semi-axes above 0 (metres)");
                    if (l.dunes != null) e.Positive(l.dunes.wavelength, p + ".dunes.wavelength");
                    if (l.terraces != null) e.Positive(l.terraces.step, p + ".terraces.step");
                    if (l.rift != null)
                    {
                        e.Positive(l.rift.width, p + ".rift.width");
                        e.Check(l.rift.path != null && l.rift.path.Count >= 2 && l.rift.path.All(x => x != null && x.Length == 2),
                            p + ".rift.path", "at least two [latitude, longitude] points");
                    }
                    if (l.craters?.sizes != null)
                        for (int k = 0; k < l.craters.sizes.Count; k++)
                        {
                            var s = l.craters.sizes[k];
                            if (s == null) continue;
                            string q = $"{p}.craters.sizes[{k}]";
                            e.Positive(s.cell, q + ".cell");
                            e.InRange(s.chance, 0, 1, q + ".chance");
                            e.NotNegative(s.depth, q + ".depth");
                        }
                }

            var flatIds = new HashSet<string>();
            if (t.flatAreas != null)
                for (int i = 0; i < t.flatAreas.Count; i++)
                {
                    var a = t.flatAreas[i];
                    string p = $"terrain.flatAreas[{i}]";
                    if (a == null) continue;
                    if (e.Text(a.id, p + ".id", "an id"))
                        e.Check(flatIds.Add(a.id), p + ".id", "an id no other flat area of this body has");
                    e.Positive(a.radius, p + ".radius", "metres");
                    e.NotNegative(a.blend, p + ".blend", "metres");
                    e.Check(a.maxSlope >= 0 && a.maxSlope < 90, p + ".maxSlope", "an angle from 0 up to but not including 90 degrees");
                    if (a.normal != null) e.Vector(a.normal, 3, p + ".normal", direction: true);
                }

            if (t.biomes != null)
            {
                var ids = new HashSet<string>();
                for (int i = 0; i < t.biomes.Count; i++)
                {
                    var b = t.biomes[i];
                    if (b == null) continue;
                    string p = $"terrain.biomes[{i}].id";
                    if (e.Text(b.id, p, "an id")) e.Check(ids.Add(b.id), p, "an id no other biome of this body has");
                }
            }
        }

        /// <summary>Exactly one of a field's or layer's sources is set.</summary>
        private static void Sources(ContentEntry e, string path, string names, params (string name, object value)[] sources)
        {
            var set = sources.Where(s => s.value != null && (!(s.value is string str) || str.Length > 0)).Select(s => s.name).ToList();
            e.Check(set.Count == 1, path, "exactly one source: " + names, found: set.Count == 0 ? "none" : string.Join(" and ", set));
        }

        /// <summary>A value's path within an entry's JSON: "terrain.fields[0].noise".</summary>
        private static string Relative(JToken t, JToken root)
        {
            string p = t.Path, r = root.Path;
            if (r.Length == 0) return p;
            return p.Length > r.Length + 1 && p.StartsWith(r, StringComparison.Ordinal) ? p.Substring(r.Length + (p[r.Length] == '.' ? 1 : 0)) : p;
        }

        // ------------------------------------------------------------------ launch site and terrain

        private static void CheckLaunchSite(ContentEntry e, SystemDefinition def)
        {
            var s = def.launchSite;
            if (s == null || e.Json == null) return;
            e.Text(s.name, "name", "the site's name");
            e.NotNegative(s.padDeckHeight, "padDeckHeight", "metres above the ground");
            var body = def.bodies.Find(b => b.id == s.body);
            if (!string.IsNullOrEmpty(s.flatArea))
            {
                if (body == null || body.type == "star") return;
                var ids = body.terrain?.flatAreas?.Where(a => a != null).Select(a => a.id).ToList() ?? new List<string>();
                e.Check(ids.Contains(s.flatArea), "flatArea",
                    ids.Count == 0 ? "the id of a flat area of " + s.body + "'s terrain (it has none)" : "the id of a flat area of " + s.body + "'s terrain (" + string.Join(", ", ids) + ")");
            }
            else
            {
                e.InRange(s.latitude, -90, 90, "latitude");
                e.InRange(s.longitude, -180, 180, "longitude");
            }
        }

        /// <summary>Builds every terrain once, so its own checks (fields it reads, presets of names) run at load.</summary>
        private static void CheckTerrainsBuild(SystemDefinition def, List<ContentEntry> entries)
        {
            for (int i = 0; i < def.bodies.Count; i++)
            {
                var b = def.bodies[i];
                if (b.type == "star") continue;
                try { TerrainGenerator.Create(b); }
                catch (FormatException ex)
                {
                    string msg = ex.Message;
                    string prefix = "Terrain of " + b.id + ": ";
                    if (msg.StartsWith(prefix, StringComparison.Ordinal)) msg = msg.Substring(prefix.Length);
                    entries[i].Error("terrain", "terrain the generator can build", msg.TrimEnd('.'));
                }
            }
        }
    }
}
