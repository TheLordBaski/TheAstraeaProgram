using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Simulation
{
    public sealed partial class Vessel
    {
        // ------------------------------------------------------------------ target

        public string TargetId { get => Record.targetId; set { Record.targetId = value; } }

        /// <summary>Target position/velocity relative to the frame body at UT (true frame). False if no target.</summary>
        public bool TryGetTargetState(double ut, out Vector3d pos, out Vector3d vel)
        {
            pos = vel = Vector3d.zero;
            var id = Record.targetId;
            if (string.IsNullOrEmpty(id) || Sim == null) return false;
            var frameBody = Sim.Frame.Body;
            if (id.StartsWith("body:"))
            {
                var b = Sim.System.Get(id.Substring(5));
                if (b == null) return false;
                pos = b.GetPositionAtUT(ut) - frameBody.GetPositionAtUT(ut);
                vel = b.GetVelocityAtUT(ut) - frameBody.GetVelocityAtUT(ut);
                return true;
            }
            if (id.StartsWith("vessel:"))
            {
                var h = Sim.FindHandle(id.Substring(7));
                if (h == null || h.Loaded == this) return false;
                pos = h.PositionRelTo(frameBody, ut);
                vel = h.VelocityRelTo(frameBody, ut);
                return true;
            }
            return false;
        }

        public string TargetName
        {
            get
            {
                var id = Record.targetId;
                if (string.IsNullOrEmpty(id) || Sim == null) return null;
                if (id.StartsWith("body:")) return Sim.System.Get(id.Substring(5))?.Name;
                if (id.StartsWith("vessel:")) return Sim.FindHandle(id.Substring(7))?.Name;
                return null;
            }
        }

        // ------------------------------------------------------------------ speed modes

        /// <summary>Velocity used by the navball/prograde markers for the current speed mode (true frame).</summary>
        public Vector3d SpeedModeVelocity
        {
            get
            {
                switch (Ctrl.SpeedMode)
                {
                    case SpeedMode.Surface: return SurfaceVelocity;
                    case SpeedMode.Target:
                        if (TryGetTargetState(Sim.UT, out _, out var tv)) return TrueVelocity - tv;
                        return TrueVelocity;
                    default: return TrueVelocity;
                }
            }
        }

        public bool TryGetSasDirection(SasMode mode, out Vector3 dir)
        {
            dir = Vector3.zero;
            Vector3d r = TruePosition;
            Vector3d vOrb = TrueVelocity;
            Vector3d vMode = SpeedModeVelocity;
            switch (mode)
            {
                case SasMode.Prograde:
                    if (vMode.magnitude < 0.05) return false;
                    dir = (Vector3)vMode.normalized; return true;
                case SasMode.Retrograde:
                    if (vMode.magnitude < 0.05) return false;
                    dir = -(Vector3)vMode.normalized; return true;
                case SasMode.Normal:
                case SasMode.AntiNormal:
                case SasMode.RadialIn:
                case SasMode.RadialOut:
                {
                    if (vOrb.magnitude < 0.05) return false;
                    PatchedConics.OrbitalFrame(r, vOrb, out var pro, out var nrm, out var rad);
                    Vector3d d = mode == SasMode.Normal ? nrm : mode == SasMode.AntiNormal ? -nrm : mode == SasMode.RadialOut ? rad : -rad;
                    dir = (Vector3)d;
                    return true;
                }
                case SasMode.Target:
                case SasMode.AntiTarget:
                    if (!TryGetTargetState(Sim.UT, out var tp, out _)) return false;
                    Vector3d to = tp - r;
                    if (to.magnitude < 0.1) return false;
                    dir = (Vector3)(mode == SasMode.Target ? to.normalized : -to.normalized);
                    return true;
                case SasMode.Maneuver:
                    if (!TryGetBurnVector(out var burn) || burn.magnitude < 0.01) return false;
                    dir = (Vector3)burn.normalized;
                    return true;
            }
            return false;
        }

        // ------------------------------------------------------------------ maneuver nodes

        public List<ManeuverNodeRecord> ManeuverNodes => Record.maneuverNodes;

        private double _mnTargetUT = double.NaN;
        private Vector3d _mnTargetVel;
        private int _mnVersionSeen = -1;
        public int ManeuverVersion;

        public void NotifyManeuverNodesChanged()
        {
            ManeuverVersion++;
        }

        /// <summary>Recomputes the planned post-burn velocity of the first node from the current orbit.</summary>
        private void RefreshManeuverTarget()
        {
            _mnVersionSeen = ManeuverVersion;
            _mnTargetUT = double.NaN;
            if (ManeuverNodes.Count == 0 || Orbit == null) return;
            var n = ManeuverNodes[0];
            Orbit.GetStateAtUT(n.ut, out var r, out var v);
            var spec = new NodeSpec(n.ut, n.prograde, n.normal, n.radial);
            _mnTargetVel = v + PatchedConics.NodeDeltaV(spec, r, v);
            _mnTargetUT = n.ut;
        }

        /// <summary>
        /// Remaining burn for the first maneuver node: planned post-burn velocity minus the current
        /// orbit's velocity at the node time. Robust to finite burns (goes to zero when orbits match).
        /// </summary>
        public bool TryGetBurnVector(out Vector3d burn)
        {
            burn = Vector3d.zero;
            if (ManeuverNodes.Count == 0 || Orbit == null) return false;
            if (_mnVersionSeen != ManeuverVersion || double.IsNaN(_mnTargetUT)) RefreshManeuverTarget();
            if (double.IsNaN(_mnTargetUT)) return false;
            Vector3d vNow = Orbit.GetVelocityAtUT(_mnTargetUT);
            burn = _mnTargetVel - vNow;
            return true;
        }

        /// <summary>
        /// Per-stage delta-v from the current state: the burn in progress first (engines already lit), then
        /// every stage still to fire. CurrentStage is the next stage to fire, so the analysis starts one above it.
        /// </summary>
        public List<TAP.Parts.StageDeltaV> ComputeStageDeltaV(double gravity, double pressureAtm)
        {
            var model = new List<TAP.Parts.StageSimPart>(Parts.Count);
            var index = new Dictionary<Part, int>();
            foreach (var p in Parts) { index[p] = model.Count; model.Add(null); }
            foreach (var p in Parts)
            {
                var sp = new TAP.Parts.StageSimPart
                {
                    Def = p.Def, Parent = p.ParentPart != null ? index[p.ParentPart] : -1, AttachNode = p.AttachNode, ParentNode = p.ParentNode, Stage = p.Stage,
                };
                foreach (var r in p.Resources) sp.Resources[r.Def.id] = r.Amount;
                var e = p.GetModule<EngineModule>();
                if (e != null) { sp.EngineIgnited = e.Ignited; sp.ThrustLimit = e.ThrustLimit; }
                model[index[p]] = sp;
            }
            return TAP.Parts.DeltaVCalculator.Compute(model, CurrentStage + 1, gravity, pressureAtm, TAP.Parts.PartDatabase.Instance);
        }

        /// <summary>Total vacuum delta-v left in the vessel (all stages).</summary>
        public double TotalDeltaVVac()
        {
            double t = 0;
            foreach (var s in ComputeStageDeltaV(9.80665, 0)) t += s.DeltaVVac;
            return t;
        }

        /// <summary>Thrust, mass flow and effective Isp of all currently ignited engines (or next stage's).</summary>
        public void GetPropulsion(out double thrust, out double isp, bool includeNextStage = true)
        {
            thrust = 0;
            double mdot = 0;
            double pAtm = StaticPressure / 101325.0;
            foreach (var p in Parts)
            {
                var e = p.GetModule<EngineModule>();
                if (e == null || !e.Ignited || e.Flameout) continue;
                double lim = e.ThrustLimit;
                double m = e.MaxMassFlow * lim;
                double ispNow = e.IspAtPressure(pAtm);
                if (Resources.Available(p, e.Def.propellant) <= 0) continue;
                thrust += m * ispNow * MathD.G0;
                mdot += m;
            }
            if (thrust <= 0 && includeNextStage)
            {
                foreach (var p in Parts)
                {
                    var e = p.GetModule<EngineModule>();
                    if (e == null || p.Stage != CurrentStage) continue;
                    double m = e.MaxMassFlow * e.ThrustLimit;
                    thrust += m * e.IspAtPressure(pAtm) * MathD.G0;
                    mdot += m;
                }
            }
            isp = mdot > 0 ? thrust / (mdot * MathD.G0) : 0;
        }
    }
}
