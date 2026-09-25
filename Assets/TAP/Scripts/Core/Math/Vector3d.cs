using System;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>
    /// Double-precision 3D vector used for all orbital / universe-scale state.
    /// Uses the same component conventions as UnityEngine.Vector3 (left-handed, Y up)
    /// so conversions are a straight component copy.
    /// </summary>
    [Serializable]
    public struct Vector3d : IEquatable<Vector3d>
    {
        public double x, y, z;

        public Vector3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public Vector3d(Vector3 v) { x = v.x; y = v.y; z = v.z; }

        public static readonly Vector3d zero = new Vector3d(0, 0, 0);
        public static readonly Vector3d one = new Vector3d(1, 1, 1);
        public static readonly Vector3d up = new Vector3d(0, 1, 0);
        public static readonly Vector3d right = new Vector3d(1, 0, 0);
        public static readonly Vector3d forward = new Vector3d(0, 0, 1);

        public double sqrMagnitude => x * x + y * y + z * z;
        public double magnitude => Math.Sqrt(x * x + y * y + z * z);

        public Vector3d normalized
        {
            get
            {
                double m = magnitude;
                return m > 1e-300 ? new Vector3d(x / m, y / m, z / m) : zero;
            }
        }

        public Vector3 ToVector3() => new Vector3((float)x, (float)y, (float)z);
        public static implicit operator Vector3d(Vector3 v) => new Vector3d(v.x, v.y, v.z);
        public static explicit operator Vector3(Vector3d v) => new Vector3((float)v.x, (float)v.y, (float)v.z);

        public static Vector3d operator +(Vector3d a, Vector3d b) => new Vector3d(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
        public static Vector3d operator -(Vector3d a) => new Vector3d(-a.x, -a.y, -a.z);
        public static Vector3d operator *(Vector3d a, double d) => new Vector3d(a.x * d, a.y * d, a.z * d);
        public static Vector3d operator *(double d, Vector3d a) => new Vector3d(a.x * d, a.y * d, a.z * d);
        public static Vector3d operator /(Vector3d a, double d) => new Vector3d(a.x / d, a.y / d, a.z / d);
        public static bool operator ==(Vector3d a, Vector3d b) => a.x == b.x && a.y == b.y && a.z == b.z;
        public static bool operator !=(Vector3d a, Vector3d b) => !(a == b);

        public static double Dot(Vector3d a, Vector3d b) => a.x * b.x + a.y * b.y + a.z * b.z;

        public static Vector3d Cross(Vector3d a, Vector3d b) =>
            new Vector3d(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);

        public static double Distance(Vector3d a, Vector3d b) => (a - b).magnitude;

        public static Vector3d Lerp(Vector3d a, Vector3d b, double t) => a + (b - a) * t;

        public static Vector3d Project(Vector3d v, Vector3d onNormal)
        {
            double sq = onNormal.sqrMagnitude;
            if (sq < 1e-300) return zero;
            return onNormal * (Dot(v, onNormal) / sq);
        }

        public static Vector3d ProjectOnPlane(Vector3d v, Vector3d planeNormal) => v - Project(v, planeNormal);

        /// <summary>Unsigned angle in radians.</summary>
        public static double AngleRad(Vector3d a, Vector3d b)
        {
            double d = Math.Sqrt(a.sqrMagnitude * b.sqrMagnitude);
            if (d < 1e-300) return 0;
            double c = Dot(a, b) / d;
            if (c > 1) c = 1; else if (c < -1) c = -1;
            return Math.Acos(c);
        }

        public bool IsFinite() => !(double.IsNaN(x) || double.IsNaN(y) || double.IsNaN(z) ||
                                    double.IsInfinity(x) || double.IsInfinity(y) || double.IsInfinity(z));

        public bool Equals(Vector3d other) => this == other;
        public override bool Equals(object obj) => obj is Vector3d o && this == o;
        public override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public override string ToString() => $"({x:F3}, {y:F3}, {z:F3})";
        public string ToString(string fmt) => $"({x.ToString(fmt)}, {y.ToString(fmt)}, {z.ToString(fmt)})";
    }

    /// <summary>
    /// Double-precision unit quaternion (same convention as UnityEngine.Quaternion).
    /// Used for planet rotation where float error at 600 km radius would be centimetres.
    /// </summary>
    [Serializable]
    public struct QuaternionD
    {
        public double x, y, z, w;

        public QuaternionD(double x, double y, double z, double w) { this.x = x; this.y = y; this.z = z; this.w = w; }

        public static readonly QuaternionD identity = new QuaternionD(0, 0, 0, 1);

        public static QuaternionD AngleAxisRad(double angleRad, Vector3d axis)
        {
            Vector3d n = axis.normalized;
            double h = angleRad * 0.5;
            double s = Math.Sin(h);
            return new QuaternionD(n.x * s, n.y * s, n.z * s, Math.Cos(h));
        }

        public QuaternionD Inverse() => new QuaternionD(-x, -y, -z, w);

        public static QuaternionD operator *(QuaternionD a, QuaternionD b) => new QuaternionD(
            a.w * b.x + a.x * b.w + a.y * b.z - a.z * b.y,
            a.w * b.y + a.y * b.w + a.z * b.x - a.x * b.z,
            a.w * b.z + a.z * b.w + a.x * b.y - a.y * b.x,
            a.w * b.w - a.x * b.x - a.y * b.y - a.z * b.z);

        public static Vector3d operator *(QuaternionD q, Vector3d v)
        {
            // v' = v + 2w(q x v) + 2 q x (q x v)
            double tx = 2 * (q.y * v.z - q.z * v.y);
            double ty = 2 * (q.z * v.x - q.x * v.z);
            double tz = 2 * (q.x * v.y - q.y * v.x);
            return new Vector3d(
                v.x + q.w * tx + (q.y * tz - q.z * ty),
                v.y + q.w * ty + (q.z * tx - q.x * tz),
                v.z + q.w * tz + (q.x * ty - q.y * tx));
        }

        public Quaternion ToQuaternion() => new Quaternion((float)x, (float)y, (float)z, (float)w);
        public static QuaternionD FromQuaternion(Quaternion q) => new QuaternionD(q.x, q.y, q.z, q.w);
    }
}
