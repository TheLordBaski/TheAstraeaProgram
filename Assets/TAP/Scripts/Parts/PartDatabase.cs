using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using TAP.Core;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>
    /// Loads and indexes part/resource definitions from Resources/Data/parts.json, checked at load (<see cref="PartValidation"/>):
    /// a part or resource with errors is left out, and <see cref="WhyMissing"/> says why it isn't there.
    /// </summary>
    public sealed class PartDatabase
    {
        public const string ResourcePath = "Data/parts";

        public readonly PartCatalog Catalog;
        private readonly Dictionary<string, PartDefinition> _parts = new Dictionary<string, PartDefinition>();
        private readonly Dictionary<string, ResourceDefinition> _resources = new Dictionary<string, ResourceDefinition>();
        private readonly Dictionary<string, MixDefinition> _mixes = new Dictionary<string, MixDefinition>();
        private readonly Dictionary<string, Propellant> _propellants = new Dictionary<string, Propellant>();
        private readonly Dictionary<string, (string title, ContentProblem problem)> _broken = new Dictionary<string, (string, ContentProblem)>();

        /// <summary>What the check found when the catalog was loaded (empty for a catalog made in code).</summary>
        public ContentReport Problems { get; private set; } = new ContentReport();

        private static PartDatabase _instance;
        public static PartDatabase Instance
        {
            get
            {
                if (_instance == null) _instance = LoadFromResources();
                return _instance;
            }
        }

        public static JsonSerializerSettings JsonSettings => new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore,
        };

        public static PartDatabase LoadFromResources(string path = ResourcePath)
        {
            var ta = UnityEngine.Resources.Load<TextAsset>(path);
            if (ta == null) throw new Exception("Missing part catalog at Resources/" + path);
            return Load(path + ".json", ta.text);
        }

        /// <summary>
        /// Loads a catalog after checking it: parts and resources with errors are left out and the rest load. Problems are
        /// logged once. Throws a <see cref="ContentException"/> only when the file can't be read at all.
        /// </summary>
        /// <param name="file">The file as problems name it ("Data/parts.json").</param>
        public static PartDatabase Load(string file, string json)
        {
            var report = new ContentReport();
            var result = PartValidation.Check(file, json, report);
            ContentLog.Add(report, result == null
                ? "no part can be loaded until it is fixed"
                : "parts and resources with errors are left out until they are fixed");
            if (result == null) throw new ContentException(file + " can't be loaded", report);
            return FromCheck(result, report);
        }

        /// <summary>A catalog of a check's good entries, knowing why the others are missing (no logging).</summary>
        public static PartDatabase FromCheck(PartValidation.Result result, ContentReport report)
        {
            var db = new PartDatabase(result.Catalog) { Problems = report };
            foreach (var kv in result.Broken) db._broken[kv.Key] = kv.Value;
            return db;
        }

        /// <summary>Reads a catalog without checking it (tests and tools that make their own).</summary>
        public static PartDatabase FromJson(string json)
        {
            var cat = JsonConvert.DeserializeObject<PartCatalog>(json, JsonSettings);
            return new PartDatabase(cat);
        }

        public PartDatabase(PartCatalog catalog)
        {
            Catalog = catalog;
            foreach (var r in catalog.resources) _resources[r.id] = r;
            if (catalog.mixes == null) catalog.mixes = new List<MixDefinition>();
            foreach (var m in catalog.mixes) _mixes[m.id] = m;
            foreach (var p in catalog.parts)
            {
                if (_parts.ContainsKey(p.id)) Debug.LogWarning("Duplicate part id " + p.id);
                _parts[p.id] = p;
            }
        }

        /// <summary>Tools and tests that need every part of the catalogue, released or not, set this.</summary>
        public static bool ShowUnreleased;

        /// <summary>Whether the part's milestone is out yet (<see cref="PartTemplates.Released"/>); the player only sees released parts.</summary>
        public static bool IsReleased(PartDefinition p) => ShowUnreleased || PartTemplates.Instance.Released(p.milestone);

        /// <summary>The parts the player can build with: released ones.</summary>
        public IReadOnlyList<PartDefinition> Parts => Catalog.parts.Where(IsReleased).ToList();

        /// <summary>Every part of the catalogue, with the ones of releases to come.</summary>
        public IReadOnlyList<PartDefinition> AllParts => Catalog.parts;

        public IReadOnlyList<ResourceDefinition> ResourceDefinitions => Catalog.resources;

        /// <summary>A released part by id; null for an unknown or unreleased one.</summary>
        public PartDefinition Get(string id)
        {
            if (id != null && _parts.TryGetValue(id, out var p) && IsReleased(p)) return p;
            return null;
        }

        /// <summary>A part by id whether or not its release is out.</summary>
        public PartDefinition GetAny(string id)
        {
            if (id != null && _parts.TryGetValue(id, out var p)) return p;
            return null;
        }

        public ResourceDefinition GetResource(string id)
        {
            if (id != null && _resources.TryGetValue(id, out var r)) return r;
            return null;
        }

        public double ResourceDensity(string id) => GetResource(id)?.density ?? 0;

        public IReadOnlyList<MixDefinition> Mixes => Catalog.mixes;

        public MixDefinition GetMix(string id)
        {
            if (id != null && _mixes.TryGetValue(id, out var m)) return m;
            return null;
        }

        /// <summary>The first mix that has this resource as a component (the one a readout groups it under), or null.</summary>
        public MixDefinition MixOf(string resourceId)
        {
            foreach (var m in Catalog.mixes)
                foreach (var c in m.components) if (c.resource == resourceId) return m;
            return null;
        }

        /// <summary>What an engine with this propellant id burns: a mix or a single resource. Null when there is no such thing.</summary>
        public Propellant GetPropellant(string id)
        {
            if (id == null) return null;
            if (_propellants.TryGetValue(id, out var cached)) return cached;
            Propellant made = null;
            if (_mixes.TryGetValue(id, out var mix)) made = Propellant.Of(mix, GetResource);
            else if (_resources.TryGetValue(id, out var res)) made = Propellant.Of(res);
            if (made != null) _propellants[id] = made;
            return made;
        }

        /// <summary>Whether a part was left out because of errors in its data (rather than not existing).</summary>
        public bool IsBroken(string id) => id != null && _broken.ContainsKey(id);

        /// <summary>
        /// Why there is no part with this id: "Hornet Engine (eng_hornet) has an error in Data/parts.json line 175:
        /// engine.ispVac: expected a number above 0, found nothing." for a part left out, else that there is no such part.
        /// </summary>
        public string WhyMissing(string id)
        {
            if (id != null && _broken.TryGetValue(id, out var b))
            {
                string name = string.IsNullOrWhiteSpace(b.title) ? id : $"{b.title} ({id})";
                return b.problem == null ? name + " has errors in its data" : $"{name} has an error in {b.problem.Place}: {b.problem.What}";
            }
            var unreleased = GetAny(id);
            if (unreleased != null && !IsReleased(unreleased)) return $"\"{unreleased.title}\" ({id}) arrives with {unreleased.milestone}";
            return $"there is no part \"{id}\"";
        }

        // ------------------------------------------------------------------ hot reload

        /// <summary>What applying a new version of the catalog did (F8 in the editor).</summary>
        public sealed class ReloadResult
        {
            public readonly ContentReport Problems = new ContentReport();
            /// <summary>"Hornet Engine: engine.thrustVac 215000 → 250000": in effect now, also on parts already built.</summary>
            public readonly List<string> Changes = new List<string>();
            /// <summary>Changes only vessels built from now on get: models, nodes, what a part carries, modules added or removed.</summary>
            public readonly List<string> ForNewVessels = new List<string>();
            /// <summary>Entries that keep their previous version: new versions with errors, parts gone from the file.</summary>
            public readonly List<string> Kept = new List<string>();
            public readonly List<string> Added = new List<string>();
            /// <summary>The file couldn't be read, so nothing changed.</summary>
            public bool Unreadable;
        }

        /// <summary>Part fields read once when a vessel is built: changing them reaches vessels built afterwards.</summary>
        private static readonly string[] BuiltOnce =
        {
            "model", "nodes", "surfaceAttach", "resources", "crew.seats", "rcs.nozzles", "landingLeg.length", "landingLeg.splayDeg",
            "dockingPort.nodeId",
        };

        /// <summary>A part's modules: adding or removing one reaches vessels built afterwards.</summary>
        private static readonly string[] Modules =
            { "engine", "decoupler", "parachute", "landingLeg", "heatShield", "reactionWheel", "rcs", "crew", "command", "dockingPort", "fin" };

        /// <summary>
        /// Applies a new version of the catalog file (F8 hot reload). Every definition is updated in place, so parts
        /// already built, in flight or in the assembly building, use the new values from their next step. A part or
        /// resource whose new version has errors keeps its old values; new ones are added; ones gone from the file stay
        /// until the game restarts. Nothing changes when the file can't be read.
        /// </summary>
        public ReloadResult Reload(string file, string json)
        {
            var r = new ReloadResult();
            var check = PartValidation.Check(file, json, r.Problems);
            if (check == null)
            {
                r.Unreadable = true;
                return r;
            }
            foreach (var res in check.Catalog.resources)
            {
                if (_resources.TryGetValue(res.id, out var old)) Apply(old, res, "resource " + res.id, r);
                else
                {
                    Catalog.resources.Add(res);
                    _resources[res.id] = res;
                    r.Added.Add("resource " + res.id);
                }
            }
            // Mixes are read again whole; the engines and parts below see them from their next step.
            foreach (var m in check.Catalog.mixes)
                if (!_mixes.TryGetValue(m.id, out var oldMix)) r.Added.Add("mix " + m.id);
                else foreach (var c in ContentUpdate.Differences(oldMix, m)) r.Changes.Add($"mix {m.id}: {c}");
            Catalog.mixes.Clear();
            Catalog.mixes.AddRange(check.Catalog.mixes);
            _mixes.Clear();
            foreach (var m in Catalog.mixes) _mixes[m.id] = m;
            _propellants.Clear();
            foreach (var p in check.Catalog.parts)
            {
                if (_parts.TryGetValue(p.id, out var old)) Apply(old, p, p.title, r);
                else
                {
                    Catalog.parts.Add(p);
                    _parts[p.id] = p;
                    _broken.Remove(p.id);
                    r.Added.Add($"{p.title} ({p.id})");
                }
            }
            foreach (var kv in check.Broken)
                if (_parts.TryGetValue(kv.Key, out var old))
                    r.Kept.Add($"{old.title} keeps its previous values: {kv.Value.problem?.What ?? "its new version has errors"}");
            foreach (var p in Catalog.parts)
                if (!check.Broken.ContainsKey(p.id) && !check.Catalog.parts.Exists(x => x.id == p.id))
                    r.Kept.Add($"{p.title} ({p.id}) is no longer in {file}; it stays until the game restarts");
            Catalog.categories = check.Catalog.categories;
            return r;
        }

        private static void Apply(object old, object next, string name, ReloadResult r)
        {
            foreach (var c in ContentUpdate.Differences(old, next))
            {
                bool once = Modules.Contains(c.Path)
                    || BuiltOnce.Any(b => c.Path == b || c.Path.StartsWith(b + ".", StringComparison.Ordinal) || c.Path.StartsWith(b + "[", StringComparison.Ordinal));
                (once ? r.ForNewVessels : r.Changes).Add($"{name}: {c}");
            }
            ContentUpdate.CopyInto(old, next);
        }
    }
}
