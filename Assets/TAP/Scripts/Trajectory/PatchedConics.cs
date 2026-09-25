using System;
using System.Collections.Generic;
using TAP.Core;

namespace TAP.Trajectory
{
    public enum PatchEnd { None, SoiEnter, SoiExit, Maneuver, Impact }

    /// <summary>One conic segment of a predicted trajectory.</summary>
    public sealed class OrbitPatch
    {
        public Orbit Orbit;
        public CelestialBody Body;
        public double StartUT;
        public double EndUT;
        public PatchEnd EndType;
        public CelestialBody NextBody;
        public int ManeuverIndex = -1;
        /// <summary>Periapsis time within this patch (NaN if not inside [Start, End]).</summary>
        public double PeriapsisUT = double.NaN;
        public double ApoapsisUT = double.NaN;
        public double AtmosphereEntryUT = double.NaN;

        public override string ToString() => $"{Body.Name} [{StartUT:F0}-{EndUT:F0}] {EndType} {Orbit}";
    }

    /// <summary>A planned impulsive burn: delta-v in the orbital frame at a time.</summary>
    public struct NodeSpec
    {
        public double UT;
        public double Prograde, Normal, Radial;
        public NodeSpec(double ut, double pro, double nrm, double rad) { UT = ut; Prograde = pro; Normal = nrm; Radial = rad; }
        public double Magnitude => Math.Sqrt(Prograde * Prograde + Normal * Normal + Radial * Radial);
    }

    /// <summary>Patched-conic trajectory prediction and orbital-geometry utilities.</summary>
    public static class PatchedConics
    {
        /// <summary>Prograde / orbit-normal / radial-out unit vectors for a state.</summary>
        public static void OrbitalFrame(Vector3d r, Vector3d v, out Vector3d prograde, out Vector3d normal, out Vector3d radial)
        {
            prograde = v.normalized;
            normal = Vector3d.Cross(r, v).normalized;
            if (normal.sqrMagnitude < 0.5)
            {
                // radial trajectory: any normal perpendicular to v
                Vector3d t = Math.Abs(prograde.y) < 0.9 ? Vector3d.up : Vector3d.right;
                normal = Vector3d.Cross(prograde, t).normalized;
            }
            radial = Vector3d.Cross(prograde, normal);
        }

        public static Vector3d NodeDeltaV(NodeSpec n, Vector3d r, Vector3d v)
        {
            OrbitalFrame(r, v, out var pro, out var nrm, out var rad);
            return pro * n.Prograde + nrm * n.Normal + rad * n.Radial;
        }

        /// <summary>Inverse of NodeDeltaV: components of a world delta-v in the orbital frame.</summary>
        public static void DecomposeDeltaV(Vector3d dv, Vector3d r, Vector3d v, out double pro, out double nrm, out double rad)
        {
            OrbitalFrame(r, v, out var p, out var n, out var rd);
            pro = Vector3d.Dot(dv, p);
            nrm = Vector3d.Dot(dv, n);
            rad = Vector3d.Dot(dv, rd);
        }

        private static double MaxSpeed(Orbit o)
        {
            if (o.IsRadial) return Math.Sqrt(o.V0.sqrMagnitude + 2 * o.Mu / Math.Max(1, o.R0.magnitude)) * 2 + 1000;
            double rp = Math.Max(o.PeriapsisRadius, 1);
            return Math.Sqrt(o.Mu * (2 / rp - o.Alpha));
        }

        /// <summary>
        /// Earliest time in [t0, t1] at which a trajectory around <paramref name="body"/> enters a child's SOI.
        /// Uses conservative advancement (never steps past the boundary), then bisection.
        /// </summary>
        public static bool FindSoiEntry(Orbit o, CelestialBody body, double t0, double t1, out double tEntry, out CelestialBody child)
        {
            tEntry = double.NaN;
            child = null;
            double best = double.PositiveInfinity;
            foreach (var c in body.Children)
            {
                double vBound = MaxSpeed(o) + MaxSpeed(c.Orbit);
                double t = t0;
                double soi = c.SOIRadius;
                int guard = 0;
                double d = Dist(o, c, t);
                if (d < soi) continue; // already inside (shouldn't happen for a patch starting outside)
                while (t < t1 && guard++ < 20000)
                {
                    double margin = d - soi;
                    double step = Math.Max(margin / vBound * 0.95, 0.5);
                    double tn = Math.Min(t1, t + step);
                    double dn = Dist(o, c, tn);
                    if (dn <= soi)
                    {
                        // bisect in [t, tn]
                        double a = t, b = tn;
                        for (int i = 0; i < 60; i++)
                        {
                            double m = 0.5 * (a + b);
                            if (Dist(o, c, m) <= soi) b = m; else a = m;
                            if (b - a < 1e-3) break;
                        }
                        if (b < best) { best = b; child = c; }
                        break;
                    }
                    t = tn;
                    d = dn;
                    if (tn >= t1) break;
                }
            }
            if (child == null) return false;
            tEntry = best;
            return true;
        }

        private static double Dist(Orbit o, CelestialBody c, double t) => (o.GetPositionAtUT(t) - c.Orbit.GetPositionAtUT(t)).magnitude;

        /// <summary>Time at which the trajectory leaves the body's SOI (after t0), NaN if it never does.</summary>
        public static double FindSoiExit(Orbit o, CelestialBody body, double t0)
        {
            if (body.Parent == null) return double.NaN;
            double soi = body.SOIRadius;
            if (o.IsRadial)
            {
                // sample outward
                double t = t0;
                for (int i = 0; i < 4000; i++)
                {
                    double tn = t + 60;
                    if (o.GetPositionAtUT(tn).magnitude > soi)
                    {
                        double a = t, b = tn;
                        for (int k = 0; k < 50; k++) { double m = 0.5 * (a + b); if (o.GetPositionAtUT(m).magnitude > soi) b = m; else a = m; }
                        return b;
                    }
                    t = tn;
                }
                return double.NaN;
            }
            if (o.IsElliptic && o.ApoapsisRadius < soi) return double.NaN;
            double nu = o.TrueAnomalyAtRadius(soi);
            if (double.IsNaN(nu)) return double.NaN;
            double tExit = o.UTAtTrueAnomaly(nu, t0);
            if (!o.IsElliptic && tExit < t0) return double.NaN;
            return tExit;
        }

        /// <summary>Next time the trajectory descends through radius r (NaN if it never does).</summary>
        public static double FindRadiusDescending(Orbit o, double r, double t0)
        {
            if (o.IsRadial)
            {
                double t = t0;
                double r0 = o.GetPositionAtUT(t).magnitude;
                if (r0 <= r) return t0;
                for (int i = 0; i < 20000; i++)
                {
                    double tn = t + 5;
                    if (o.GetPositionAtUT(tn).magnitude <= r)
                    {
                        double a = t, b = tn;
                        for (int k = 0; k < 50; k++) { double m = 0.5 * (a + b); if (o.GetPositionAtUT(m).magnitude <= r) b = m; else a = m; }
                        return b;
                    }
                    t = tn;
                }
                return double.NaN;
            }
            if (o.PeriapsisRadius >= r) return double.NaN;
            double nu = o.TrueAnomalyAtRadius(r);
            if (double.IsNaN(nu)) return double.NaN; // circular below r: already inside
            double tIn = o.UTAtTrueAnomaly(-nu, t0);
            if (!o.IsElliptic && tIn < t0) return double.NaN;
            return tIn;
        }

        /// <summary>
        /// Predicts the trajectory from an initial orbit, applying maneuver nodes in order and following
        /// SOI transitions. Closed orbits are shown for one revolution (from the last event).
        /// </summary>
        public static List<OrbitPatch> Predict(Orbit start, CelestialBody body, double startUT, IList<NodeSpec> nodes,
            int maxPatches = 6, double openHorizon = 40 * 86400.0, bool stopAtImpact = true)
        {
            var result = new List<OrbitPatch>();
            if (start == null || body == null) return result;
            Orbit orbit = start;
            double t = startUT;
            int nodeIdx = 0;
            if (nodes != null) while (nodeIdx < nodes.Count && nodes[nodeIdx].UT < startUT - 1e-6) nodeIdx++;

            for (int k = 0; k < maxPatches; k++)
            {
                double horizon = orbit.IsElliptic && orbit.ApoapsisRadius < body.SOIRadius
                    ? t + orbit.Period
                    : t + openHorizon;
                if (orbit.IsElliptic && orbit.ApoapsisRadius >= body.SOIRadius) horizon = t + Math.Min(openHorizon, orbit.Period);
                // A closed orbit repeats, so it runs on to the next planned manoeuvre even when that is several
                // revolutions away (encounters on the way are still found below).
                if (nodes != null && nodeIdx < nodes.Count && orbit.IsElliptic && nodes[nodeIdx].UT > horizon)
                    horizon = Math.Min(nodes[nodeIdx].UT, t + openHorizon);

                var patch = new OrbitPatch { Orbit = orbit, Body = body, StartUT = t, EndUT = horizon, EndType = PatchEnd.None };
                double tEvent = horizon;
                PatchEnd type = PatchEnd.None;
                CelestialBody next = null;

                if (nodes != null && nodeIdx < nodes.Count && nodes[nodeIdx].UT <= tEvent)
                {
                    tEvent = nodes[nodeIdx].UT;
                    type = PatchEnd.Maneuver;
                }
                double tExit = FindSoiExit(orbit, body, t);
                if (!double.IsNaN(tExit) && tExit < tEvent) { tEvent = tExit; type = PatchEnd.SoiExit; next = body.Parent; }
                if (body.Children.Count > 0 && FindSoiEntry(orbit, body, t, tEvent, out double tEnt, out CelestialBody child) && tEnt < tEvent)
                {
                    tEvent = tEnt; type = PatchEnd.SoiEnter; next = child;
                }
                double tImpact = FindRadiusDescending(orbit, body.Radius, t);
                if (!double.IsNaN(tImpact) && tImpact < tEvent && tImpact >= t)
                {
                    tEvent = tImpact; type = PatchEnd.Impact;
                }
                if (body.Atmosphere != null)
                {
                    double tAtm = FindRadiusDescending(orbit, body.Radius + body.Atmosphere.Height, t);
                    if (!double.IsNaN(tAtm) && tAtm >= t && tAtm <= tEvent) patch.AtmosphereEntryUT = tAtm;
                }
                patch.EndUT = tEvent;
                patch.EndType = type;
                patch.NextBody = next;
                if (!orbit.IsRadial)
                {
                    double tp = t + orbit.TimeToPeriapsis(t);
                    if (!orbit.IsElliptic) tp = t + orbit.TimeToPeriapsis(t);
                    if (tp >= t - 1e-6 && tp <= tEvent) patch.PeriapsisUT = tp;
                    if (orbit.IsElliptic)
                    {
                        double ta = t + orbit.TimeToApoapsis(t);
                        if (ta <= tEvent) patch.ApoapsisUT = ta;
                    }
                }
                result.Add(patch);

                if (type == PatchEnd.None || (type == PatchEnd.Impact && stopAtImpact)) break;
                orbit.GetStateAtUT(tEvent, out Vector3d r, out Vector3d v);
                if (type == PatchEnd.Maneuver)
                {
                    var n = nodes[nodeIdx];
                    patch.ManeuverIndex = nodeIdx;
                    v = v + NodeDeltaV(n, r, v);
                    nodeIdx++;
                    orbit = new Orbit(r, v, tEvent, body.GM);
                }
                else if (type == PatchEnd.SoiEnter)
                {
                    next.Orbit.GetStateAtUT(tEvent, out Vector3d rc, out Vector3d vc);
                    orbit = new Orbit(r - rc, v - vc, tEvent, next.GM);
                    body = next;
                }
                else if (type == PatchEnd.SoiExit)
                {
                    body.Orbit.GetStateAtUT(tEvent, out Vector3d rb, out Vector3d vb);
                    orbit = new Orbit(r + rb, v + vb, tEvent, next.GM);
                    body = next;
                }
                else break;
                t = tEvent;
            }
            return result;
        }

        /// <summary>Position (relative to the root body) of a patch trajectory at UT.</summary>
        public static Vector3d AbsolutePosition(OrbitPatch p, double ut) => p.Body.GetPositionAtUT(ut) + p.Orbit.GetPositionAtUT(ut);
        public static Vector3d AbsoluteVelocity(OrbitPatch p, double ut) => p.Body.GetVelocityAtUT(ut) + p.Orbit.GetVelocityAtUT(ut);

        /// <summary>
        /// Closest approach between the predicted patches and a target (given as an absolute position function).
        /// Returns the minimum distance, its time and the relative speed there.
        /// </summary>
        public static bool ClosestApproach(IList<OrbitPatch> patches, Func<double, Vector3d> targetPos, Func<double, Vector3d> targetVel,
            out double minDist, out double tMin, out double relSpeed, double maxTime = double.PositiveInfinity)
        {
            minDist = double.PositiveInfinity; tMin = double.NaN; relSpeed = double.NaN;
            if (patches == null || patches.Count == 0) return false;
            foreach (var p in patches)
            {
                double t0 = p.StartUT, t1 = Math.Min(p.EndUT, maxTime);
                if (t1 <= t0) continue;
                int n = 240;
                double dt = (t1 - t0) / n;
                double prevD = double.NaN, prev2D = double.NaN;
                for (int i = 0; i <= n; i++)
                {
                    double t = t0 + dt * i;
                    double d = (AbsolutePosition(p, t) - targetPos(t)).magnitude;
                    if (d < minDist) { minDist = d; tMin = t; }
                    // local minimum in the previous sample -> refine
                    if (i >= 2 && prevD <= prev2D && prevD <= d)
                    {
                        double a = t - 2 * dt, b = t;
                        Refine(p, targetPos, ref a, ref b, out double tm, out double dm);
                        if (dm < minDist) { minDist = dm; tMin = tm; }
                    }
                    prev2D = prevD;
                    prevD = d;
                }
            }
            if (double.IsNaN(tMin)) return false;
            foreach (var p in patches)
                if (tMin >= p.StartUT && tMin <= p.EndUT)
                {
                    relSpeed = (AbsoluteVelocity(p, tMin) - targetVel(tMin)).magnitude;
                    break;
                }
            return true;
        }

        private static void Refine(OrbitPatch p, Func<double, Vector3d> target, ref double a, ref double b, out double tm, out double dm)
        {
            const double gr = 0.6180339887498949;
            double c = b - gr * (b - a), d = a + gr * (b - a);
            double fc = (AbsolutePosition(p, c) - target(c)).magnitude;
            double fd = (AbsolutePosition(p, d) - target(d)).magnitude;
            for (int i = 0; i < 60 && b - a > 1e-3; i++)
            {
                if (fc < fd) { b = d; d = c; fd = fc; c = b - gr * (b - a); fc = (AbsolutePosition(p, c) - target(c)).magnitude; }
                else { a = c; c = d; fc = fd; d = a + gr * (b - a); fd = (AbsolutePosition(p, d) - target(d)).magnitude; }
            }
            tm = 0.5 * (a + b);
            dm = (AbsolutePosition(p, tm) - target(tm)).magnitude;
        }

        /// <summary>Burn duration (s) for a delta-v with given thrust (N), Isp (s) and initial mass (kg).</summary>
        public static double BurnTime(double deltaV, double thrust, double isp, double mass)
        {
            if (thrust <= 0 || isp <= 0 || mass <= 0) return double.NaN;
            double ve = isp * MathD.G0;
            double mdot = thrust / ve;
            double m1 = mass * Math.Exp(-deltaV / ve);
            return (mass - m1) / mdot;
        }
    }
}
