using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed partial class FlightSim
    {
        private readonly List<Action> _requests = new List<Action>();

        /// <summary>Requests are executed at the start of the next physics step (never mid-iteration).</summary>
        public void Enqueue(Action a) => _requests.Add(a);

        private void ProcessRequests()
        {
            if (_requests.Count == 0) return;
            var list = new List<Action>(_requests);
            _requests.Clear();
            foreach (var a in list)
            {
                try { a(); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        public void OnCrewLost(string crew, Vessel v, string reason)
        {
            Log($"{crew} was lost ({reason})", true);
            CrewLost?.Invoke(crew, reason);
        }

        // ------------------------------------------------------------------ EVA

        public string LastEvaError;

        public void RequestEva(Part part, string crewName)
        {
            // Leave warp properly: ExitRails puts every loaded vessel back into physics.
            StopWarp();
            Enqueue(() => DoEva(part, crewName));
        }

        public Vessel DoEva(Part part, string crewName)
        {
            LastEvaError = null;
            if (part == null || part.Vessel == null || !part.Crew.Contains(crewName)) { LastEvaError = "Crew not found"; return null; }
            var v = part.Vessel;
            var crewMod = part.GetModule<CrewModule>();
            Vector3 hatch = crewMod != null ? crewMod.HatchWorldPosition : part.transform.position;
            Vector3 normal = crewMod != null ? crewMod.HatchWorldNormal : part.transform.forward;
            Vector3 spawn = hatch + normal * 0.75f;
            // Obstruction check (ignore the EVA itself).
            if (Physics.CheckCapsule(spawn - Vector3.up * 0.3f, spawn + Vector3.up * 0.3f, 0.25f, 1 << Layers.Parts, QueryTriggerInteraction.Ignore))
            {
                // Try a bit further out.
                spawn = hatch + normal * 1.2f;
                if (Physics.CheckCapsule(spawn - Vector3.up * 0.3f, spawn + Vector3.up * 0.3f, 0.25f, 1 << Layers.Parts, QueryTriggerInteraction.Ignore))
                {
                    LastEvaError = "Hatch is obstructed";
                    Log($"Cannot EVA {crewName}: hatch obstructed", true);
                    return null;
                }
            }
            // EVA pack propellant comes from the vessel's monopropellant (up to the suit's capacity from the part
            // data, minimum 1 kg reserve pack); what is left goes back into the vessel on boarding.
            double packMax = 5.0;
            var suit = PartDb.Get("eva_crew");
            if (suit?.resources != null)
                foreach (var r in suit.resources) if (r.id == "Monoprop") packMax = r.max > 0 ? r.max : r.amount;
            double pack = v.Resources.Request(part, "Monoprop", packMax);
            if (pack < 1.0) pack = 1.0;

            var rec = new VesselRecord
            {
                name = crewName,
                kind = VesselKind.EVA,
                situation = v.Situation,
                evaCrew = crewName,
                launchUT = v.Record.launchUT,
            };
            var pr = new PartRecord { uid = 1, partId = "eva_crew", parent = -1 };
            pr.resources.Add(new ResourceRecord { id = "Monoprop", amount = pack, max = packMax });
            rec.parts.Add(pr);

            Vector3 g = v.GravityAccel;
            Vector3 up = g.sqrMagnitude > 1e-4f ? -g.normalized : part.transform.up;
            Vector3 fwd = Vector3.ProjectOnPlane(normal, up);
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.ProjectOnPlane(part.transform.forward, up);
            Quaternion rot = Quaternion.LookRotation(fwd.normalized, up);
            Vector3 vel = v.Rb.GetPointVelocity(spawn) + normal * 0.4f;
            part.Crew.Remove(crewName);
            var eva = SpawnVessel(rec, spawn, rot, vel, Vector3.zero);
            IgnoreCollisions(eva, v, 0.6f);
            eva.LoadedUT = UT - 10;
            SetActive(eva);
            CrewStatusChanged?.Invoke(crewName, "");
            Log($"{crewName} is on EVA");
            return eva;
        }

        /// <summary>Finds the closest boardable hatch for an EVA crew member (null if none in range).</summary>
        public Part FindBoardableHatch(Vessel eva, out float distance, out float relSpeed)
        {
            distance = float.MaxValue; relSpeed = 0;
            Part best = null;
            if (eva == null || !eva.IsEva) return null;
            Vector3 pos = eva.WorldCoM;
            foreach (var v in LoadedVessels)
            {
                if (v == null || v == eva || v.IsEva || v.IsFlag || v.Rb == null) continue;
                foreach (var p in v.Parts)
                {
                    if (!p.HasFreeSeat) continue;
                    var cm = p.GetModule<CrewModule>();
                    if (cm == null) continue;
                    float d = (cm.HatchWorldPosition - pos).magnitude;
                    if (d < distance)
                    {
                        distance = d;
                        best = p;
                        relSpeed = (eva.Rb.linearVelocity - v.Rb.GetPointVelocity(cm.HatchWorldPosition)).magnitude;
                    }
                }
            }
            return best;
        }

        public void RequestBoard(Vessel eva)
        {
            Enqueue(() => DoBoard(eva));
        }

        public bool DoBoard(Vessel eva)
        {
            var part = FindBoardableHatch(eva, out float d, out float rs);
            if (part == null) { Log("No hatch with a free seat nearby", true); return false; }
            if (d > EvaModule.BoardRange) { Log($"Too far from the hatch ({d:F1} m > {EvaModule.BoardRange:F1} m)", true); return false; }
            if (rs > EvaModule.BoardMaxRelSpeed) { Log($"Moving too fast relative to the vessel ({rs:F1} m/s)", true); return false; }
            string crew = eva.Record.evaCrew;
            var target = part.Vessel;
            // Return remaining pack propellant.
            var mono = eva.RootPart.GetResource("Monoprop");
            if (mono != null && mono.Amount > 0) target.Resources.Produce(part, "Monoprop", mono.Amount);
            part.Crew.Add(crew);
            var h = eva.Handle;
            LoadedVessels.Remove(eva);
            if (h != null) Handles.Remove(h);
            eva.Handle = null;
            eva.gameObject.SetActive(false); Destroy(eva.gameObject); // deactivate first: colliders leave physics now, not at end of frame
            SetActive(target);
            CrewStatusChanged?.Invoke(crew, target.Id);
            Log($"{crew} boarded {target.VesselName}");
            return true;
        }

        // ------------------------------------------------------------------ flag

        public Vessel PlantFlag(Vessel eva, string plaque)
        {
            var em = eva?.RootPart?.GetModule<EvaModule>();
            if (em == null || !em.Grounded) { Log("You must be standing on the ground to plant a flag", true); return null; }
            Vector3 up = -eva.GravityAccel.normalized;
            Vector3 fwd = Vector3.ProjectOnPlane(eva.transform.forward, up).normalized;
            if (fwd.sqrMagnitude < 0.5f) fwd = Vector3.Cross(up, Vector3.right).normalized;
            Vector3 feet = eva.WorldCoM - up * 0.65f;
            Vector3 pos = feet + fwd * 0.9f;
            // snap to ground
            if (Physics.Raycast(pos + up * 2f, -up, out RaycastHit hit, 6f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) pos = hit.point;
            var rec = new VesselRecord
            {
                name = $"Flag: {eva.Record.evaCrew} on {Frame.Body.Name}",
                kind = VesselKind.Flag,
                situation = Situation.Landed,
            };
            var pr = new PartRecord { uid = 1, partId = "flag", parent = -1 };
            pr.modules["Flag"] = new Dictionary<string, string> { { "plaque", plaque } };
            rec.parts.Add(pr);
            Quaternion rot = Quaternion.LookRotation(-fwd, up);
            var flag = SpawnVessel(rec, pos, rot, Vector3.zero, Vector3.zero);
            flag.Rb.isKinematic = true;
            CaptureState(flag);
            Log($"Flag planted on {Frame.Body.Name}: \"{plaque}\"", true);
            return flag;
        }

        /// <summary>Keeps static (flag) vessels glued to the rotating surface.</summary>
        private void UpdateStaticVessels(double ut, bool physicsMove)
        {
            foreach (var v in LoadedVessels)
            {
                if (v == null || !v.IsFlag || v.Handle == null || !v.Handle.Landed) continue;
                var h = v.Handle;
                Vector3d pos = h.Body.BodyFixedToInertial(h.LandedPosBF, ut) + Frame.BodyPosition(h.Body, ut);
                Vector3d originAt = Frame.Origin + Frame.Velocity * (ut - UT);
                Vector3 p = (Vector3)(pos - originAt);
                Quaternion r = (h.Body.RotationAtUT(ut) * h.LandedRotBF).ToQuaternion();
                if (physicsMove) { v.Rb.MovePosition(p); v.Rb.MoveRotation(r); }
                else { v.Rb.position = p; v.Rb.rotation = r; v.transform.SetPositionAndRotation(p, r); }
            }
        }

        private void UpdateStaticVesselVisuals(double rut, double alpha) { }

        // ------------------------------------------------------------------ launch pad hold

        /// <summary>Moves pad-held vessels with the rotating surface (kinematic) to their pose at <paramref name="ut"/>.</summary>
        private void UpdatePadHolds(double ut, bool physicsMove)
        {
            foreach (var v in LoadedVessels)
            {
                if (v == null || !v.PadHold || v.Handle == null || !v.Handle.Landed) continue;
                var h = v.Handle;
                Vector3d com = h.Body.BodyFixedToInertial(h.LandedPosBF, ut) + Frame.BodyPosition(h.Body, ut);
                Vector3d originAt = Frame.Origin + Frame.Velocity * (ut - UT);
                Quaternion r = (h.Body.RotationAtUT(ut) * h.LandedRotBF).ToQuaternion();
                Vector3 comOffset = r * Vessel.ToV3(h.Record.comOffset);
                Vector3 p = (Vector3)(com - originAt) - comOffset;
                if (physicsMove) { v.Rb.MovePosition(p); v.Rb.MoveRotation(r); }
                else { v.Rb.position = p; v.Rb.rotation = r; v.transform.SetPositionAndRotation(p, r); }
            }
        }

        public void ReleasePadHold(Vessel v)
        {
            if (v == null || !v.PadHold) return;
            v.PadHold = false;
            if (v.Rb == null) return;
            v.Rb.isKinematic = Warp.OnRails;
            Vector3d pos = Frame.ToTrue(v.WorldCoM);
            v.Rb.linearVelocity = Frame.ToUnityVelocity(Frame.Body.FrameVelocityAt(pos));
            v.Rb.angularVelocity = Frame.Body.AngularVelocity.ToVector3();
            v.LoadedUT = UT - 10;
            v.ForceUnlockThisStep = true;
            v.Handle.Landed = false;
        }

        // ------------------------------------------------------------------ docking

        public void RequestDock(DockingPortModule a, DockingPortModule b)
        {
            Enqueue(() => DoDock(a, b));
        }

        private void DoDock(DockingPortModule a, DockingPortModule b)
        {
            if (a == null || b == null || a.Part == null || b.Part == null || a.Part.Destroyed || b.Part.Destroyed) return;
            if (!a.CanDock || !b.CanDock) return;
            var va = a.Vessel; var vb = b.Vessel;
            if (va == vb) return;
            // Absorb into the active (or heavier) vessel.
            if (vb == ActiveVessel || (va != ActiveVessel && vb.TotalMass > va.TotalMass))
            {
                (a, b) = (b, a);
                (va, vb) = (vb, va);
            }
            // Snap vb so that b's face sits on a's face with opposite normals.
            Quaternion align = Quaternion.FromToRotation(b.FaceNormalWorld, -a.FaceNormalWorld);
            Vector3 pivot = b.FaceWorld;
            vb.transform.rotation = align * vb.transform.rotation;
            vb.transform.position = align * (vb.transform.position - pivot) + pivot;
            Vector3 offset = a.FaceWorld - b.FaceWorld;
            vb.transform.position += offset;
            vb.Rb.position = vb.transform.position;
            vb.Rb.rotation = vb.transform.rotation;
            Physics.SyncTransforms();
            string nodeA = a.Def.nodeId, nodeB = b.Def.nodeId;
            bool wasActiveB = vb == ActiveVessel;
            va.AbsorbVessel(vb, b.Part, a.Part, nodeB, nodeA);
            a.State = DockingPortModule.PortState.Docked;
            b.State = DockingPortModule.PortState.Docked;
            a.PartnerUid = b.Part.Uid;
            b.PartnerUid = a.Part.Uid;
            // Keep part uids unique inside the merged vessel.
            int maxUid = 0;
            foreach (var p in va.Parts) maxUid = Mathf.Max(maxUid, p.Uid);
            var seen = new HashSet<int>();
            foreach (var p in va.Parts) if (!seen.Add(p.Uid)) p.Uid = ++maxUid;
            if (wasActiveB || ActiveVessel == null) SetActive(va);
            Log($"Docked: {va.VesselName}", true);
        }

        public void RequestUndock(DockingPortModule port)
        {
            Enqueue(() => DoUndock(port));
        }

        private void DoUndock(DockingPortModule port)
        {
            var v = port.Vessel;
            if (v == null) return;
            Part child = null;
            DockingPortModule other = null;
            // Connection at our docking node: either we are the child, or a child sits on our node.
            if (port.Part.AttachNode == port.Def.nodeId && port.Part.ParentPart != null)
            {
                child = port.Part;
                other = port.Part.ParentPart.GetModule<DockingPortModule>();
            }
            else
            {
                foreach (var c in port.Part.Children)
                    if (c.ParentNode == port.Def.nodeId) { child = c; other = c.GetModule<DockingPortModule>(); break; }
            }
            if (child == null) return;
            Vector3 jp = child.JointWorldPosition;
            Vector3 axis = child.JointAxisTowardsParent;
            var parentPart = child.ParentPart;
            var nv = v.DetachSubtree(child, true);
            if (nv == null) return;
            float J = (float)port.Def.undockImpulse;
            v.QueueImpulse(parentPart, axis * J, jp);
            nv.QueueImpulse(child, -axis * J, jp);
            v.ForceUnlockThisStep = true;
            nv.ForceUnlockThisStep = true;
            foreach (var m in new[] { port, other })
            {
                if (m == null) continue;
                m.State = DockingPortModule.PortState.Cooldown;
                m.CooldownUntil = UT + 4;
                m.PartnerUid = -1;
            }
            nv.VesselName = nv.Kind == VesselKind.Ship ? v.VesselName + " (undocked)" : nv.VesselName;
            Log("Undocked", true);
        }

        // ------------------------------------------------------------------ recovery

        public bool CanRecover(Vessel v)
        {
            if (v == null || v.IsFlag) return false;
            if (v.MainBody != System.Root) return false;
            return (v.Situation == Situation.Landed || v.Situation == Situation.Splashed || v.Situation == Situation.Prelaunch) && v.SurfaceSpeed < 2;
        }

        public void Recover(Vessel v)
        {
            if (!CanRecover(v)) return;
            var h = v.Handle;
            CaptureState(v);
            foreach (var p in v.Parts)
                foreach (var c in p.Crew) CrewStatusChanged?.Invoke(c, null);
            if (v.IsEva && v.Record.evaCrew != null) CrewStatusChanged?.Invoke(v.Record.evaCrew, null);
            LoadedVessels.Remove(v);
            if (h != null) { Handles.Remove(h); VesselRecovered?.Invoke(h); }
            bool wasActive = ActiveVessel == v;
            v.Handle = null;
            v.gameObject.SetActive(false); Destroy(v.gameObject);
            if (wasActive) PickNewActive(Vector3.zero);
            Log($"{v.VesselName} recovered", true);
        }
    }
}
