using System;
using System.Collections;
using System.Collections.Generic;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Test-only craft used by the failure scenarios.</summary>
    public static class TestCraft
    {
        /// <summary>
        /// A deliberately bad design: a Goliath engine mounted on top of a light stack decoupler as a "puller".
        /// At ignition the decoupler joint carries almost the whole thrust in tension and fails.
        /// </summary>
        public static CraftDesign Puller(PartDatabase db)
        {
            var d = new CraftDesign { name = "Puller Test Rig", description = "Structural-failure test: engine pulling on a weak joint." };
            var a = new CraftAssembler(d, db);
            int core = a.AddRoot("probe_wren");
            int tank = a.AttachStack(core, "bottom", "tank_s2_long", "top");
            int dec = a.AttachStack(core, "top", "dec_s1", "bottom");
            int eng = a.AttachStack(dec, "top", "eng_goliath", "bottom");
            // The engine's own propellant: the decoupler below it (correctly) blocks fuel from the rig's tank.
            a.AttachStack(eng, "top", "tank_s2_short", "bottom");
            a.AttachRadial(tank, "leg_ls5", -3.3f, 0, 4);
            a.AutoStage();
            return d;
        }

        /// <summary>Crew pod with a parachute and no heat shield.</summary>
        public static CraftDesign BareCapsule(PartDatabase db)
        {
            var d = new CraftDesign { name = "Test Capsule", description = "Pod and parachute, no heat shield." };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            a.AttachStack(pod, "top", "chute_main", "bottom");
            a.AutoStage();
            return d;
        }

        /// <summary>Crewed docking chaser: docking port on the pod's nose, monopropellant tank and four RCS blocks below.</summary>
        public static CraftDesign DockChaser(PartDatabase db)
        {
            var d = new CraftDesign { name = "Dock Chaser", description = "Docking test: crewed pod with a nose docking port and RCS." };
            var a = new CraftAssembler(d, db);
            int pod = a.AddRoot("pod_kestrel");
            a.AttachStack(pod, "top", "dock_s1", "bottom");
            a.AttachStack(pod, "bottom", "mono_s1", "top");
            // RCS blocks at the height of the centre of mass: translation then hardly turns the craft.
            a.AttachRadial(pod, "rcs_quad", -0.45f, 45, 4);
            a.AutoStage();
            return d;
        }

        /// <summary>Uncrewed docking target: probe core with a docking port on top and a fuel tank below.</summary>
        public static CraftDesign DockTarget(PartDatabase db)
        {
            var d = new CraftDesign { name = "Dock Target", description = "Docking test: probe core, docking port and a tank." };
            var a = new CraftAssembler(d, db);
            int probe = a.AddRoot("probe_wren");
            a.AttachStack(probe, "top", "dock_s1", "bottom");
            a.AttachStack(probe, "bottom", "tank_s1_short", "top");
            a.AutoStage();
            return d;
        }
    }

    /// <summary>State that survives a quickload (scene reload) during the persistence test.</summary>
    public static class AutoTestState
    {
        public static int PersistStep;
        /// <summary>Step of the resumed "lunar-surface" test (0 = load the landed snapshot, 1 = continue).</summary>
        public static int ResumeStep;
        public static double SavedUT;
        public static Vector3d SavedPos, SavedVel;
        public static string SavedVesselName, SavedBody;
        public static int SavedParts, SavedHandles, SavedCrew;
        public static Dictionary<string, double> SavedResources = new Dictionary<string, double>();
        public static int SavedMilestones;
        public static Orbit SavedOrbit;
        /// <summary>Checks and log lines recorded before the reload, merged into the final report.</summary>
        public static readonly List<(string check, bool pass, string detail)> CarriedChecks = new List<(string, bool, string)>();
        public static readonly List<string> CarriedLog = new List<string>();
    }

    public sealed partial class MissionAutopilot
    {
        /// <summary>Failure scenarios expect vessels and crew to be lost; the watchdog then stays quiet.</summary>
        public bool ExpectLosses;
        private readonly List<string> _messages = new List<string>();

        private void CaptureMessages()
        {
            Sim.Message += (m, important) => { _messages.Add(m); if (_messages.Count > 500) _messages.RemoveAt(0); };
        }

        private string FindMessage(int fromIndex, params string[] needles)
        {
            for (int i = Math.Max(0, fromIndex); i < _messages.Count; i++)
                foreach (var n in needles)
                    if (_messages[i].IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0) return _messages[i];
            return null;
        }

        // ------------------------------------------------------------------ failure consequences

        private IEnumerator FailuresMission()
        {
            ExpectLosses = true;
            CaptureMessages();

            // 1) Structural overload on a weak connection (pad test).
            SetPhase("structural overload");
            int m0 = _messages.Count;
            int loaded0 = Sim.LoadedVessels.Count;
            Throttle(1);
            Stage();
            yield return WaitSecondsAny(4);
            string sf = FindMessage(m0, "structural failure");
            Check("overloaded joint fails from simulated loads", sf != null, sf ?? "no structural failure reported");
            Check("broken-off parts continue as a separate vessel", Sim.LoadedVessels.Count > loaded0, $"{Sim.LoadedVessels.Count} loaded vessels (was {loaded0})");
            if (V != null) Throttle(0);
            yield return WaitSecondsAny(2);

            // 2) Overheating: a capsule without heat shield on a lunar-return entry (3.13 km/s at 85 km).
            SetPhase("overheating");
            m0 = _messages.Count;
            double oceanLon = FindOceanLongitude(30);
            // A brutal, fast and steep entry (4.5 km/s through the air at 85 km, 26° down). In this model an unshielded pod
            // survives the gentler lunar-return corridor (3.1 km/s, periapsis ~30 km: ~1,050 K against its 1,600 K skin).
            var cap = SpawnScenario(TestCraft.BareCapsule(PartDatabase.Instance), "Heat Test Capsule", 85000, oceanLon - 25, 4500, -26);
            if (cap == null) { Check("heat test capsule spawned", false, "spawn failed"); yield break; }
            Note($"spawned {cap.VesselName} at 85 km, 4,500 m/s, flight path -26° (no heat shield, flying nose first), periapsis {(cap.Orbit.PeriapsisRadius - Tellus.Radius) / 1000:F0} km");
            // Nose first: the parachute housing on top (rated 1,400 K) faces the hot flow instead of the pod's blunt base.
            Sas(SasMode.Prograde, SpeedMode.Surface);
            double maxSkin = 0;
            string hotPart = "";
            double t0 = Sim.UT;
            while (Sim.UT - t0 < 900)
            {
                var v = V;
                if (v == null || v.VesselName != cap.VesselName) break;
                foreach (var p in v.Parts) if (p.SkinTemp > maxSkin) { maxSkin = p.SkinTemp; hotPart = p.Def.title; }
                if (v.Situation == Situation.Landed || v.Situation == Situation.Splashed) break;
                if (FindMessage(m0, "overheated") != null && v.Parts.Count == 0) break;
                yield return null;
            }
            string heat = FindMessage(m0, "overheated");
            Check("capsule without heat shield is destroyed by reentry heating", heat != null,
                heat != null ? $"{heat} (max skin {maxSkin:F0} K on {hotPart})" : $"survived, max skin {maxSkin:F0} K on {hotPart}");
            yield return WaitSecondsAny(1);

            // 3) Unsafe parachute deployment at high speed, then the impact.
            SetPhase("unsafe parachute");
            m0 = _messages.Count;
            var cap2 = SpawnScenario(TestCraft.BareCapsule(PartDatabase.Instance), "Chute Test Capsule", 5000, oceanLon, 450, -45);
            if (cap2 == null) { Check("chute test capsule spawned", false, "spawn failed"); yield break; }
            Note($"spawned {cap2.VesselName} at 5 km, 450 m/s air speed, diving 45°; arming the parachute immediately");
            Sas(SasMode.Retrograde, SpeedMode.Surface);
            yield return WaitSecondsAny(0.5);
            string armedAt = V != null ? $"armed at {V.Altitude / 1000:F1} km, {V.SurfaceSpeed:F0} m/s, q {V.DynamicPressure / 1000:F0} kPa" : "";
            Stage();
            ParachuteModule chute = null;
            foreach (var p in cap2.Parts) { chute = p.GetModule<ParachuteModule>(); if (chute != null) break; }
            t0 = Sim.UT;
            double maxLoad = 0;
            while (Sim.UT - t0 < 30 && chute != null && chute.State != ParachuteModule.ChuteState.Failed)
            {
                maxLoad = Math.Max(maxLoad, chute.CanopyLoad);
                yield return null;
            }
            string torn = FindMessage(m0, "torn", "FAILED");
            Check("parachute deployed too fast is torn by the canopy load", chute != null && chute.State == ParachuteModule.ChuteState.Failed,
                chute != null ? $"{chute.State}: {chute.FailReason} ({armedAt}; peak canopy load {maxLoad / 1000:F0} kN)" : "no chute");
            // Without a canopy the capsule hits the surface fast.
            t0 = Sim.UT;
            while (Sim.UT - t0 < 300)
            {
                if (FindMessage(m0, "destroyed: impact", "destroyed: splashdown", "destroyed: crashed", "was lost") != null) break;
                yield return null;
            }
            string impact = FindMessage(m0, "destroyed: impact", "destroyed: splashdown", "destroyed: crashed");
            string crewLost = FindMessage(m0, "was lost");
            Check("high-speed impact destroys the capsule (collision damage)", impact != null, impact ?? "no impact destruction");
            Check("crew loss is reported", crewLost != null, crewLost ?? "none");
        }

        private IEnumerator WaitSecondsAny(double s)
        {
            double t = Sim.UT + s;
            float rt = Time.realtimeSinceStartup + (float)s + 5f;
            while (Sim.UT < t && Time.realtimeSinceStartup < rt) yield return null;
        }

        /// <summary>Longitude on the equator (east of the pad) where the terrain is ocean for a long stretch.</summary>
        private double FindOceanLongitude(double startLon)
        {
            var terrain = Tellus.Terrain;
            for (double lon = startLon; lon < startLon + 360; lon += 2)
            {
                bool ok = true;
                for (double d = -30; d <= 6 && ok; d += 1.5)
                {
                    double h = terrain.Height(TerrainGenerator.DirectionFromLatLon(0, lon + d));
                    if (h > -200) ok = false;
                }
                if (ok) return lon;
            }
            return startLon + 90;
        }

        /// <summary>
        /// Test scenario setup: creates a vessel record (like a saved game) above the equator and switches to it.
        /// airSpeed is relative to the rotating atmosphere, flightPathDeg below the local horizon (negative = descending).
        /// </summary>
        private Vessel SpawnScenario(CraftDesign d, string name, double altitude, double lonDeg, double airSpeed, double flightPathDeg)
        {
            var body = Tellus;
            var rec = LaunchService.CreateLaunchRecord(d, PartDatabase.Instance, Sim.UT);
            rec.name = name;
            rec.designName = name;
            rec.landed = false;
            rec.landedPos = null;
            rec.landedRot = null;
            rec.situation = Situation.Flying;
            rec.launchUT = Sim.UT;
            Vector3d up = body.BodyFixedToInertial(TerrainGenerator.DirectionFromLatLon(0, lonDeg), Sim.UT).normalized;
            Vector3d east = Geo.East(up);
            Vector3d pos = up * (body.Radius + altitude);
            double fpa = flightPathDeg * MathD.Deg2Rad;
            Vector3d vAir = east * (airSpeed * Math.Cos(fpa)) + up * (airSpeed * Math.Sin(fpa));
            Vector3d vel = vAir + body.FrameVelocityAt(pos);
            rec.orbitPos = new[] { pos.x, pos.y, pos.z };
            rec.orbitVel = new[] { vel.x, vel.y, vel.z };
            rec.orbitEpoch = Sim.UT;
            Quaternion rot = Quaternion.FromToRotation(Vector3.up, -(Vector3)vAir.normalized);
            rec.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
            rec.angularVelocity = new[] { 0f, 0f, 0f };
            var h = Sim.CreateHandle(rec);
            Sim.Handles.Add(h);
            if (!Sim.SwitchTo(h)) return null;
            return h.Loaded;
        }

        // ------------------------------------------------------------------ orbital EVA

        /// <summary>EVA in orbit: exit, jetpack 8 m away from the hatch, come back and board.</summary>
        private IEnumerator OrbitalEva()
        {
            SetPhase("orbital EVA");
            var ship = V;
            Part pod = null;
            foreach (var p in ship.Parts) if (p.Crew.Count > 0) { pod = p; break; }
            if (pod == null) { Check("crew aboard for orbital EVA", false, "no crew"); yield break; }
            string crew = pod.Crew[0];
            Sim.RequestEva(pod, crew);
            yield return WaitUntil(() => V != null && V.IsEva, 10, "EVA spawn");
            if (Failed) yield break;
            var eva = V;
            var em = eva.RootPart.GetModule<EvaModule>();
            double da = Math.Abs(eva.Orbit.SemiMajorAxis - ship.Orbit.SemiMajorAxis);
            float rel = (eva.Rb.linearVelocity - ship.Rb.linearVelocity).magnitude;
            Snap("orbital_eva");
            Check("EVA in orbit: rocketeer inherits the vessel's orbit", eva.Situation == Situation.Orbiting && da < 2500,
                $"{crew} outside, relative speed {rel:F2} m/s, Δa {da:F0} m, Pe {(eva.Orbit.PeriapsisRadius - Tellus.Radius) / 1000:F1} km");
            var hatch = pod.GetModule<CrewModule>();
            Vector3 up = LocalUp;
            em.JetpackOn = true;
            // Leg 1: 8 m out along the hatch normal; leg 2: back to the hatch and board.
            bool boarded = false;
            double maxDist = 0;
            for (int leg = 0; leg < 2 && !boarded; leg++)
            {
                double t0 = Sim.UT;
                while (Sim.UT - t0 < 180)
                {
                    if (V == null) yield break;
                    if (!V.IsEva) { boarded = true; break; }
                    eva = V;
                    em = eva.RootPart.GetModule<EvaModule>();
                    Vector3 target = hatch.HatchWorldPosition + hatch.HatchWorldNormal * (leg == 0 ? 8f : 0.8f);
                    Vector3 delta = target - eva.WorldCoM;
                    Vector3 relVel = eva.Rb.linearVelocity - ship.Rb.GetPointVelocity(hatch.HatchWorldPosition);
                    maxDist = Math.Max(maxDist, (eva.WorldCoM - hatch.HatchWorldPosition).magnitude);
                    Vector3 cmd = delta * 0.5f - relVel * 1.3f;
                    Vector3 horiz = Vector3.ProjectOnPlane(cmd, up);
                    if (horiz.sqrMagnitude > 1e-4f) em.CameraRotation = Quaternion.LookRotation(horiz.normalized, up);
                    em.MoveInput = new Vector2(0, Mathf.Clamp01(horiz.magnitude));
                    em.VerticalInput = Mathf.Clamp(Vector3.Dot(cmd, up), -1f, 1f);
                    if (leg == 0 && delta.magnitude < 1.0f && relVel.magnitude < 0.4f) break;
                    if (leg == 1 && (hatch.HatchWorldPosition - eva.WorldCoM).magnitude < EvaModule.BoardRange - 0.3f && relVel.magnitude < 1.2f)
                    {
                        em.MoveInput = Vector2.zero;
                        em.VerticalInput = 0;
                        Sim.RequestBoard(eva);
                        yield return WaitSeconds(0.5);
                        if (V != null && !V.IsEva) { boarded = true; break; }
                    }
                    yield return null;
                }
                if (leg == 0) Check("jetpack manoeuvre in orbit", maxDist > 6, $"moved {maxDist:F1} m from the hatch");
            }
            Check("crew boarded the vessel in orbit", boarded && V != null && V.CrewCount > 0, boarded ? $"{crew} back aboard {V.VesselName}" : "failed to board");
            Sas(SasMode.StabilityAssist);
        }

        // ------------------------------------------------------------------ docking

        /// <summary>Test scenario setup: a vessel record in orbit with the given state and root orientation (not yet loaded).</summary>
        private VesselHandle CreateOrbitalHandle(CraftDesign d, string name, Vector3d pos, Vector3d vel, Quaternion rot)
        {
            var rec = LaunchService.CreateLaunchRecord(d, PartDatabase.Instance, Sim.UT);
            rec.name = name;
            rec.designName = name;
            rec.landed = false;
            rec.landedPos = null;
            rec.landedRot = null;
            rec.situation = Situation.Orbiting;
            rec.launchUT = Sim.UT;
            rec.orbitPos = new[] { pos.x, pos.y, pos.z };
            rec.orbitVel = new[] { vel.x, vel.y, vel.z };
            rec.orbitEpoch = Sim.UT;
            rec.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
            rec.angularVelocity = new[] { 0f, 0f, 0f };
            var h = Sim.CreateHandle(rec);
            Sim.Handles.Add(h);
            return h;
        }

        /// <summary>
        /// Docking in orbit (no rendezvous): a crewed chaser and an uncrewed target start 12 m apart on the same 100 km
        /// orbit with their ports facing each other. The pilot flies the chaser in with RCS translation only (as with
        /// the H/N, J/L, I/K keys), the ports capture and merge the vessels, the stack holds together, then the ports
        /// undock, push the vessels apart and re-arm only once they are clear of each other.
        /// </summary>
        private IEnumerator DockingMission()
        {
            SetPhase("docking setup");
            var db = PartDatabase.Instance;
            var body = Tellus;
            double r = body.Radius + 100000;
            double vc = Math.Sqrt(body.GM / r);
            Vector3d up = body.BodyFixedToInertial(TerrainGenerator.DirectionFromLatLon(0, 0), Sim.UT).normalized;
            Vector3d east = Geo.East(up);
            var orbit = new Orbit(up * r, east * vc, Sim.UT, body.GM);
            orbit.GetStateAtUT(Sim.UT, out Vector3d pT, out Vector3d vT);
            orbit.GetStateAtUT(Sim.UT - 12.0 / vc, out Vector3d pC, out Vector3d vC);
            Vector3 axis = ((Vector3)(pT - pC)).normalized;
            var hT = CreateOrbitalHandle(TestCraft.DockTarget(db), "Dock Target", pT, vT, Quaternion.FromToRotation(Vector3.up, -axis));
            var hC = CreateOrbitalHandle(TestCraft.DockChaser(db), "Dock Chaser", pC, vC, Quaternion.FromToRotation(Vector3.up, axis));
            if (!Sim.SwitchTo(hC)) { Check("docking vessels in orbit", false, "could not switch to the chaser"); yield break; }
            yield return WaitSecondsAny(1);
            var chaser = V;
            var target = hT.Loaded;
            if (chaser == null || target == null || chaser.Handle != hC)
            {
                Check("docking vessels in orbit", false, $"active {chaser?.VesselName}, target loaded {target != null}");
                yield break;
            }
            DockingPortModule cp = null, tp = null;
            foreach (var p in chaser.Parts) { var m = p.GetModule<DockingPortModule>(); if (m != null) cp = m; }
            foreach (var p in target.Parts) { var m = p.GetModule<DockingPortModule>(); if (m != null) tp = m; }
            if (cp == null || tp == null) { Check("docking vessels in orbit", false, "docking port missing"); yield break; }
            int nC = chaser.Parts.Count, nT = target.Parts.Count, loaded0 = Sim.LoadedVessels.Count, crew0 = chaser.CrewCount;
            double mC = chaser.TotalMass, mT = target.TotalMass;
            float gap0 = (tp.FaceWorld - cp.FaceWorld).magnitude;
            Check("docking vessels in orbit", chaser.Situation == Situation.Orbiting && target.Situation == Situation.Orbiting,
                $"{chaser.VesselName} ({nC} parts, {mC / 1000:F2} t, crew {crew0}) and {target.VesselName} ({nT} parts, {mT / 1000:F2} t), ports {gap0:F1} m apart");
            Snap("docking_start");

            SetPhase("docking approach");
            Sas(SasMode.StabilityAssist);
            chaser.Ctrl.Rcs = true;
            double t0 = Sim.UT;
            float lastDist = gap0, lastAngle = 0, lastSpeed = 0;
            double nextLog = Sim.UT;
            bool docked = false;
            while (Sim.UT - t0 < 600)
            {
                if (V == null) yield break;
                if (cp.State == DockingPortModule.PortState.Docked) { docked = true; break; }
                if (target == null || target.Rb == null) break;
                Vector3 face = cp.FaceWorld, tface = tp.FaceWorld;
                Vector3 approach = -tp.FaceNormalWorld; // direction of travel into the target port
                Vector3 d = tface - face;
                float along = Vector3.Dot(d, approach);
                Vector3 lateral = d - approach * along;
                // Translation is controlled on the centre-of-mass velocities (a port face also moves when the craft
                // wobbles, which RCS translation cannot fix); the capture limit applies to the face velocities.
                Vector3 vRel = chaser.Rb.linearVelocity - target.Rb.linearVelocity;
                lastDist = d.magnitude;
                lastAngle = Vector3.Angle(cp.FaceNormalWorld, -tp.FaceNormalWorld);
                lastSpeed = (chaser.Rb.GetPointVelocity(face) - target.Rb.GetPointVelocity(tface)).magnitude;
                // Close slowly along the target port's axis; line up first when off-axis in the last metres.
                float vClose = Mathf.Clamp(along * 0.1f, 0.15f, 0.5f);
                if (lateral.magnitude > 0.25f && along < 3f) vClose = 0f;
                Vector3 vWanted = approach * vClose + Vector3.ClampMagnitude(lateral * 0.5f, 0.3f);
                Vector3 local = Quaternion.Inverse(chaser.ControlRotation) * ((vWanted - vRel) * 3f);
                chaser.Ctrl.TransX = Mathf.Clamp(local.x, -1f, 1f);
                chaser.Ctrl.TransY = Mathf.Clamp(local.y, -1f, 1f);
                chaser.Ctrl.TransZ = Mathf.Clamp(local.z, -1f, 1f);
                if (Sim.UT >= nextLog)
                {
                    nextLog = Sim.UT + 2;
                    Note($"approach: {along:F2} m to go, {lateral.magnitude:F2} m off-axis, closing {Vector3.Dot(vRel, approach):F2} m/s (want {vClose:F2}), RCS ({chaser.Ctrl.TransX:F2}, {chaser.Ctrl.TransY:F2}, {chaser.Ctrl.TransZ:F2}), spin {chaser.Rb.angularVelocity.magnitude:F2} / {target.Rb.angularVelocity.magnitude:F2} rad/s");
                }
                yield return null;
            }
            if (V != null) { V.Ctrl.TransX = 0; V.Ctrl.TransY = 0; V.Ctrl.TransZ = 0; }
            Check("docking ports capture and merge the vessels", docked && V != null && V.Parts.Count == nC + nT && Sim.LoadedVessels.Count == loaded0 - 1,
                docked ? $"captured {Sim.UT - t0:F0} s after the start at {lastSpeed:F2} m/s and {lastAngle:F1}° (last sample {lastDist:F2} m apart); {V.VesselName}: {V.Parts.Count} parts, {V.TotalMass / 1000:F2} t, crew {V.CrewCount}"
                       : $"not docked after {Sim.UT - t0:F0} s: ports {lastDist:F2} m apart, {lastAngle:F1}°, {lastSpeed:F2} m/s");
            if (!docked) yield break;
            Snap("docked");

            // The docked stack must stay in one piece under SAS.
            int partsDocked = V.Parts.Count;
            double tDock = Sim.UT;
            while (Sim.UT - tDock < 10 && V != null) yield return null;
            Check("docked stack holds together", V != null && V.Parts.Count == partsDocked && cp.State == DockingPortModule.PortState.Docked
                                                  && tp.State == DockingPortModule.PortState.Docked && V.CrewCount == crew0,
                $"{V?.Parts.Count} parts after {Sim.UT - tDock:F0} s, mass {V?.TotalMass / 1000:F2} t (sum {(mC + mT) / 1000:F2} t), crew {V?.CrewCount}");

            SetPhase("undocking");
            cp.Undock();
            yield return WaitSecondsAny(1);
            Vessel other = null;
            foreach (var lv in Sim.LoadedVessels) if (lv != null && lv != V && lv.Parts.Contains(tp.Part)) other = lv;
            float relSep = other != null ? (other.Rb.linearVelocity - V.Rb.linearVelocity).magnitude : 0f;
            Check("undocking splits the stack and pushes the vessels apart", other != null && V.Parts.Count == nC && other.Parts.Count == nT && relSep > 0.05f,
                other != null ? $"{V.VesselName} ({V.Parts.Count} parts) and {other.VesselName} ({other.Parts.Count} parts) separating at {relSep:F2} m/s" : "no second vessel after undocking");
            if (other == null) yield break;
            // They must not be pulled back together: the ports re-arm only once clear of each other.
            double tU = Sim.UT;
            float sep = 0;
            bool redocked = false;
            while (Sim.UT - tU < 90)
            {
                if (V == null) yield break;
                if (cp.State == DockingPortModule.PortState.Docked) { redocked = true; break; }
                sep = (tp.FaceWorld - cp.FaceWorld).magnitude;
                if (sep > cp.Def.captureRange * 5f + 0.5f && cp.State == DockingPortModule.PortState.Ready && tp.State == DockingPortModule.PortState.Ready) break;
                yield return null;
            }
            Check("ports re-arm only after the vessels are clear of each other", !redocked && cp.State == DockingPortModule.PortState.Ready && tp.State == DockingPortModule.PortState.Ready,
                redocked ? "the ports pulled the vessels back together and re-docked" : $"ports {sep:F1} m apart after {Sim.UT - tU:F0} s, states {cp.State} / {tp.State}");
            Snap("undocked");
        }

        // ------------------------------------------------------------------ persistence

        /// <summary>
        /// Quicksave in orbit, change the state (warp), quickload (full scene reload through the save file),
        /// then compare everything with the snapshot and check that propagation continues seamlessly.
        /// </summary>
        private IEnumerator PersistenceMission()
        {
            if (AutoTestState.PersistStep == 0)
            {
                yield return Ascent(80000);
                if (Failed) yield break;
                SetPhase("quicksave");
                var v = V;
                AutoTestState.SavedUT = Sim.UT;
                AutoTestState.SavedPos = v.TruePosition;
                AutoTestState.SavedVel = v.TrueVelocity;
                AutoTestState.SavedOrbit = v.Orbit;
                AutoTestState.SavedVesselName = v.VesselName;
                AutoTestState.SavedBody = v.MainBody.Id;
                AutoTestState.SavedParts = v.Parts.Count;
                AutoTestState.SavedHandles = Sim.Handles.Count;
                AutoTestState.SavedCrew = v.CrewCount;
                AutoTestState.SavedMilestones = GameSession.Save.milestones.Count;
                AutoTestState.SavedResources.Clear();
                foreach (var p in v.Parts)
                    foreach (var r in p.Resources)
                    {
                        AutoTestState.SavedResources.TryGetValue(r.Def.id, out double a);
                        AutoTestState.SavedResources[r.Def.id] = a + r.Amount;
                    }
                Scene.Quicksave();
                Check("quicksave file written", SaveStorage.SlotExists(GameSession.Save.saveName, "quicksave"), SaveStorage.SlotPath(GameSession.Save.saveName, "quicksave"));
                // Change the world state, then load the quicksave.
                yield return WarpUntil(Sim.UT + v.Orbit.Period * 0.4, "warp before quickload");
                Note($"warped to UT {Sim.UT:F1}; quickloading");
                AutoTestState.PersistStep = 1;
                AutoTestState.CarriedChecks.Clear();
                AutoTestState.CarriedChecks.AddRange(Checks);
                AutoTestState.CarriedLog.Clear();
                AutoTestState.CarriedLog.AddRange(Log);
                Scene.Quickload(); // reloads the flight scene; a new autopilot continues with step 1
                yield return WaitSecondsAny(30);
                yield break;
            }

            AutoTestState.PersistStep = 0;
            Checks.InsertRange(0, AutoTestState.CarriedChecks);
            Log.InsertRange(0, AutoTestState.CarriedLog);
            SetPhase("verify quickload");
            yield return WaitSecondsAny(1);
            var lv = V;
            // The clock restarts at the saved UT and runs on while the scene loads and the pilot waits: compare the
            // time gained with the real time since the reload.
            double dt = Sim.UT - AutoTestState.SavedUT;
            double sinceLoad = Time.timeSinceLevelLoadAsDouble;
            Check("quickload restores the universe time", dt >= 0 && Math.Abs(dt - sinceLoad) < 1.0,
                $"UT {Sim.UT:F2}: {dt:F2} s after the saved {AutoTestState.SavedUT:F2}, {sinceLoad:F2} s after the reload");
            // Compare with the analytic state of the saved orbit at the current time.
            AutoTestState.SavedOrbit.GetStateAtUT(Sim.UT, out Vector3d expPos, out Vector3d expVel);
            double dr = (lv.TruePosition - expPos).magnitude, dv = (lv.TrueVelocity - expVel).magnitude;
            Check("quickload restores position and velocity", dr < 0.5 && dv < 0.05, $"Δr {dr:F3} m, Δv {dv:F4} m/s");
            Check("quickload restores the vessel", lv.VesselName == AutoTestState.SavedVesselName && lv.Parts.Count == AutoTestState.SavedParts && lv.CrewCount == AutoTestState.SavedCrew,
                $"{lv.VesselName}: {lv.Parts.Count} parts, crew {lv.CrewCount}");
            double worst = 0;
            string worstRes = "";
            foreach (var kv in AutoTestState.SavedResources)
            {
                double a = 0;
                foreach (var p in lv.Parts) foreach (var r in p.Resources) if (r.Def.id == kv.Key) a += r.Amount;
                // Electric charge may be drawn by SAS/command parts in the second since loading.
                double diff = Math.Abs(a - kv.Value) / (kv.Key == "Electric" ? 20.0 : 1.0);
                if (diff > worst) { worst = diff; worstRes = kv.Key; }
            }
            Check("quickload restores resources", worst < 0.05, worst > 0 ? $"largest difference {worst:F4} ({worstRes})" : "identical");
            Check("quickload restores all vessels and milestones", Sim.Handles.Count == AutoTestState.SavedHandles && GameSession.Save.milestones.Count == AutoTestState.SavedMilestones,
                $"{Sim.Handles.Count} vessels, {GameSession.Save.milestones.Count} milestones");
            // Continue on rails for one orbit and compare with the saved orbit's analytic prediction.
            double target = Sim.UT + lv.Orbit.Period;
            yield return WarpUntil(target, "warp after quickload");
            yield return WaitSecondsAny(2);
            AutoTestState.SavedOrbit.GetStateAtUT(Sim.UT, out expPos, out expVel);
            dr = (V.TruePosition - expPos).magnitude;
            Check("orbit continues seamlessly after quickload + warp", dr < 10, $"Δr {dr:F2} m after one more orbit");
        }
    }
}
