using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// A loaded, physically simulated vessel: one Rigidbody for the whole part tree with compound
    /// colliders. Internal loads, damage and separation are computed analytically from the
    /// rigid-body motion (see VesselPhysics / StructuralAnalysis).
    /// </summary>
    public sealed partial class Vessel : MonoBehaviour
    {
        public VesselRecord Record;
        public VesselHandle Handle;
        public string Id => Record.id;
        public string VesselName { get => Record.name; set => Record.name = value; }
        public VesselKind Kind { get => Record.kind; set => Record.kind = value; }

        public Rigidbody Rb { get; private set; }
        public Part RootPart;
        public readonly List<Part> Parts = new List<Part>();
        public int CurrentStage;
        public readonly VesselControl Ctrl = new VesselControl();
        public readonly AttitudeController Attitude = new AttitudeController();
        public ResourceSystem Resources { get; private set; }

        public FlightSim Sim => FlightSim.Instance;
        public bool IsActive => Sim != null && Sim.ActiveVessel == this;
        public bool IsEva => Kind == VesselKind.EVA;
        public bool IsFlag => Kind == VesselKind.Flag;

        /// <summary>UT when this vessel was loaded into physics; impacts are ignored briefly after load.</summary>
        public double LoadedUT;
        /// <summary>Until this UT a vessel loaded on the ground is never pinned to an analytic (free-fall) orbit.</summary>
        public double SettleUntilUT;
        /// <summary>Held on the launch pad (kinematic, carried by the rotating surface) until the first stage fires.</summary>
        public bool PadHold;
        public bool ImpactProtected => Sim != null && Sim.UT - LoadedUT < 1.5;

        private readonly List<Part> _pendingDestroy = new List<Part>();
        private readonly List<string> _pendingDestroyReasons = new List<string>();
        private readonly List<Part> _pendingDecouple = new List<Part>();
        private readonly List<Part> _pendingJointFailures = new List<Part>();
        private readonly List<string> _pendingJointReasons = new List<string>();
        private bool _structureDirty;

        // ------------------------------------------------------------------ construction

        public static Vessel Create(VesselRecord rec, PartDatabase db, Vector3 rootWorldPos, Quaternion rootWorldRot)
        {
            var go = new GameObject("Vessel: " + rec.name);
            go.transform.SetPositionAndRotation(rootWorldPos, rootWorldRot);
            var v = go.AddComponent<Vessel>();
            v.Record = rec;
            v.Resources = new ResourceSystem(v);
            v.Rb = go.AddComponent<Rigidbody>();
            v.ConfigureRigidbody();

            var parts = new Part[rec.parts.Count];
            for (int i = 0; i < rec.parts.Count; i++)
            {
                var pr = rec.parts[i];
                var def = db.Get(pr.partId);
                if (def == null)
                {
                    Debug.LogError($"Unknown part '{pr.partId}' in vessel {rec.name}");
                    continue;
                }
                int layer = rec.kind == VesselKind.EVA ? Layers.EVA : Layers.Parts;
                var pgo = PartModelFactory.Build(def, layer);
                pgo.name = def.id;
                pgo.transform.SetParent(go.transform, false);
                pgo.transform.localPosition = ToV3(pr.pos);
                pgo.transform.localRotation = ToQ(pr.rot);
                var part = pgo.AddComponent<Part>();
                part.Def = def;
                part.Vessel = v;
                part.Uid = pr.uid;
                part.AttachNode = pr.attachNode;
                part.ParentNode = pr.parentNode;
                part.Stage = pr.stage;
                part.SymmetryGroup = pr.symmetryGroup;
                part.SkinTemp = pr.skinTemp;
                part.InternalTemp = pr.internalTemp;
                if (pr.settings != null) part.Settings = new Dictionary<string, double>(pr.settings);
                if (pr.crew != null) part.Crew.AddRange(pr.crew);
                // Resources: record values override definition defaults.
                if (pr.resources != null && pr.resources.Count > 0)
                {
                    foreach (var rr in pr.resources)
                    {
                        var rd = db.GetResource(rr.id);
                        if (rd != null) part.Resources.Add(new PartResource(rd, rr.amount, rr.max));
                    }
                }
                else
                {
                    foreach (var ra in def.resources)
                    {
                        var rd = db.GetResource(ra.id);
                        if (rd != null) part.Resources.Add(new PartResource(rd, ra.amount, ra.Max));
                    }
                }
                PartModuleFactory.AddModules(part);
                part.InitVisuals();
                parts[i] = part;
            }
            for (int i = 0; i < rec.parts.Count; i++)
            {
                var p = parts[i];
                if (p == null) continue;
                int par = rec.parts[i].parent;
                if (par >= 0 && par < parts.Length && parts[par] != null)
                {
                    p.ParentPart = parts[par];
                    parts[par].Children.Add(p);
                }
                v.Parts.Add(p);
            }
            v.RootPart = parts[Mathf.Clamp(rec.rootIndex, 0, parts.Length - 1)];
            v.CurrentStage = rec.currentStage;
            v.Ctrl.FromRecord(rec.control);
            // Module state
            for (int i = 0; i < rec.parts.Count; i++)
            {
                var p = parts[i];
                if (p == null) continue;
                var mods = rec.parts[i].modules;
                foreach (var m in p.Modules)
                    if (mods != null && mods.TryGetValue(m.ModuleName, out var st)) m.Load(st);
            }
            foreach (var p in v.Parts) foreach (var m in p.Modules) m.OnInit();
            v.UpdateMassProperties();
            return v;
        }

        private void ConfigureRigidbody()
        {
            Rb.useGravity = false;
            Rb.interpolation = RigidbodyInterpolation.Interpolate;
            Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Rb.maxAngularVelocity = 40f;
            Rb.maxDepenetrationVelocity = 2f;
            Rb.linearDamping = 0;
            Rb.angularDamping = 0;
            Rb.automaticCenterOfMass = false;
            Rb.automaticInertiaTensor = false;
            Rb.sleepThreshold = 0f;
            Rb.solverIterations = 12;
            Rb.solverVelocityIterations = 4;
        }

        public static Vector3 ToV3(float[] a) => a != null && a.Length >= 3 ? new Vector3(a[0], a[1], a[2]) : Vector3.zero;
        public static Quaternion ToQ(float[] a) => a != null && a.Length >= 4 ? new Quaternion(a[0], a[1], a[2], a[3]) : Quaternion.identity;
        public static float[] FromV3(Vector3 v) => new[] { v.x, v.y, v.z };
        public static float[] FromQ(Quaternion q) => new[] { q.x, q.y, q.z, q.w };

        // ------------------------------------------------------------------ control

        /// <summary>Part the vessel is controlled from (defines navball/control orientation).</summary>
        public Part ControlPart
        {
            get
            {
                Part best = null;
                foreach (var p in Parts)
                {
                    if (p.Def.command == null) continue;
                    if (p.Def.command.requiresCrew && p.Crew.Count == 0) { if (best == null) best = p; continue; }
                    return p;
                }
                return best ?? RootPart;
            }
        }

        public Quaternion ControlRotation => ControlPart != null ? ControlPart.transform.rotation : transform.rotation;

        /// <summary>True if the vessel can be commanded (crewed pod or powered probe core, or EVA).</summary>
        public bool HasControl
        {
            get
            {
                if (IsEva) return true;
                foreach (var p in Parts)
                {
                    var c = p.Def.command;
                    if (c == null) continue;
                    if (c.requiresCrew && p.Crew.Count > 0) return true;
                    if (!c.requiresCrew)
                    {
                        Resources.Totals("Electric", out double ec, out _);
                        if (ec > 0.01) return true;
                    }
                }
                return false;
            }
        }

        public bool HasSas
        {
            get
            {
                foreach (var p in Parts) if (p.Def.command != null && p.Def.command.sas) return true;
                return false;
            }
        }

        public int CrewCount
        {
            get { int n = 0; foreach (var p in Parts) n += p.Crew.Count; return n; }
        }

        public int CrewCapacity
        {
            get { int n = 0; foreach (var p in Parts) n += p.SeatCount; return n; }
        }

        public IEnumerable<(Part part, string name)> AllCrew()
        {
            foreach (var p in Parts)
                foreach (var c in p.Crew) yield return (p, c);
        }

        // ------------------------------------------------------------------ staging

        public int MaxStage
        {
            get { int m = -1; foreach (var p in Parts) if (p.Stage > m) m = p.Stage; return m; }
        }

        /// <summary>Activates the current stage (Space). Returns false if nothing is left to stage.</summary>
        public bool ActivateNextStage()
        {
            if (!HasControl && !IsEva) return false;
            // Skip empty stages.
            while (CurrentStage >= 0 && !StageHasParts(CurrentStage)) CurrentStage--;
            if (CurrentStage < 0) return false;
            int s = CurrentStage;
            var list = new List<Part>();
            foreach (var p in Parts) if (p.Stage == s) list.Add(p);
            foreach (var p in list)
                foreach (var m in p.Modules)
                    if (m.IsStageable) m.OnActivate();
            CurrentStage = s - 1;
            Sim?.Log($"Stage {s} activated ({list.Count} part{(list.Count == 1 ? "" : "s")})");
            if (Record.situation == Situation.Prelaunch && Record.launchUT < 0)
            {
                Record.launchUT = Sim != null ? Sim.UT : 0;
                Sim?.ReleasePadHold(this);
                Sim?.Log($"Liftoff of {VesselName}!", true);
            }
            return true;
        }

        public bool StageHasParts(int s)
        {
            foreach (var p in Parts) if (p.Stage == s) return true;
            return false;
        }

        // ------------------------------------------------------------------ structure changes (queued)

        public void QueueDecouple(Part decouplerPart)
        {
            if (!_pendingDecouple.Contains(decouplerPart)) _pendingDecouple.Add(decouplerPart);
        }

        public void QueueDestroy(Part p, string reason)
        {
            if (p == null || p.Destroyed || _pendingDestroy.Contains(p)) return;
            _pendingDestroy.Add(p);
            _pendingDestroyReasons.Add(reason);
        }

        public void QueueJointFailure(Part p, string reason)
        {
            if (p == null || p.Destroyed || p == RootPart || _pendingJointFailures.Contains(p) || DevCheats.UnbreakableJoints) return;
            _pendingJointFailures.Add(p);
            _pendingJointReasons.Add(reason);
        }

        public bool HasPendingStructureEvents => _pendingDecouple.Count > 0 || _pendingDestroy.Count > 0 || _pendingJointFailures.Count > 0;

        /// <summary>Processes queued decouples/destructions. New vessels are registered with the sim.</summary>
        public void ProcessStructureEvents()
        {
            if (_pendingDecouple.Count > 0)
            {
                var list = new List<Part>(_pendingDecouple);
                _pendingDecouple.Clear();
                foreach (var d in list)
                {
                    if (d == null || d.Destroyed || d.Vessel == null) continue;
                    d.Vessel.ExecuteDecouple(d);
                }
            }
            if (_pendingJointFailures.Count > 0)
            {
                var list = new List<Part>(_pendingJointFailures);
                var reasons = new List<string>(_pendingJointReasons);
                _pendingJointFailures.Clear();
                _pendingJointReasons.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (p == null || p.Destroyed || p.Vessel == null || p.ParentPart == null) continue;
                    Sim?.Log(reasons[i], true);
                    Sim?.Effects?.SpawnSeparation(p.JointWorldPosition, p.JointAxisTowardsParent, p.Def.diameter * 0.5f);
                    p.SmoothedJointLoad = 0;
                    p.Vessel.DetachSubtree(p, true);
                }
            }
            if (_pendingDestroy.Count > 0)
            {
                var list = new List<Part>(_pendingDestroy);
                var reasons = new List<string>(_pendingDestroyReasons);
                _pendingDestroy.Clear();
                _pendingDestroyReasons.Clear();
                for (int i = 0; i < list.Count; i++)
                {
                    var p = list[i];
                    if (p == null || p.Destroyed || p.Vessel == null) continue;
                    p.Vessel.ExplodePart(p, reasons[i]);
                }
            }
        }

        public static bool DebugSeparation;

        private void ExecuteDecouple(Part d)
        {
            var dec = d.Def.decoupler;
            if (dec == null) return;
            string node = dec.explosiveNode ?? "top";
            Part subRoot = null;
            if (d.ParentPart != null && (d.AttachNode == node || (node == "srf" && d.AttachNode == "srf")))
                subRoot = d;
            else
            {
                foreach (var c in d.Children)
                    if (c.ParentNode == node) { subRoot = c; break; }
                if (subRoot == null && d.ParentPart != null) subRoot = d;
            }
            if (subRoot == null) return;

            Vector3 jointPos = subRoot.JointWorldPosition;
            Vector3 axisToParent = subRoot.JointAxisTowardsParent;
            Vector3 vBefore = Rb.linearVelocity;
            var parentPart = subRoot.ParentPart;
            var nv = DetachSubtree(subRoot, true);
            if (nv == null) return;
            if (DebugSeparation)
                Debug.Log($"[SepDebug] {VesselName}: decouple {d.Def.id} axis {axisToParent:F2} joint {jointPos:F2} com {WorldCoM:F2} vBefore {vBefore:F3} vAfterSplit {Rb.linearVelocity:F3} nv {nv.VesselName} v {nv.Rb.linearVelocity:F3} massCore {Rb.mass:F0} massNv {nv.Rb.mass:F0}");
            // Equal and opposite separation impulse along the joint axis.
            float J = (float)dec.ejectionImpulse;
            QueueImpulse(parentPart, axisToParent * J, jointPos);
            nv.QueueImpulse(subRoot, -axisToParent * J, jointPos);
            Sim?.Effects?.SpawnSeparation(jointPos, -axisToParent, d.Def.diameter);
        }

        /// <summary>
        /// Splits the subtree rooted at <paramref name="subRoot"/> off into a new vessel which inherits
        /// the local velocity of the old rigid body (v + w x r) and its angular velocity.
        /// </summary>
        public Vessel DetachSubtree(Part subRoot, bool ignoreCollisionBriefly)
        {
            if (subRoot == null || subRoot == RootPart || subRoot.Vessel != this) return null;
            Vector3 com0 = WorldCoM;
            Vector3 v0 = Rb.linearVelocity;
            Vector3 w0 = Rb.angularVelocity;

            subRoot.ParentPart?.Children.Remove(subRoot);
            subRoot.ParentPart = null;
            var moved = new List<Part>();
            subRoot.CollectSubtree(moved);
            foreach (var p in moved) Parts.Remove(p);

            var nv = Sim.CreateVesselFromLiveParts(this, moved, subRoot);
            nv.Rb.linearVelocity = v0 + Vector3.Cross(w0, nv.WorldCoM - com0);
            nv.Rb.angularVelocity = w0;

            OnStructureChanged();
            Rb.linearVelocity = v0 + Vector3.Cross(w0, WorldCoM - com0);
            Rb.angularVelocity = w0;
            // Both halves must integrate this step (separation impulses, new mass): a coasting vessel would
            // otherwise stay pinned to its old analytic orbit and silently discard the impulse.
            ForceUnlockThisStep = true;
            nv.ForceUnlockThisStep = true;

            if (ignoreCollisionBriefly) Sim.IgnoreCollisions(this, nv, 0.5f); // long enough to clear the initial overlap, short enough that tumbling debris can still hit
            return nv;
        }

        /// <summary>Destroys a part with an explosion; children become separate debris vessels.</summary>
        public void ExplodePart(Part p, string reason)
        {
            if (p.Destroyed) return;
            Sim?.Effects?.SpawnExplosion(p.transform.position, p.Def.diameter, Rb != null ? Rb.GetPointVelocity(p.transform.position) : Vector3.zero);
            foreach (var crew in p.Crew) Sim?.OnCrewLost(crew, this, reason);
            p.Crew.Clear();
            Sim?.Log($"{p.Def.title} destroyed: {reason}", true);
            RemovePart(p);
        }

        public void RemovePart(Part p)
        {
            if (p.Destroyed) return;
            Vector3 com0 = WorldCoM;
            Vector3 v0 = Rb.linearVelocity;
            Vector3 w0 = Rb.angularVelocity;
            var kids = new List<Part>(p.Children);
            foreach (var c in kids)
            {
                p.Children.Remove(c);
                c.ParentPart = null;
                var moved = new List<Part>();
                c.CollectSubtree(moved);
                foreach (var m in moved) Parts.Remove(m);
                var nv = Sim.CreateVesselFromLiveParts(this, moved, c);
                nv.Rb.linearVelocity = v0 + Vector3.Cross(w0, nv.WorldCoM - com0);
                nv.Rb.angularVelocity = w0;
                nv.ForceUnlockThisStep = true;
            }
            ForceUnlockThisStep = true;
            p.ParentPart?.Children.Remove(p);
            p.ParentPart = null;
            Parts.Remove(p);
            p.Destroyed = true;
            foreach (var m in p.Modules) m.OnPartDestroyed();
            p.transform.SetParent(null);
            p.gameObject.SetActive(false); Destroy(p.gameObject);

            if (Parts.Count == 0 || p == RootPart)
            {
                RootPart = null;
                Sim.OnVesselEmptied(this);
                return;
            }
            OnStructureChanged();
            Rb.linearVelocity = v0 + Vector3.Cross(w0, WorldCoM - com0);
            Rb.angularVelocity = w0;
        }

        /// <summary>Recomputes caches after parts were added/removed.</summary>
        public void OnStructureChanged()
        {
            Resources.MarkDirty();
            _structureDirty = true;
            UpdateMassProperties();
            foreach (var p in Parts) foreach (var m in p.Modules) m.OnVesselChanged();
            // Update kind: debris if no command part left.
            if (Kind == VesselKind.Ship)
            {
                bool hasCommand = false;
                foreach (var p in Parts) if (p.Def.command != null) hasCommand = true;
                if (!hasCommand) Kind = VesselKind.Debris;
            }
            if (CurrentStage > MaxStage) CurrentStage = MaxStage;
        }

        /// <summary>Adopts parts from another vessel (docking). Re-parents the other vessel's tree under <paramref name="attachTo"/>.</summary>
        public void AbsorbVessel(Vessel other, Part otherRootForAttach, Part attachTo, string attachNodeOnOther, string nodeOnThis)
        {
            // Re-root the other vessel's tree at otherRootForAttach.
            other.ReRoot(otherRootForAttach);
            Vector3 com0 = WorldCoM;
            double m0 = Rb.mass, m1 = other.Rb.mass;
            Vector3 p0 = Rb.linearVelocity * (float)m0 + other.Rb.linearVelocity * (float)m1;
            Vector3 vNew = p0 / (float)(m0 + m1);
            Vector3 wNew = (Rb.angularVelocity * (float)m0 + other.Rb.angularVelocity * (float)m1) / (float)(m0 + m1);

            var moved = new List<Part>(other.Parts);
            foreach (var p in moved)
            {
                p.transform.SetParent(transform, true);
                p.Vessel = this;
                Parts.Add(p);
            }
            otherRootForAttach.ParentPart = attachTo;
            otherRootForAttach.AttachNode = attachNodeOnOther;
            otherRootForAttach.ParentNode = nodeOnThis;
            attachTo.Children.Add(otherRootForAttach);
            // Staging of absorbed parts is appended below the current stage numbering.
            other.Parts.Clear();
            other.RootPart = null;
            Sim.RemoveVesselAfterMerge(other);
            OnStructureChanged();
            Rb.linearVelocity = vNew;
            Rb.angularVelocity = wNew;
        }

        /// <summary>Makes <paramref name="newRoot"/> the root, reversing parent links along the path.</summary>
        public void ReRoot(Part newRoot)
        {
            if (newRoot == RootPart || newRoot == null) return;
            // Collect path from newRoot to current root.
            var path = new List<Part>();
            var cur = newRoot;
            while (cur != null) { path.Add(cur); cur = cur.ParentPart; }
            // Reverse links pairwise from the root end.
            for (int i = path.Count - 1; i > 0; i--)
            {
                Part parent = path[i];      // old parent
                Part child = path[i - 1];   // old child becomes parent
                parent.Children.Remove(child);
                child.Children.Add(parent);
                parent.ParentPart = child;
                // swap node names: the old parent's attach is the old child's parent node
                string childAttach = child.AttachNode, childParentNode = child.ParentNode;
                parent.AttachNode = childParentNode;
                parent.ParentNode = childAttach;
            }
            newRoot.ParentPart = null;
            newRoot.AttachNode = null;
            newRoot.ParentNode = null;
            RootPart = newRoot;
        }

        // ------------------------------------------------------------------ crew

        public Part FindSeatFor()
        {
            foreach (var p in Parts) if (p.HasFreeSeat) return p;
            return null;
        }

        public Part PartWithCrew(string crewName)
        {
            foreach (var p in Parts) if (p.Crew.Contains(crewName)) return p;
            return null;
        }

        private void OnDestroy()
        {
            if (Sim != null) Sim.NotifyVesselObjectDestroyed(this);
        }
    }
}
