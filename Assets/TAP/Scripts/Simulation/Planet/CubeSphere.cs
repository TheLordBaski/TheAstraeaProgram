using System;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>Equi-angular cube-sphere mapping used by the terrain quadtree.</summary>
    public static class CubeSphere
    {
        public static readonly Vector3d[] Normals =
        {
            new Vector3d(1, 0, 0), new Vector3d(-1, 0, 0), new Vector3d(0, 1, 0),
            new Vector3d(0, -1, 0), new Vector3d(0, 0, 1), new Vector3d(0, 0, -1),
        };
        public static readonly Vector3d[] AxisA =
        {
            new Vector3d(0, 0, 1), new Vector3d(0, 0, -1), new Vector3d(1, 0, 0),
            new Vector3d(1, 0, 0), new Vector3d(-1, 0, 0), new Vector3d(1, 0, 0),
        };
        public static readonly Vector3d[] AxisB =
        {
            new Vector3d(0, 1, 0), new Vector3d(0, 1, 0), new Vector3d(0, 0, 1),
            new Vector3d(0, 0, -1), new Vector3d(0, 1, 0), new Vector3d(0, 1, 0),
        };

        /// <summary>Unit sphere direction for face coordinates u, v in [-1, 1].</summary>
        public static Vector3d ToSphere(int face, double u, double v)
        {
            double tu = Math.Tan(u * Math.PI * 0.25);
            double tv = Math.Tan(v * Math.PI * 0.25);
            Vector3d p = Normals[face] + AxisA[face] * tu + AxisB[face] * tv;
            return p.normalized;
        }

        public static void FromSphere(Vector3d dir, out int face, out double u, out double v)
        {
            double ax = Math.Abs(dir.x), ay = Math.Abs(dir.y), az = Math.Abs(dir.z);
            if (ax >= ay && ax >= az) face = dir.x > 0 ? 0 : 1;
            else if (ay >= az) face = dir.y > 0 ? 2 : 3;
            else face = dir.z > 0 ? 4 : 5;
            double dn = Vector3d.Dot(dir, Normals[face]);
            u = Math.Atan(Vector3d.Dot(dir, AxisA[face]) / dn) * 4.0 / Math.PI;
            v = Math.Atan(Vector3d.Dot(dir, AxisB[face]) / dn) * 4.0 / Math.PI;
        }

        /// <summary>True when (a x b) points along the face normal (determines triangle winding).</summary>
        public static bool FaceIsRightHanded(int face) => Vector3d.Dot(Vector3d.Cross(AxisA[face], AxisB[face]), Normals[face]) > 0;
    }

    /// <summary>CPU-side chunk geometry produced on worker threads.</summary>
    public sealed class ChunkData
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Color32[] Colors;
        public Vector2[] UV0;
        public Vector2[] UV1;
        public int[] Triangles;
        public Vector3d CenterBF;
        public Vector3 BoundsMin, BoundsMax;
        public double MinHeight, MaxHeight;
    }

    public static class TerrainChunkGenerator
    {
        public const double DetailPeriod = 4096.0;
        private static readonly System.Collections.Generic.Dictionary<(int, bool, bool), int[]> TriCache = new System.Collections.Generic.Dictionary<(int, bool, bool), int[]>();

        public static int[] Triangles(int n, bool rightHanded, bool skirts)
        {
            lock (TriCache)
            {
                if (TriCache.TryGetValue((n, rightHanded, skirts), out var t)) return t;
                var list = new System.Collections.Generic.List<int>();
                int row = n + 1;
                for (int j = 0; j < n; j++)
                    for (int i = 0; i < n; i++)
                    {
                        int a = j * row + i, b = a + 1, c = a + row + 1, d = a + row;
                        // Right-handed faces (A x B = outward normal) appear mirrored from outside, so
                        // (a,b,c) is clockwise = front-facing in Unity; left-handed faces need the reverse.
                        if (rightHanded) { list.Add(a); list.Add(b); list.Add(c); list.Add(a); list.Add(c); list.Add(d); }
                        else { list.Add(a); list.Add(c); list.Add(b); list.Add(a); list.Add(d); list.Add(c); }
                    }
                if (skirts)
                {
                    int baseSkirt = row * row;
                    // skirt vertex k corresponds to perimeter vertex k in order: bottom row, right col, top row (reversed), left col (reversed)
                    var perim = Perimeter(n);
                    int m = perim.Length;
                    for (int k = 0; k < m; k++)
                    {
                        int k2 = (k + 1) % m;
                        int p0 = perim[k], p1 = perim[k2];
                        int s0 = baseSkirt + k, s1 = baseSkirt + k2;
                        // perimeter runs counter-clockwise in (i,j); outward side depends on handedness
                        if (rightHanded) { list.Add(p0); list.Add(s0); list.Add(p1); list.Add(p1); list.Add(s0); list.Add(s1); }
                        else { list.Add(p0); list.Add(p1); list.Add(s0); list.Add(p1); list.Add(s1); list.Add(s0); }
                    }
                }
                t = list.ToArray();
                TriCache[(n, rightHanded, skirts)] = t;
                return t;
            }
        }

        public static int[] Perimeter(int n)
        {
            int row = n + 1;
            var list = new System.Collections.Generic.List<int>();
            for (int i = 0; i < n; i++) list.Add(0 * row + i);          // bottom (j=0) left->right
            for (int j = 0; j < n; j++) list.Add(j * row + n);          // right (i=n) bottom->top
            for (int i = n; i > 0; i--) list.Add(n * row + i);          // top (j=n) right->left
            for (int j = n; j > 0; j--) list.Add(j * row + 0);          // left (i=0) top->bottom
            return list.ToArray();
        }

        /// <summary>Generates chunk geometry. Thread-safe (pure math on the terrain generator).</summary>
        public static ChunkData Generate(TerrainGenerator gen, double radius, int face, double u0, double v0, double size, int n, bool ocean, bool skirts)
        {
            int g = n + 3; // grid with 1-sample border for normals
            var dirs = new Vector3d[g * g];
            var heights = new double[g * g];
            var shades = new float[g * g];
            for (int j = 0; j < g; j++)
                for (int i = 0; i < g; i++)
                {
                    double u = u0 + size * (i - 1) / n;
                    double v = v0 + size * (j - 1) / n;
                    var d = CubeSphere.ToSphere(face, u, v);
                    gen.Sample(d, out double h, out float s);
                    int k = j * g + i;
                    dirs[k] = d;
                    heights[k] = h;
                    shades[k] = s;
                }
            Vector3d cDir = CubeSphere.ToSphere(face, u0 + size * 0.5, v0 + size * 0.5);
            double ch = gen.Height(cDir);
            if (ocean && ch < 0) ch = 0;
            var center = cDir * (radius + ch);
            var origin = new Vector3d(Math.Floor(center.x / DetailPeriod) * DetailPeriod, Math.Floor(center.y / DetailPeriod) * DetailPeriod, Math.Floor(center.z / DetailPeriod) * DetailPeriod);

            int row = n + 1;
            var perim = skirts ? Perimeter(n) : new int[0];
            int vcount = row * row + perim.Length;
            var data = new ChunkData
            {
                Positions = new Vector3[vcount],
                Normals = new Vector3[vcount],
                Colors = new Color32[vcount],
                UV0 = new Vector2[vcount],
                UV1 = new Vector2[vcount],
                CenterBF = center,
                MinHeight = double.MaxValue,
                MaxHeight = double.MinValue,
            };
            Vector3d P(int i, int j)
            {
                int k = (j + 1) * g + (i + 1);
                double h = heights[k];
                if (ocean && h < 0) h = 0;
                return dirs[k] * (radius + h);
            }
            Vector3 bmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), bmax = -bmin;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    int k = (j + 1) * g + (i + 1);
                    double hRaw = heights[k];
                    bool water = ocean && hRaw < 0;
                    Vector3d p = P(i, j);
                    Vector3d dir = dirs[k];
                    Vector3d du = P(i + 1, j) - P(i - 1, j);
                    Vector3d dv = P(i, j + 1) - P(i, j - 1);
                    Vector3d nrm = Vector3d.Cross(du, dv).normalized;
                    if (Vector3d.Dot(nrm, dir) < 0) nrm = -nrm;
                    if (water) nrm = dir;
                    float slope = (float)MathD.Clamp01(1 - Vector3d.Dot(nrm, dir));
                    var col = gen.Colorize(dir, hRaw, shades[k], slope * 3f);
                    col.a = water ? (byte)255 : (byte)0;
                    int vi = j * row + i;
                    Vector3 local = (Vector3)(p - center);
                    data.Positions[vi] = local;
                    data.Normals[vi] = (Vector3)nrm;
                    data.Colors[vi] = col;
                    Vector3d dc = p - origin;
                    data.UV0[vi] = new Vector2((float)dc.x, (float)dc.y);
                    data.UV1[vi] = new Vector2((float)dc.z, (float)(water ? 0 : hRaw));
                    bmin = Vector3.Min(bmin, local);
                    bmax = Vector3.Max(bmax, local);
                    double hh = water ? 0 : hRaw;
                    if (hh < data.MinHeight) data.MinHeight = hh;
                    if (hh > data.MaxHeight) data.MaxHeight = hh;
                }
            if (skirts)
            {
                double depth = Math.Max(2.0, size * Math.PI * 0.25 * radius / n * 0.8);
                for (int k = 0; k < perim.Length; k++)
                {
                    int src = perim[k];
                    int i = src % row, j = src / row;
                    int gk = (j + 1) * g + (i + 1);
                    Vector3d p = P(i, j) - dirs[gk] * depth;
                    int vi = row * row + k;
                    Vector3 local = (Vector3)(p - center);
                    data.Positions[vi] = local;
                    data.Normals[vi] = data.Normals[src];
                    data.Colors[vi] = data.Colors[src];
                    data.UV0[vi] = data.UV0[src];
                    data.UV1[vi] = data.UV1[src];
                    bmin = Vector3.Min(bmin, local);
                    bmax = Vector3.Max(bmax, local);
                }
            }
            data.BoundsMin = bmin;
            data.BoundsMax = bmax;
            data.Triangles = Triangles(n, CubeSphere.FaceIsRightHanded(face), skirts);
            return data;
        }
    }
}
