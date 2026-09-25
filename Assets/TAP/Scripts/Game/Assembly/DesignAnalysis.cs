using System;
using System.Collections.Generic;
using TAP.Construction;
using TAP.Core;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;

namespace TAP.Game
{
    /// <summary>Reference environment for the editor's delta-v / TWR readouts.</summary>
    public enum EditorEnvironment { TellusSeaLevel, TellusVacuum, LumaSurface }

    public struct DesignWarning
    {
        public string Text;
        /// <summary>Launch is not possible while this problem exists.</summary>
        public bool Blocking;
        /// <summary>Advice rather than a problem.</summary>
        public bool Info;
    }

    /// <summary>Everything the assembly UI shows about a design (engineer's report).</summary>
    public sealed class DesignStats
    {
        public double WetMass, DryMass;
        public int PartCount, CrewSeats, CrewAvailable;
        public double ElectricCharge, Monoprop, LiquidFuel, SolidFuel, Ablator;
        public float Height, Width;
        public EditorEnvironment Environment;
        public double Gravity, PressureAtm;
        /// <summary>Per-stage results, first-firing stage first.</summary>
        public readonly List<StageDeltaV> Stages = new List<StageDeltaV>();
        public double TotalDvVac, TotalDvAsl, TotalDvEnv;
        /// <summary>Thrust-to-weight of the first stage on the launch pad (Tellus sea level).</summary>
        public double LaunchTwr;
        public Vector3 CoM, CoT, ThrustDir, CoP;
        public bool HasThrust, HasCoP;
        /// <summary>CoP height above the CoM along the vehicle axis (negative = stable, CoP behind CoM).</summary>
        public float StabilityMargin;
        public readonly List<DesignWarning> Warnings = new List<DesignWarning>();
        public bool HasBlocking
        {
            get { foreach (var w in Warnings) if (w.Blocking) return true; return false; }
        }
        public StageDeltaV Stage(int s)
        {
            foreach (var st in Stages) if (st.Stage == s) return st;
            return null;
        }
    }

    /// <summary>Engineer's report for a craft design: masses, resources, per-stage delta-v/TWR, CoM/CoT/CoP, warnings.</summary>
    public static class DesignAnalysis
    {
        public static DesignStats Analyze(CraftDesign d, PartDatabase db, EditorEnvironment env)
        {
            var s = new DesignStats { Environment = env };
            var sys = CelestialSystem.Default;
            var home = sys.Root;
            var moon = sys.Get("luma");
            CelestialBody envBody = env == EditorEnvironment.LumaSurface && moon != null ? moon : home;
            s.Gravity = envBody.GM / (envBody.Radius * envBody.Radius);
            s.PressureAtm = env == EditorEnvironment.TellusSeaLevel ? 1.0 : 0.0;
            s.PartCount = d.parts.Count;
            if (d.parts.Count == 0)
            {
                s.Warnings.Add(new DesignWarning { Text = "The craft is empty: pick a command pod from the parts list.", Blocking = true });
                return s;
            }

            var asm = new CraftAssembler(d, db);
            s.WetMass = asm.TotalMass();
            s.DryMass = asm.DryMass();
            s.CrewSeats = asm.CrewCapacity();
            asm.ResourceTotals("Electric", out s.ElectricCharge);
            asm.ResourceTotals("Monoprop", out s.Monoprop);
            asm.ResourceTotals("LiquidFuel", out s.LiquidFuel);
            asm.ResourceTotals("SolidFuel", out s.SolidFuel);
            asm.ResourceTotals("Ablator", out s.Ablator);
            s.CoM = asm.CenterOfMass();
            if (GameSession.Save != null)
                foreach (var c in GameSession.Save.crew) if (c.status == CrewStatus.Available) s.CrewAvailable++;

            // Size (bounding cylinders).
            float minY = float.MaxValue, maxY = float.MinValue, maxR = 0;
            foreach (var p in d.parts)
            {
                var def = db.Get(p.partId);
                if (def == null) continue;
                Vector3 pos = CraftAssembler.V(p.pos);
                Vector3 axis = CraftAssembler.Q(p.rot) * Vector3.up;
                float r = def.diameter * 0.5f, h = def.height * 0.5f;
                float ext = Mathf.Abs(axis.y) * h + Mathf.Sqrt(Mathf.Max(0, 1 - axis.y * axis.y)) * r;
                minY = Mathf.Min(minY, pos.y - ext);
                maxY = Mathf.Max(maxY, pos.y + ext);
                float radial = new Vector2(pos.x, pos.z).magnitude + Mathf.Sqrt(Mathf.Max(0, 1 - axis.y * axis.y)) * h + Mathf.Abs(axis.y) * r;
                maxR = Mathf.Max(maxR, radial);
            }
            s.Height = maxY - minY;
            s.Width = maxR * 2;

            // Delta-v per stage in the selected environment plus sea-level/vacuum totals.
            var model = asm.ToStageModel();
            int maxStage = -1;
            foreach (var p in d.parts) maxStage = Math.Max(maxStage, p.stage);
            if (maxStage >= 0)
            {
                var envStages = DeltaVCalculator.Compute(model, maxStage, s.Gravity, s.PressureAtm, db);
                s.Stages.AddRange(envStages);
                foreach (var st in envStages)
                {
                    s.TotalDvVac += st.DeltaVVac;
                    s.TotalDvAsl += st.DeltaVAsl;
                    s.TotalDvEnv += st.DeltaVCurrent;
                }
                double g0 = home.GM / (home.Radius * home.Radius);
                var launch = DeltaVCalculator.Compute(asm.ToStageModel(), maxStage, g0, 1.0, db);
                foreach (var st in launch)
                    if (st.HasEngines) { s.LaunchTwr = st.TwrAsl; break; }
            }

            ComputeThrustCentre(d, db, model, maxStage, s);
            ComputePressureCentre(d, db, s);
            CollectWarnings(d, db, model, maxStage, s);
            return s;
        }

        /// <summary>Thrust-weighted centre and direction of the engines that fire in the first engine stage.</summary>
        private static void ComputeThrustCentre(CraftDesign d, PartDatabase db, List<StageSimPart> model, int maxStage, DesignStats s)
        {
            int firstEngineStage = -1;
            for (int st = maxStage; st >= 0 && firstEngineStage < 0; st--)
                for (int i = 0; i < d.parts.Count; i++)
                    if (d.parts[i].stage == st && model[i].Def?.engine != null) { firstEngineStage = st; break; }
            if (firstEngineStage < 0) return;
            Vector3 sum = Vector3.zero, dir = Vector3.zero;
            double total = 0;
            for (int i = 0; i < d.parts.Count; i++)
            {
                var def = model[i].Def;
                if (def?.engine == null || d.parts[i].stage != firstEngineStage) continue;
                double limit = model[i].ThrustLimit;
                double t = def.engine.thrustVac * limit;
                Vector3 pos = CraftAssembler.V(d.parts[i].pos);
                Quaternion rot = CraftAssembler.Q(d.parts[i].rot);
                sum += (pos + rot * def.engine.NozzlePosition) * (float)t;
                dir += rot * def.engine.ThrustDirection * (float)t;
                total += t;
            }
            if (total <= 0) return;
            s.HasThrust = true;
            s.CoT = sum / (float)total;
            s.ThrustDir = dir.normalized;
        }

        /// <summary>
        /// Centre of pressure at a small angle of attack, using the same part aerodynamics as the flight model
        /// (side cross-flow, slender-body nose lift, fin lift). Averaged over two perpendicular sideslip planes.
        /// </summary>
        private static void ComputePressureCentre(CraftDesign d, PartDatabase db, DesignStats s)
        {
            const float alpha = 4f * Mathf.Deg2Rad;
            float sumN = 0, sumNY = 0;
            for (int plane = 0; plane < 2; plane++)
            {
                Vector3 side = plane == 0 ? Vector3.right : Vector3.forward;
                Vector3 vRel = Vector3.up * Mathf.Cos(alpha) + side * Mathf.Sin(alpha); // vessel velocity relative to the air
                foreach (var p in d.parts)
                {
                    var def = db.Get(p.partId);
                    if (def == null) continue;
                    Vector3 pos = CraftAssembler.V(p.pos);
                    Quaternion rot = CraftAssembler.Q(p.rot);
                    Vector3 f;
                    Vector3 at = pos;
                    if (def.fin != null)
                    {
                        Vector3 n = rot * def.fin.Normal;
                        float sinA = Vector3.Dot(vRel, n);
                        float a = Mathf.Asin(Mathf.Clamp(sinA, -1, 1));
                        float cl = def.fin.liftSlope * Mathf.Min(Mathf.Abs(a), def.fin.stallDeg * Mathf.Deg2Rad);
                        f = -Mathf.Sign(sinA) * n * 0.5f * def.fin.area * cl;
                        float span = def.model != null ? def.model.diameter : 0.6f;
                        at = pos + rot * new Vector3(0, 0, span * 0.45f);
                    }
                    else
                    {
                        Vector3 axis = rot * Vector3.up;
                        float D = Mathf.Max(0.05f, def.diameter), H = Mathf.Max(0.05f, def.height);
                        float aEnd = Mathf.PI * D * D * 0.25f, aSide = D * H;
                        float vAx = Vector3.Dot(vRel, axis);
                        Vector3 vN = vRel - vAx * axis;
                        float vNmag = vN.magnitude;
                        if (vNmag < 1e-5f) continue;
                        Vector3 nDir = vN / vNmag;
                        f = -nDir * 0.5f * vNmag * vNmag * def.drag.cdSide * aSide;
                        if (def.drag.bodyLift > 0) f += -nDir * 0.5f * aEnd * def.drag.bodyLift * 2f * vNmag * Mathf.Abs(vAx);
                    }
                    float normal = -Vector3.Dot(f, side); // restoring component (opposes the sideslip)
                    sumN += normal;
                    sumNY += normal * at.y;
                }
            }
            if (Mathf.Abs(sumN) < 1e-6f) return;
            s.HasCoP = true;
            // Shown on the vehicle axis (the craft is built around the root part's axis).
            s.CoP = new Vector3(0, sumNY / sumN, 0);
            s.StabilityMargin = s.CoP.y - s.CoM.y;
        }

        private static void CollectWarnings(CraftDesign d, PartDatabase db, List<StageSimPart> model, int maxStage, DesignStats s)
        {
            void Add(string t, bool blocking = false, bool info = false) => s.Warnings.Add(new DesignWarning { Text = t, Blocking = blocking, Info = info });

            int setAside = 0;
            if (d.detached != null) foreach (var g in d.detached) setAside += g.parts.Count;
            if (setAside > 0)
                Add(setAside == 1 ? "1 part set aside (grey) is not part of the craft and stays in the building"
                                  : $"{setAside} parts set aside (grey) are not part of the craft and stay in the building", info: true);

            bool hasCommand = false, hasCrewedCommand = false, hasProbe = false, hasChute = false, hasShield = false, hasLegs = false;
            int stageableUnstaged = 0;
            foreach (var p in d.parts)
            {
                var def = db.Get(p.partId);
                if (def == null) continue;
                if (def.command != null)
                {
                    hasCommand = true;
                    if (def.command.requiresCrew) hasCrewedCommand = true; else hasProbe = true;
                }
                if (def.parachute != null) hasChute = true;
                if (def.heatShield != null) hasShield = true;
                if (def.landingLeg != null) hasLegs = true;
                if (def.IsStageable && p.stage < 0) stageableUnstaged++;
            }
            if (!hasCommand) Add("No command pod or probe core: the craft can't be controlled.", blocking: true);
            if (hasCrewedCommand && !hasProbe && s.CrewAvailable == 0)
                Add("No rocketeers are available (all are flying or lost): the command pod will launch empty and can't be controlled.");
            else if (s.CrewSeats > s.CrewAvailable && s.CrewAvailable > 0)
                Add($"Only {s.CrewAvailable} of {s.CrewSeats} seats can be filled from the roster.", info: true);

            // First stage: engines and lift-off.
            if (maxStage >= 0)
            {
                bool firstHasEngine = false;
                for (int i = 0; i < d.parts.Count; i++)
                    if (d.parts[i].stage == maxStage && model[i].Def?.engine != null) firstHasEngine = true;
                if (!firstHasEngine)
                {
                    bool anyEngine = false;
                    foreach (var m in model) if (m.Def?.engine != null) anyEngine = true;
                    if (anyEngine) Add("The first stage (first Space press) has no engine: the rocket will just sit on the pad.");
                }
                if (s.LaunchTwr > 0 && s.LaunchTwr < 1.0)
                    Add($"Launch thrust-to-weight is {s.LaunchTwr:F2}: below 1.0 the rocket can't lift off. Add thrust or remove mass.");
                else if (s.LaunchTwr >= 1.0 && s.LaunchTwr < 1.2)
                    Add($"Launch TWR {s.LaunchTwr:F2} is low: gravity losses will be large (1.3–1.8 is typical).", info: true);
            }
            else
            {
                bool anyEngine = false;
                foreach (var m in model) if (m.Def?.engine != null) anyEngine = true;
                if (anyEngine) Add("No staging sequence: engines can only be started from their part menus.");
            }

            // Engines without propellant access.
            for (int i = 0; i < model.Count; i++)
            {
                var e = model[i].Def?.engine;
                if (e == null || e.type == "solid") continue;
                var group = PartTree.CrossfeedGroup(model, i, null);
                double fuel = 0;
                foreach (int j in group) if (model[j].Resources.TryGetValue(e.propellant, out double a)) fuel += a;
                if (fuel <= 0) Add($"{model[i].Def.title} has no propellant it can reach (fuel doesn't flow through decouplers).");
            }
            if (stageableUnstaged > 0) Add($"{stageableUnstaged} stageable part(s) are not in the staging sequence (activate them from their part menu).", info: true);

            if (hasCrewedCommand)
            {
                if (!hasChute) Add("No parachute: the crew can't land safely on Tellus.");
                if (!hasShield && s.TotalDvVac > 3000) Add("No heat shield: returning from orbit will overheat the capsule. Put a heat shield under the pod.");
            }
            if (s.HasCoP && s.StabilityMargin > 0.3f && s.LaunchTwr > 0)
                Add($"Aerodynamically unstable: the centre of pressure is {s.StabilityMargin:F1} m ahead of the centre of mass. Add fins near the bottom or move mass up.");
            if (!hasLegs && s.TotalDvVac > 6000) Add("No landing legs: needed for a safe touchdown on Luma.", info: true);
            if (s.ElectricCharge <= 0 && hasCommand) Add("No electric charge: SAS, reaction wheels and probe cores need power.");
        }
    }
}
