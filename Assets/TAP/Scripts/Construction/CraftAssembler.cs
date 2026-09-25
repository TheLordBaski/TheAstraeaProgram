using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Construction
{
    /// <summary>
    /// Pure attachment math and design operations shared by the assembly editor and generated starter craft.
    /// Positions/rotations are in root-part space (root at origin, identity rotation).
    /// </summary>
    public sealed class CraftAssembler
    {
        public readonly CraftDesign Design;
        public readonly PartDatabase Db;
        private int _nextUid = 1;
        private int _nextSym = 1;

        public CraftAssembler(CraftDesign design, PartDatabase db)
        {
            Design = design;
            Db = db;
            foreach (var p in design.parts)
            {
                _nextUid = Math.Max(_nextUid, p.uid + 1);
                _nextSym = Math.Max(_nextSym, p.symmetryGroup + 1);
            }
        }

        public static Vector3 V(float[] a) => new Vector3(a[0], a[1], a[2]);
        public static Quaternion Q(float[] a) => new Quaternion(a[0], a[1], a[2], a[3]);
        public static float[] A(Vector3 v) => new[] { v.x, v.y, v.z };
        public static float[] A(Quaternion q) => new[] { q.x, q.y, q.z, q.w };

        public PartDefinition Def(int index) => Db.Get(Design.parts[index].partId);

        public int AddRoot(string partId)
        {
            Design.parts.Clear();
            Design.parts.Add(new PartNodeRecord { uid = _nextUid++, partId = partId, parent = -1, pos = A(Vector3.zero), rot = A(Quaternion.identity) });
            return 0;
        }

        /// <summary>Pose a child would have when its node <paramref name="childNode"/> mates the parent's node.</summary>
        public bool StackPose(int parent, string parentNode, PartDefinition child, string childNode, out Vector3 pos, out Quaternion rot)
        {
            pos = Vector3.zero; rot = Quaternion.identity;
            var pdef = Def(parent);
            var pn = pdef?.FindNode(parentNode);
            var cn = child.FindNode(childNode);
            if (pn == null || cn == null) return false;
            var pp = Design.parts[parent];
            Quaternion rp = Q(pp.rot);
            Vector3 targetDir = -(rp * pn.Direction);
            rot = Quaternion.FromToRotation(rp * cn.Direction, targetDir) * rp;
            pos = V(pp.pos) + rp * pn.Position - rot * cn.Position;
            return true;
        }

        public int AttachStack(int parent, string parentNode, string partId, string childNode, int stage = -1)
        {
            var def = Db.Get(partId) ?? throw new ArgumentException("Unknown part " + partId);
            if (!StackPose(parent, parentNode, def, childNode, out var pos, out var rot))
                throw new ArgumentException($"Cannot attach {partId}.{childNode} to {Design.parts[parent].partId}.{parentNode}");
            Design.parts.Add(new PartNodeRecord
            {
                uid = _nextUid++, partId = partId, parent = parent, parentNode = parentNode, attachNode = childNode,
                pos = A(pos), rot = A(rot), stage = stage,
            });
            return Design.parts.Count - 1;
        }

        /// <summary>Pose for surface attachment at a parent-local point/normal (child's up aligned with parent's up).</summary>
        public static void SurfacePose(Vector3 parentPos, Quaternion parentRot, Vector3 localPoint, Vector3 localNormal, PartDefinition child,
            float extraRollDeg, out Vector3 pos, out Quaternion rot)
        {
            var sa = child.surfaceAttach;
            Vector3 n = localNormal.normalized;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, n);
            if (up.sqrMagnitude < 1e-4f) up = Vector3.ProjectOnPlane(Vector3.forward, n);
            Quaternion local = Quaternion.LookRotation(n, up.normalized);
            // Map the child's attach direction (+Z for all parts) onto the normal.
            Quaternion align = Quaternion.Inverse(Quaternion.LookRotation(sa.Direction, Vector3.up));
            Quaternion rollQ = Quaternion.AngleAxis(extraRollDeg, n);
            rot = parentRot * rollQ * local * align;
            pos = parentPos + parentRot * localPoint - rot * sa.Position;
        }

        /// <summary>
        /// Surface-attaches a part (and N-1 radially symmetric copies around the parent's axis) at
        /// parent-local height <paramref name="y"/> and angle <paramref name="angleDeg"/> (from +Z towards +X).
        /// Returns the indices of all created parts.
        /// </summary>
        public List<int> AttachRadial(int parent, string partId, float y, float angleDeg, int symmetry = 1, float radiusOverride = -1, int stage = -1)
        {
            var def = Db.Get(partId) ?? throw new ArgumentException("Unknown part " + partId);
            var pdef = Def(parent);
            var pp = Design.parts[parent];
            float r = radiusOverride > 0 ? radiusOverride : pdef.diameter * 0.5f;
            int group = symmetry > 1 ? _nextSym++ : -1;
            var result = new List<int>();
            for (int k = 0; k < symmetry; k++)
            {
                float a = (angleDeg + 360f * k / symmetry) * Mathf.Deg2Rad;
                Vector3 nrm = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                Vector3 pt = nrm * r + Vector3.up * y;
                SurfacePose(V(pp.pos), Q(pp.rot), pt, nrm, def, 0, out var pos, out var rot);
                Design.parts.Add(new PartNodeRecord
                {
                    uid = _nextUid++, partId = partId, parent = parent, parentNode = "srf", attachNode = "srf",
                    pos = A(pos), rot = A(rot), stage = stage, symmetryGroup = group,
                });
                result.Add(Design.parts.Count - 1);
            }
            return result;
        }

        /// <summary>Stack-attach below each part in a list (e.g. nose cones on radial boosters), mirroring symmetry.</summary>
        public List<int> AttachStackToAll(List<int> parents, string parentNode, string partId, string childNode)
        {
            var list = new List<int>();
            int group = parents.Count > 1 ? _nextSym++ : -1;
            foreach (int p in parents)
            {
                int i = AttachStack(p, parentNode, partId, childNode);
                Design.parts[i].symmetryGroup = group;
                list.Add(i);
            }
            return list;
        }

        public List<int> AttachRadialToAll(List<int> parents, string partId, float y, float angleDeg, float radiusOverride = -1)
        {
            // one child per parent, oriented outward from the vessel axis
            var list = new List<int>();
            int group = parents.Count > 1 ? _nextSym++ : -1;
            foreach (int pi in parents)
            {
                var pp = Design.parts[pi];
                Vector3 ppos = V(pp.pos);
                Vector3 outward = new Vector3(ppos.x, 0, ppos.z);
                if (outward.sqrMagnitude < 1e-4f) outward = Vector3.forward;
                outward.Normalize();
                // convert to parent-local
                Vector3 localOut = Quaternion.Inverse(Q(pp.rot)) * outward;
                float a = Mathf.Atan2(localOut.x, localOut.z) * Mathf.Rad2Deg + angleDeg;
                var created = AttachRadial(pi, partId, y, a, 1, radiusOverride);
                foreach (var c in created) Design.parts[c].symmetryGroup = group;
                list.AddRange(created);
            }
            return list;
        }

        // ------------------------------------------------------------------ analysis helpers

        public List<StageSimPart> ToStageModel()
        {
            var list = new List<StageSimPart>();
            foreach (var p in Design.parts)
            {
                var def = Db.Get(p.partId);
                var sp = new StageSimPart { Def = def, Parent = p.parent, AttachNode = p.attachNode, ParentNode = p.parentNode, Stage = p.stage };
                if (def?.engine != null) sp.ThrustLimit = def.engine.thrustLimit / 100f;
                if (p.settings != null && p.settings.TryGetValue("thrustLimit", out double tl)) sp.ThrustLimit = (float)(tl / 100.0);
                if (def != null) foreach (var r in def.resources) sp.Resources[r.id] = r.amount;
                list.Add(sp);
            }
            PartTree.LinkChildren(list);
            return list;
        }

        public void AutoStage()
        {
            var model = ToStageModel();
            StagingPlanner.AutoStage(model);
            for (int i = 0; i < model.Count; i++) Design.parts[i].stage = model[i].Stage;
        }

        public double TotalMass()
        {
            double m = 0;
            foreach (var p in Design.parts) { var d = Db.Get(p.partId); if (d != null) m += d.WetMass(Db); }
            return m;
        }

        public double DryMass()
        {
            double m = 0;
            foreach (var p in Design.parts) { var d = Db.Get(p.partId); if (d != null) m += d.dryMass; }
            return m;
        }

        public int CrewCapacity()
        {
            int n = 0;
            foreach (var p in Design.parts) { var d = Db.Get(p.partId); if (d?.crew != null) n += d.crew.seats; }
            return n;
        }

        public Vector3 CenterOfMass()
        {
            double m = 0; Vector3d s = Vector3d.zero;
            foreach (var p in Design.parts)
            {
                var d = Db.Get(p.partId);
                if (d == null) continue;
                double pm = d.WetMass(Db);
                m += pm;
                s += new Vector3d(p.pos[0], p.pos[1], p.pos[2]) * pm;
            }
            return m > 0 ? (Vector3)(s / m) : Vector3.zero;
        }

        public void ResourceTotals(string resId, out double amount)
        {
            amount = 0;
            foreach (var p in Design.parts)
            {
                var d = Db.Get(p.partId);
                if (d == null) continue;
                foreach (var r in d.resources) if (r.id == resId) amount += r.amount;
            }
        }

        /// <summary>Children indices of a part.</summary>
        public List<int> Children(int index)
        {
            var list = new List<int>();
            for (int i = 0; i < Design.parts.Count; i++) if (Design.parts[i].parent == index) list.Add(i);
            return list;
        }

        /// <summary>Removes a part and its whole subtree, compacting indices.</summary>
        public List<PartNodeRecord> RemoveSubtree(int index)
        {
            var remove = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(index);
            while (stack.Count > 0)
            {
                int i = stack.Pop();
                remove.Add(i);
                foreach (int c in Children(i)) stack.Push(c);
            }
            var removed = new List<PartNodeRecord>();
            var map = new Dictionary<int, int>();
            var kept = new List<PartNodeRecord>();
            for (int i = 0; i < Design.parts.Count; i++)
            {
                if (remove.Contains(i)) { removed.Add(Design.parts[i]); continue; }
                map[i] = kept.Count;
                kept.Add(Design.parts[i]);
            }
            foreach (var p in kept) if (p.parent >= 0) p.parent = map.TryGetValue(p.parent, out int np) ? np : -1;
            Design.parts.Clear();
            Design.parts.AddRange(kept);
            return removed;
        }
    }
}
