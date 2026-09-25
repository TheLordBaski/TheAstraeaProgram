using System;
using UnityEngine;

namespace TAP.Core
{
    /// <summary>Symmetric 3x3 matrix helpers (inertia tensors).</summary>
    public struct Matrix3d
    {
        public double m00, m01, m02, m10, m11, m12, m20, m21, m22;

        public static Matrix3d Zero => new Matrix3d();

        public double this[int r, int c]
        {
            get
            {
                switch (r * 3 + c)
                {
                    case 0: return m00; case 1: return m01; case 2: return m02;
                    case 3: return m10; case 4: return m11; case 5: return m12;
                    case 6: return m20; case 7: return m21; default: return m22;
                }
            }
            set
            {
                switch (r * 3 + c)
                {
                    case 0: m00 = value; break; case 1: m01 = value; break; case 2: m02 = value; break;
                    case 3: m10 = value; break; case 4: m11 = value; break; case 5: m12 = value; break;
                    case 6: m20 = value; break; case 7: m21 = value; break; default: m22 = value; break;
                }
            }
        }

        public static Matrix3d operator +(Matrix3d a, Matrix3d b)
        {
            var r = new Matrix3d();
            for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) r[i, j] = a[i, j] + b[i, j];
            return r;
        }

        /// <summary>Inertia contribution of a point mass at offset r: m(|r|^2 I - r r^T).</summary>
        public static Matrix3d PointMass(double m, Vector3d r)
        {
            double rr = r.sqrMagnitude;
            var M = new Matrix3d
            {
                m00 = m * (rr - r.x * r.x), m01 = -m * r.x * r.y, m02 = -m * r.x * r.z,
                m10 = -m * r.y * r.x, m11 = m * (rr - r.y * r.y), m12 = -m * r.y * r.z,
                m20 = -m * r.z * r.x, m21 = -m * r.z * r.y, m22 = m * (rr - r.z * r.z),
            };
            return M;
        }

        /// <summary>R * diag(d) * R^T for a rotation R.</summary>
        public static Matrix3d RotatedDiagonal(Quaternion q, Vector3d d)
        {
            Vector3 ax = q * Vector3.right, ay = q * Vector3.up, az = q * Vector3.forward;
            double[,] R = { { ax.x, ay.x, az.x }, { ax.y, ay.y, az.y }, { ax.z, ay.z, az.z } };
            var M = new Matrix3d();
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    M[i, j] = R[i, 0] * d.x * R[j, 0] + R[i, 1] * d.y * R[j, 1] + R[i, 2] * d.z * R[j, 2];
            return M;
        }

        /// <summary>Jacobi eigen-decomposition of a symmetric matrix. Columns of V are eigenvectors.</summary>
        public void Eigen(out Vector3d values, out double[,] V)
        {
            double[,] a = { { m00, m01, m02 }, { m10, m11, m12 }, { m20, m21, m22 } };
            V = new double[,] { { 1, 0, 0 }, { 0, 1, 0 }, { 0, 0, 1 } };
            for (int sweep = 0; sweep < 50; sweep++)
            {
                double off = Math.Abs(a[0, 1]) + Math.Abs(a[0, 2]) + Math.Abs(a[1, 2]);
                double diag = Math.Abs(a[0, 0]) + Math.Abs(a[1, 1]) + Math.Abs(a[2, 2]);
                if (off <= 1e-14 * Math.Max(diag, 1e-30)) break;
                for (int p = 0; p < 2; p++)
                    for (int q = p + 1; q < 3; q++)
                    {
                        if (Math.Abs(a[p, q]) < 1e-300) continue;
                        double theta = (a[q, q] - a[p, p]) / (2 * a[p, q]);
                        double t = Math.Sign(theta) / (Math.Abs(theta) + Math.Sqrt(theta * theta + 1));
                        if (theta == 0) t = 1;
                        double c = 1 / Math.Sqrt(t * t + 1), s = t * c;
                        for (int k = 0; k < 3; k++)
                        {
                            double akp = a[k, p], akq = a[k, q];
                            a[k, p] = c * akp - s * akq;
                            a[k, q] = s * akp + c * akq;
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            double apk = a[p, k], aqk = a[q, k];
                            a[p, k] = c * apk - s * aqk;
                            a[q, k] = s * apk + c * aqk;
                        }
                        for (int k = 0; k < 3; k++)
                        {
                            double vkp = V[k, p], vkq = V[k, q];
                            V[k, p] = c * vkp - s * vkq;
                            V[k, q] = s * vkp + c * vkq;
                        }
                    }
            }
            values = new Vector3d(a[0, 0], a[1, 1], a[2, 2]);
        }
    }
}
