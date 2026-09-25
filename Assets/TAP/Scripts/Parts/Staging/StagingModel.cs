using System;
using System.Collections.Generic;
using TAP.Core;

namespace TAP.Parts
{
    /// <summary>Minimal part-tree representation used for staging and delta-v analysis (editor and flight).</summary>
    public class StageSimPart
    {
        public PartDefinition Def;
        public int Parent = -1;
        /// <summary>Node on this part that connects to the parent ("srf" = surface attached).</summary>
        public string AttachNode;
        /// <summary>Node on the parent this part connects to.</summary>
        public string ParentNode;
        public int Stage = -1;
        public readonly Dictionary<string, double> Resources = new Dictionary<string, double>();
        public bool EngineIgnited;
        public bool EngineShutdown;
        public float ThrustLimit = 1f;
        public bool Decoupled;
        public List<int> Children = new List<int>();
    }

    public class StageDeltaV
    {
        public int Stage;
        public double DeltaVVac, DeltaVAsl, DeltaVCurrent;
        public double BurnTime;
        public double StartMass, EndMass;
        public double ThrustVac, ThrustAsl;
        public double TwrAsl, TwrVac;
        public bool HasEngines;
        public int PartCount;
    }

    /// <summary>Tree helpers shared by staging and delta-v code.</summary>
    public static class PartTree
    {
        public static void LinkChildren(IList<StageSimPart> parts)
        {
            foreach (var p in parts) p.Children.Clear();
            for (int i = 0; i < parts.Count; i++)
                if (parts[i].Parent >= 0) parts[parts[i].Parent].Children.Add(i);
        }

        public static void CollectSubtree(IList<StageSimPart> parts, int root, ICollection<int> result)
        {
            var stack = new Stack<int>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                result.Add(i);
                foreach (int c in parts[i].Children) stack.Push(c);
            }
        }

        /// <summary>Parts jettisoned when decoupler <paramref name="d"/> fires.</summary>
        public static List<int> DecouplerJettisonSet(IList<StageSimPart> parts, int d)
        {
            var result = new List<int>();
            var def = parts[d].Def.decoupler;
            if (def == null) return result;
            string node = def.explosiveNode ?? "top";
            var dp = parts[d];
            bool severParent = dp.Parent >= 0 && (dp.AttachNode == node || (node == "srf" && dp.AttachNode == "srf"));
            if (severParent)
            {
                CollectSubtree(parts, d, result);
                return result;
            }
            foreach (int c in dp.Children)
            {
                if (parts[c].ParentNode == node)
                {
                    CollectSubtree(parts, c, result);
                    return result;
                }
            }
            // Nothing attached at the explosive node: decoupler detaches from its parent if not root.
            if (dp.Parent >= 0) CollectSubtree(parts, d, result);
            return result;
        }

        /// <summary>
        /// Parts reachable from <paramref name="start"/> without entering parts that block crossfeed
        /// (decouplers). The start part is always included.
        /// </summary>
        public static HashSet<int> CrossfeedGroup(IList<StageSimPart> parts, int start, ICollection<int> attached)
        {
            var group = new HashSet<int> { start };
            var q = new Queue<int>();
            q.Enqueue(start);
            while (q.Count > 0)
            {
                int i = q.Dequeue();
                var p = parts[i];
                if (i != start && !p.Def.crossfeed) continue;
                void Visit(int j)
                {
                    if (j < 0 || group.Contains(j) || (attached != null && !attached.Contains(j))) return;
                    if (!parts[j].Def.crossfeed) return;
                    group.Add(j);
                    q.Enqueue(j);
                }
                Visit(p.Parent);
                foreach (int c in p.Children) Visit(c);
            }
            return group;
        }
    }

    /// <summary>KSP-style automatic staging: engines of the deepest serial stage fire first.</summary>
    public static class StagingPlanner
    {
        public static int SerialDepth(IList<StageSimPart> parts, int i)
        {
            int depth = 0;
            int cur = i;
            while (cur >= 0)
            {
                var d = parts[cur].Def.decoupler;
                if (d != null && d.kind != "radial") depth++;
                cur = parts[cur].Parent;
            }
            return depth;
        }

        public static int ParallelDepth(IList<StageSimPart> parts, int i)
        {
            int depth = 0;
            int cur = i;
            while (cur >= 0)
            {
                var d = parts[cur].Def.decoupler;
                if (d != null && d.kind == "radial") depth++;
                cur = parts[cur].Parent;
            }
            return depth;
        }

        /// <summary>Assigns inverse stage numbers to all stageable parts. Returns the number of stages.</summary>
        public static int AutoStage(IList<StageSimPart> parts)
        {
            PartTree.LinkChildren(parts);
            int n = parts.Count;
            var serial = new int[n];
            var parallel = new int[n];
            int maxSerial = 0;
            for (int i = 0; i < n; i++)
            {
                serial[i] = SerialDepth(parts, i);
                parallel[i] = ParallelDepth(parts, i);
                maxSerial = Math.Max(maxSerial, serial[i]);
                parts[i].Stage = -1;
            }

            // Ordered list of stage groups (firing order); converted to inverse numbering at the end.
            var groups = new List<List<int>>();
            for (int d = maxSerial; d >= 0; d--)
            {
                var ignite = new List<int>();
                // Stack decouplers that separate depth d+1 fire together with depth d engines (hot staging).
                for (int i = 0; i < n; i++)
                {
                    var dec = parts[i].Def.decoupler;
                    if (dec != null && dec.kind != "radial" && serial[i] == d + 1) ignite.Add(i);
                }
                for (int i = 0; i < n; i++)
                    if (parts[i].Def.engine != null && serial[i] == d) ignite.Add(i);
                if (ignite.Count > 0) groups.Add(ignite);

                // Radial decouplers at this serial depth, deepest parallel level first.
                int maxPar = 0;
                for (int i = 0; i < n; i++)
                    if (parts[i].Def.decoupler?.kind == "radial" && serial[i] == d) maxPar = Math.Max(maxPar, parallel[i]);
                for (int p = maxPar; p >= 1; p--)
                {
                    var rad = new List<int>();
                    for (int i = 0; i < n; i++)
                        if (parts[i].Def.decoupler?.kind == "radial" && serial[i] == d && parallel[i] == p) rad.Add(i);
                    if (rad.Count > 0) groups.Add(rad);
                }
            }
            // Top-level stack decouplers not yet assigned (e.g. a decoupler with nothing below) at depth 0+1 handled above.
            var chutes = new List<int>();
            for (int i = 0; i < n; i++)
                if (parts[i].Def.parachute != null) chutes.Add(i);
            if (chutes.Count > 0) groups.Add(chutes);

            int total = groups.Count;
            for (int g = 0; g < total; g++)
                foreach (int i in groups[g]) parts[i].Stage = total - 1 - g;
            return total;
        }

        /// <summary>Removes gaps so stage numbers are contiguous (0..n-1), preserving order.</summary>
        public static int Compact(IList<StageSimPart> parts)
        {
            var used = new SortedSet<int>();
            foreach (var p in parts) if (p.Stage >= 0) used.Add(p.Stage);
            var map = new Dictionary<int, int>();
            int k = 0;
            foreach (int s in used) map[s] = k++;
            foreach (var p in parts) if (p.Stage >= 0) p.Stage = map[p.Stage];
            return k;
        }
    }

    /// <summary>Per-stage delta-v / TWR / burn-time estimates by simulating staging and fuel flow.</summary>
    public static class DeltaVCalculator
    {
        private class EngineRun
        {
            public int Part;
            public EngineDefinition Def;
            public double MdotFull;
            public HashSet<int> Group;
        }

        public static List<StageDeltaV> Compute(IList<StageSimPart> source, int currentStage, double gravity, double pressureAtm, PartDatabase db)
        {
            // Work on a copy of resources/flags.
            int n = source.Count;
            var parts = new List<StageSimPart>(n);
            foreach (var s in source)
            {
                var c = new StageSimPart
                {
                    Def = s.Def, Parent = s.Parent, AttachNode = s.AttachNode, ParentNode = s.ParentNode,
                    Stage = s.Stage, EngineIgnited = s.EngineIgnited, EngineShutdown = s.EngineShutdown, ThrustLimit = s.ThrustLimit,
                };
                foreach (var kv in s.Resources) c.Resources[kv.Key] = kv.Value;
                parts.Add(c);
            }
            PartTree.LinkChildren(parts);

            var attached = new HashSet<int>();
            for (int i = 0; i < n; i++) if (!source[i].Decoupled) attached.Add(i);
            var ignited = new HashSet<int>();
            for (int i = 0; i < n; i++) if (parts[i].EngineIgnited && !parts[i].EngineShutdown && parts[i].Def.engine != null) ignited.Add(i);

            var result = new List<StageDeltaV>();
            for (int s = currentStage; s >= 0; s--)
            {
                // Activate stage s
                foreach (int i in new List<int>(attached))
                {
                    if (parts[i].Stage != s) continue;
                    if (parts[i].Def.decoupler != null)
                    {
                        var jet = PartTree.DecouplerJettisonSet(parts, i);
                        foreach (int j in jet) { attached.Remove(j); ignited.Remove(j); }
                    }
                }
                foreach (int i in attached)
                    if (parts[i].Stage == s && parts[i].Def.engine != null) ignited.Add(i);

                // Which engines will be jettisoned by the next stage's decouplers?
                var nextJettison = new HashSet<int>();
                if (s - 1 >= 0)
                    foreach (int i in attached)
                        if (parts[i].Stage == s - 1 && parts[i].Def.decoupler != null)
                            foreach (int j in PartTree.DecouplerJettisonSet(parts, i)) nextJettison.Add(j);

                var info = new StageDeltaV { Stage = s, StartMass = Mass(parts, attached, db) };
                int engCount = 0;
                foreach (int i in ignited) if (attached.Contains(i)) engCount++;
                info.HasEngines = engCount > 0;
                info.PartCount = attached.Count;

                double dvVac = 0, dvAsl = 0, dvCur = 0, time = 0;
                bool first = true;
                for (int iter = 0; iter < 64; iter++)
                {
                    var runs = new List<EngineRun>();
                    foreach (int i in ignited)
                    {
                        if (!attached.Contains(i)) continue;
                        var e = parts[i].Def.engine;
                        HashSet<int> group = e.type == "solid" || db.GetResource(e.propellant)?.flow == ResourceFlow.Part
                            ? new HashSet<int> { i }
                            : PartTree.CrossfeedGroup(parts, i, attached);
                        if (GroupAmount(parts, group, e.propellant) <= 1e-6) continue;
                        double limit = parts[i].ThrustLimit;
                        double mdot = e.thrustVac / (e.ispVac * MathD.G0) * limit;
                        runs.Add(new EngineRun { Part = i, Def = e, MdotFull = mdot, Group = group });
                    }
                    if (runs.Count == 0) break;

                    // Stage end condition: if next stage jettisons active engines, stop when all of those are dry.
                    bool anyJettisonedActive = false;
                    foreach (var r in runs) if (nextJettison.Contains(r.Part)) anyJettisonedActive = true;
                    if (!first && nextJettison.Count > 0 && !anyJettisonedActive && HasJettisonedEngine(parts, nextJettison))
                        break;
                    first = false;

                    // Consumption per (group, resource)
                    double thrustVac = 0, thrustAsl = 0, thrustCur = 0, mdotTotal = 0;
                    var groupRates = new Dictionary<HashSet<int>, (string res, double rate)>();
                    foreach (var r in runs)
                    {
                        thrustVac += r.MdotFull * r.Def.ispVac * MathD.G0;
                        thrustAsl += r.MdotFull * r.Def.ispAsl * MathD.G0;
                        double ispCur = Math.Max(r.Def.ispVac + (r.Def.ispAsl - r.Def.ispVac) * pressureAtm, 1);
                        thrustCur += r.MdotFull * ispCur * MathD.G0;
                        mdotTotal += r.MdotFull;
                        HashSet<int> key = null;
                        foreach (var k in groupRates.Keys) if (k.SetEquals(r.Group)) { key = k; break; }
                        if (key == null) groupRates[r.Group] = (r.Def.propellant, r.MdotFull);
                        else groupRates[key] = (groupRates[key].res, groupRates[key].rate + r.MdotFull);
                    }
                    if (info.ThrustVac <= 0) { info.ThrustVac = thrustVac; info.ThrustAsl = thrustAsl; }

                    double dt = double.MaxValue;
                    foreach (var kv in groupRates)
                    {
                        double amount = GroupAmount(parts, kv.Key, kv.Value.res) * (db.ResourceDensity(kv.Value.res));
                        if (kv.Value.rate > 0) dt = Math.Min(dt, amount / kv.Value.rate);
                    }
                    if (dt == double.MaxValue || dt <= 0) break;

                    double m0 = Mass(parts, attached, db);
                    double m1 = m0 - mdotTotal * dt;
                    if (m1 <= 0) break;
                    double ln = Math.Log(m0 / m1);
                    dvVac += thrustVac / mdotTotal * ln;
                    dvAsl += thrustAsl / mdotTotal * ln;
                    dvCur += thrustCur / mdotTotal * ln;
                    time += dt;

                    // Drain proportionally within each group.
                    foreach (var kv in groupRates)
                        Drain(parts, kv.Key, kv.Value.res, kv.Value.rate * dt / Math.Max(db.ResourceDensity(kv.Value.res), 1e-9));
                }
                info.DeltaVVac = dvVac;
                info.DeltaVAsl = dvAsl;
                info.DeltaVCurrent = dvCur;
                info.BurnTime = time;
                info.EndMass = Mass(parts, attached, db);
                info.TwrAsl = gravity > 0 && info.StartMass > 0 ? info.ThrustAsl / (info.StartMass * gravity) : 0;
                info.TwrVac = gravity > 0 && info.StartMass > 0 ? info.ThrustVac / (info.StartMass * gravity) : 0;
                result.Add(info);
            }
            return result;
        }

        private static bool HasJettisonedEngine(List<StageSimPart> parts, HashSet<int> set)
        {
            foreach (int i in set) if (parts[i].Def.engine != null) return true;
            return false;
        }

        private static double GroupAmount(List<StageSimPart> parts, HashSet<int> group, string res)
        {
            double a = 0;
            foreach (int i in group)
                if (parts[i].Resources.TryGetValue(res, out double v)) a += v;
            return a;
        }

        private static void Drain(List<StageSimPart> parts, HashSet<int> group, string res, double amount)
        {
            double total = GroupAmount(parts, group, res);
            if (total <= 0) return;
            double frac = Math.Min(1.0, amount / total);
            foreach (int i in group)
                if (parts[i].Resources.TryGetValue(res, out double v)) parts[i].Resources[res] = Math.Max(0, v - v * frac);
            // Guard against rounding leaving microscopic residue.
            if (frac >= 1.0 - 1e-9)
                foreach (int i in group)
                    if (parts[i].Resources.ContainsKey(res)) parts[i].Resources[res] = 0;
        }

        public static double Mass(IList<StageSimPart> parts, ICollection<int> attached, PartDatabase db)
        {
            double m = 0;
            foreach (int i in attached)
            {
                m += parts[i].Def.dryMass;
                foreach (var kv in parts[i].Resources) m += kv.Value * db.ResourceDensity(kv.Key);
            }
            return m;
        }
    }
}
