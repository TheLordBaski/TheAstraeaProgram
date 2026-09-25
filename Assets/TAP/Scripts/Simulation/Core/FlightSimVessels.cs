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
        // ------------------------------------------------------------------ setup

        /// <summary>
        /// Initializes the universe from saved records. The active vessel (and anything near it)
        /// is loaded into physics; everything else stays on rails.
        /// </summary>
        public void Initialize(double ut, List<VesselRecord> records, string activeId)
        {
            UT = ut;
            PreviousUT = ut;
            Handles.Clear();
            foreach (var r in records) Handles.Add(CreateHandle(r));
            var active = Handles.Find(h => h.Id == activeId) ?? (Handles.Count > 0 ? Handles[0] : null);
            if (active == null) return;
            // Frame on the active vessel.
            Frame = new ReferenceFrame(active.Body);
            Vector3d pos = active.PositionRelBody(ut);
            Vector3d vel = active.VelocityRelBody(ut);
            Frame.Origin = pos;
            Frame.PreviousOrigin = pos;
            Frame.Velocity = vel;
            Planets?.OnFrameBodyChanged(active.Body);
            LoadHandle(active);
            SetActive(active.Loaded, false);
            UpdateLoadedSet();
        }

        public VesselHandle CreateHandle(VesselRecord r)
        {
            var h = new VesselHandle { Record = r, Body = System.Get(r.bodyId) ?? System.Root };
            if (r.landed && r.landedPos != null)
            {
                h.Landed = true;
                h.LandedPosBF = new Vector3d(r.landedPos[0], r.landedPos[1], r.landedPos[2]);
                if (r.landedRot != null) h.LandedRotBF = new QuaternionD(r.landedRot[0], r.landedRot[1], r.landedRot[2], r.landedRot[3]);
            }
            else if (r.orbitPos != null && r.orbitVel != null)
            {
                h.Orbit = new Orbit(new Vector3d(r.orbitPos[0], r.orbitPos[1], r.orbitPos[2]),
                    new Vector3d(r.orbitVel[0], r.orbitVel[1], r.orbitVel[2]), r.orbitEpoch, h.Body.GM);
            }
            h.LastSoiCheckUT = UT;
            return h;
        }

        public VesselHandle FindHandle(string id) => Handles.Find(h => h.Id == id);

        // ------------------------------------------------------------------ loading

        /// <summary>Instantiates the physical vessel for a handle at its current rails state.</summary>
        public Vessel LoadHandle(VesselHandle h)
        {
            if (h.Loaded != null) return h.Loaded;
            if (h.Body != Frame.Body) return null; // only same-SOI vessels are loaded
            var rec = h.Record;
            Quaternion rootRot;
            if (h.Landed)
            {
                var q = h.Body.RotationAtUT(UT) * h.LandedRotBF;
                rootRot = q.ToQuaternion();
            }
            else rootRot = Vessel.ToQ(rec.rotation);

            // Rails state first: once the handle points at the new (not yet placed) vessel, its position and velocity
            // queries read that vessel's rigidbody instead (a landed vessel then appeared metres off, with zero
            // inertial velocity, and sank into the ground).
            Vector3d pos = h.PositionRelBody(UT);
            Vector3d vel = h.VelocityRelBody(UT);
            var v = Vessel.Create(rec, PartDb, Vector3.zero, rootRot);
            v.Handle = h;
            h.Loaded = v;
            // Place the centre of mass at the true position.
            // Computed from our own mass model: PhysX may not have applied the new CoM yet.
            Vector3 comOffset = v.transform.rotation * v.LocalCenterOfMass;
            v.transform.position = Frame.ToUnity(pos) - comOffset;
            v.Rb.position = v.transform.position;
            v.Rb.rotation = v.transform.rotation;
            Physics.SyncTransforms();
            // A vessel resting on the ground needs its ground now, not after the background terrain worker has
            // caught up; and it must not coast along a free-fall arc while its legs find the surface.
            double agl = pos.magnitude - h.Body.Radius - h.Body.TerrainHeightAt(pos, UT);
            if (!v.IsFlag && (h.Landed || agl < 200) && h.Body == Frame.Body)
            {
                Planets?.EnsureColliderNow(pos, 40);
                if (h.Landed) v.SettleUntilUT = UT + 3;
            }
            if (v.IsFlag)
            {
                v.Rb.isKinematic = true;
            }
            else if (h.Landed && rec.situation == Situation.Prelaunch && rec.launchUT < 0)
            {
                v.PadHold = true;
                v.Rb.isKinematic = true;
            }
            else
            {
                v.Rb.linearVelocity = Frame.ToUnityVelocity(vel);
                v.Rb.angularVelocity = h.Landed ? Vector3.zero : Vessel.ToV3(rec.angularVelocity);
            }
            v.LoadedUT = UT;
            v.Orbit = h.Landed ? new Orbit(pos, vel, UT, h.Body.GM) : (h.Orbit ?? new Orbit(pos, vel, UT, h.Body.GM));
            v.UpdateFlightState(UT);
            LoadedVessels.Add(v);
            if (Warp.OnRails && !v.IsFlag) v.Rb.isKinematic = true;
            return v;
        }

        /// <summary>Removes the physical vessel, storing its state back into the handle/record.</summary>
        public void UnloadHandle(VesselHandle h)
        {
            var v = h.Loaded;
            if (v == null) return;
            CaptureState(v);
            LoadedVessels.Remove(v);
            h.Loaded = null;
            v.Handle = null;
            v.gameObject.SetActive(false); Destroy(v.gameObject); // deactivate first: colliders leave physics now, not at end of frame

            // Vessels unloaded deep inside an atmosphere are lost (they would be destroyed by the time they land).
            var body = h.Body;
            if (!h.Landed && body.Atmosphere != null && h.Record.kind != VesselKind.Flag)
            {
                double alt = h.PositionRelBody(UT).magnitude - body.Radius;
                if (alt < body.Atmosphere.Height * AtmosphereDeleteAltitudeFraction)
                {
                    Log($"{h.Name} was lost in the atmosphere after leaving physics range");
                    DestroyHandle(h, "lost in the atmosphere");
                }
            }
        }

        /// <summary>Writes a loaded vessel's full state into its record and rails representation.</summary>
        public void CaptureState(Vessel v)
        {
            var h = v.Handle;
            if (h == null) return;
            v.WriteRecord(UT);
            h.Body = Frame.Body;
            h.Record.bodyId = h.Body.Id;
            bool landed = v.IsFlag || ((v.Situation == Situation.Landed || v.Situation == Situation.Prelaunch || v.Situation == Situation.Splashed) && v.SurfaceSpeed < 1.0);
            if (landed)
            {
                h.Landed = true;
                Vector3d pos = v.IsFlag ? Frame.ToTrue(v.transform.position) : Frame.ToTrue(v.WorldCoM);
                h.LandedPosBF = h.Body.InertialToBodyFixed(pos, UT);
                QuaternionD bodyInv = h.Body.RotationAtUT(UT).Inverse();
                h.LandedRotBF = bodyInv * QuaternionD.FromQuaternion(v.transform.rotation);
                h.Orbit = null;
            }
            else
            {
                h.Landed = false;
                h.Orbit = v.Orbit ?? new Orbit(v.TruePosition, v.TrueVelocity, UT, h.Body.GM);
                if (v.Rb != null && !v.Rb.isKinematic && !v.KeplerLocked)
                    h.Orbit = new Orbit(Frame.ToTrue(v.WorldCoM), Frame.ToTrueVelocity(v.Rb.linearVelocity), UT, h.Body.GM);
            }
            var rec = h.Record;
            rec.landed = h.Landed;
            if (h.Landed)
            {
                rec.landedPos = new[] { h.LandedPosBF.x, h.LandedPosBF.y, h.LandedPosBF.z };
                rec.landedRot = new[] { (float)h.LandedRotBF.x, (float)h.LandedRotBF.y, (float)h.LandedRotBF.z, (float)h.LandedRotBF.w };
                rec.orbitPos = null; rec.orbitVel = null;
            }
            else
            {
                var o = h.Orbit.Reanchored(UT);
                h.Orbit = o;
                rec.orbitPos = new[] { o.R0.x, o.R0.y, o.R0.z };
                rec.orbitVel = new[] { o.V0.x, o.V0.y, o.V0.z };
                rec.orbitEpoch = UT;
            }
        }

        /// <summary>Loads vessels close to the active one and unloads distant ones.</summary>
        public void UpdateLoadedSet()
        {
            var a = ActiveHandle;
            if (a == null) return;
            Vector3d ap = a.AbsolutePosition(UT);
            foreach (var h in Handles.ToArray())
            {
                if (h == a) continue;
                double d = (h.AbsolutePosition(UT) - ap).magnitude;
                if (h.Loaded != null)
                {
                    if (d > UnloadDistance || h.Body != Frame.Body) UnloadHandle(h);
                }
                else if (d < LoadDistance && h.Body == Frame.Body)
                {
                    LoadHandle(h);
                }
            }
        }

        // ------------------------------------------------------------------ creation

        /// <summary>Registers a brand-new vessel record (launch, EVA, flag) and loads it.</summary>
        public Vessel SpawnVessel(VesselRecord rec, Vector3 rootWorldPos, Quaternion rootWorldRot, Vector3 unityVelocity, Vector3 angularVelocity)
        {
            rec.bodyId = Frame.Body.Id;
            var h = new VesselHandle { Record = rec, Body = Frame.Body };
            Handles.Add(h);
            var v = Vessel.Create(rec, PartDb, rootWorldPos, rootWorldRot);
            v.Handle = h;
            h.Loaded = v;
            v.Rb.position = rootWorldPos;
            v.Rb.rotation = rootWorldRot;
            v.Rb.linearVelocity = unityVelocity;
            v.Rb.angularVelocity = angularVelocity;
            v.LoadedUT = UT;
            v.UpdateFlightState(UT);
            v.Orbit = new Orbit(Frame.ToTrue(v.WorldCoM), Frame.ToTrueVelocity(unityVelocity), UT, Frame.Body.GM);
            LoadedVessels.Add(v);
            if (v.IsFlag) v.Rb.isKinematic = true;
            return v;
        }

        /// <summary>Creates a new vessel object that adopts live part GameObjects (after a split).</summary>
        public Vessel CreateVesselFromLiveParts(Vessel source, List<Part> parts, Part root)
        {
            var go = new GameObject("Vessel: " + source.VesselName + " debris");
            go.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
            var nv = go.AddComponent<Vessel>();
            bool hasCommand = false;
            foreach (var p in parts) if (p.Def.command != null) hasCommand = true;
            string baseName = source.VesselName;
            while (baseName.EndsWith(" Debris")) baseName = baseName.Substring(0, baseName.Length - " Debris".Length);
            nv.Record = new VesselRecord
            {
                name = hasCommand ? baseName + " (separated)" : baseName + " Debris",
                kind = hasCommand ? VesselKind.Ship : VesselKind.Debris,
                situation = source.Situation == Situation.Prelaunch ? Situation.Flying : source.Situation,
                bodyId = Frame.Body.Id,
                designName = source.Record.designName,
                launchUT = source.Record.launchUT,
            };
            nv.InitializeLive(parts, root);
            nv.CurrentStage = Mathf.Min(source.CurrentStage, nv.MaxStage);
            var h = new VesselHandle { Record = nv.Record, Body = Frame.Body, Loaded = nv };
            nv.Handle = h;
            Handles.Add(h);
            LoadedVessels.Add(nv);
            nv.LoadedUT = UT - 10; // not impact-protected
            nv.UpdateFlightState(UT);
            nv.Orbit = new Orbit(nv.TruePosition, Frame.ToTrueVelocity(nv.Rb.linearVelocity), UT, Frame.Body.GM);
            return nv;
        }

        // ------------------------------------------------------------------ active vessel

        public void SetActive(Vessel v, bool recenter = true)
        {
            if (v == null) return;
            var prev = ActiveVessel;
            ActiveVessel = v;
            if (prev != null && prev != v) prev.Ctrl.ClearInputs();
            if (recenter && v.Rb != null)
            {
                // Make the new active vessel the frame's anchor.
                ShiftOrigin(v.WorldCoM);
                if (!v.Rb.isKinematic)
                {
                    Vector3 dv = v.Rb.linearVelocity;
                    Frame.Velocity += (Vector3d)dv;
                    foreach (var o in LoadedVessels)
                        if (o != null && o.Rb != null && !o.Rb.isKinematic) o.Rb.linearVelocity -= dv;
                }
            }
            ActiveVesselChanged?.Invoke(v);
        }

        /// <summary>Switch control to any vessel (loads it and re-centres the universe if needed).</summary>
        public bool SwitchTo(VesselHandle h)
        {
            if (h == null) return false;
            if (h.Loaded != null)
            {
                SetActive(h.Loaded);
                UpdateLoadedSet();
                return true;
            }
            if (Warp.OnRails) Warp.RailsIndex = 0;
            ExitRails();
            // Capture all loaded vessels back to rails, rebuild the frame on the target.
            foreach (var lh in Handles.ToArray()) if (lh.Loaded != null) UnloadHandle(lh);
            if (!Handles.Contains(h)) return false;
            Frame = new ReferenceFrame(h.Body);
            Vector3d pos = h.PositionRelBody(UT);
            Frame.Origin = pos;
            Frame.PreviousOrigin = pos;
            Frame.Velocity = h.VelocityRelBody(UT);
            Planets?.OnFrameBodyChanged(h.Body);
            var v = LoadHandle(h);
            if (v == null) return false;
            SetActive(v, false);
            UpdateLoadedSet();
            return true;
        }

        public void CycleActive(int dir)
        {
            var list = LoadedVessels.FindAll(x => x != null && !x.IsFlag);
            if (list.Count < 2 || ActiveVessel == null) return;
            int i = list.IndexOf(ActiveVessel);
            i = (i + dir + list.Count) % list.Count;
            SetActive(list[i]);
            Log($"Switched to {list[i].VesselName}");
        }

        // ------------------------------------------------------------------ removal

        public void OnVesselEmptied(Vessel v)
        {
            var h = v.Handle;
            LoadedVessels.Remove(v);
            if (h != null)
            {
                Handles.Remove(h);
                VesselDestroyed?.Invoke(h);
            }
            bool wasActive = ActiveVessel == v;
            v.Handle = null;
            v.gameObject.SetActive(false); Destroy(v.gameObject);
            if (wasActive) PickNewActive(v.transform.position);
        }

        public void DestroyHandle(VesselHandle h, string reason)
        {
            if (h.Loaded != null)
            {
                var v = h.Loaded;
                foreach (var p in v.Parts) foreach (var c in p.Crew) OnCrewLost(c, v, reason);
                LoadedVessels.Remove(v);
                h.Loaded = null;
                v.gameObject.SetActive(false); Destroy(v.gameObject);
            }
            else
            {
                foreach (var p in h.Record.parts)
                    if (p.crew != null) foreach (var c in p.crew) CrewLost?.Invoke(c, reason);
                if (!string.IsNullOrEmpty(h.Record.evaCrew)) CrewLost?.Invoke(h.Record.evaCrew, reason);
            }
            Handles.Remove(h);
            VesselDestroyed?.Invoke(h);
        }

        public void RemoveVesselAfterMerge(Vessel other)
        {
            var h = other.Handle;
            LoadedVessels.Remove(other);
            if (h != null) Handles.Remove(h);
            other.Handle = null;
            bool wasActive = ActiveVessel == other;
            other.gameObject.SetActive(false); Destroy(other.gameObject);
            if (wasActive) ActiveVessel = null;
        }

        private void PickNewActive(Vector3 near)
        {
            Vessel best = null;
            float bestScore = float.MaxValue;
            foreach (var v in LoadedVessels)
            {
                if (v == null || v.IsFlag || v.Parts.Count == 0) continue;
                float d = (v.transform.position - near).magnitude;
                float score = d - (v.HasControl ? 10000f : 0) - v.CrewCount * 1000f;
                if (score < bestScore) { bestScore = score; best = v; }
            }
            if (best != null) SetActive(best);
            else
            {
                ActiveVessel = null;
                ActiveVesselChanged?.Invoke(null);
            }
        }

        internal void NotifyVesselObjectDestroyed(Vessel v)
        {
            LoadedVessels.Remove(v);
            if (ActiveVessel == v) ActiveVessel = null;
        }

        // ------------------------------------------------------------------ records

        /// <summary>Captures every vessel's current state (for saving).</summary>
        public List<VesselRecord> CaptureAllRecords()
        {
            foreach (var h in Handles)
            {
                if (h.Loaded != null) CaptureState(h.Loaded);
                else if (h.Orbit != null && !h.Landed)
                {
                    var o = h.Orbit;
                    h.Record.orbitPos = new[] { o.R0.x, o.R0.y, o.R0.z };
                    h.Record.orbitVel = new[] { o.V0.x, o.V0.y, o.V0.z };
                    h.Record.orbitEpoch = o.Epoch;
                    h.Record.bodyId = h.Body.Id;
                }
            }
            var list = new List<VesselRecord>();
            foreach (var h in Handles) list.Add(h.Record);
            return list;
        }
    }
}
