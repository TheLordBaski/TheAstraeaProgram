using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>The planetary system (data-driven from Resources/Data/system.json).</summary>
    public sealed class CelestialSystem
    {
        public readonly SystemDefinition Def;
        public readonly List<CelestialBody> Bodies = new List<CelestialBody>();
        public CelestialBody Root { get; private set; }
        public Vector3d SunDirection { get; private set; }
        private readonly Dictionary<string, CelestialBody> _byId = new Dictionary<string, CelestialBody>();

        private static CelestialSystem _default;

        /// <summary>Lazily loaded shared system instance.</summary>
        public static CelestialSystem Default
        {
            get
            {
                if (_default == null) _default = LoadFromResources();
                return _default;
            }
        }

        public static CelestialSystem LoadFromResources(string path = "Data/system")
        {
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null) throw new Exception("Missing system definition at Resources/" + path);
            return FromJson(ta.text);
        }

        public static CelestialSystem FromJson(string json)
        {
            var def = JsonConvert.DeserializeObject<SystemDefinition>(json);
            return new CelestialSystem(def);
        }

        public CelestialSystem(SystemDefinition def)
        {
            Def = def;
            foreach (var bd in def.bodies)
            {
                var b = new CelestialBody(bd);
                Bodies.Add(b);
                _byId[bd.id] = b;
            }
            // Link parents in dependency order (parents before children).
            var linked = new HashSet<string>();
            int guard = 0;
            while (linked.Count < Bodies.Count && guard++ < 100)
            {
                foreach (var b in Bodies)
                {
                    if (linked.Contains(b.Id)) continue;
                    if (string.IsNullOrEmpty(b.Def.parent))
                    {
                        b.Link(null, def.launchSite);
                        Root = b;
                        linked.Add(b.Id);
                    }
                    else if (linked.Contains(b.Def.parent))
                    {
                        b.Link(_byId[b.Def.parent], def.launchSite);
                        linked.Add(b.Id);
                    }
                }
            }
            var sd = def.sun?.direction ?? new double[] { 1, 0.2, -0.3 };
            SunDirection = new Vector3d(sd[0], sd[1], sd[2]).normalized;
        }

        public CelestialBody Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out var b)) return b;
            return null;
        }

        public CelestialBody LaunchBody => Get(Def.launchSite.body) ?? Root;

        /// <summary>
        /// Finds the innermost sphere of influence containing an absolute (root-relative) position.
        /// </summary>
        public CelestialBody FindSOI(Vector3d absolutePos, double ut)
        {
            CelestialBody best = Root;
            // Walk children depth-first.
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var c in best.Children)
                {
                    Vector3d rel = absolutePos - c.GetPositionAtUT(ut);
                    if (rel.magnitude < c.SOIRadius)
                    {
                        best = c;
                        changed = true;
                        break;
                    }
                }
            }
            return best;
        }
    }
}
