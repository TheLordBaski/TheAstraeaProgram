using System;
using System.Collections.Generic;
using TAP.Core;
using TAP.Persistence;
using TAP.Simulation;
using TAP.Trajectory;

namespace TAP.Game
{
    /// <summary>Burn information for the next maneuver node (for the HUD and the autopilot).</summary>
    public struct NodeBurnInfo
    {
        public bool Valid;
        public double NodeUT;
        public double TotalDeltaV;
        public double RemainingDeltaV;
        public Vector3d RemainingVector;
        public double BurnTime;       // for the remaining delta-v at current thrust
        public double TimeToNode;
        public double TimeToBurnStart; // node - burnTime/2
        public double AngleToBurn;     // degrees between nose and burn vector
    }

    /// <summary>
    /// Create/edit/delete maneuver nodes and derive burn info. Every edit invalidates the vessel's
    /// planned-burn cache so the navball marker and the planned trajectory update immediately.
    /// </summary>
    public static class ManeuverPlanner
    {
        public static ManeuverNodeRecord AddNode(Vessel v, double ut, double pro = 0, double nrm = 0, double rad = 0)
        {
            var n = new ManeuverNodeRecord { ut = ut, prograde = pro, normal = nrm, radial = rad };
            v.ManeuverNodes.Add(n);
            v.ManeuverNodes.Sort((a, b) => a.ut.CompareTo(b.ut));
            v.NotifyManeuverNodesChanged();
            TrajectoryService.Instance?.ForceUpdate();
            return n;
        }

        public static void RemoveNode(Vessel v, ManeuverNodeRecord n)
        {
            v.ManeuverNodes.Remove(n);
            v.NotifyManeuverNodesChanged();
            TrajectoryService.Instance?.ForceUpdate();
        }

        public static void ClearNodes(Vessel v)
        {
            v.ManeuverNodes.Clear();
            v.NotifyManeuverNodesChanged();
            TrajectoryService.Instance?.ForceUpdate();
        }

        public static void Changed(Vessel v)
        {
            v.ManeuverNodes.Sort((a, b) => a.ut.CompareTo(b.ut));
            v.NotifyManeuverNodesChanged();
            TrajectoryService.Instance?.ForceUpdate();
        }

        public static double Magnitude(ManeuverNodeRecord n) => Math.Sqrt(n.prograde * n.prograde + n.normal * n.normal + n.radial * n.radial);

        public static NodeBurnInfo Info(Vessel v, double ut)
        {
            var info = new NodeBurnInfo();
            if (v == null || v.ManeuverNodes.Count == 0) return info;
            var n = v.ManeuverNodes[0];
            info.NodeUT = n.ut;
            info.TotalDeltaV = Magnitude(n);
            if (!v.TryGetBurnVector(out var burn)) return info;
            info.Valid = true;
            info.RemainingVector = burn;
            info.RemainingDeltaV = burn.magnitude;
            v.GetPropulsion(out double thrust, out double isp);
            double bt = PatchedConics.BurnTime(info.RemainingDeltaV, thrust, isp, v.TotalMass);
            double btFull = PatchedConics.BurnTime(info.TotalDeltaV, thrust, isp, v.TotalMass);
            info.BurnTime = bt;
            info.TimeToNode = n.ut - ut;
            info.TimeToBurnStart = double.IsNaN(btFull) ? info.TimeToNode : info.TimeToNode - btFull * 0.5;
            if (info.RemainingDeltaV > 1e-3)
            {
                var nose = v.ControlRotation * UnityEngine.Vector3.up;
                info.AngleToBurn = UnityEngine.Vector3.Angle(nose, (UnityEngine.Vector3)burn.normalized);
            }
            return info;
        }

        /// <summary>Prograde delta-v needed at apoapsis (or at a given UT) to circularize.</summary>
        public static double CircularizeDeltaV(Orbit o, double ut)
        {
            o.GetStateAtUT(ut, out var r, out var vel);
            double vc = Math.Sqrt(o.Mu / r.magnitude);
            // component of velocity perpendicular to r
            Vector3d rhat = r.normalized;
            Vector3d vt = vel - rhat * Vector3d.Dot(vel, rhat);
            return vc - vt.magnitude;
        }

        /// <summary>
        /// Searches node time and prograde delta-v for a trajectory that encounters <paramref name="target"/>
        /// with a periapsis close to <paramref name="targetPeAltitude"/>. Uses the same patched-conic
        /// predictions displayed on the map. Returns false if no encounter is found.
        /// </summary>
        public static bool FindTransfer(Orbit orbit, CelestialBody body, CelestialBody target, double ut, double targetPeAltitude,
            double dvMin, double dvMax, out double bestUT, out double bestDv, out double bestPe)
        {
            bestUT = double.NaN; bestDv = double.NaN; bestPe = double.NaN;
            double period = orbit.IsElliptic ? orbit.Period : 3600;
            double bestScore = double.MaxValue;
            int nt = 90, nv = 12;
            for (int i = 0; i < nt; i++)
            {
                double t = ut + 120 + period * i / nt;
                for (int j = 0; j <= nv; j++)
                {
                    double dv = dvMin + (dvMax - dvMin) * j / nv;
                    double score = Score(orbit, body, target, t, dv, 0, targetPeAltitude, out double pe) + 50 * (dv - dvMin);
                    if (score < bestScore) { bestScore = score; bestUT = t; bestDv = dv; bestPe = pe; }
                }
            }
            if (double.IsNaN(bestUT)) return false;
            // Local refinement (coordinate descent on time and delta-v).
            double stepT = period / nt, stepV = (dvMax - dvMin) / nv;
            for (int it = 0; it < 60; it++)
            {
                bool improved = false;
                foreach (var (dt, ddv) in new[] { (stepT, 0.0), (-stepT, 0.0), (0.0, stepV), (0.0, -stepV) })
                {
                    double dvTry = bestDv + ddv;
                    if (dvTry < dvMin || dvTry > dvMax) continue;
                    double s = Score(orbit, body, target, bestUT + dt, dvTry, 0, targetPeAltitude, out double pe) + 50 * (dvTry - dvMin);
                    if (s < bestScore) { bestScore = s; bestUT += dt; bestDv += ddv; bestPe = pe; improved = true; }
                }
                if (!improved) { stepT *= 0.5; stepV *= 0.5; }
                if (stepT < 0.05 && stepV < 0.005) break;
            }
            return bestScore < 1e6;
        }

        /// <summary>
        /// Encounter score: |Pe - target| in metres plus 0.5 m per second of coast to the encounter (a quick
        /// outbound encounter beats one on the way back down from a high apoapsis, where small burn errors have
        /// many more hours to grow); huge if there is no encounter.
        /// </summary>
        public static double Score(Orbit orbit, CelestialBody body, CelestialBody target, double t, double pro, double nrm, double targetPeAlt, out double peAlt)
        {
            peAlt = double.NaN;
            var nodes = new List<NodeSpec> { new NodeSpec(t, pro, nrm, 0) };
            var patches = PatchedConics.Predict(orbit, body, t - 1, nodes, 4, 30 * 86400.0, false);
            foreach (var p in patches)
            {
                if (p.Body != target) continue;
                double pe = p.Orbit.PeriapsisRadius - target.Radius;
                peAlt = pe;
                return Math.Abs(pe - targetPeAlt) + 0.5 * Math.Max(0, p.StartUT - t);
            }
            // no encounter: distance of closest approach to target SOI as a shaping term
            double best = double.MaxValue;
            foreach (var p in patches)
            {
                if (p.Body != body) continue;
                for (int k = 0; k <= 60; k++)
                {
                    double tt = p.StartUT + (Math.Min(p.EndUT, p.StartUT + 20 * 86400) - p.StartUT) * k / 60;
                    double d = (p.Orbit.GetPositionAtUT(tt) - target.Orbit.GetPositionAtUT(tt)).magnitude;
                    best = Math.Min(best, d);
                }
            }
            return 1e6 + best;
        }
    }
}
