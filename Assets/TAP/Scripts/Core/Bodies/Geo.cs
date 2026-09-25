using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// Local surface directions (north, east) for a body-relative "up" direction. All bodies spin about the Y axis
    /// with the surface at +X moving towards -Z; the orbital maths is right-handed, but Unity renders the world
    /// left-handed. Seen on screen that spin is counter-clockwise when looking from -Y, so -Y is the geographic
    /// north pole: facing north, east is on the right, as on Earth, and headings grow clockwise on the navball.
    /// </summary>
    public static class Geo
    {
        public static readonly Vector3d NorthPole = new Vector3d(0, -1, 0);

        /// <summary>Horizontal unit vector towards the north pole (falls back to an arbitrary direction at a pole).</summary>
        public static Vector3d North(Vector3d up)
        {
            Vector3d n = Vector3d.ProjectOnPlane(NorthPole, up);
            if (n.sqrMagnitude < 1e-12) n = Vector3d.ProjectOnPlane(Vector3d.right, up);
            return n.normalized;
        }

        /// <summary>Horizontal unit vector towards the east: the direction the surface moves as the body spins.</summary>
        public static Vector3d East(Vector3d up) => Vector3d.Cross(up, North(up)).normalized;

        public static Vector3 North(Vector3 up)
        {
            Vector3 n = Vector3.ProjectOnPlane((Vector3)NorthPole, up);
            if (n.sqrMagnitude < 1e-8f) n = Vector3.ProjectOnPlane(Vector3.right, up);
            return n.normalized;
        }

        public static Vector3 East(Vector3 up) => Vector3.Cross(up, North(up)).normalized;

        /// <summary>
        /// Body-fixed direction for a geographic latitude (north positive, towards <see cref="NorthPole"/>) and longitude
        /// (east positive). The terrain generator's own latitude counts towards +Y, which is geographic south.
        /// </summary>
        public static Vector3d FromLatLon(double latDeg, double lonDeg) => TerrainGenerator.DirectionFromLatLon(-latDeg, lonDeg);

        /// <summary>Geographic latitude (north positive) and longitude of a body-fixed direction.</summary>
        public static void ToLatLon(Vector3d dir, out double latDeg, out double lonDeg)
        {
            TerrainGenerator.LatLonFromDirection(dir, out latDeg, out lonDeg);
            latDeg = -latDeg;
        }
    }
}
