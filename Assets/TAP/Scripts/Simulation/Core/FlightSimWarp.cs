using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed class TimeWarp
    {
        public static readonly double[] RailsRates = { 1, 5, 10, 50, 100, 1000, 10000, 100000 };
        public static readonly float[] PhysicsRates = { 1, 2, 3, 4 };
        public int RailsIndex;
        public int PhysicsIndex;
        public bool OnRails => RailsIndex > 0;
        public double Rate => OnRails ? RailsRates[RailsIndex] : PhysicsRates[PhysicsIndex];
        public double WarpToUT = double.NaN;
        public string LimitReason;
    }

    public sealed partial class FlightSim
    {
        // ------------------------------------------------------------------ warp control

        /// <summary>Highest rails warp index allowed for the active vessel right now (0 = none).</summary>
        public int MaxRailsIndex(out string reason)
        {
            reason = null;
            var v = ActiveVessel;
            if (v == null) return TimeWarp.RailsRates.Length - 1;
            if (!Warp.OnRails && v.Rb != null && !v.Rb.isKinematic)
            {
                double thrust = 0;
                foreach (var p in v.Parts)
                {
                    var e = p.GetModule<EngineModule>();
                    if (e != null) thrust += e.CurrentThrust;
                }
                if (thrust > 1) { reason = "Cannot time warp under thrust"; return 0; }
                if (v.IsEva && v.RootPart.GetModule<EvaModule>().JetpackOn && !v.GroundContact) { }
            }
            bool landed = v.Situation == Situation.Landed || v.Situation == Situation.Splashed || v.Situation == Situation.Prelaunch;
            if (landed && v.SurfaceSpeed < 0.5) return TimeWarp.RailsRates.Length - 1;
            var body = v.MainBody;
            double alt = v.Altitude;
            if (body.Atmosphere != null && alt < body.Atmosphere.Height) { reason = "In atmosphere (physics warp only)"; return 0; }
            var lim = body.WarpAltitudeLimits;
            int max = TimeWarp.RailsRates.Length - 1;
            if (lim != null)
            {
                for (int i = 1; i < lim.Length && i < TimeWarp.RailsRates.Length; i++)
                    if (alt < lim[i]) { max = i - 1; reason = $"Altitude too low for {TimeWarp.RailsRates[i]}x"; break; }
            }
            return max;
        }

        public bool SetRailsWarp(int index)
        {
            index = Mathf.Clamp(index, 0, TimeWarp.RailsRates.Length - 1);
            int max = MaxRailsIndex(out string reason);
            if (index > max)
            {
                Warp.LimitReason = reason;
                if (reason != null) Log(reason, true);
                index = max;
            }
            if (index > 0 && Warp.PhysicsIndex > 0) SetPhysicsWarp(0);
            bool was = Warp.OnRails;
            Warp.RailsIndex = index;
            if (!was && Warp.OnRails) EnterRails();
            else if (was && !Warp.OnRails) ExitRails();
            return true;
        }

        public void SetPhysicsWarp(int index)
        {
            if (Warp.OnRails) SetRailsWarp(0);
            index = Mathf.Clamp(index, 0, TimeWarp.PhysicsRates.Length - 1);
            Warp.PhysicsIndex = index;
            Time.timeScale = TimeWarp.PhysicsRates[index];
        }

        public void StopWarp()
        {
            Warp.WarpToUT = double.NaN;
            if (Warp.OnRails) SetRailsWarp(0);
            if (Warp.PhysicsIndex > 0) SetPhysicsWarp(0);
        }

        /// <summary>Warps until the given UT, slowing down as it approaches.</summary>
        public void WarpTo(double ut)
        {
            if (ut <= UT + 1) return;
            Warp.WarpToUT = ut;
            SetRailsWarp(TimeWarp.RailsRates.Length - 1);
        }

        private void CheckWarpSafety()
        {
            if (Warp.PhysicsIndex > 0 && ActiveVessel != null && ActiveVessel.GForce > 12) SetPhysicsWarp(0);
        }

        // ------------------------------------------------------------------ rails

        private void EnterRails()
        {
            foreach (var v in LoadedVessels.ToArray())
            {
                if (v == null || v.Rb == null) continue;
                CaptureState(v);
                v.Rb.isKinematic = true;
            }
            foreach (var h in Handles) h.LastSoiCheckUT = UT;
        }

        public void ExitRails()
        {
            Warp.WarpToUT = double.NaN;
            foreach (var v in LoadedVessels.ToArray())
            {
                // Flags and rockets still held on the launch pad stay kinematic.
                if (v == null || v.Rb == null || v.IsFlag || v.PadHold) continue;
                var h = v.Handle;
                if (!v.Rb.isKinematic || h == null) continue;
                // Rails state first: once the rigidbody is dynamic again the handle reads it instead (its velocity would
                // be the one from before the warp, pointing wherever the surface pointed back then).
                Vector3d pos = h.PositionRelBody(UT);
                Vector3d vel = h.VelocityRelBody(UT);
                PlaceOnRails(v, h, UT, true);
                v.Rb.isKinematic = false;
                v.Rb.linearVelocity = Frame.ToUnityVelocity(vel);
                v.Rb.angularVelocity = Vector3.zero;
                v.LoadedUT = UT;
                v.Orbit = h.Landed ? new Orbit(pos, vel, UT, h.Body.GM) : h.Orbit;
                if (h.Landed && h.Body == Frame.Body)
                {
                    // Ground under the vessel right away, and no coasting on a free-fall arc while the legs settle.
                    Planets?.EnsureColliderNow(pos, 40);
                    v.SettleUntilUT = UT + 3;
                }
                v.Attitude.ResetHold();
                v.UpdateFlightState(UT);
            }
            Physics.SyncTransforms();
        }

        /// <summary>Sets a (kinematic) loaded vessel's transform from its rails state at UT.</summary>
        private void PlaceOnRails(Vessel v, VesselHandle h, double ut, bool immediate)
        {
            Vector3d pos = h.PositionRelBody(ut) + Frame.BodyPosition(h.Body, ut);
            Quaternion rot;
            if (h.Landed) rot = (h.Body.RotationAtUT(ut) * h.LandedRotBF).ToQuaternion();
            else rot = v.transform.rotation;
            Vector3 comOffsetWorld = rot * Vessel.ToV3(h.Record.comOffset);
            Vector3 p = Frame.ToUnity(pos) - comOffsetWorld;
            v.transform.SetPositionAndRotation(p, rot);
            v.Rb.position = p;
            v.Rb.rotation = rot;
        }

        private void RailsUpdate(double realDt)
        {
            var a = ActiveHandle;
            if (a == null) { StopWarp(); return; }
            int max = MaxRailsIndex(out string reason);
            if (Warp.RailsIndex > max)
            {
                if (reason != null) Log(reason + " — warp reduced");
                SetRailsWarp(max);
                if (!Warp.OnRails) return;
            }
            double rate = Warp.Rate;
            double dtUT = Math.Min(realDt, 0.1) * rate;
            if (!double.IsNaN(Warp.WarpToUT))
            {
                double remaining = Warp.WarpToUT - UT;
                if (remaining <= 0.05) { StopWarp(); return; }
                // pick the highest rate that takes at least ~1.5 s of real time to cover the remainder
                int idx = Warp.RailsIndex;
                while (idx > 1 && TimeWarp.RailsRates[idx] * 1.5 > remaining) idx--;
                if (idx != Warp.RailsIndex) Warp.RailsIndex = idx;
                rate = Warp.Rate;
                dtUT = Math.Min(Math.Min(realDt, 0.1) * rate, remaining);
            }

            double t0 = UT, t1 = UT + dtUT;
            // Rails SOI transitions and losses (exact times).
            foreach (var h in Handles.ToArray())
            {
                if (h.Landed || h.Orbit == null) continue;
                AdvanceHandleSoi(h, t0, t1);
            }
            // Don't warp the active vessel into an atmosphere or below the warp altitude limit.
            if (a.Orbit != null && !a.Landed && a.Body.Atmosphere != null)
            {
                double tAtm = PatchedConics.FindRadiusDescending(a.Orbit, a.Body.Radius + a.Body.Atmosphere.Height, t0);
                if (!double.IsNaN(tAtm) && tAtm < t1)
                {
                    t1 = Math.Max(t0, tAtm - 1);
                    UT = t1;
                    SyncRailsFrame();
                    Log("Entering atmosphere — time warp stopped", true);
                    StopWarp();
                    return;
                }
            }
            if (a.Orbit != null && !a.Landed && a.Body.WarpAltitudeLimits != null && Warp.RailsIndex < a.Body.WarpAltitudeLimits.Length)
            {
                double lim = a.Body.WarpAltitudeLimits[Warp.RailsIndex];
                double tLow = PatchedConics.FindRadiusDescending(a.Orbit, a.Body.Radius + lim, t0);
                if (!double.IsNaN(tLow) && tLow < t1 && tLow > t0)
                {
                    t1 = tLow;
                    UT = t1;
                    SyncRailsFrame();
                    SetRailsWarp(Math.Max(0, Warp.RailsIndex - 1));
                    return;
                }
            }
            UT = t1;
            SyncRailsFrame();
            if (UT >= _nextLoadCheck)
            {
                _nextLoadCheck = UT + Math.Max(0.5, rate * 0.25);
                UpdateLoadedSet();
            }
        }

        /// <summary>Places the frame on the active vessel and moves all loaded (kinematic) vessels to UT.</summary>
        private void SyncRailsFrame()
        {
            var a = ActiveHandle;
            if (a == null) return;
            if (a.Body != Frame.Body) ChangeFrameBody(a.Body);
            Vector3d pos = a.PositionRelBody(UT);
            Frame.Origin = pos;
            Frame.PreviousOrigin = pos;
            Frame.Velocity = a.VelocityRelBody(UT);
            foreach (var v in LoadedVessels)
            {
                if (v == null || v.Handle == null) continue;
                if (v.IsFlag) continue;
                PlaceOnRails(v, v.Handle, UT, true);
                v.Orbit = v.Handle.Orbit;
            }
            UpdateStaticVessels(UT, false);
            foreach (var v in LoadedVessels) if (v != null && !v.IsFlag) v.UpdateFlightStateRails(UT);
        }

        /// <summary>Applies any SOI transitions or surface impacts of an on-rails handle between t0 and t1.</summary>
        private void AdvanceHandleSoi(VesselHandle h, double t0, double t1)
        {
            for (int guard = 0; guard < 4; guard++)
            {
                var o = h.Orbit;
                var body = h.Body;
                double tEvent = t1;
                CelestialBody next = null;
                bool exit = false;
                double tExit = PatchedConics.FindSoiExit(o, body, t0);
                if (!double.IsNaN(tExit) && tExit >= t0 && tExit < tEvent) { tEvent = tExit; next = body.Parent; exit = true; }
                if (body.Children.Count > 0 && PatchedConics.FindSoiEntry(o, body, t0, tEvent, out double tEnt, out var child) && tEnt < tEvent)
                {
                    tEvent = tEnt; next = child; exit = false;
                }
                // Impacts / atmosphere loss for vessels on rails.
                double lossR = body.Radius + (body.Atmosphere != null ? body.Atmosphere.Height * AtmosphereDeleteAltitudeFraction : 0);
                double tLoss = PatchedConics.FindRadiusDescending(o, lossR, t0);
                if (!double.IsNaN(tLoss) && tLoss >= t0 && tLoss < tEvent && h != ActiveHandle)
                {
                    Log($"{h.Name} {(body.Atmosphere != null ? "burned up in the atmosphere" : "crashed into " + body.Name)}");
                    DestroyHandle(h, body.Atmosphere != null ? "lost in the atmosphere" : "crashed");
                    return;
                }
                if (next == null) return;
                o.GetStateAtUT(tEvent, out Vector3d r, out Vector3d v);
                Vector3d rn, vn;
                if (exit)
                {
                    body.Orbit.GetStateAtUT(tEvent, out Vector3d rb, out Vector3d vb);
                    rn = r + rb; vn = v + vb;
                }
                else
                {
                    next.Orbit.GetStateAtUT(tEvent, out Vector3d rc, out Vector3d vc);
                    rn = r - rc; vn = v - vc;
                }
                h.Orbit = new Orbit(rn, vn, tEvent, next.GM);
                h.Body = next;
                h.Record.bodyId = next.Id;
                if (h == ActiveHandle) Log($"Entering {next.Name}'s sphere of influence");
                t0 = tEvent;
            }
        }
    }
}
