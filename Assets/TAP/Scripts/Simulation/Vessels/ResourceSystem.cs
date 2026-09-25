using System;
using System.Collections.Generic;
using TAP.Parts;

namespace TAP.Simulation
{
    /// <summary>
    /// Resource transfer inside one vessel, respecting each resource's flow mode:
    ///  Part   — only the requesting part (solid fuel, ablator)
    ///  Stack  — parts reachable without crossing crossfeed-blocking parts (liquid propellant)
    ///  Vessel — any part (monopropellant, electric charge)
    /// Draws are proportional across all containers in the flow group.
    /// </summary>
    public sealed class ResourceSystem
    {
        private readonly Vessel _vessel;
        private readonly Dictionary<Part, List<Part>> _stackGroups = new Dictionary<Part, List<Part>>();
        private bool _dirty = true;

        public ResourceSystem(Vessel vessel) { _vessel = vessel; }

        public void MarkDirty() { _dirty = true; _stackGroups.Clear(); }

        private List<Part> StackGroup(Part start)
        {
            if (_dirty) { _stackGroups.Clear(); _dirty = false; }
            if (_stackGroups.TryGetValue(start, out var g)) return g;
            g = new List<Part>();
            var seen = new HashSet<Part> { start };
            var q = new Queue<Part>();
            q.Enqueue(start);
            g.Add(start);
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                if (p != start && !p.Def.crossfeed) continue;
                void Visit(Part o)
                {
                    if (o == null || o.Destroyed || seen.Contains(o) || !o.Def.crossfeed) return;
                    seen.Add(o);
                    g.Add(o);
                    q.Enqueue(o);
                }
                Visit(p.ParentPart);
                for (int i = 0; i < p.Children.Count; i++) Visit(p.Children[i]);
            }
            _stackGroups[start] = g;
            return g;
        }

        private IList<Part> GroupFor(Part requester, ResourceFlow flow)
        {
            switch (flow)
            {
                case ResourceFlow.Part: return new[] { requester };
                case ResourceFlow.Stack: return StackGroup(requester);
                default: return _vessel.Parts;
            }
        }

        public double Available(Part requester, string resId)
        {
            var def = PartDatabase.Instance.GetResource(resId);
            if (def == null) return 0;
            double total = 0;
            var group = GroupFor(requester, def.flow);
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r != null) total += r.Amount;
            }
            return total;
        }

        public double Capacity(Part requester, string resId)
        {
            var def = PartDatabase.Instance.GetResource(resId);
            if (def == null) return 0;
            double total = 0;
            var group = GroupFor(requester, def.flow);
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r != null) total += r.Max;
            }
            return total;
        }

        /// <summary>Removes up to <paramref name="amount"/> units; returns the amount actually drawn.</summary>
        public double Request(Part requester, string resId, double amount)
        {
            if (amount <= 0) return 0;
            var def = PartDatabase.Instance.GetResource(resId);
            if (def == null) return 0;
            var group = GroupFor(requester, def.flow);
            double total = 0;
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r != null) total += r.Amount;
            }
            // Developer cheat: tanks the requester can reach supply without draining (empty ones too).
            if (resId == "Electric" ? DevCheats.InfiniteElectricity : DevCheats.InfinitePropellant)
            {
                double cap = 0;
                for (int i = 0; i < group.Count; i++)
                {
                    var r = group[i].GetResource(resId);
                    if (r != null) cap += r.Max;
                }
                if (cap > 0) return amount;
            }
            if (total <= 1e-12) return 0;
            double drawn = Math.Min(amount, total);
            double frac = drawn / total;
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r == null) continue;
                r.Amount -= r.Amount * frac;
                if (r.Amount < 1e-9) r.Amount = 0;
            }
            return drawn;
        }

        /// <summary>Adds up to <paramref name="amount"/> units (e.g. alternators); returns the amount stored.</summary>
        public double Produce(Part producer, string resId, double amount)
        {
            if (amount <= 0) return 0;
            var def = PartDatabase.Instance.GetResource(resId);
            if (def == null) return 0;
            var group = GroupFor(producer, def.flow);
            double space = 0;
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r != null) space += r.Max - r.Amount;
            }
            if (space <= 1e-12) return 0;
            double stored = Math.Min(space, amount);
            for (int i = 0; i < group.Count; i++)
            {
                var r = group[i].GetResource(resId);
                if (r == null) continue;
                double s = r.Max - r.Amount;
                r.Amount += stored * (s / space);
                if (r.Amount > r.Max) r.Amount = r.Max;
            }
            return stored;
        }

        /// <summary>Vessel-wide totals for UI readouts.</summary>
        public void Totals(string resId, out double amount, out double max)
        {
            amount = 0; max = 0;
            foreach (var p in _vessel.Parts)
            {
                var r = p.GetResource(resId);
                if (r == null) continue;
                amount += r.Amount;
                max += r.Max;
            }
        }
    }
}
