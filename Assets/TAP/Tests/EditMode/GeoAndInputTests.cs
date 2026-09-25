using NUnit.Framework;
using TAP.Core;
using TAP.Game;

namespace TAP.Tests
{
    public class GeoAndInputTests
    {
        [Test]
        public void ScrollZoom_OneNotchIsTheSameInTheEditorAndInABuild()
        {
            // The Input System reports 120 per wheel notch in the Windows editor and 1 in a player build.
            Assert.AreEqual(1f, ScrollZoom.Notches(120f), 1e-6f);
            Assert.AreEqual(-2f, ScrollZoom.Notches(-240f), 1e-6f);
            Assert.AreEqual(1f, ScrollZoom.Notches(1f), 1e-6f);
            Assert.AreEqual(-1f, ScrollZoom.Notches(-1f), 1e-6f);
            Assert.AreEqual(0.25f, ScrollZoom.Notches(0.25f), 1e-6f, "touchpads report fractions of a notch");
            Assert.AreEqual(ScrollZoom.Factor(120f, 0.75f), ScrollZoom.Factor(1f, 0.75f), 1e-6f);
            Assert.Less(ScrollZoom.Factor(1f, 0.75f), 1f, "scrolling up zooms in");
        }

        [Test]
        public void GeographicLatitude_IsNorthPositive_AndLongitudeGrowsEast()
        {
            Vector3d north = Geo.FromLatLon(10, 0);
            Assert.Less(north.y, 0, "north is the -Y pole");
            Vector3d origin = Geo.FromLatLon(0, 0);
            Vector3d east = (Geo.FromLatLon(0, 0.1) - origin).normalized;
            Assert.Greater(Vector3d.Dot(east, Geo.East(origin)), 0.999, "longitude grows towards Geo.East");
            Vector3d northStep = (Geo.FromLatLon(0.1, 0) - origin).normalized;
            Assert.Greater(Vector3d.Dot(northStep, Geo.North(origin)), 0.999, "latitude grows towards Geo.North");
            foreach (var (lat, lon) in new[] { (0.0, 0.0), (12.5, -40.0), (-63.0, 171.0), (45.0, 90.0) })
            {
                Geo.ToLatLon(Geo.FromLatLon(lat, lon), out double lat2, out double lon2);
                Assert.AreEqual(lat, lat2, 1e-9);
                Assert.AreEqual(lon, lon2, 1e-9);
            }
        }
    }
}
