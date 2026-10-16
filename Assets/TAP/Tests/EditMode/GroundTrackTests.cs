using System;
using NUnit.Framework;
using TAP.Core;

namespace TAP.Tests
{
    public class GroundTrackTests
    {
        private static CelestialBody Tellus => CelestialSystem.LoadFromResources().Get("tellus");

        [Test]
        public void EquatorialOrbit_StaysOnTheEquatorAndEndsEachLineAtTheDateLine()
        {
            var tellus = Tellus;
            double r = tellus.Radius + 83000;
            var o = new Orbit(new Vector3d(r, 0, 0), new Vector3d(0, 0, -Math.Sqrt(tellus.GM / r)), 0, tellus.GM); // prograde runs from +X towards -Z
            var pts = GroundTrack.Sample(o, tellus, 0, o.Period * 3, 540);
            Assert.IsTrue(pts[0].Start);
            int starts = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                Assert.AreEqual(0, pts[i].Lat, 1e-6, "an equatorial orbit has no latitude");
                if (i == 0) continue;
                if (pts[i].Start)
                {
                    starts++;
                    Assert.AreEqual(180, Math.Abs(pts[i].Lon), 1e-9);
                    Assert.AreEqual(-pts[i - 1].Lon, pts[i].Lon, 1e-9, "the new line starts on the other edge");
                }
                else Assert.Greater(pts[i].Lon - pts[i - 1].Lon, 0, "a prograde orbit moves east");
            }
            Assert.GreaterOrEqual(starts, 2, "three orbits cross the date line at least twice");
        }

        [Test]
        public void InclinedOrbit_ReachesItsInclinationInLatitude()
        {
            var tellus = Tellus;
            double inc = 51.6 * MathD.Deg2Rad;
            var o = Orbit.FromElements(tellus.Radius + 400000, 0, inc, 0.3, 0, 0, 0, tellus.GM);
            var pts = GroundTrack.Sample(o, tellus, 0, o.Period, 360);
            double max = 0;
            foreach (var p in pts) max = Math.Max(max, Math.Abs(p.Lat));
            Assert.AreEqual(51.6, max, 0.5);
        }

        [Test]
        public void TrackStartsOverThePointItWasBuiltAbove()
        {
            var tellus = Tellus;
            double ut = 1234;
            var up = tellus.BodyFixedToInertial(Geo.FromLatLon(20, 30), ut);
            var o = new Orbit(up * ((tellus.Radius + 100000) / up.magnitude), Vector3d.Cross(up, Vector3d.up).normalized * 2000, ut, tellus.GM);
            var first = GroundTrack.Sample(o, tellus, ut, ut + 600, 10)[0];
            Assert.AreEqual(20, first.Lat, 1e-6, "north is positive");
            Assert.AreEqual(30, first.Lon, 1e-6, "east is positive");
        }
    }
}
