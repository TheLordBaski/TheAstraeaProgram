using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Parts;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Liquid or solid rocket engine. Mass flow is fixed by the throttle; thrust = mdot * Isp(p) * g0,
    /// so thrust rises from sea level to vacuum exactly like Isp. Liquid engines throttle, shut down
    /// and restart; solids burn at their set thrust until empty.
    /// </summary>
    public sealed class EngineModule : PartModule
    {
        public EngineDefinition Def;
        public bool Ignited;        // activated (by staging or manually)
        public bool Flameout;
        public float CurrentThrottle;
        public double CurrentThrust;   // N
        public double CurrentMassFlow; // kg/s
        public double CurrentIsp;
        public Vector2 Gimbal;          // radians about the engine's local X and Z axes
        public float ThrustLimit = 1f;  // 0..1

        private Transform _bell, _nozzle;
        private Quaternion _bellRest;

        public override string ModuleName => "Engine";
        public override bool IsStageable => true;
        public bool IsSolid => Def.type == "solid";
        public bool IsRunning => Ignited && !Flameout && CurrentThrust > 1;

        public override void OnInit()
        {
            Def = Part.Def.engine;
            if (Part.Settings != null && Part.Settings.TryGetValue("thrustLimit", out double tl)) ThrustLimit = (float)MathD.Clamp01(tl / 100.0);
            else ThrustLimit = Def.thrustLimit / 100f;
            _bell = Part.ModelRoot != null ? Part.ModelRoot.Find("Bell") : null;
            if (_bell != null) _bellRest = _bell.localRotation;
            _nozzle = Part.transform.Find("Model/Bell/Nozzle");
            if (_nozzle == null) _nozzle = FindDeep(Part.transform, "Nozzle");
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        public override void OnActivate()
        {
            if (Ignited) return;
            Ignited = true;
            Flameout = false;
            if (IsSolid) CurrentThrottle = 1f;
        }

        public void Shutdown()
        {
            if (IsSolid) return;
            Ignited = false;
            CurrentThrust = 0;
        }

        public Vector3 NozzleWorldPosition => _nozzle != null ? _nozzle.position : Part.transform.TransformPoint(Def.NozzlePosition);

        /// <summary>Thrust direction (force on vessel) in world space, including gimbal deflection.</summary>
        public Vector3 ThrustDirectionWorld
        {
            get
            {
                Vector3 local = Def.ThrustDirection;
                Quaternion g = Quaternion.Euler(Gimbal.x * Mathf.Rad2Deg, 0, Gimbal.y * Mathf.Rad2Deg);
                return Part.transform.TransformDirection(g * local);
            }
        }

        public double MaxMassFlow => Def.thrustVac / (Def.ispVac * MathD.G0);

        public double IspAtPressure(double pressureAtm) => Math.Max(Def.ispVac + (Def.ispAsl - Def.ispVac) * pressureAtm, Def.ispVac * 0.05);

        public override void OnPreStep(double dt)
        {
            var v = Vessel;
            CurrentThrust = 0;
            CurrentMassFlow = 0;
            if (!Ignited)
            {
                CurrentThrottle = Mathf.MoveTowards(CurrentThrottle, 0, (float)dt * 4f);
                return;
            }

            float target = IsSolid ? ThrustLimit : (v.HasControl || v.IsEva ? v.Ctrl.Throttle * ThrustLimit : 0f);
            if (!IsSolid && target > 0 && target < Def.minThrottle) target = Def.minThrottle;
            if (Def.throttleResponse <= 0 || IsSolid) CurrentThrottle = target;
            else CurrentThrottle = Mathf.MoveTowards(CurrentThrottle, target, (float)(Def.throttleResponse * dt));
            if (CurrentThrottle <= 1e-4f) { UpdateGimbal(dt, 0); return; }

            double pAtm = v.StaticPressure / 101325.0;
            double isp = IspAtPressure(pAtm);
            double mdot = MaxMassFlow * CurrentThrottle;
            var res = PartDatabase.Instance.GetResource(Def.propellant);
            double density = res != null && res.density > 0 ? res.density : 1;
            double wantUnits = mdot * dt / density;
            double got = v.Resources.Request(Part, Def.propellant, wantUnits);
            double ratio = wantUnits > 0 ? got / wantUnits : 0;
            if (ratio < 0.02)
            {
                if (!Flameout) Sim.Log($"{Part.Def.title}: flameout (out of {res?.name ?? Def.propellant})");
                Flameout = true;
                CurrentThrottle = 0;
                if (IsSolid) Ignited = true;
                UpdateGimbal(dt, 0);
                return;
            }
            Flameout = false;
            CurrentMassFlow = mdot * ratio;
            CurrentIsp = isp;
            CurrentThrust = CurrentMassFlow * isp * MathD.G0;

            UpdateGimbal(dt, CurrentThrust);
            Vector3 dir = ThrustDirectionWorld;
            v.AddForceAtPosition(Part, dir * (float)CurrentThrust, NozzleWorldPosition);

            if (Def.alternator > 0)
                v.Resources.Produce(Part, "Electric", Def.alternator * CurrentThrottle * dt);
            if (Def.heatProduction > 0)
                Part.InternalTemp += Def.heatProduction * CurrentThrottle * dt / Math.Max(1, Part.Mass * Part.Def.specificHeat);
        }

        private FlightSim Sim => FlightSim.Instance;

        /// <summary>Chooses gimbal angles to produce the vessel's requested torque direction.</summary>
        private void UpdateGimbal(double dt, double thrust)
        {
            float range = Def.gimbalRange * Mathf.Deg2Rad;
            Vector2 target = Vector2.zero;
            var v = Vessel;
            if (range > 0 && thrust > 0 && v.HasControl)
            {
                Quaternion ctrlRot = v.ControlRotation;
                Vector3 cmdWorld = ctrlRot * v.Ctrl.TorqueCommand;
                Vector3 r = NozzleWorldPosition - v.Rb.worldCenterOfMass;
                Vector3 d = Part.transform.TransformDirection(Def.ThrustDirection);
                Vector3 ex = Part.transform.right, ez = Part.transform.forward;
                // Torque per radian of deflection about each gimbal axis: r x (T * (axis x d))
                Vector3 tx = Vector3.Cross(r, (float)thrust * Vector3.Cross(ex, d));
                Vector3 tz = Vector3.Cross(r, (float)thrust * Vector3.Cross(ez, d));
                if (cmdWorld.sqrMagnitude > 1e-8f)
                {
                    float gx = tx.sqrMagnitude > 1e-3f ? Vector3.Dot(tx.normalized, cmdWorld) : 0;
                    float gz = tz.sqrMagnitude > 1e-3f ? Vector3.Dot(tz.normalized, cmdWorld) : 0;
                    target = new Vector2(Mathf.Clamp(gx, -1, 1) * range, Mathf.Clamp(gz, -1, 1) * range);
                }
                // Authority in control frame for SAS.
                Quaternion inv = Quaternion.Inverse(ctrlRot);
                v.AddAuthority(inv * (tx * range));
                v.AddAuthority(inv * (tz * range));
            }
            float rate = 1.2f * (float)dt; // rad/s gimbal slew
            Gimbal.x = Mathf.MoveTowards(Gimbal.x, target.x, rate);
            Gimbal.y = Mathf.MoveTowards(Gimbal.y, target.y, rate);
        }

        public override void OnRenderUpdate(float dt)
        {
            if (_bell != null)
                _bell.localRotation = _bellRest * Quaternion.Euler(Gimbal.x * Mathf.Rad2Deg, 0, Gimbal.y * Mathf.Rad2Deg);
        }

        public override void Save(Dictionary<string, string> s)
        {
            s["ignited"] = B(Ignited);
            s["flameout"] = B(Flameout);
            s["throttle"] = F(CurrentThrottle);
            s["limit"] = F(ThrustLimit);
        }

        public override void Load(Dictionary<string, string> s)
        {
            Ignited = GetBool(s, "ignited");
            Flameout = GetBool(s, "flameout");
            CurrentThrottle = (float)GetDouble(s, "throttle");
            if (s.ContainsKey("limit")) ThrustLimit = (float)GetDouble(s, "limit", 1);
        }

        public override void CollectActions(List<PartAction> actions)
        {
            if (!IsSolid)
            {
                if (Ignited) actions.Add(new PartAction("Shut down engine", Shutdown));
                else actions.Add(new PartAction("Activate engine", OnActivate));
            }
            else if (!Ignited) actions.Add(new PartAction("Ignite booster", OnActivate));
        }

        public override void CollectInfo(List<string> info)
        {
            info.Add($"Thrust {CurrentThrust / 1000:F1} kN  Isp {CurrentIsp:F0} s");
            info.Add(Ignited ? (Flameout ? "FLAMEOUT" : $"Throttle {CurrentThrottle * 100:F0}%") : "Inactive");
        }

        public override string StageStatus => !Ignited ? null : (Flameout ? "Flameout" : $"{CurrentThrust / 1000:F0} kN");
    }
}
