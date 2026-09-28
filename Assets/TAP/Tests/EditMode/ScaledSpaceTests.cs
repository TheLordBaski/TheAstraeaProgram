using System;
using NUnit.Framework;
using TAP.Core;
using TAP.Game;
using TAP.Trajectory;
using UnityEngine;

namespace TAP.Tests
{
    /// <summary>The far view (FND-03) and the map at every zoom.</summary>
    public class ScaledSpaceTests
    {
        [Test]
        public void FarView_ShowsEveryBodyWhereTheFlightCameraWouldSeeIt()
        {
            // Two cameras with the same view: the flight camera somewhere in the flight scene, the far camera at the
            // origin of the far view. A body anywhere from low orbit to 10¹¹ m lands on the same pixel in both.
            var flightGo = new GameObject("flight");
            var farGo = new GameObject("far");
            try
            {
                var flight = flightGo.AddComponent<Camera>();
                var far = farGo.AddComponent<Camera>();
                flight.aspect = far.aspect = 16f / 9f;
                flight.fieldOfView = far.fieldOfView = 60;
                flightGo.transform.SetPositionAndRotation(new Vector3(812.5f, -40.25f, 3001f), Quaternion.Euler(12, 73, 4));
                farGo.transform.SetPositionAndRotation(Vector3.zero, flightGo.transform.rotation);
                far.nearClipPlane = 1e-3f;
                far.farClipPlane = 1e7f;
                var rng = new System.Random(3);
                int checkedBodies = 0;
                for (int i = 0; i < 200; i++)
                {
                    double dist = Math.Pow(10, 5 + 6 * rng.NextDouble()); // 100 km ... 10¹¹ m
                    var dir = new Vector3d(rng.NextDouble() - 0.5, rng.NextDouble() - 0.5, rng.NextDouble() - 0.5).normalized;
                    if (Vector3.Dot((Vector3)dir, flightGo.transform.forward) < 0.5f) continue;
                    Vector3 seen = flight.WorldToScreenPoint(flightGo.transform.position + (Vector3)dir * 100f);
                    Vector3 drawn = far.WorldToScreenPoint(ScaledSpace.ToFar(dir * dist));
                    Assert.Less(Vector2.Distance(seen, drawn), 0.05f, $"a body {dist:E1} m away");
                    checkedBodies++;
                }
                Assert.Greater(checkedBodies, 20);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(flightGo);
                UnityEngine.Object.DestroyImmediate(farGo);
            }
        }

        [Test]
        public void StarCoverage_IsTheOverlapOfTheDiscs()
        {
            Assert.AreEqual(0, ScaledSpace.CoveredFraction(0.01, 0.02, 0.031), 1e-12, "apart");
            Assert.AreEqual(1, ScaledSpace.CoveredFraction(0.01, 0.02, 0.005), 1e-12, "a larger disc in front");
            Assert.AreEqual(0.25, ScaledSpace.CoveredFraction(0.02, 0.01, 0.005), 1e-12, "a smaller disc inside");
            // Two equal discs one radius apart overlap by (2π/3 − √3/2) r².
            Assert.AreEqual((2 * Math.PI / 3 - Math.Sqrt(3) / 2) / Math.PI, ScaledSpace.CoveredFraction(0.01, 0.01, 0.01), 1e-9);
            // Sliding a planet across the star covers it steadily.
            double last = 1;
            for (double sep = 0; sep <= 0.03; sep += 0.001)
            {
                double f = ScaledSpace.CoveredFraction(0.01, 0.015, sep);
                Assert.LessOrEqual(f, last + 1e-12);
                last = f;
            }
        }

        [Test]
        public void PointsOfLight_DimWithDistanceButStayVisible()
        {
            var tellus = CelestialSystem.LoadFromResources().HomeBody;
            double px = 2 * Math.Tan(30 * MathD.Deg2Rad) / 1080;
            // As bright as a lit disc where the disc hands over (1.5 px radius).
            double handover = tellus.Radius / (ScaledSpace.PointRadiusPx * px);
            Assert.AreEqual(1.0, ScaledSpace.PointBrightness(tellus.Radius, handover, px, 1), 0.1);
            float last = float.MaxValue;
            foreach (double d in new[] { 1e9, 1e10, 3e10, 1e11, 1e12 })
            {
                float b = ScaledSpace.PointBrightness(tellus.Radius, d, px, 1);
                Assert.LessOrEqual(b, last, $"at {d:E0} m");
                Assert.GreaterOrEqual(b, 0.45f, $"still findable at {d:E0} m");
                last = b;
            }
            Assert.Less(ScaledSpace.PointBrightness(tellus.Radius, 1e10, px, 0.1), ScaledSpace.PointBrightness(tellus.Radius, 1e10, px, 1), "a crescent is dimmer");
        }

        [Test]
        public void OrbitLine_RunsThroughTheFocusAtEveryZoom()
        {
            // A probe on a solar orbit 14 Gm from the star, the map focused on it, zoomed from 1 km to 100 Gm: the line
            // runs through the probe to a thousandth of the view (float rounding and the chords between samples).
            var sys = CelestialSystem.LoadFromResources();
            var star = sys.Star;
            var orbit = new Orbit(new Vector3d(1.4e10, 0, 0), new Vector3d(0, 0, -Math.Sqrt(star.GM / 1.4e10) * 1.02), 0, star.GM);
            var patch = new OrbitPatch { Orbit = orbit, Body = star, StartUT = 0, EndUT = orbit.Period, EndType = PatchEnd.None };
            var parent = new GameObject("map");
            try
            {
                var line = new OrbitLine(parent.transform, null, 0);
                line.SamplePatch(patch, 320, double.MaxValue);
                line.BuildMesh(MapView.Scale, Color.white);
                foreach (double ut in new[] { 1e5, 3.3e6, 7.7e6 })
                {
                    Vector3d focus = orbit.GetPositionAtUT(ut);
                    for (double view = 1e3; view <= 1e11; view *= 10)
                    {
                        line.KeepExactNear(focus, view);
                        double miss = DistanceToDrawnLine(line, focus);
                        Assert.Less(miss, view * 1e-3, $"UT {ut:E1}, view {view:E0} m: the line passes {miss:E2} m from the focus");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
            }
        }

        /// <summary>
        /// Distance from a point (metres, relative to the line's body) to the line as drawn: the float vertices of its
        /// mesh, placed at its anchor.
        /// </summary>
        private static double DistanceToDrawnLine(OrbitLine line, Vector3d p)
        {
            var v = line.Mesh.vertices;
            double best = double.MaxValue;
            // Each segment is a quad of four vertices: two at its start, two at its end.
            for (int k = 0; k + 2 < v.Length; k += 4)
            {
                Vector3d a = line.Anchor + (Vector3d)v[k] * MapView.Scale;
                Vector3d b = line.Anchor + (Vector3d)v[k + 2] * MapView.Scale;
                Vector3d ab = b - a;
                double t = ab.sqrMagnitude > 0 ? MathD.Clamp01(Vector3d.Dot(p - a, ab) / ab.sqrMagnitude) : 0;
                best = Math.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }
    }
}
