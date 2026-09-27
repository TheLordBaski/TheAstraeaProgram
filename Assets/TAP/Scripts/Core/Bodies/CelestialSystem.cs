using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// One star system (data-driven from a system file listed in the galaxy, e.g. Resources/Data/system.json). The root
    /// of the body hierarchy is the star; gameplay asks for <see cref="HomeBody"/> (where the launch site is) or
    /// <see cref="Star"/>, never for the root as "the planet".
    /// </summary>
    public sealed class CelestialSystem
    {
        public readonly SystemDefinition Def;
        /// <summary>System id in the galaxy ("home" for the home system).</summary>
        public readonly string Id;
        public readonly List<CelestialBody> Bodies = new List<CelestialBody>();
        /// <summary>Root of the body hierarchy: the star (or, in a system without a star body, its central planet).</summary>
        public CelestialBody Root { get; private set; }
        private readonly Dictionary<string, CelestialBody> _byId = new Dictionary<string, CelestialBody>();
        private readonly Vector3d _fixedSunDirection;

        private static CelestialSystem _default;

        /// <summary>The system this session plays in (see <see cref="Galaxy.SessionSystemId"/>), loaded once.</summary>
        public static CelestialSystem Default
        {
            get
            {
                if (_default == null) _default = Galaxy.Instance.LoadSystem(Galaxy.Instance.SessionSystemId);
                return _default;
            }
        }

        /// <summary>Forgets the loaded session system (the next <see cref="Default"/> loads it again).</summary>
        public static void ResetDefault() => _default = null;

        public static CelestialSystem LoadFromResources(string path = "Data/system", string id = null)
        {
            var ta = Resources.Load<TextAsset>(path);
            if (ta == null) throw new Exception("Missing system definition at Resources/" + path);
            if (id == null)
                foreach (var s in Galaxy.Instance.Systems)
                    if (s.file == path) { id = s.id; break; }
            return FromJson(ta.text, id ?? "home");
        }

        public static CelestialSystem FromJson(string json, string id = "home")
        {
            var def = JsonConvert.DeserializeObject<SystemDefinition>(json);
            return new CelestialSystem(def, id);
        }

        public CelestialSystem(SystemDefinition def, string id = "home")
        {
            Def = def;
            Id = id;
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
            _fixedSunDirection = new Vector3d(sd[0], sd[1], sd[2]).normalized;
        }

        /// <summary>A body by id: its plain id ("tellus") or qualified with this system's id ("home/tellus").</summary>
        public CelestialBody Get(string id)
        {
            if (id == null) return null;
            int slash = id.IndexOf('/');
            if (slash >= 0)
            {
                if (id.Substring(0, slash) != Id) return null;
                id = id.Substring(slash + 1);
            }
            return _byId.TryGetValue(id, out var b) ? b : null;
        }

        /// <summary>The star lighting this system (null in a system without a star body).</summary>
        public CelestialBody Star => Root != null && Root.IsStar ? Root : null;

        /// <summary>The body with the launch site: where new games start and vessels are recovered.</summary>
        public CelestialBody HomeBody
        {
            get
            {
                var b = Get(Def.launchSite.body);
                if (b != null) return b;
                foreach (var x in Bodies) if (!x.IsStar) return x;
                return Root;
            }
        }

        public CelestialBody LaunchBody => HomeBody;

        /// <summary>The home body's first moon (the target of the first mission), or null when it has none.</summary>
        public CelestialBody HomeMoon
        {
            get
            {
                var h = HomeBody;
                return h != null && h.Children.Count > 0 ? h.Children[0] : null;
            }
        }

        private Calendar _calendar;

        /// <summary>The clock and calendar of this system's home planet.</summary>
        public Calendar Calendar => _calendar ??= Calendar.Of(this);

        /// <summary>Unit direction towards the star from a position relative to the root (the star's centre).</summary>
        public Vector3d SunDirectionFrom(Vector3d rootRelative)
        {
            if (Star == null) return _fixedSunDirection;
            Vector3d d = Star.GetPositionAtUT(0) - rootRelative;
            double m = d.magnitude;
            return m > 1 ? d / m : _fixedSunDirection;
        }

        /// <summary>Unit direction towards the star from a position relative to <paramref name="body"/> at UT.</summary>
        public Vector3d SunDirectionFrom(CelestialBody body, Vector3d bodyRelative, double ut) =>
            SunDirectionFrom(body.GetPositionAtUT(ut) + bodyRelative);

        /// <summary>Solar flux (W/m²) at a position relative to the root; 0 without a star.</summary>
        public double SolarFlux(Vector3d rootRelative)
        {
            if (Star == null || Star.Luminosity <= 0) return 0;
            double r2 = Math.Max((Star.GetPositionAtUT(0) - rootRelative).sqrMagnitude, Star.Radius * Star.Radius);
            return Star.Luminosity / (4 * Math.PI * r2);
        }

        /// <summary>
        /// Finds the innermost sphere of influence containing a position relative to the root (the star).
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
