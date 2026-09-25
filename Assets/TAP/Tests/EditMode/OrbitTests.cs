using System;
using NUnit.Framework;
using TAP.Core;

namespace TAP.Tests
{
    public class OrbitTests
    {
        private const double Mu = 3.5316e12;
        private const double R = 600000;

        private static void Rk4Propagate(ref Vector3d r, ref Vector3d v, double mu, double dt, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Vector3d a1 = Acc(r, mu);
                Vector3d k1r = v, k1v = a1;
                Vector3d k2r = v + k1v * (dt / 2), k2v = Acc(r + k1r * (dt / 2), mu);
                Vector3d k3r = v + k2v * (dt / 2), k3v = Acc(r + k2r * (dt / 2), mu);
                Vector3d k4r = v + k3v * dt, k4v = Acc(r + k3r * dt, mu);
                r = r + (k1r + 2 * k2r + 2 * k3r + k4r) * (dt / 6);
                v = v + (k1v + 2 * k2v + 2 * k3v + k4v) * (dt / 6);
            }
        }

        private static Vector3d Acc(Vector3d r, double mu)
        {
            double m = r.magnitude;
            return r * (-mu / (m * m * m));
        }

        [Test]
        public void CircularOrbit_ReturnsAfterOnePeriod()
        {
            double rr = R + 80000;
            var r0 = new Vector3d(rr, 0, 0);
            var v0 = new Vector3d(0, 0, -Math.Sqrt(Mu / rr));
            var o = new Orbit(r0, v0, 100, Mu);
            Assert.AreEqual(0, o.Eccentricity, 1e-9);
            Assert.AreEqual(0, o.Inclination, 1e-9, "prograde equatorial should have zero inclination");
            o.GetStateAtUT(100 + o.Period, out var r1, out var v1);
            Assert.Less((r1 - r0).magnitude, 1e-3);
            o.GetStateAtUT(100 + o.Period * 0.5, out var rh, out _);
            Assert.Less((rh + r0).magnitude, 1e-3, "half period should be on the opposite side");
            // Many periods later (time warp)
            o.GetStateAtUT(100 + o.Period * 250.25, out var rq, out _);
            Assert.AreEqual(rr, rq.magnitude, 1e-3);
        }

        [Test]
        public void EllipticOrbit_MatchesNumericalIntegration()
        {
            var r0 = new Vector3d(R + 100000, 1000, 2000);
            var v0 = new Vector3d(150, 300, -2600);
            var o = new Orbit(r0, v0, 0, Mu);
            Assert.IsTrue(o.IsElliptic);
            Vector3d r = r0, v = v0;
            double dt = 0.05; int steps = 20000; // 1000 s
            Rk4Propagate(ref r, ref v, Mu, dt, steps);
            o.GetStateAtUT(dt * steps, out var rk, out var vk);
            Assert.Less((rk - r).magnitude, 0.05, "position agrees with RK4");
            Assert.Less((vk - v).magnitude, 1e-4, "velocity agrees with RK4");
        }

        [Test]
        public void HyperbolicOrbit_MatchesNumericalIntegration_BothDirections()
        {
            var r0 = new Vector3d(R + 200000, 0, 0);
            var v0 = new Vector3d(500, 200, -4200); // above escape speed (~3027 m/s)
            var o = new Orbit(r0, v0, 50, Mu);
            Assert.IsTrue(o.IsHyperbolic);
            Vector3d r = r0, v = v0;
            Rk4Propagate(ref r, ref v, Mu, 0.05, 40000); // 2000 s
            o.GetStateAtUT(50 + 2000, out var rk, out _);
            Assert.Less((rk - r).magnitude, 0.2);
            // Backwards in time
            r = r0; v = v0;
            Rk4Propagate(ref r, ref v, Mu, -0.05, 10000);
            o.GetStateAtUT(50 - 500, out var rb, out _);
            Assert.Less((rb - r).magnitude, 0.05);
        }

        [Test]
        public void RadialTrajectory_DoesNotProduceNaN()
        {
            var r0 = new Vector3d(0, R + 50000, 0);
            var v0 = new Vector3d(0, 300, 0);
            var o = new Orbit(r0, v0, 0, Mu);
            Assert.IsTrue(o.IsRadial);
            o.GetStateAtUT(60, out var r1, out var v1);
            Assert.IsTrue(r1.IsFinite() && v1.IsFinite());
            Vector3d r = r0, v = v0;
            Rk4Propagate(ref r, ref v, Mu, 0.01, 6000);
            Assert.Less((r1 - r).magnitude, 0.05);
        }

        [Test]
        public void ElementsRoundTrip()
        {
            double a = 9_000_000, e = 0.35, i = 0.3, lan = 1.1, argPe = 2.0, M0 = 0.7;
            var o = Orbit.FromElements(a, e, i, lan, argPe, M0, 0, Mu);
            Assert.AreEqual(a, o.SemiMajorAxis, 1e-3);
            Assert.AreEqual(e, o.Eccentricity, 1e-9);
            Assert.AreEqual(i, o.Inclination, 1e-9);
            Assert.AreEqual(lan, o.LongitudeOfAscendingNode, 1e-9);
            Assert.AreEqual(argPe, o.ArgumentOfPeriapsis, 1e-9);
            Assert.AreEqual(M0, o.MeanAnomalyAtEpoch, 1e-9);
        }

        [Test]
        public void TimeToApsides_LandsOnApsides()
        {
            var r0 = new Vector3d(R + 90000, 0, 0);
            var v0 = new Vector3d(120, 0, -2500);
            var o = new Orbit(r0, v0, 1000, Mu);
            double tpe = o.TimeToPeriapsis(1000);
            double tap = o.TimeToApoapsis(1000);
            Assert.AreEqual(o.PeriapsisRadius, o.GetPositionAtUT(1000 + tpe).magnitude, 0.01);
            Assert.AreEqual(o.ApoapsisRadius, o.GetPositionAtUT(1000 + tap).magnitude, 0.01);
            double nuTarget = 1.234;
            double ut = o.UTAtTrueAnomaly(nuTarget, 1000);
            Assert.AreEqual(nuTarget, MathD.WrapPi(o.TrueAnomalyAtUT(ut)), 1e-7);
        }

        [Test]
        public void ProgradeRotationConventionMatchesUnity()
        {
            // A point on the equator at +X should move towards -Z for positive rotation about +Y.
            var q = QuaternionD.AngleAxisRad(0.1, Vector3d.up);
            var p = q * new Vector3d(1, 0, 0);
            Assert.Less(p.z, 0);
            var uq = UnityEngine.Quaternion.AngleAxis(0.1f * (float)MathD.Rad2Deg, UnityEngine.Vector3.up) * new UnityEngine.Vector3(1, 0, 0);
            Assert.AreEqual(uq.x, p.x, 1e-6);
            Assert.AreEqual(uq.z, p.z, 1e-6);
            // Angular velocity cross product gives the same direction.
            var w = new Vector3d(0, 1, 0);
            Assert.Less(Vector3d.Cross(w, new Vector3d(1, 0, 0)).z, 0);
        }

        [Test]
        public void PlannedTrajectory_AppliesNodeSeveralOrbitsAhead()
        {
            var sys = CelestialSystem.LoadFromResources();
            var tellus = sys.Get("tellus");
            double r = tellus.Radius + 83000;
            double vc = Math.Sqrt(tellus.GM / r);
            var o = new Orbit(new Vector3d(r, 0, 0), new Vector3d(0, 0, vc), 0, tellus.GM);
            double nodeUT = o.Period * 2.4;
            var nodes = new System.Collections.Generic.List<TAP.Trajectory.NodeSpec> { new TAP.Trajectory.NodeSpec(nodeUT, 300, 0, 0) };
            var patches = TAP.Trajectory.PatchedConics.Predict(o, tellus, 0, nodes, 4);
            Assert.GreaterOrEqual(patches.Count, 2, "the node must start a new patch");
            Assert.AreEqual(TAP.Trajectory.PatchEnd.Maneuver, patches[0].EndType);
            Assert.AreEqual(nodeUT, patches[0].EndUT, 1e-6);
            Assert.Greater(patches[1].Orbit.ApoapsisRadius, r + 500000, "300 m/s prograde raises the apoapsis");
        }

        [Test]
        public void SystemDefinitionLoads()
        {
            var sys = CelestialSystem.LoadFromResources();
            var tellus = sys.Get("tellus");
            var luma = sys.Get("luma");
            Assert.NotNull(tellus);
            Assert.NotNull(luma);
            Assert.AreEqual(9.81, tellus.SurfaceGravity, 0.01);
            Assert.AreEqual(1.225, tellus.Atmosphere.Density(0), 0.01);
            Assert.Greater(luma.SOIRadius, 2.5e6);
            Assert.Less(luma.SOIRadius, 3.2e6);
            Assert.AreEqual(luma.Orbit.Period, luma.RotationPeriod, 1e-6, "Luma is tidally locked");
            // The launch site must be dry land close to pad altitude.
            var site = sys.Def.launchSite;
            double h = tellus.Terrain.Height(TerrainGenerator.DirectionFromLatLon(site.latitude, site.longitude));
            Assert.AreEqual(site.padAltitude, h, 0.5);
        }
    }
}
