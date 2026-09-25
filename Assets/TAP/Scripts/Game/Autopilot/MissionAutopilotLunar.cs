using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    public sealed partial class MissionAutopilot
    {
        private IEnumerator SuborbitalMission()
        {
            SetPhase("suborbital launch");
            Throttle(1);
            Sas(SasMode.StabilityAssist, SpeedMode.Surface);
            Stage();
            yield return WaitSeconds(2);
            Check("liftoff", V.VerticalSpeed > 5, $"vs {V.VerticalSpeed:F1} m/s");
            double maxAlt = 0;
            yield return WaitUntil(() => { maxAlt = Math.Max(maxAlt, V.Altitude); return V.VerticalSpeed < 0; }, 600, "apex");
            Check("reached space (70 km)", maxAlt > 70000, $"apex {maxAlt / 1000:F1} km");
            // Separate the capsule, hold retrograde, arm the chute.
            Stage();
            yield return WaitSeconds(2);
            yield return Reentry();
        }

        private CelestialBody Luma => Sim.System.Get("luma");
        private CelestialBody Tellus => Sim.System.Root;

        private IEnumerator LunarMission()
        {
            yield return Ascent(82000);
            if (Failed) yield break;
            yield return TransLunarInjection();
            if (Failed) yield break;
            yield return LunarCapture();
            if (Failed) yield break;
            yield return LunarLanding();
            if (Failed) yield break;
            yield return SurfaceEva();
            if (Failed) yield break;
            yield return LunarAscent();
            if (Failed) yield break;
            yield return TransEarthInjection();
            if (Failed) yield break;
            yield return Reentry();
        }

        // ------------------------------------------------------------------ TLI

        private IEnumerator TransLunarInjection()
        {
            SetPhase("plan TLI");
            var v = V;
            var o = v.Orbit;
            v.TargetId = "body:luma";
            bool found = ManeuverPlanner.FindTransfer(o, Tellus, Luma, Sim.UT + 60, 40000, 800, 940, out double ut, out double dv, out double pe);
            Check("TLI encounter found by patched-conic search", found, $"node in {ut - Sim.UT:F0} s, {dv:F1} m/s prograde, Luma Pe {pe / 1000:F1} km");
            if (!found) yield break;
            ManeuverPlanner.AddNode(v, ut, dv);
            yield return WaitSeconds(0.5);
            var traj = Scene.Trajectory;
            traj.Recompute(v);
            var enc = traj.FirstEncounter(true);
            Check("planned trajectory shows Luma encounter", enc != null && enc.Body == Luma, enc != null ? $"Pe {(enc.Orbit.PeriapsisRadius - Luma.Radius) / 1000:F1} km" : "none");
            SetPhase("TLI burn");
            yield return ExecuteNode("TLI", 0.3);
            // After the finite burn, check the actual trajectory still encounters Luma, then correct.
            traj.Recompute(V);
            enc = traj.FirstEncounter(false);
            double actualPe = enc != null && enc.Body == Luma ? enc.Orbit.PeriapsisRadius - Luma.Radius : double.NaN;
            Check("actual post-burn trajectory encounters Luma", enc != null && enc.Body == Luma, $"predicted Pe {actualPe / 1000:F1} km (planned {pe / 1000:F1} km)");
            Note($"delta-v left after TLI: {DvLeft()}");
            yield return MidCourseCorrection(35000);
        }

        /// <summary>Small correction burn to trim the Luma periapsis (searches prograde/radial/normal delta-v).</summary>
        private IEnumerator MidCourseCorrection(double targetPe)
        {
            SetPhase("mid-course correction");
            var traj = Scene.Trajectory;
            traj.Recompute(V);
            var enc = traj.FirstEncounter(false);
            double pe = enc != null && enc.Body == Luma ? enc.Orbit.PeriapsisRadius - Luma.Radius : double.NaN;
            if (!double.IsNaN(pe) && Math.Abs(pe - targetPe) < 15000 && pe > 12000) { Note($"no correction needed (Pe {pe / 1000:F1} km)"); yield break; }
            double t = Sim.UT + 1800;
            double best = double.MaxValue, bp = 0, br = 0, bn = 0;
            var o = V.Orbit;
            for (double pro = -12; pro <= 12; pro += 1.5)
                for (double rad = -12; rad <= 12; rad += 1.5)
                {
                    double s = ScoreNode(o, Tellus, t, pro, 0, rad, targetPe);
                    if (s < best) { best = s; bp = pro; br = rad; }
                }
            // refine
            double step = 0.75;
            for (int it = 0; it < 40 && step > 0.01; it++)
            {
                bool imp = false;
                foreach (var (dp, dr, dn) in new[] { (step, 0.0, 0.0), (-step, 0.0, 0.0), (0.0, step, 0.0), (0.0, -step, 0.0), (0.0, 0.0, step), (0.0, 0.0, -step) })
                {
                    double s = ScoreNode(o, Tellus, t, bp + dp, bn + dn, br + dr, targetPe);
                    if (s < best) { best = s; bp += dp; br += dr; bn += dn; imp = true; }
                }
                if (!imp) step *= 0.5;
            }
            Note($"correction: pro {bp:F2} rad {br:F2} nrm {bn:F2} m/s (score {best:F0})");
            if (best > 1e6) { Check("mid-course correction found", false, "no encounter solution"); yield break; }
            ManeuverPlanner.AddNode(V, t, bp, bn, br);
            yield return ExecuteNode("MCC", 0.15);
        }

        private double ScoreNode(Orbit o, CelestialBody body, double t, double pro, double nrm, double rad, double targetPe)
        {
            var nodes = new List<NodeSpec> { new NodeSpec(t, pro, nrm, rad) };
            var patches = PatchedConics.Predict(o, body, t - 1, nodes, 4, 30 * 86400.0, false);
            foreach (var p in patches)
                if (p.Body == Luma) return Math.Abs(p.Orbit.PeriapsisRadius - Luma.Radius - targetPe) + Math.Sqrt(pro * pro + nrm * nrm + rad * rad) * 50;
            return 1e7;
        }

        // ------------------------------------------------------------------ capture

        private IEnumerator LunarCapture()
        {
            SetPhase("coast to Luma");
            // Warp to SOI change
            var traj = Scene.Trajectory;
            traj.Recompute(V);
            var enc = traj.FirstEncounter(false);
            if (enc == null) { Check("encounter before coast", false, "lost encounter"); yield break; }
            yield return WarpUntil(enc.StartUT + 60, "coast to Luma SOI");
            Check("entered Luma sphere of influence", Sim.Frame.Body == Luma || V.Handle.Body == Luma, $"frame body {Sim.Frame.Body.Name}");
            // Small burn errors grow over the long coast: trim the periapsis now that Luma is close (a few m/s here
            // move it by kilometres), so the capture burn happens safely above the terrain.
            yield return LumaPeriapsisCorrection(30000);
            if (Failed) yield break;
            SetPhase("capture burn");
            var o = V.Orbit;
            double tPe = Sim.UT + o.TimeToPeriapsis(Sim.UT);
            double pe = o.PeriapsisRadius - Luma.Radius;
            Note($"Luma periapsis {pe / 1000:F1} km in {tPe - Sim.UT:F0} s");
            // Circularize at periapsis: retrograde delta-v.
            o.GetStateAtUT(tPe, out var r, out var vel);
            double vc = Math.Sqrt(Luma.GM / r.magnitude);
            double dv = vc - vel.magnitude; // negative -> retrograde
            ManeuverPlanner.AddNode(V, tPe, dv);
            yield return ExecuteNode("Luma orbit insertion", 0.3);
            var oc = V.Orbit;
            Snap("luma_orbit");
            Check("captured into Luma orbit", oc.IsElliptic && oc.PeriapsisRadius - Luma.Radius > 5000 && oc.ApoapsisRadius < Luma.SOIRadius,
                $"Pe {(oc.PeriapsisRadius - Luma.Radius) / 1000:F1} km, Ap {(oc.ApoapsisRadius - Luma.Radius) / 1000:F1} km");
            Note($"delta-v left in Luma orbit: {DvLeft()}");
            // The landing legs are on the lander, so the transfer stage goes before the descent even if it still
            // holds some propellant: stage once so the lander engine is the active one.
            if (HasEngineStageLeft())
            {
                Note($"jettisoning transfer stage ({StageFuelFraction() * 100:F0}% propellant left in it)");
                yield return StageUntilThrust();
                Throttle(0);
                Note($"lander delta-v: {DvLeft()}");
            }
            // Snapshot in Luma orbit: the "lunar-landing" test resumes from here (landing onwards).
            if (!Failed) { yield return WaitSeconds(2); SaveSnapshot(OrbitSnapshotPath, "Luma orbit"); }
        }

        private IEnumerator LumaPeriapsisCorrection(double targetPe)
        {
            double pe0 = V.Orbit.PeriapsisRadius - Luma.Radius;
            if (pe0 > 20000 && pe0 < 60000) { Note($"Luma periapsis {pe0 / 1000:F1} km: no correction needed"); yield break; }
            SetPhase("periapsis correction");
            var o = V.Orbit;
            double tn = Sim.UT + 240;
            double Cost(double pro, double rad, out double pe)
            {
                pe = double.NaN;
                var nodes = new List<NodeSpec> { new NodeSpec(tn, pro, 0, rad) };
                var pp = PatchedConics.Predict(o, Luma, tn - 1, nodes, 2, 10 * 86400.0, false);
                if (pp.Count < 2 || pp[1].Body != Luma) return double.MaxValue;
                pe = pp[1].Orbit.PeriapsisRadius - Luma.Radius;
                return Math.Abs(pe - targetPe) + Math.Sqrt(pro * pro + rad * rad) * 20;
            }
            double best = double.MaxValue, bp = 0, br = 0, bpe = double.NaN;
            for (double pro = -30; pro <= 30; pro += 1.5)
                for (double rad = -30; rad <= 30; rad += 1.5)
                {
                    double c = Cost(pro, rad, out double pe);
                    if (c < best) { best = c; bp = pro; br = rad; bpe = pe; }
                }
            for (double step = 0.75; step > 0.01;)
            {
                bool improved = false;
                foreach (var (dp, dr) in new[] { (step, 0.0), (-step, 0.0), (0.0, step), (0.0, -step) })
                {
                    double c = Cost(bp + dp, br + dr, out double pe);
                    if (c < best) { best = c; bp += dp; br += dr; bpe = pe; improved = true; }
                }
                if (!improved) step *= 0.5;
            }
            Note($"Luma periapsis {pe0 / 1000:F1} km: correction pro {bp:F2} rad {br:F2} m/s -> predicted {bpe / 1000:F1} km");
            if (double.IsNaN(bpe)) { Check("Luma periapsis correction found", false, "no solution"); yield break; }
            ManeuverPlanner.AddNode(V, tn, bp, 0, br);
            yield return ExecuteNode("Luma periapsis correction", 0.1);
            double pe1 = V.Orbit.PeriapsisRadius - Luma.Radius;
            Check("Luma periapsis safely above the terrain", pe1 > 10000, $"Pe {pe1 / 1000:F1} km after the correction");
        }

        private double StageFuelFraction()
        {
            // Fraction of propellant left in tanks reachable by ignited engines.
            double amt = 0, max = 0;
            foreach (var p in V.Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e == null || !e.Ignited) continue;
                amt += V.Resources.Available(p, e.Def.propellant);
                max += V.Resources.Capacity(p, e.Def.propellant);
            }
            return max > 0 ? amt / max : 0;
        }

        // ------------------------------------------------------------------ landing

        private IEnumerator LunarLanding()
        {
            SetPhase("deorbit");
            var v = V;
            // Make sure the lander engine is the active one.
            if (CurrentThrustCapability() <= 0) yield return StageUntilThrust();
            Throttle(0);
            v = V;
            v.Ctrl.LegsDeployed = true;
            // Retrograde burn to drop periapsis below the surface (~ -8 km) -> descent trajectory.
            Sas(SasMode.Retrograde, SpeedMode.Orbit);
            yield return WaitUntil(() => NoseAngle(-(Vector3)V.TrueVelocity.normalized) < 4, 180, "align retrograde");
            while (V.Orbit.PeriapsisRadius - Luma.Radius > -6000)
            {
                Throttle(1);
                yield return null;
            }
            Throttle(0);
            SetPhase("descent");
            // Coast down on rails (like pressing warp) until ~12 km above the terrain datum, then fly the final descent.
            double tLow = PatchedConics.FindRadiusDescending(V.Orbit, Luma.Radius + 12000, Sim.UT);
            if (!double.IsNaN(tLow) && tLow - Sim.UT > 120) yield return WarpUntil(tLow - 30, "descent coast");
            // Coast until the suicide-burn point, then brake along surface retrograde with a speed-vs-altitude
            // profile. The last stretch is flown like a pilot hovering down: nose up, leaning against any sideways
            // drift, throttle holding a gentle *signed* sink rate (a speed magnitude would read a climb as
            // "too fast" and push the lander up and away).
            Sas(SasMode.Retrograde, SpeedMode.Surface);
            bool burning = false, terminal = false, siteChosen = false;
            Vector3d siteBF = Vector3d.zero;
            while (true)
            {
                v = V;
                if (v == null) { Check("lander survived descent", false, "destroyed"); yield break; }
                double h = v.AltitudeAGL;
                v.GetPropulsion(out double thrust, out _);
                double g = Luma.GM / (v.TruePosition.sqrMagnitude);
                double aThrust = thrust / v.TotalMass;
                double aMax = aThrust - g;
                double speed = v.SurfaceSpeed;
                double vs = v.VerticalSpeed;
                Vector3 up = LocalUp;
                // Stopping distance at 85% of available deceleration
                double stopDist = speed * speed / (2 * Math.Max(aMax * 0.85, 0.1));
                if (!burning && (stopDist > h - 60 || h < 300)) { burning = true; Note($"suicide burn start at {h:F0} m AGL, {speed:F0} m/s"); }
                if (burning && !terminal && (h < 150 || speed < 10))
                {
                    terminal = true;
                    Note($"final descent from {h:F0} m AGL at {speed:F1} m/s (vertical {vs:F1} m/s)");
                }
                if (terminal && !siteChosen && h < 130)
                {
                    // Like a pilot hovering in: pick the flattest spot within reach instead of wherever the descent ends
                    // (Luma's crater walls can topple even a wide lander).
                    siteChosen = true;
                    GroundSlope(v.WorldCoM, up, out _, out float hereSlope);
                    if (ChooseLandingSite(v.WorldCoM, up, out Vector3 site, out float slope, out float dist))
                    {
                        siteBF = Luma.InertialToBodyFixed(Sim.Frame.ToTrue(site), Sim.UT);
                        Note($"landing site: {slope:F1}° slope {dist:F0} m away (ground below: {hereSlope:F1}°)");
                    }
                    else siteBF = Luma.InertialToBodyFixed(Sim.Frame.ToTrue(v.WorldCoM), Sim.UT);
                }
                if (terminal)
                {
                    // Wanted acceleration: the vertical part holds the sink rate, the sideways part flies to the chosen
                    // site and cancels drift. Leaning the thrust at most 30° off vertical provides the sideways part.
                    Vector3 drift = Vector3.ProjectOnPlane((Vector3)v.SurfaceVelocity, up);
                    Vector3 vWanted = Vector3.zero;
                    float toSiteDist = 0;
                    if (siteChosen)
                    {
                        Vector3 siteWorld = Sim.Frame.ToUnity(Luma.BodyFixedToInertial(siteBF, Sim.UT));
                        Vector3 toSite = Vector3.ProjectOnPlane(siteWorld - v.WorldCoM, up);
                        toSiteDist = toSite.magnitude;
                        vWanted = Vector3.ClampMagnitude(toSite * 0.25f, 6f);
                    }
                    double vsTarget = -Math.Max(1.5, Math.Min(h * 0.12, 8));
                    if (toSiteDist > 5) vsTarget = h > 30 ? Math.Max(vsTarget, -2.5) : Math.Max(vsTarget, 0.5); // cross over high
                    double aUp = g + (vsTarget - vs) * 1.2;
                    float lift = (float)Math.Max(aUp, g);
                    Vector3 aSide = (vWanted - drift) * 0.5f;
                    float maxSide = lift * Mathf.Tan(30 * Mathf.Deg2Rad);
                    if (aSide.magnitude > maxSide) aSide = aSide.normalized * maxSide;
                    SteerTowards((up + aSide / lift).normalized);
                    double cosTilt = Math.Max(0.5, Vector3.Dot(v.ControlRotation * Vector3.up, up));
                    Throttle((float)MathD.Clamp(aUp / (Math.Max(aThrust, 0.01) * cosTilt), 0, 1));
                }
                else if (burning)
                {
                    double vTarget = Math.Sqrt(2 * aMax * 0.6 * Math.Max(h - 60, 0)) + 8;
                    double need = (g + (speed - vTarget) * 1.5) / Math.Max(aThrust, 0.01);
                    Throttle((float)MathD.Clamp(need, 0, 1));
                }
                if (v.Situation == Situation.Landed || (v.GroundContact && speed < 1.0))
                {
                    Throttle(0);
                    break;
                }
                if (Sim.UT - _phaseStartUT > 3000) { Check("landing timeout", false, ""); yield break; }
                yield return null;
            }
            Throttle(0);
            ReleaseKeys();
            yield return WaitSeconds(5);
            v = V;
            int legs = 0, brokenLegs = 0;
            foreach (var p in v.Parts) { var l = p.GetModule<LandingLegModule>(); if (l != null) { legs++; if (l.Broken) brokenLegs++; } }
            double tilt = Vector3.Angle(v.ControlRotation * Vector3.up, LocalUp);
            Snap("luma_landed");
            Check("landed on Luma", v.Situation == Situation.Landed && v.MainBody == Luma && tilt < 20,
                $"situation {v.Situation}, tilt {tilt:F1} deg, legs intact {legs - brokenLegs}/{legs}, crew {v.CrewCount}");
            Note($"delta-v left on the surface: {DvLeft()}");
            Sas(SasMode.StabilityAssist);
            // Snapshot of the landed state: the "lunar-surface" test resumes from here (EVA onwards).
            if (!Failed) SaveSnapshot(LandedSnapshotPath, "landed on Luma");
        }

        public static string LandedSnapshotPath => Path.Combine(SaveStorage.Root, "autotest_luma_landed.json");
        public static string OrbitSnapshotPath => Path.Combine(SaveStorage.Root, "autotest_luma_orbit.json");

        private void SaveSnapshot(string path, string what)
        {
            try
            {
                File.WriteAllText(path, SaveStorage.ToJson(Scene.CaptureSave()));
                Note($"snapshot ({what}) saved: {Path.GetFileName(path)}");
            }
            catch (Exception e) { Note("snapshot failed: " + e.Message); }
        }

        /// <summary>
        /// The second half of the lunar mission, resumed from the snapshot taken after the landing: EVA, flag,
        /// boarding, lunar ascent, return and reentry. Loading works like a quickload (the scene reloads and a new
        /// pilot continues with step 1).
        /// </summary>
        /// <summary>Second half of the lunar mission resumed from Luma orbit: landing, EVA, ascent, return, reentry.</summary>
        private IEnumerator LunarLandingMission()
        {
            if (AutoTestState.ResumeStep == 0)
            {
                string path = OrbitSnapshotPath;
                if (!File.Exists(path)) { Check("Luma orbit snapshot available", false, path + " (fly the lunar test first)"); yield break; }
                AutoTestState.ResumeStep = 1;
                Note("loading snapshot " + Path.GetFileName(path));
                GameSession.LoadFlightState(SaveStorage.FromJson<GameSave>(File.ReadAllText(path)), FlightEntry.Quickload);
                yield return WaitSecondsAny(30);
                yield break;
            }
            AutoTestState.ResumeStep = 0;
            SetPhase("resume in Luma orbit");
            yield return WaitSecondsAny(2);
            var v = V;
            Check("resumed in Luma orbit", v != null && v.MainBody == Luma && v.Orbit != null && v.Orbit.IsElliptic,
                v != null ? $"{v.VesselName}: {v.Situation} at {v.MainBody.Name}, delta-v {DvLeft()}" : "no vessel");
            if (Failed) yield break;
            yield return LunarLanding();
            if (Failed) yield break;
            yield return SurfaceEva();
            if (Failed) yield break;
            yield return LunarAscent();
            if (Failed) yield break;
            yield return TransEarthInjection();
            if (Failed) yield break;
            yield return Reentry();
        }

        private IEnumerator LunarSurfaceMission(bool idle = false)
        {
            if (AutoTestState.ResumeStep == 0)
            {
                string path = LandedSnapshotPath;
                if (!File.Exists(path)) { Check("landed snapshot available", false, path + " (fly the lunar test first)"); yield break; }
                AutoTestState.ResumeStep = 1;
                Note("loading snapshot " + Path.GetFileName(path));
                GameSession.LoadFlightState(SaveStorage.FromJson<GameSave>(File.ReadAllText(path)), FlightEntry.Quickload);
                yield return WaitSecondsAny(30);
                yield break;
            }
            AutoTestState.ResumeStep = 0;
            SetPhase("resume on Luma");
            yield return WaitSecondsAny(2);
            var v = V;
            Check("resumed landed on Luma", v != null && v.MainBody == Luma && v.Situation == Situation.Landed,
                v != null ? $"{v.VesselName}: {v.Situation} on {v.MainBody.Name}, crew {v.CrewCount}, delta-v {DvLeft()}" : "no vessel");
            if (Failed) yield break;
            // A reloaded lander must stand where it was saved, its weight on the legs (not sunk into the ground, resting
            // on its hull). Leg loads are averaged over a second: on uneven ground a lander may rock on two legs.
            yield return WaitSecondsAny(2);
            double loadSum = 0, weightSum = 0;
            for (int i = 0; i < 50; i++)
            {
                v = V;
                foreach (var p in v.Parts) { var l = p.GetModule<LandingLegModule>(); if (l != null) loadSum += l.LastLoad; }
                weightSum += v.TotalMass * v.GravityAccel.magnitude;
                yield return new WaitForFixedUpdate();
            }
            v = V;
            float tiltNow = Vector3.Angle(v.ControlRotation * Vector3.up, LocalUp);
            Check("reloaded lander stands on its legs", loadSum > 0.7 * weightSum && tiltNow < 20,
                $"legs carry {loadSum / Math.Max(weightSum, 1) * 100:F0}% of the weight, tilt {tiltNow:F1} deg, {LegSummary(v)}");
            if (Failed) yield break;
            if (idle)
            {
                // Inspection mode: stay on the surface (no pilot input) until the test is stopped.
                SetPhase("idle on Luma");
                while (true) yield return null;
            }
            yield return SurfaceEva();
            if (Failed) yield break;
            yield return LunarAscent();
            if (Failed) yield break;
            yield return TransEarthInjection();
            if (Failed) yield break;
            yield return Reentry();
        }

        private bool _watchLander;

        /// <summary>While the crew is outside, logs the parked lander's attitude and leg loads twice a second.</summary>
        private IEnumerator LanderWatch(Vessel lander)
        {
            double next = 0;
            while (_watchLander && lander != null && lander.Rb != null)
            {
                if (Sim.UT >= next)
                {
                    next = Sim.UT + 0.5;
                    Vector3 up = -lander.GravityAccel.normalized;
                    float tilt = Vector3.Angle(lander.ControlRotation * Vector3.up, up);
                    Note($"lander watch: tilt {tilt:F1} deg, surface speed {lander.SurfaceSpeed:F2} m/s, unity v {lander.Rb.linearVelocity.magnitude:F2}, w {lander.Rb.angularVelocity.magnitude:F2}, parts {lander.Parts.Count}, {LegSummary(lander)}, frame v {Sim.Frame.Velocity.magnitude:F2}");
                }
                yield return null;
            }
        }

        private static string LegSummary(Vessel v)
        {
            var sb = new System.Text.StringBuilder("legs");
            foreach (var p in v.Parts)
            {
                var l = p.GetModule<LandingLegModule>();
                if (l != null) sb.Append($" [{(l.Broken ? "broken" : l.InContact ? "contact" : "free")} {l.Compression:F2} m {l.LastLoad / 1000:F1} kN]");
            }
            return sb.ToString();
        }

        private string EvaDiag(Vessel eva, Vessel lander)
        {
            if (eva == null) return "no EVA vessel";
            var em = eva.RootPart.GetModule<EvaModule>();
            Vector3 up = -eva.GravityAccel.normalized;
            float axis = lander != null ? Vector3.ProjectOnPlane(eva.WorldCoM - lander.WorldCoM, up).magnitude : -1;
            string on = em != null && em.GroundCollider != null ? em.GroundCollider.name : "nothing";
            return $"AGL {eva.AltitudeAGL:F1} m, {axis:F1} m from the lander axis, grounded {em?.Grounded} on {on}, speed {eva.SurfaceSpeed:F2} m/s, jetpack {em?.Propellant:F2} kg";
        }

        /// <summary>
        /// Samples the collision surface on rings up to 60 m around <paramref name="center"/> and returns the flattest
        /// spot (slope over the lander's footprint plus roughness, with a small cost for distance).
        /// </summary>
        private static bool ChooseLandingSite(Vector3 center, Vector3 up, out Vector3 site, out float slope, out float dist)
        {
            Vector3 e1 = Vector3.ProjectOnPlane(Vector3.right, up);
            if (e1.sqrMagnitude < 1e-4f) e1 = Vector3.ProjectOnPlane(Vector3.forward, up);
            e1.Normalize();
            Vector3 e2 = Vector3.Cross(up, e1);
            site = center; slope = 90; dist = 0;
            float best = float.MaxValue;
            bool any = false;
            for (int ring = 0; ring <= 5; ring++)
            {
                float r = ring * 12f;
                int n = ring == 0 ? 1 : 8 + ring * 4;
                for (int k = 0; k < n; k++)
                {
                    float a = k * Mathf.PI * 2f / n;
                    Vector3 p = center + (e1 * Mathf.Cos(a) + e2 * Mathf.Sin(a)) * r;
                    if (!GroundSlope(p, up, out Vector3 ground, out float sl)) continue;
                    float score = sl + r * 0.05f;
                    if (score < best) { best = score; site = ground; slope = sl; dist = r; any = true; }
                }
            }
            return any;
        }

        /// <summary>Slope (degrees, plus a roughness penalty) of the ground under <paramref name="p"/> over ±4 m.</summary>
        private static bool GroundSlope(Vector3 p, Vector3 up, out Vector3 ground, out float slopeDeg)
        {
            ground = p; slopeDeg = 90;
            Vector3 e1 = Vector3.ProjectOnPlane(Vector3.right, up);
            if (e1.sqrMagnitude < 1e-4f) e1 = Vector3.ProjectOnPlane(Vector3.forward, up);
            e1 = e1.normalized * 4f;
            Vector3 e2 = Vector3.Cross(up, e1);
            Vector3[] offs = { Vector3.zero, e1, -e1, e2, -e2 };
            var pts = new Vector3[5];
            for (int i = 0; i < 5; i++)
            {
                if (!Physics.Raycast(p + offs[i] + up * 300f, -up, out RaycastHit hit, 900f, Layers.GroundMask, QueryTriggerInteraction.Ignore)) return false;
                pts[i] = hit.point;
            }
            ground = pts[0];
            Vector3 n = Vector3.Cross(pts[1] - pts[2], pts[3] - pts[4]);
            if (n.sqrMagnitude < 1e-8f) return false;
            float ang = Vector3.Angle(n, up);
            slopeDeg = Mathf.Min(ang, 180f - ang);
            float mid = Vector3.Dot((pts[1] + pts[2] + pts[3] + pts[4]) * 0.25f - pts[0], up);
            slopeDeg += Mathf.Abs(mid) * 5f; // a bump or a hole under the engine
            return true;
        }

        private double CurrentThrustCapability()
        {
            V.GetPropulsion(out double thrust, out _, false);
            return thrust;
        }

        // ------------------------------------------------------------------ EVA

        private IEnumerator SurfaceEva()
        {
            SetPhase("EVA");
            var lander = V;
            Part pod = null;
            foreach (var p in lander.Parts) if (p.Crew.Count > 0) { pod = p; break; }
            if (pod == null) { Check("crew aboard for EVA", false, "no crew"); yield break; }
            string crew = pod.Crew[0];
            Sim.RequestEva(pod, crew);
            yield return WaitUntil(() => V != null && V.IsEva, 10, "EVA spawn");
            var eva = V;
            Check("EVA started", eva.IsEva, $"{crew} outside, inherited velocity {(eva.Rb.linearVelocity - lander.Rb.linearVelocity).magnitude:F2} m/s rel. lander");
            _watchLander = true;
            StartCoroutine(LanderWatch(lander));
            var em = eva.RootPart.GetModule<EvaModule>();
            // Let go beside the hatch and drop to the ground (about 6 m/s in Luma's gravity).
            yield return WaitUntil(() => em.Grounded, 60, "EVA touches down");
            if (Failed) yield break;
            if (!em.OnTerrain) Note($"EVA came to rest on the lander ({EvaDiag(V, lander)}): walking off");
            // Walk ~12 m away from the lander, like a player: if stuck against a part, hop clear with the jetpack.
            Vector3 up = -eva.GravityAccel.normalized;
            Vector3 away = Vector3.ProjectOnPlane(eva.WorldCoM - lander.WorldCoM, up);
            if (away.sqrMagnitude < 0.01f) away = Vector3.ProjectOnPlane(Vector3.right, up);
            away.Normalize();
            // Distances are measured relative to the lander: the world frame follows the active vessel (floating
            // origin), so a world position remembered earlier is not a fixed point on the ground.
            double d0 = Vector3.ProjectOnPlane(eva.WorldCoM - lander.WorldCoM, up).magnitude;
            em.CameraRotation = Quaternion.LookRotation(away, up);
            em.MoveInput = new Vector2(0, 1);
            double walkStart = Sim.UT, lastProgressUT = Sim.UT, best = 0;
            int hops = 0;
            while (true)
            {
                if (V == null || !V.IsEva) { Check("EVA vessel kept", false, "the EVA vessel was lost"); yield break; }
                double d = Vector3.ProjectOnPlane(V.WorldCoM - lander.WorldCoM, up).magnitude - d0;
                if (d > 12) break;
                if (d > best + 0.3) { best = d; lastProgressUT = Sim.UT; }
                if (Sim.UT - lastProgressUT > 6 && hops < 3)
                {
                    hops++;
                    Note($"EVA stuck after {d:F1} m ({EvaDiag(V, lander)}): jetpack hop away from the lander");
                    em.JetpackOn = true;
                    em.VerticalInput = 1f;
                    yield return WaitSeconds(1.2);
                    em.VerticalInput = 0;
                    em.JetpackOn = false;
                    lastProgressUT = Sim.UT;
                }
                if (Sim.UT - walkStart > 120) break;
                yield return null;
            }
            em.MoveInput = Vector2.zero;
            yield return WaitUntil(() => em.Grounded, 15, "EVA settles after the walk");
            double walked = Vector3.ProjectOnPlane(V.WorldCoM - lander.WorldCoM, up).magnitude - d0;
            Check("walking in low gravity", walked > 10, $"{walked:F1} m in {Sim.UT - walkStart:F1} s ({EvaDiag(V, lander)})");
            if (Failed) yield break;
            Check("EVA on the surface", em.OnTerrain, $"standing on {(em.GroundCollider != null ? em.GroundCollider.name : "nothing")}, gravity {V.GravityAccel.magnitude:F2} m/s²");
            // Jump test
            em.JumpPressed = true;
            double maxH = 0;
            double h0 = V.AltitudeAGL;
            yield return WaitSeconds(0.3);
            yield return WaitUntil(() => { maxH = Math.Max(maxH, V.AltitudeAGL - h0); return em.Grounded; }, 20, "jump landing");
            Check("jump in low gravity", maxH > 0.8, $"apex {maxH:F2} m");
            yield return WaitSeconds(1);
            // Plant a flag.
            int flagsBefore = Sim.Handles.FindAll(x => x.Kind == VesselKind.Flag).Count;
            Sim.Enqueue(() => Sim.PlantFlag(V, $"{crew} - first landing on Luma"));
            yield return WaitSeconds(1);
            int flagsAfter = Sim.Handles.FindAll(x => x.Kind == VesselKind.Flag).Count;
            Check("flag planted on Luma", flagsAfter > flagsBefore, $"flags: {flagsAfter}");
            Snap("eva_flag");
            // Return to the hatch: walk back, then jetpack up to the hatch.
            SetPhase("return to hatch");
            Snap("lander_before_return");
            var hatch = pod.GetModule<CrewModule>();
            em.JetpackOn = false;
            double t0 = Sim.UT;
            bool boarded = false;
            while (Sim.UT - t0 < 240)
            {
                if (V == null || !V.IsEva) break;
                eva = V;
                em = eva.RootPart.GetModule<EvaModule>();
                Vector3 target = hatch.HatchWorldPosition + hatch.HatchWorldNormal * 0.9f;
                Vector3 delta = target - eva.WorldCoM;
                Vector3 horiz = Vector3.ProjectOnPlane(delta, up);
                float vert = Vector3.Dot(delta, up);
                Vector3 relVel = eva.Rb.linearVelocity - lander.Rb.GetPointVelocity(hatch.HatchWorldPosition);
                if (horiz.magnitude > 2.2f && em.Grounded && !em.JetpackOn)
                {
                    em.CameraRotation = Quaternion.LookRotation(horiz.normalized, up);
                    em.MoveInput = new Vector2(0, 1);
                }
                else
                {
                    // Jetpack PD control towards the hatch.
                    em.JetpackOn = true;
                    Vector3 hv = Vector3.ProjectOnPlane(relVel, up);
                    float vv = Vector3.Dot(relVel, up);
                    Vector3 hCmd = horiz * 0.6f - hv * 1.2f;
                    float vCmd = vert * 0.8f - vv * 1.4f + 0.62f; // + hover bias (g / thrust accel)
                    if (hCmd.sqrMagnitude > 1e-4f) em.CameraRotation = Quaternion.LookRotation(hCmd.normalized, up);
                    em.MoveInput = new Vector2(0, Mathf.Clamp01(hCmd.magnitude));
                    em.VerticalInput = Mathf.Clamp(vCmd, -1f, 1f);
                }
                if ((hatch.HatchWorldPosition - eva.WorldCoM).magnitude < EvaModule.BoardRange - 0.3f && relVel.magnitude < 1.5f)
                {
                    em.MoveInput = Vector2.zero;
                    em.VerticalInput = 0;
                    Snap("before_boarding");
                    Note($"boarding: lander AGL {lander.AltitudeAGL:F2} m, situation {lander.Situation}, {LegSummary(lander)}");
                    Sim.RequestBoard(eva);
                    yield return WaitSeconds(0.5);
                    if (Sim.ActiveVessel != null && !Sim.ActiveVessel.IsEva) { boarded = true; break; }
                }
                yield return null;
            }
            _watchLander = false;
            Snap("boarded");
            Check("crew boarded the lander", boarded && V.CrewCount > 0, boarded ? $"{crew} back aboard" : "failed to board");
        }

        // ------------------------------------------------------------------ ascent from Luma

        private IEnumerator LunarAscent()
        {
            SetPhase("lunar ascent");
            var v = V;
            // Head east (the way the surface turns: a landed vessel's orbital velocity points there) so the orbit
            // lies in Luma's orbital plane, which is what the return burn needs. The lander's roll after touchdown
            // is arbitrary, so steer by direction rather than holding a fixed key.
            Vector3 east = Vector3.ProjectOnPlane((Vector3)v.TrueVelocity, LocalUp);
            east = east.sqrMagnitude > 1e-4f ? east.normalized : East;
            Sas(SasMode.StabilityAssist, SpeedMode.Surface);
            Throttle(1);
            // Legs come up once clear of the ground (retracting them first would drop the lander onto its engine).
            yield return WaitUntil(() => V.AltitudeAGL > 30, 60, "lift-off");
            if (Failed) yield break;
            V.Ctrl.LegsDeployed = false;
            yield return WaitUntil(() => V.AltitudeAGL > 150, 60, "clear surface");
            SetPhase("lunar gravity turn");
            while (Sim.UT - _phaseStartUT < 40)
            {
                Vector3 up = LocalUp;
                Vector3 dir = Vector3.RotateTowards(up, Vector3.ProjectOnPlane(east, up).normalized, 50 * Mathf.Deg2Rad, 0);
                SteerTowards(dir);
                if (NoseAngle(dir) < 3 && Sim.UT - _phaseStartUT > 4) break;
                yield return null;
            }
            ReleaseKeys();
            yield return WaitSeconds(3);
            Sas(SasMode.Prograde, SpeedMode.Orbit);
            yield return WaitUntil(() => V.Orbit.ApoapsisRadius - Luma.Radius > 22000, 400, "lunar apoapsis");
            Throttle(0);
            var o = V.Orbit;
            double tAp = Sim.UT + o.TimeToApoapsis(Sim.UT);
            ManeuverPlanner.AddNode(V, tAp, ManeuverPlanner.CircularizeDeltaV(o, tAp));
            yield return ExecuteNode("Luma orbit circularization", 0.3);
            var oc = V.Orbit;
            Check("lunar orbit after ascent", oc.PeriapsisRadius - Luma.Radius > 8000, $"Pe {(oc.PeriapsisRadius - Luma.Radius) / 1000:F1} km");
            Note($"delta-v left in Luma orbit after ascent: {DvLeft()}");
        }

        // ------------------------------------------------------------------ return

        private IEnumerator TransEarthInjection()
        {
            SetPhase("plan return");
            var v = V;
            var o = v.Orbit;
            double best = double.MaxValue, bt = 0, bdv = 0, bpe = 0;
            for (int i = 0; i < 90; i++)
            {
                double t = Sim.UT + 120 + o.Period * i / 90.0;
                for (double dv = 220; dv <= 420; dv += 10)
                {
                    double s = ReturnScore(o, t, dv, 32000, out double pe);
                    if (s < best) { best = s; bt = t; bdv = dv; bpe = pe; }
                }
            }
            double stepT = o.Period / 90, stepV = 10;
            for (int it = 0; it < 60; it++)
            {
                bool imp = false;
                foreach (var (dt, dd) in new[] { (stepT, 0.0), (-stepT, 0.0), (0.0, stepV), (0.0, -stepV) })
                {
                    double s = ReturnScore(o, bt + dt, bdv + dd, 32000, out double pe);
                    if (s < best) { best = s; bt += dt; bdv += dd; bpe = pe; imp = true; }
                }
                if (!imp) { stepT *= 0.5; stepV *= 0.5; }
                if (stepT < 0.05 && stepV < 0.01) break;
            }
            Check("return trajectory to Tellus found", best < 20000, $"burn {bdv:F1} m/s in {bt - Sim.UT:F0} s, Tellus Pe {bpe / 1000:F1} km");
            if (best > 1e6) yield break;
            ManeuverPlanner.AddNode(v, bt, bdv);
            SetPhase("trans-Tellus injection");
            yield return ExecuteNode("TTI", 0.2);
            Note($"delta-v left after trans-Tellus injection: {DvLeft()}");
            // Coast home with warp, correcting periapsis once outside Luma's SOI.
            SetPhase("coast home");
            var traj = Scene.Trajectory;
            traj.Recompute(V);
            OrbitPatch home = null;
            foreach (var p in traj.Current) if (p.Body == Tellus) { home = p; break; }
            if (home == null) { Check("trajectory returns to Tellus", false, "no Tellus patch"); yield break; }
            yield return WarpUntil(home.StartUT + 120, "leave Luma SOI");
            double pe2 = V.Orbit.PeriapsisRadius - Tellus.Radius;
            Note($"Tellus periapsis after SOI exit {pe2 / 1000:F1} km");
            if (Math.Abs(pe2 - 32000) > 6000)
            {
                // Radial/prograde correction burn now.
                double target = 32000;
                double bestS = double.MaxValue, bp = 0, br = 0;
                var oo = V.Orbit;
                double tn = Sim.UT + 300;
                for (double pro = -30; pro <= 30; pro += 2)
                    for (double rad = -30; rad <= 30; rad += 2)
                    {
                        var nodes = new List<NodeSpec> { new NodeSpec(tn, pro, 0, rad) };
                        var pp = PatchedConics.Predict(oo, Tellus, tn - 1, nodes, 2, 20 * 86400.0, false);
                        if (pp.Count < 2) continue;
                        double pe = pp[1].Orbit.PeriapsisRadius - Tellus.Radius;
                        double s = Math.Abs(pe - target) + Math.Sqrt(pro * pro + rad * rad) * 100;
                        if (s < bestS) { bestS = s; bp = pro; br = rad; }
                    }
                ManeuverPlanner.AddNode(V, tn, bp, 0, br);
                yield return ExecuteNode("return correction", 0.1);
                pe2 = V.Orbit.PeriapsisRadius - Tellus.Radius;
            }
            Check("return periapsis in reentry corridor", pe2 > 15000 && pe2 < 50000, $"Pe {pe2 / 1000:F1} km");
            // Warp to just above the atmosphere.
            double tAtm = PatchedConics.FindRadiusDescending(V.Orbit, Tellus.Radius + 75000, Sim.UT);
            if (!double.IsNaN(tAtm)) yield return WarpUntil(tAtm - 60, "approach Tellus");
        }

        private double ReturnScore(Orbit o, double t, double dv, double targetPe, out double pe)
        {
            pe = double.NaN;
            var nodes = new List<NodeSpec> { new NodeSpec(t, dv, 0, 0) };
            var patches = PatchedConics.Predict(o, Luma, t - 1, nodes, 4, 20 * 86400.0, false);
            foreach (var p in patches)
            {
                if (p.Body != Tellus) continue;
                pe = p.Orbit.PeriapsisRadius - Tellus.Radius;
                return Math.Abs(pe - targetPe) + dv * 2;
            }
            return 1e7;
        }
    }
}
