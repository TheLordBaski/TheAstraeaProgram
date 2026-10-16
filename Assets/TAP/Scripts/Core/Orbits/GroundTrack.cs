using System;
using System.Collections.Generic;

namespace TAP.Core
{
    /// <summary>
    /// A point of a ground track: geographic latitude (north positive) and longitude (east positive) in degrees, at a time.
    /// <see cref="Start"/> opens a new line: the first point, or the far side of the date line after the track wrapped round it.
    /// </summary>
    public struct GroundPoint
    {
        public double Lat, Lon, UT;
        public bool Start;
    }

    /// <summary>The path an orbit traces over the rotating surface of its body, for the map's flat ground-track view.</summary>
    public static class GroundTrack
    {
        /// <summary>The geographic latitude and longitude (degrees) under a body-relative inertial position at UT.</summary>
        public static void LatLon(CelestialBody body, Vector3d relPos, double ut, out double lat, out double lon) =>
            Geo.ToLatLon(body.InertialToBodyFixed(relPos, ut), out lat, out lon);

        /// <summary>
        /// The ground track of an orbit from <paramref name="fromUT"/> to <paramref name="toUT"/>, in equal time steps. Where the
        /// track crosses the date line it ends on one edge and starts again on the other at the crossing latitude, so a
        /// flat map draws it as separate lines. Uniform steps are coarse near the periapsis of a very eccentric orbit.
        /// </summary>
        public static List<GroundPoint> Sample(Orbit orbit, CelestialBody body, double fromUT, double toUT, int steps)
        {
            var list = new List<GroundPoint>(steps + 8);
            double prevLat = 0, prevLon = 0, prevUT = 0;
            for (int i = 0; i <= steps; i++)
            {
                double ut = fromUT + (toUT - fromUT) * i / steps;
                LatLon(body, orbit.GetPositionAtUT(ut), ut, out double lat, out double lon);
                if (i > 0 && Math.Abs(lon - prevLon) > 180)
                {
                    bool east = prevLon > 0;
                    double edge = east ? 180 : -180;
                    double t = (edge - prevLon) / (lon + (east ? 360 : -360) - prevLon);
                    double crossLat = prevLat + (lat - prevLat) * t, crossUT = prevUT + (ut - prevUT) * t;
                    list.Add(new GroundPoint { Lat = crossLat, Lon = edge, UT = crossUT });
                    list.Add(new GroundPoint { Lat = crossLat, Lon = -edge, UT = crossUT, Start = true });
                }
                list.Add(new GroundPoint { Lat = lat, Lon = lon, UT = ut, Start = i == 0 });
                prevLat = lat; prevLon = lon; prevUT = ut;
            }
            return list;
        }
    }
}
