using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>
    /// Scripted pilot used for automated verification and the demo mode. It flies exclusively through
    /// player-equivalent controls: throttle, pitch/yaw/roll input, SAS modes and speed modes, staging,
    /// landing-leg toggle, maneuver nodes (same API as the map UI), time warp, EVA inputs, flag, board.
    /// No positions or velocities are ever written directly.
    /// </summary>
    public sealed partial class MissionAutopilot : MonoBehaviour
    {
        public FlightSceneController Scene;
        public string Mission = "orbit";
        public bool QuitWhenDone;
        public readonly List<string> Log = new List<string>();
        public readonly List<(string check, bool pass, string detail)> Checks = new List<(string, bool, string)>();
        public string Phase = "idle";
        public bool Finished;
        public bool Failed;
        /// <summary>Use 4x physics warp during long burns/coasts (as a player would with Alt+.).</summary>
        public bool UsePhysicsWarp = true;

        private FlightSim Sim => Scene.Sim;
        private Vessel V => Sim.ActiveVessel;
        private double _phaseStartUT;

        /// <summary>Craft launched for an automated mission.</summary>
        public static Persistence.CraftDesign CraftFor(string mission)
        {
            var db = Parts.PartDatabase.Instance;
            if (mission == "failures") return TestCraft.Puller(db);
            if (mission == "docking") return TestCraft.DockChaser(db);
            string name = mission == "orbit" || mission == "persistence" ? "Meridian Orbiter" : mission == "suborbital" ? "Skylark Suborbital" : "Luma Pathfinder";
            // Any craft (a .craft.json file or a starter name) can be flown with -craft; the ascent test uses it.
            string custom = GameSession.AutoTestCraft;
            if (!string.IsNullOrEmpty(custom))
            {
                if (File.Exists(custom)) return SaveStorage.LoadCraft(custom);
                name = custom;
            }
            foreach (var c in SaveStorage.LoadStarterCraft()) if (c.name == name) return c;
            foreach (var c in Construction.StarterCraft.All(db)) if (c.name == name) return c;
            return Construction.StarterCraft.Meridian(db);
        }

        public static MissionAutopilot Start(FlightSceneController scene, string mission, bool quitWhenDone)
        {
            if (mission == "idle") return null; // test setup only (separate save, no scripted pilot)
            var ap = scene.gameObject.AddComponent<MissionAutopilot>();
            ap.Scene = scene;
            ap.Mission = mission;
            ap.QuitWhenDone = quitWhenDone;
            scene.Input.ControlsLocked = true;
            scene.Sim.CrewLost += (crew, reason) => { if (ap._crewLostReason == null) ap._crewLostReason = $"{crew} lost: {reason}"; };
            ap.StartCoroutine(ap.Run());
            return ap;
        }

        private void Note(string msg)
        {
            string line = $"[AutoTest {Sim.UT,10:F1}] {msg}";
            Log.Add(line);
            Debug.Log(line);
        }

        private void Check(string name, bool pass, string detail)
        {
            Checks.Add((name, pass, detail));
            Note($"CHECK {(pass ? "PASS" : "FAIL")}: {name} — {detail}");
            if (!pass) Failed = true;
        }

        /// <summary>Saves a screenshot of the current view next to the report (visual record of the run).</summary>
        private void Snap(string name)
        {
            try
            {
                string dir = Path.Combine(SaveStorage.Root, "autotest_shots");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"{Mission}_{Log.Count:000}_{name}.png");
                ScreenCapture.CaptureScreenshot(path);
                Note($"screenshot: {Path.GetFileName(path)}");
            }
            catch (Exception e) { Note("screenshot failed: " + e.Message); }
        }

        private void SetPhase(string p)
        {
            Phase = p;
            _phaseStartUT = Sim.UT;
            Note("PHASE " + p);
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(1.5f);
            IEnumerator body;
            switch (Mission)
            {
                case "suborbital": body = SuborbitalMission(); break;
                case "orbit": body = OrbitMission(); break;
                case "failures": body = FailuresMission(); break;
                case "docking": body = DockingMission(); break;
                case "persistence": body = PersistenceMission(); break;
                case "ascent": body = AscentOnlyMission(); break;
                case "lunar-surface": body = LunarSurfaceMission(); break;
                case "luma-idle": body = LunarSurfaceMission(true); break;
                case "lunar-landing": body = LunarLandingMission(); break;
                default: body = LunarMission(); break;
            }
            // Nested routines are driven here (not by Unity) so every step runs inside the exception
            // capture and the watchdog below sees vessel or crew loss immediately.
            var stack = new Stack<IEnumerator>();
            stack.Push(body);
            float nullSince = -1f;
            while (stack.Count > 0)
            {
                if (!Failed && _crewLostReason != null && !ExpectLosses) Check("crew survived", false, _crewLostReason);
                if (!Failed && !ExpectLosses)
                {
                    if (Sim.ActiveVessel == null)
                    {
                        if (nullSince < 0) nullSince = Time.realtimeSinceStartup;
                        else if (Time.realtimeSinceStartup - nullSince > 2f) Check("active vessel exists", false, "the vessel was destroyed");
                    }
                    else nullSince = -1f;
                }
                if (Failed) break;
                if (Sim.ActiveVessel == null && !ExpectLosses) { yield return null; continue; }
                var top = stack.Peek();
                object current;
                try
                {
                    if (!top.MoveNext()) { stack.Pop(); continue; }
                    current = top.Current;
                }
                catch (Exception e)
                {
                    Note("EXCEPTION: " + e);
                    Failed = true;
                    break;
                }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }

        private string _crewLostReason;

        private void Finish()
        {
            Finished = true;
            Phase = "report";
            Sim.StopWarp();
            Scene.Input.ControlsLocked = false;
            var sb = new StringBuilder();
            sb.AppendLine($"# The Astraea Program automated mission report: {Mission}");
            sb.AppendLine($"Result: {(Failed ? "FAILED" : "PASSED")}");
            sb.AppendLine($"Checks: {Checks.FindAll(c => c.pass).Count}/{Checks.Count} passed");
            sb.AppendLine();
            foreach (var c in Checks) sb.AppendLine($"- [{(c.pass ? "x" : " ")}] {c.check}: {c.detail}");
            sb.AppendLine();
            sb.AppendLine("## Log");
            foreach (var l in Log) sb.AppendLine("    " + l);
            string text = sb.ToString();
            Debug.Log(text);
            try
            {
                string path = GameSession.AutoTestReportPath;
                if (string.IsNullOrEmpty(path)) path = Path.Combine(SaveStorage.Root, $"autotest_{Mission}.md");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text);
                Debug.Log("[AutoTest] report written to " + path);
            }
            catch (Exception e) { Debug.LogError("report write failed: " + e.Message); }
            if (QuitWhenDone) Application.Quit(Failed ? 1 : 0);
        }

        // ------------------------------------------------------------------ control helpers

        private void Throttle(float t) { if (V != null) V.Ctrl.Throttle = Mathf.Clamp01(t); }

        private void Sas(SasMode mode, SpeedMode? speed = null)
        {
            var v = V;
            if (v == null) return;
            if (speed.HasValue) v.Ctrl.SpeedMode = speed.Value;
            if (v.Ctrl.SasMode != mode || !v.Ctrl.Sas) FlightInput.SetSasMode(v, mode);
        }

        private void Stage()
        {
            var v = V;
            if (v == null) return;
            v.ActivateNextStage();
        }

        private void ReleaseKeys()
        {
            var v = V;
            if (v == null) return;
            v.Ctrl.Pitch = v.Ctrl.Yaw = v.Ctrl.Roll = 0;
        }

        /// <summary>
        /// Flies the nose towards <paramref name="dir"/> with pitch/yaw input, like a pilot steering by hand with
        /// SAS in stability-assist (which damps roll meanwhile). Call every frame while steering.
        /// </summary>
        private void SteerTowards(Vector3 dir)
        {
            var v = V;
            if (v == null) return;
            if (!v.Ctrl.Sas || v.Ctrl.SasMode != SasMode.StabilityAssist) Sas(SasMode.StabilityAssist);
            Quaternion rot = v.ControlRotation;
            Vector3 nose = rot * Vector3.up;
            Vector3 axis = Vector3.Cross(nose, dir.normalized);
            float sin = axis.magnitude;
            float ang = Mathf.Atan2(sin, Vector3.Dot(nose, dir.normalized));
            Vector3 err = sin > 1e-6f ? Quaternion.Inverse(rot) * (axis / sin * ang) : Vector3.zero;
            Vector3 w = Quaternion.Inverse(rot) * v.Rb.angularVelocity;
            Vector3 inertia = v.ControlFrameInertia, authority = v.TorqueAuthority;
            float Axis(int k)
            {
                float I = Mathf.Max(inertia[k], 1f), tau = Mathf.Max(authority[k], 1f);
                float alphaMax = tau / I, e = err[k];
                float wDes = Mathf.Sign(e) * Mathf.Min(Mathf.Clamp(alphaMax * 3f, 0.08f, 0.5f), Mathf.Min(Mathf.Abs(e) * 2f, Mathf.Sqrt(alphaMax * Mathf.Abs(e))));
                return Mathf.Clamp((wDes - w[k]) / 0.2f * I / tau, -1f, 1f);
            }
            // PilotTorque = (Pitch, -Roll, -Yaw) in the control frame.
            v.Ctrl.Pitch = Axis(0);
            v.Ctrl.Yaw = -Axis(2);
        }

        /// <summary>Delta-v left, as the flight HUD shows it (vacuum figures).</summary>
        private string DvLeft()
        {
            var v = V;
            if (v == null) return "no vessel";
            var stages = v.ComputeStageDeltaV(9.80665, 0);
            double total = 0;
            var sb = new StringBuilder();
            foreach (var s in stages)
            {
                if (s.DeltaVVac < 0.5) continue;
                total += s.DeltaVVac;
                sb.Append(sb.Length == 0 ? "" : " + ").Append($"{s.DeltaVVac:F0}");
            }
            return $"{total:F0} m/s vac ({(sb.Length > 0 ? sb.ToString() : "empty")})";
        }

        private IEnumerator WaitUntil(Func<bool> cond, double timeoutUT, string what)
        {
            double deadline = Sim.UT + timeoutUT;
            while (!cond())
            {
                if (Sim.UT > deadline) { Check("timeout: " + what, false, $"not reached within {timeoutUT:F0} s"); yield break; }
                if (V == null) { Check("vessel lost while waiting: " + what, false, ""); yield break; }
                yield return null;
            }
        }

        /// <summary>
        /// Time-warps to <paramref name="ut"/> like a player: throttle to zero, wait for the engines to spool down
        /// (warp is refused under thrust), engage rails warp and re-engage it if it drops out early (e.g. at an
        /// SOI change). Falls back to real time if warp is not allowed (atmosphere, low altitude).
        /// </summary>
        private IEnumerator WarpUntil(double ut, string what)
        {
            Throttle(0);
            int refused = 0;
            bool warned = false;
            while (Sim.UT < ut - 2)
            {
                if (V == null) yield break;
                if (!Sim.Warp.OnRails)
                {
                    Sim.WarpTo(ut);
                    if (!Sim.Warp.OnRails && ++refused > 300 && !warned)
                    {
                        warned = true;
                        Sim.MaxRailsIndex(out string why);
                        Note($"{what}: rails warp unavailable ({why}), continuing at physics speed");
                        Sim.SetPhysicsWarp(3);
                    }
                }
                yield return null;
            }
            // Let the warp run out (it slows down near the target) so the vessel is back in physics on return.
            double tEnd = Sim.UT + 10;
            while (Sim.Warp.OnRails && Sim.UT < tEnd) yield return null;
            if (Sim.Warp.OnRails) Sim.StopWarp();
            if (Sim.Warp.PhysicsIndex > 0) Sim.SetPhysicsWarp(0);
        }

        private IEnumerator WaitSeconds(double s)
        {
            double t = Sim.UT + s;
            while (Sim.UT < t) yield return null;
        }

        /// <summary>Angle between the vessel nose and a world direction (degrees).</summary>
        private float NoseAngle(Vector3 dir)
        {
            var v = V;
            return Vector3.Angle(v.ControlRotation * Vector3.up, dir);
        }

        private Vector3 LocalUp => (Vector3)V.TruePosition.normalized;
        private Vector3 East
        {
            get
            {
                return Geo.East(LocalUp);
            }
        }

        /// <summary>Stage automatically when the running engines have burned out (like a player watching fuel).</summary>
        private bool ShouldStage()
        {
            var v = V;
            if (v == null || v.CurrentStage < 0) return false;
            bool anyIgnited = false, anyRunning = false, anySolidEmpty = false;
            foreach (var p in v.Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e == null || !e.Ignited) continue;
                anyIgnited = true;
                bool hasFuel = v.Resources.Available(p, e.Def.propellant) > 0.5;
                if (hasFuel && !e.Flameout) anyRunning = true;
                if (e.IsSolid && !hasFuel) anySolidEmpty = true;
            }
            if (!anyIgnited) return true;
            if (anySolidEmpty) return true;
            return !anyRunning;
        }

        private IEnumerator StageUntilThrust()
        {
            // Stage (with small pauses) until an engine is running — but never beyond the last propulsive stage
            // (that would drop the capsule and arm the chute). "Running" means ignited with propellant, so an
            // engine still spooling up counts: an instantaneous thrust reading could trigger a spurious extra stage.
            for (int i = 0; i < 4; i++)
            {
                if (!HasEngineStageLeft()) { Note("no propulsive stage left"); yield break; }
                Stage();
                yield return WaitSeconds(0.6);
                if (!ShouldStage()) yield break;
                Note($"no engine running after staging ({EngineSummary()}): staging again");
            }
        }

        private string EngineSummary()
        {
            var v = V;
            if (v == null) return "no vessel";
            var sb = new StringBuilder($"{v.VesselName} {v.Parts.Count} parts, stage {v.CurrentStage}");
            foreach (var p in v.Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e != null) sb.Append($"; {p.Def.title} s{p.Stage} ign {e.Ignited} out {e.Flameout} thr {e.CurrentThrust / 1000:F0} kN");
            }
            return sb.ToString();
        }

        private double CurrentThrust()
        {
            double t = 0;
            if (V == null) return 0;
            foreach (var p in V.Parts) { var e = p.GetModule<EngineModule>(); if (e != null) t += e.CurrentThrust; }
            return t;
        }

        // ------------------------------------------------------------------ maneuver execution

        /// <summary>Warps to, points at and executes the first maneuver node until the remaining delta-v is ~0.</summary>
        private IEnumerator ExecuteNode(string label, double tolerance = 0.4)
        {
            var v = V;
            if (v.ManeuverNodes.Count == 0) { Check(label + " node exists", false, "no node"); yield break; }
            var info = ManeuverPlanner.Info(v, Sim.UT);
            Note($"{label}: dv {info.TotalDeltaV:F1} m/s, burn {info.BurnTime:F1} s, in {info.TimeToNode:F0} s");
            Sas(SasMode.Maneuver);
            // Point at the node first (attitude is kept while on rails), then warp to ~45 s before the burn,
            // like a player turning to the node marker before pressing warp-to-node.
            yield return WaitUntil(() => ManeuverPlanner.Info(V, Sim.UT).AngleToBurn < 5 || ManeuverPlanner.Info(V, Sim.UT).TimeToBurnStart < 50, 900, label + " pre-align");
            double warpTarget = info.NodeUT - info.BurnTime * 0.5 - 45;
            if (warpTarget > Sim.UT + 60)
            {
                yield return WarpUntil(warpTarget, label + " warp");
            }
            Sas(SasMode.Maneuver);
            // Settle attitude.
            yield return WaitUntil(() => ManeuverPlanner.Info(V, Sim.UT).AngleToBurn < 3 || ManeuverPlanner.Info(V, Sim.UT).TimeToBurnStart < 0, 400, label + " align");
            yield return WaitUntil(() => ManeuverPlanner.Info(V, Sim.UT).TimeToBurnStart <= 0, 3600, label + " burn start");
            double startUT = Sim.UT;
            double lastRemaining = double.MaxValue;
            double nextLog = Sim.UT;
            while (true)
            {
                var i = ManeuverPlanner.Info(V, Sim.UT);
                if (!i.Valid) break;
                double rem = i.RemainingDeltaV;
                if (rem < tolerance) break;
                if (rem > lastRemaining + 0.5 && rem < 5) break; // overshoot
                lastRemaining = Math.Min(lastRemaining, rem);
                // Throttle down for the last few m/s; don't burn while misaligned.
                V.GetPropulsion(out double thrust, out _);
                double accel = thrust / Math.Max(V.TotalMass, 1);
                float thr = accel > 0 ? (float)Math.Min(1.0, rem / (accel * 1.2) + 0.03) : 1f;
                if (i.AngleToBurn > 8) thr = 0;
                else if (i.AngleToBurn > 3) thr *= 0.3f;
                Throttle(thr);
                if (Sim.UT >= nextLog)
                {
                    nextLog = Sim.UT + 4;
                    Note($"{label}: {rem:F0} m/s to go, {i.AngleToBurn:F1}° off, throttle {thr:F2}, spin {V.Rb.angularVelocity.magnitude:F3} rad/s, SAS {V.Ctrl.SasMode}");
                }
                if (ShouldStage() && CurrentThrust() < 1 && thr > 0) { Note("staging during burn"); yield return StageUntilThrust(); }
                if (Sim.UT - startUT > 900) { Check(label + " burn duration", false, "burn took too long"); break; }
                yield return null;
            }
            Throttle(0);
            var fin = ManeuverPlanner.Info(V, Sim.UT);
            Note($"{label}: burn complete, residual {fin.RemainingDeltaV:F2} m/s after {Sim.UT - startUT:F1} s");
            if (V.ManeuverNodes.Count > 0) ManeuverPlanner.RemoveNode(V, V.ManeuverNodes[0]);
            Sas(SasMode.StabilityAssist);
            yield return null;
        }

        // ------------------------------------------------------------------ ascent

        /// <summary>
        /// Player-style ascent: full throttle, SAS hold, a short W (pitch) input to tip ~8 deg east,
        /// then SAS prograde (surface) gravity turn, stage on burnout, throttle back as apoapsis nears target.
        /// </summary>
        private IEnumerator Ascent(double targetAp)
        {
            SetPhase("ascent");
            var v = V;
            double ascentStartUT = Sim.UT;
            Throttle(1);
            Sas(SasMode.StabilityAssist, SpeedMode.Surface);
            Stage();
            yield return WaitSeconds(1.0);
            Check("liftoff", V != null && V.VerticalSpeed > 1, $"vertical speed {V?.VerticalSpeed:F1} m/s");
            yield return WaitUntil(() => V.SurfaceSpeed > 55 || V.AltitudeAGL > 900, 120, "pitch-over speed");
            // Pitch-over: hold D until the nose is tipped ~9-10° east (the rocket stands on the pad with its right side
            // facing east, as in KSP), then let the gravity turn do the rest.
            V.GetPropulsion(out double liftThrust, out _);
            double twr = liftThrust / (V.TotalMass * V.GravityAccel.magnitude);
            float kick = (float)MathD.Clamp(6 + 1.5 * twr, 9, 10);
            SetPhase("pitch-over");
            Note($"pitch-over to {kick:F1}° (TWR {twr:F2})");
            while (NoseAngle(LocalUp) < kick && Sim.UT - _phaseStartUT < 20)
            {
                V.Ctrl.Yaw = 1f;
                V.Ctrl.Pitch = 0;
                yield return null;
            }
            ReleaseKeys();
            yield return WaitSeconds(4);
            Sas(SasMode.Prograde, SpeedMode.Surface);
            SetPhase("gravity turn");
            if (UsePhysicsWarp) Sim.SetPhysicsWarp(3);
            bool orbitMode = false, holdingAp = false, steering = false;
            double maxPitchUp = 0;
            double maxQ = 0;
            while (true)
            {
                v = V;
                if (v == null) yield break;
                maxQ = Math.Max(maxQ, v.DynamicPressure);
                if (!orbitMode && v.Altitude > 38000)
                {
                    v.Ctrl.SpeedMode = SpeedMode.Orbit;
                    orbitMode = true;
                }
                double ap = v.Orbit != null ? v.Orbit.ApoapsisRadius - v.MainBody.Radius : 0;
                double peNow = v.Orbit != null ? v.Orbit.PeriapsisRadius - v.MainBody.Radius : -v.MainBody.Radius;
                if (ap > targetAp && v.Orbit.IsElliptic)
                {
                    // Apoapsis reached. Above the thick air it is safe to coast; lower down, keep the apoapsis at
                    // the target with a trickle of thrust so drag does not eat it during the long coast.
                    if (v.Altitude > 60000 || peNow > 70000) break;
                    if (!holdingAp) { holdingAp = true; Note($"apoapsis {ap / 1000:F1} km reached at {v.Altitude / 1000:F1} km: holding it until clear of the air"); }
                }
                // A player watching the vertical speed: when a low-thrust upper stage lets the climb flatten out,
                // fly a few degrees above prograde (just enough to keep climbing) rather than following prograde
                // back down into the thick air. The wanted climb rate shrinks with altitude; the pitch-up is
                // limited by dynamic pressure so the angle of attack stays safe.
                double vsWanted = MathD.Clamp((50000 - v.Altitude) * 0.008, 0, 200);
                double pitchUp = 0;
                if (v.Altitude > 12000 && !holdingAp && ap < targetAp && v.VerticalSpeed < vsWanted)
                {
                    double maxUp = MathD.Clamp(25 * (1 - (v.DynamicPressure - 3000) / 15000), 3, 25);
                    pitchUp = MathD.Clamp((vsWanted - v.VerticalSpeed) * 0.15, 0, maxUp);
                }
                if (pitchUp > 1.5 || (steering && pitchUp > 0.2))
                {
                    if (!steering)
                    {
                        steering = true;
                        Note($"climb flattening at {v.Altitude / 1000:F1} km (vertical speed {v.VerticalSpeed:F0} m/s): flying above prograde");
                    }
                    maxPitchUp = Math.Max(maxPitchUp, pitchUp);
                    if (v.TryGetSasDirection(SasMode.Prograde, out Vector3 pro))
                        SteerTowards(Vector3.RotateTowards(pro, LocalUp, (float)(pitchUp * Mathf.Deg2Rad), 0));
                }
                else if (steering)
                {
                    steering = false;
                    ReleaseKeys();
                    Sas(SasMode.Prograde);
                    Note($"climb restored at {v.Altitude / 1000:F1} km (vertical speed {v.VerticalSpeed:F0} m/s, up to {maxPitchUp:F0}° above prograde): back to prograde");
                }
                // Ease off when close to target apoapsis for precision.
                float thr = holdingAp ? (float)MathD.Clamp((targetAp + 1500 - ap) / 3000.0, 0, 1)
                                      : ap > targetAp * 0.95 ? 0.25f : 1f;
                Throttle(thr);
                if (ShouldStage())
                {
                    if (!HasEngineStageLeft()) { Check("ascent propulsion", false, "no engine left to stage (vessel lost its engines)"); yield break; }
                    Note($"staging at {v.Altitude / 1000:F1} km, {v.SurfaceSpeed:F0} m/s");
                    yield return StageUntilThrust();
                }
                if (Sim.UT - _phaseStartUT > 900) { Check("ascent duration", false, "ascent too long"); yield break; }
                yield return null;
            }
            Throttle(0);
            if (steering) { ReleaseKeys(); Sas(SasMode.Prograde); }
            Note($"MECO: Ap {(V.Orbit.ApoapsisRadius - V.MainBody.Radius) / 1000:F1} km, max Q {maxQ / 1000:F1} kPa, delta-v left {DvLeft()}");
            Check("max dynamic pressure survivable", maxQ < 60000, $"{maxQ / 1000:F1} kPa");
            // Coast out of the atmosphere with SAS prograde.
            SetPhase("coast to space");
            Sas(SasMode.Prograde, SpeedMode.Orbit);
            for (int attempt = 0; attempt < 4; attempt++)
            {
                yield return WaitUntil(() => V.Altitude > 70500 || V.VerticalSpeed < 0, 900, "leave atmosphere");
                if (Failed || V.Altitude > 70500) break;
                // Fell back before leaving the atmosphere (drag ate the apoapsis): boost again, pointing up while
                // the vertical speed is low, then coast out of the air before circularising.
                yield return Reboost(targetAp);
                if (Failed) yield break;
            }
            {
                // Top up apoapsis lost to drag.
                double ap2 = V.Orbit.ApoapsisRadius - V.MainBody.Radius;
                if (ap2 < targetAp - 1500)
                {
                    Throttle(0.3f);
                    yield return WaitUntil(() => V.Orbit.ApoapsisRadius - V.MainBody.Radius > targetAp, 120, "apoapsis top-up");
                    Throttle(0);
                }
                // Circularize with a maneuver node at apoapsis.
                SetPhase("circularize");
                var o = V.Orbit;
                double tAp = Sim.UT + o.TimeToApoapsis(Sim.UT);
                double dv = ManeuverPlanner.CircularizeDeltaV(o, tAp);
                ManeuverPlanner.AddNode(V, tAp, dv);
                yield return ExecuteNode("circularization", 0.3);
            }
            var orbit = V.Orbit;
            double pe = orbit.PeriapsisRadius - V.MainBody.Radius, apf = orbit.ApoapsisRadius - V.MainBody.Radius;
            Check("stable orbit reached", pe > 70000 && orbit.IsElliptic, $"Pe {pe / 1000:F1} km, Ap {apf / 1000:F1} km, e {orbit.Eccentricity:F4}");
            Note($"in orbit {Sim.UT - ascentStartUT:F0} s after launch with {DvLeft()} left");
            Snap("orbit");
        }

        private IEnumerator Reboost(double targetAp)
        {
            SetPhase("re-boost");
            Note($"apoapsis decayed; falling at {V.Altitude / 1000:F1} km: boosting the apoapsis again");
            double t0 = Sim.UT;
            double R = V.MainBody.Radius;
            while (V.Orbit.ApoapsisRadius - R < targetAp || V.VerticalSpeed < 20)
            {
                Sas(V.VerticalSpeed < 20 ? SasMode.RadialOut : SasMode.Prograde, SpeedMode.Orbit);
                Throttle(1);
                if (ShouldStage())
                {
                    if (!HasEngineStageLeft()) { Check("re-boost", false, "out of propellant"); yield break; }
                    yield return StageUntilThrust();
                }
                if (V.Altitude < 25000) { Check("re-boost", false, "fell back into the lower atmosphere"); yield break; }
                if (V.Orbit.ApoapsisRadius - R > targetAp * 1.5) break;
                if (Sim.UT - t0 > 600) { Check("re-boost", false, "burn took too long"); yield break; }
                yield return null;
            }
            Throttle(0);
            Sas(SasMode.Prograde, SpeedMode.Orbit);
            SetPhase("coast to space");
        }

        /// <summary>True if a later stage still contains an engine (so staging can restore thrust).</summary>
        private bool HasEngineStageLeft()
        {
            var v = V;
            if (v == null) return false;
            foreach (var p in v.Parts)
                if (p.Stage >= 0 && p.Stage <= v.CurrentStage && p.Def.engine != null) return true; // CurrentStage = next to fire
            return false;
        }

        // ------------------------------------------------------------------ missions

        /// <summary>Launch to an 82 km orbit and report the delta-v left: used to compare designs.</summary>
        private IEnumerator AscentOnlyMission()
        {
            Note($"craft: {V.VesselName}, {V.Parts.Count} parts, {V.TotalMass / 1000:F1} t, delta-v {DvLeft()}");
            yield return Ascent(82000);
            if (Failed) yield break;
            Check("delta-v left in orbit", true, DvLeft());
        }

        private IEnumerator OrbitMission()
        {
            yield return Ascent(80000);
            if (Failed) yield break;
            yield return OrbitStabilityCheck(2);
            if (Failed) yield break;
            yield return OrbitalEva();
            if (Failed) yield break;
            yield return Deorbit();
        }

        /// <summary>Coasts for several orbits (rails warp + physics) and verifies there is no artificial drift.</summary>
        private IEnumerator OrbitStabilityCheck(int orbits)
        {
            SetPhase("orbit stability");
            var o0 = V.Orbit;
            double a0 = o0.SemiMajorAxis, e0 = o0.Eccentricity;
            // Physics coast for 60 s
            yield return WaitSeconds(60);
            var o1 = V.Orbit;
            Check("physics coast: no drift in semi-major axis", Math.Abs(o1.SemiMajorAxis - a0) < 5, $"da = {o1.SemiMajorAxis - a0:F3} m after 60 s");
            // Rails warp several orbits
            double t = Sim.UT + o0.Period * orbits;
            yield return WarpUntil(t, "warp orbits");
            var o2 = V.Orbit;
            Check("rails warp: orbit preserved", Math.Abs(o2.SemiMajorAxis - a0) < 5 && Math.Abs(o2.Eccentricity - e0) < 1e-4,
                $"da = {o2.SemiMajorAxis - a0:F3} m, de = {o2.Eccentricity - e0:E2} after {orbits} orbits");
        }

        private IEnumerator Deorbit()
        {
            SetPhase("deorbit");
            // Retrograde burn to put periapsis at ~25 km.
            Sas(SasMode.Retrograde, SpeedMode.Orbit);
            yield return WaitUntil(() => NoseAngle(-(Vector3)V.TrueVelocity.normalized) < 5, 120, "align retrograde");
            while (V.Orbit.PeriapsisRadius - V.MainBody.Radius > 25000)
            {
                Throttle(1);
                if (ShouldStage()) yield return StageUntilThrust();
                yield return null;
            }
            Throttle(0);
            yield return Reentry();
        }

        /// <summary>Reentry: drop propulsion stages, hold retrograde, arm chutes, land.</summary>
        private IEnumerator Reentry()
        {
            SetPhase("reentry");
            // Engines off and wound down first: a stage dropped while its engine is still spooling down pushes itself
            // into the capsule (seen as a 6.8 m/s collision right after the deorbit burn).
            Throttle(0);
            yield return WaitUntil(() => CurrentThrust() < 1, 10, "engines shut down");
            // Separate everything except the capsule (stage until only chutes remain).
            int guard = 0;
            while (guard++ < 6 && V.CurrentStage > 0)
            {
                bool hasDecoupler = false;
                foreach (var p in V.Parts) if (p.Stage == V.CurrentStage && p.Def.decoupler != null) hasDecoupler = true;
                bool hasChute = false;
                foreach (var p in V.Parts) if (p.Stage == V.CurrentStage && p.Def.parachute != null) hasChute = true;
                if (hasChute) break;
                Stage();
                yield return WaitSeconds(1.0);
                if (!hasDecoupler) continue;
            }
            Sas(SasMode.Retrograde, SpeedMode.Surface);
            double maxTemp = 0, maxG = 0;
            string hottest = "";
            yield return WaitUntil(() => V.Altitude < 45000 || V.Situation == Situation.Landed, 3600, "reach 45 km");
            Snap("reentry");
            yield return WaitUntil(() => V.Altitude < 30000 || V.Situation == Situation.Landed, 3600, "reach 30 km");
            // Arm the chutes once the HUD shows them safe to open: an armed canopy opens as soon as the air is thick
            // enough, and after a steep fall the capsule can still be supersonic here (an early canopy tears).
            ParachuteModule chute = null;
            foreach (var p in V.Parts) { var c = p.GetModule<ParachuteModule>(); if (c != null) { chute = c; break; } }
            if (chute != null)
                yield return WaitUntil(() => V.Situation == Situation.Landed || V.Situation == Situation.Splashed || V.AltitudeAGL < 2500
                                             || chute.DeploySafety(out _) == ParachuteModule.Safety.Safe, 3600, "parachute safe to open");
            Note($"arming the parachute at {V.Altitude / 1000:F1} km, {V.SurfaceSpeed:F0} m/s");
            Stage();
            while (V != null && V.Situation != Situation.Landed && V.Situation != Situation.Splashed)
            {
                foreach (var p in V.Parts)
                    if (p.SkinTemp > maxTemp) { maxTemp = p.SkinTemp; hottest = p.Def.title; }
                maxG = Math.Max(maxG, V.GForce);
                if (Sim.UT - _phaseStartUT > 3600) break;
                yield return null;
            }
            if (V == null) { Check("capsule survived reentry", false, "vessel destroyed"); yield break; }
            yield return WaitSeconds(3);
            bool crewOk = V.CrewCount > 0;
            Snap("touchdown");
            Check("capsule landed safely with crew", crewOk && (V.Situation == Situation.Landed || V.Situation == Situation.Splashed),
                $"{V.Situation} at {V.SurfaceSpeed:F1} m/s, crew {V.CrewCount}, max skin {maxTemp:F0} K ({hottest}), max {maxG:F1} g");
        }
    }
}
