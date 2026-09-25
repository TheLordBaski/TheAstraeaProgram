using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>Stack or radial decoupler: severs its explosive-node connection when staged.</summary>
    public sealed class DecouplerModule : PartModule
    {
        public bool Fired;
        public override string ModuleName => "Decoupler";
        public override bool IsStageable => true;

        public override void OnActivate()
        {
            if (Fired) return;
            Fired = true;
            Vessel.QueueDecouple(Part);
            FlightSim.Instance?.RequestStructurePass(Vessel);
        }

        public override void Save(Dictionary<string, string> s) { s["fired"] = B(Fired); }
        public override void Load(Dictionary<string, string> s) { Fired = GetBool(s, "fired"); }

        public override void CollectActions(List<PartAction> actions)
        {
            if (!Fired) actions.Add(new PartAction("Decouple", OnActivate));
        }
    }

    /// <summary>Reaction wheel: torque on command, draws electric charge proportional to torque used.</summary>
    public sealed class ReactionWheelModule : PartModule
    {
        public ReactionWheelDefinition Def;
        public bool Enabled = true;
        public Vector3 LastTorque;
        public override string ModuleName => "ReactionWheel";

        public override void OnInit() { Def = Part.Def.reactionWheel; }

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            LastTorque = Vector3.zero;
            if (!Enabled || !v.HasControl) return;
            v.Resources.Totals("Electric", out double ec, out _);
            if (ec <= 0.001) return;
            float T = (float)Def.torque;
            v.AddAuthority(new Vector3(T, T, T));
            Vector3 cmd = v.Ctrl.TorqueCommand;
            if (cmd.sqrMagnitude < 1e-8f) return;
            double use = Def.ecPerSecond * (Mathf.Abs(cmd.x) + Mathf.Abs(cmd.y) + Mathf.Abs(cmd.z)) / 3.0 * dt;
            double got = v.Resources.Request(Part, "Electric", use);
            float eff = use > 0 ? (float)(got / use) : 1f;
            Vector3 torqueLocal = cmd * T * eff;
            Vector3 world = v.ControlRotation * torqueLocal;
            v.AddTorque(Part, world);
            LastTorque = world;
        }

        public override void Save(Dictionary<string, string> s) { s["enabled"] = B(Enabled); }
        public override void Load(Dictionary<string, string> s) { Enabled = GetBool(s, "enabled", true); }

        public override void CollectActions(List<PartAction> actions)
        {
            actions.Add(new PartAction(Enabled ? "Disable reaction wheel" : "Enable reaction wheel", () => Enabled = !Enabled));
        }

        public override void CollectInfo(List<string> info) { info.Add($"Wheel torque {Def.torque / 1000:F1} kN*m ({(Enabled ? "on" : "off")})"); }
    }

    /// <summary>RCS thruster block: fires nozzles to produce requested rotation and translation.</summary>
    public sealed class RcsModule : PartModule
    {
        public RcsDefinition Def;
        public bool Enabled = true;
        public float[] NozzleThrottle;
        private Transform[] _nozzles;
        public override string ModuleName => "RCS";

        public override void OnInit()
        {
            Def = Part.Def.rcs;
            int n = Def.nozzles?.Length ?? 0;
            NozzleThrottle = new float[n];
            _nozzles = new Transform[n];
            for (int i = 0; i < n; i++) _nozzles[i] = Part.transform.Find("RcsNozzle" + i);
        }

        public Transform Nozzle(int i) => _nozzles[i];

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            for (int i = 0; i < NozzleThrottle.Length; i++) NozzleThrottle[i] = 0;
            if (!Enabled || !v.Ctrl.Rcs || !v.HasControl) return;
            Quaternion ctrlRot = v.ControlRotation;
            Vector3 rotCmd = ctrlRot * v.Ctrl.TorqueCommand;
            Vector3 transCmd = ctrlRot * v.Ctrl.TranslationCommand;
            Vector3 com = v.Rb.worldCenterOfMass;
            double pAtm = v.StaticPressure / 101325.0;
            double isp = Math.Max(Def.ispVac + (Def.ispAsl - Def.ispVac) * pAtm, 1);
            var res = PartDatabase.Instance.GetResource(Def.propellant);
            double dens = res != null && res.density > 0 ? res.density : 1;
            Quaternion inv = Quaternion.Inverse(ctrlRot);
            for (int i = 0; i < _nozzles.Length; i++)
            {
                var t = _nozzles[i];
                if (t == null) continue;
                Vector3 forceDir = -t.forward; // exhaust along forward
                Vector3 torque = Vector3.Cross(t.position - com, forceDir * (float)Def.thrust);
                v.AddAuthority(inv * torque);
                float thr = 0;
                if (rotCmd.sqrMagnitude > 1e-6f && torque.sqrMagnitude > 1e-6f)
                    thr += Vector3.Dot(torque.normalized, rotCmd);
                if (transCmd.sqrMagnitude > 1e-6f)
                    thr += Vector3.Dot(forceDir, transCmd);
                thr = Mathf.Clamp01(thr);
                if (thr < 0.05f) continue;
                double mdot = Def.thrust * thr / (isp * MathD.G0);
                double got = v.Resources.Request(Part, Def.propellant, mdot * dt / dens);
                double ratio = got / Math.Max(mdot * dt / dens, 1e-12);
                if (ratio < 0.05) continue;
                float f = (float)(Def.thrust * thr * ratio);
                NozzleThrottle[i] = (float)(thr * ratio);
                v.AddForceAtPosition(Part, forceDir * f, t.position);
            }
        }

        public override void Save(Dictionary<string, string> s) { s["enabled"] = B(Enabled); }
        public override void Load(Dictionary<string, string> s) { Enabled = GetBool(s, "enabled", true); }
        public override void CollectActions(List<PartAction> actions)
        {
            actions.Add(new PartAction(Enabled ? "Disable RCS block" : "Enable RCS block", () => Enabled = !Enabled));
        }
    }

    /// <summary>Command source; probe cores consume a trickle of electric charge.</summary>
    public sealed class CommandModule : PartModule
    {
        public override string ModuleName => "Command";
        public override void OnPreStep(double dt)
        {
            var c = Part.Def.command;
            if (c.ecPerSecond > 0) Vessel.Resources.Request(Part, "Electric", c.ecPerSecond * dt);
        }
    }

    /// <summary>Marks an ablative heat shield (ablation handled by the thermal model).</summary>
    public sealed class HeatShieldModule : PartModule
    {
        public HeatShieldDefinition Def;
        public double AblationRate;
        public override string ModuleName => "HeatShield";
        public override void OnInit() { Def = Part.Def.heatShield; }
        public override void CollectInfo(List<string> info)
        {
            var a = Part.GetResource("Ablator");
            if (a != null) info.Add($"Ablator {a.Amount:F0}/{a.Max:F0} kg ({AblationRate:F2} kg/s)");
        }
    }

    /// <summary>Stabilizer fin: flat-plate lift normal to the fin, applied at the fin.</summary>
    public sealed class FinModule : PartModule
    {
        public FinDefinition Def;
        public override string ModuleName => "Fin";
        public override void OnInit() { Def = Part.Def.fin; }

        /// <summary>Returns the aerodynamic force (world) for relative air velocity vRel (vessel moving through air).</summary>
        public Vector3 ComputeForce(Vector3 vRel, double density, double machFactor)
        {
            float speed = vRel.magnitude;
            if (speed < 0.1f) return Vector3.zero;
            Vector3 n = Part.transform.TransformDirection(Def.Normal);
            Vector3 dir = vRel / speed;
            float sinA = Vector3.Dot(dir, n);
            float a = Mathf.Asin(Mathf.Clamp(sinA, -1, 1));
            float stall = Def.stallDeg * Mathf.Deg2Rad;
            float cl;
            float absA = Mathf.Abs(a);
            if (absA < stall) cl = Def.liftSlope * absA;
            else cl = Def.liftSlope * stall * Mathf.Lerp(1f, 0.55f, Mathf.Clamp01((absA - stall) / 0.5f)) * Mathf.Cos(absA - stall);
            float cd90 = 1.2f * sinA * sinA;
            float q = 0.5f * (float)density * speed * speed * (float)machFactor;
            Vector3 normalForce = -Mathf.Sign(sinA) * n * q * Def.area * cl;
            Vector3 drag = -dir * q * Def.area * (0.02f + cd90 * 0.3f);
            return normalForce + drag;
        }
    }

    /// <summary>Crew seating + hatch (EVA exit / boarding point).</summary>
    public sealed class CrewModule : PartModule
    {
        private Transform _hatch;
        public override string ModuleName => "Crew";
        public override void OnInit() { _hatch = Part.transform.Find("Hatch"); }
        public Vector3 HatchWorldPosition => _hatch != null ? _hatch.position : Part.transform.position;
        public Vector3 HatchWorldNormal => _hatch != null ? _hatch.forward : Part.transform.forward;

        public override void CollectActions(List<PartAction> actions)
        {
            foreach (var c in Part.Crew)
            {
                string name = c;
                actions.Add(new PartAction($"EVA: {name}", () => FlightSim.Instance?.RequestEva(Part, name)));
            }
        }

        public override void CollectInfo(List<string> info)
        {
            info.Add($"Crew {Part.Crew.Count}/{Part.SeatCount}" + (Part.Crew.Count > 0 ? ": " + string.Join(", ", Part.Crew) : ""));
        }
    }
}
