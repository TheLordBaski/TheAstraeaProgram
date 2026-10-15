using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using TAP.Core;
using UnityEngine;

namespace TAP.Parts
{
    /// <summary>A size class: the diameter of its parts, the size of their attach nodes and how strong their joints are.</summary>
    [Serializable]
    public class SizeClass
    {
        public string id;
        public float diameter;
        public int nodeSize;
        /// <summary>The band of <c>breakingForce</c> (N) a part of this size has.</summary>
        public double[] breakingForce;
    }

    /// <summary>A fictional manufacturer. Its personality shows in the descriptions of its parts.</summary>
    [Serializable]
    public class ManufacturerInfo
    {
        public string id;
        public string personality;
    }

    /// <summary>What a role of part looks like: where it sits in the part list, how its ids start, and the bands its stats stay in.</summary>
    [Serializable]
    public class RoleTemplate
    {
        public string id;
        public string category;
        public string[] idPrefixes;
        public string note;
        /// <summary>Metric (see <see cref="PartTemplates.Metrics"/>) to its [min, max].</summary>
        public Dictionary<string, double[]> limits = new Dictionary<string, double[]>();
    }

    /// <summary>
    /// The part catalogue's conventions as data (PRT-01, Data/part_templates.json): the release milestones and the one that is out,
    /// the size classes, the manufacturers, and the stat band of every role. <see cref="Check"/> measures every part against them;
    /// PRT-11 balances against these bands.
    /// </summary>
    [Serializable]
    public class PartTemplates
    {
        public const string ResourcePath = "Data/part_templates";

        /// <summary>The milestones in order: "M0" is the vertical slice, the rest are the Early Access releases.</summary>
        public List<string> milestones = new List<string>();
        /// <summary>The milestone released now: parts of later ones stay hidden. The next release raises it.</summary>
        public string release = "M0";
        public List<SizeClass> sizes = new List<SizeClass>();
        public List<ManufacturerInfo> manufacturers = new List<ManufacturerInfo>();
        public List<RoleTemplate> roles = new List<RoleTemplate>();

        private HashSet<string> _released;

        private static PartTemplates _instance;

        /// <summary>The shipped conventions; with none (a project without the file) every part counts as released.</summary>
        public static PartTemplates Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var ta = UnityEngine.Resources.Load<TextAsset>(ResourcePath);
                _instance = ta != null ? Load(ta.text) : new PartTemplates();
                return _instance;
            }
        }

        public static PartTemplates Load(string json)
        {
            var t = JsonConvert.DeserializeObject<PartTemplates>(json);
            if (t == null) throw new Exception("Data/part_templates.json is empty");
            return t;
        }

        /// <summary>Whether parts of this milestone are out. An unknown milestone counts as out (the check reports it).</summary>
        public bool Released(string milestone)
        {
            if (milestones.Count == 0) return true;
            if (_released == null)
            {
                _released = new HashSet<string>();
                int last = milestones.IndexOf(release);
                for (int i = 0; i <= last && i < milestones.Count; i++) _released.Add(milestones[i]);
            }
            return milestone == null || _released.Contains(milestone) || !milestones.Contains(milestone);
        }

        public SizeClass Size(string id) => sizes.FirstOrDefault(s => s.id == id);
        public RoleTemplate Role(string id) => roles.FirstOrDefault(r => r.id == id);

        // ------------------------------------------------------------------ measuring a part

        /// <summary>
        /// The numbers a role's limits are written in: mass and price, joint strength, propellant share of an engine's or tank's
        /// weight, engine thrust-to-weight and Isp, mass per seat.
        /// </summary>
        public static Dictionary<string, double> Metrics(PartDefinition p, PartDatabase db)
        {
            var m = new Dictionary<string, double>
            {
                ["dryMass"] = p.dryMass,
                ["cost"] = p.cost,
                ["costPerKg"] = p.dryMass > 0 ? p.cost / p.dryMass : 0,
                ["breakingForce"] = p.breakingForce,
                ["impactTolerance"] = p.impactTolerance,
            };
            double resources = p.ResourceMass(db);
            if (resources > 0) m["dryFraction"] = p.dryMass / (p.dryMass + resources);
            var e = p.engine;
            if (e != null && p.dryMass > 0)
            {
                double thrustAsl = e.thrustVac * e.ispAsl / e.ispVac;
                m["ispVac"] = e.ispVac;
                m["ispAsl"] = e.ispAsl;
                m["ispRatio"] = e.ispAsl / e.ispVac;
                m["twrVac"] = e.thrustVac / (p.dryMass * MathD.G0);
                m["twrAsl"] = thrustAsl / (p.dryMass * MathD.G0);
                if (resources > 0) m["twrWet"] = e.thrustVac / ((p.dryMass + resources) * MathD.G0);
            }
            if (p.crew != null && p.crew.seats > 0) m["massPerSeat"] = p.dryMass / p.crew.seats;
            if (p.reactionWheel != null && p.dryMass > 0) m["torquePerKg"] = p.reactionWheel.torque / p.dryMass;
            return m;
        }

        // ------------------------------------------------------------------ the check

        /// <summary>
        /// Measures every part of the catalogue, released or not, against these conventions and says what is off, one line each:
        /// an unknown milestone, size, manufacturer or role; a diameter or node size that isn't its class's; an id that doesn't
        /// start as its role's do; a stat outside its role's band.
        /// </summary>
        public List<string> Check(PartDatabase db)
        {
            var problems = new List<string>();
            foreach (var p in db.AllParts)
            {
                string who = $"{p.id}: ";
                if (!milestones.Contains(p.milestone)) problems.Add(who + $"milestone \"{p.milestone}\" is not one of {string.Join(", ", milestones)}");
                if (!manufacturers.Any(x => x.id == p.manufacturer)) problems.Add(who + $"manufacturer \"{p.manufacturer}\" is not in the list");
                CheckSize(p, who, problems);

                var role = Role(p.role);
                if (role == null) { problems.Add(who + $"role \"{p.role}\" has no template"); continue; }
                if (role.category != p.category) problems.Add(who + $"role {role.id} belongs in category {role.category}, not {p.category}");
                if (role.idPrefixes != null && role.idPrefixes.Length > 0 && !role.idPrefixes.Any(x => p.id.StartsWith(x, StringComparison.Ordinal)))
                    problems.Add(who + $"the id of a {role.id} starts with {string.Join(" or ", role.idPrefixes)}");
                var metrics = Metrics(p, db);
                foreach (var kv in role.limits)
                {
                    if (!metrics.TryGetValue(kv.Key, out double v)) { problems.Add(who + $"{role.id} limits {kv.Key}, which this part has no value for"); continue; }
                    if (v < kv.Value[0] - 1e-9 || v > kv.Value[1] + 1e-9)
                        problems.Add(who + $"{kv.Key} {Format(v)} is outside {role.id}'s {Format(kv.Value[0])} to {Format(kv.Value[1])}");
                }
            }
            return problems;
        }

        private void CheckSize(PartDefinition p, string who, List<string> problems)
        {
            var ids = (p.size ?? "").Split('/');
            var classes = ids.Select(Size).ToList();
            if (string.IsNullOrEmpty(p.size) || classes.Any(c => c == null)) { problems.Add(who + $"size \"{p.size}\" is not a size class"); return; }

            // Joint strength: the union of the bands of the sizes the part spans (the rocketeer and the flag are not parts that break).
            if (p.category != "Hidden")
            {
                double lo = classes.Min(c => c.breakingForce[0]), hi = classes.Max(c => c.breakingForce[1]);
                if (p.breakingForce < lo - 1e-9 || p.breakingForce > hi + 1e-9)
                    problems.Add(who + $"breakingForce {Format(p.breakingForce)} is outside size {p.size}'s {Format(lo)} to {Format(hi)}");
            }

            var big = classes.OrderByDescending(c => c.diameter).First();
            if (big.diameter > 0 && Math.Abs(p.diameter - big.diameter) > big.diameter * 0.12f)
                problems.Add(who + $"diameter {p.diameter} m is not {big.id}'s {big.diameter} m (within 12%)");
            if (big.diameter > 0)
                foreach (var n in p.nodes)
                    if (!classes.Any(c => c.nodeSize == n.size) && !(p.category == "Command" && n.size <= big.nodeSize))
                        problems.Add(who + $"node {n.id} has size {n.size}, not one of {p.size}");
            if (p.size == "radial" && p.nodes.Count > 0) problems.Add(who + "a radial part has no stack nodes");
            if (p.size == "radial" && !(p.surfaceAttach != null && p.surfaceAttach.allowed)) problems.Add(who + "a radial part attaches to a surface");
        }

        private static string Format(double v) => Math.Abs(v) >= 1000 ? v.ToString("N0") : v.ToString("0.###");
    }
}
