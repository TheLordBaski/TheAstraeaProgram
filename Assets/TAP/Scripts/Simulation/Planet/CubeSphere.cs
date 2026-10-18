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

    /// <summary>
    /// CPU-side chunk geometry produced on worker threads. Its arrays come from a pool: whoever is done with a chunk may
    /// give it back with <see cref="TerrainChunkGenerator.Release"/>.
    /// </summary>
    public sealed class ChunkData
    {
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Color32[] Colors;
        public Vector2[] UV0;
        public Vector2[] UV1;
        /// <summary>
        /// LOD morph (visual chunks only): UV2 = the move from this vertex to its parent chunk's surface, and the parent's arc
        /// (m; 0 for a root chunk, which never morphs); UV3 = the parent's normal (octahedral), its colour (RGB packed into
        /// one float) and its water flag. See TerrainMorph.hlsl.
        /// </summary>
        public Vector4[] UV2, UV3;
        public int[] Triangles;
        public Vector3d CenterBF;
        public Vector3 BoundsMin, BoundsMax;
        public double MinHeight, MaxHeight;
        internal (int n, bool skirts, bool colors) Layout;
        internal bool Pooled;
    }

    public static class TerrainChunkGenerator
    {
        public const double DetailPeriod = 4096.0;
        private static readonly System.Collections.Generic.Dictionary<(int, bool, bool), int[]> TriCache = new System.Collections.Generic.Dictionary<(int, bool, bool), int[]>();
        private static readonly System.Collections.Generic.Dictionary<int, int[]> PerimCache = new System.Collections.Generic.Dictionary<int, int[]>();

        // Spare chunks by layout (resolution, skirts, colours), so streaming terrain makes no garbage. Bounded: a burst of
        // remakes can't pin memory, and a chunk never given back is simply collected.
        private const int PoolCap = 64;
        private static readonly System.Collections.Generic.Dictionary<(int, bool, bool), System.Collections.Generic.Stack<ChunkData>> Pool =
            new System.Collections.Generic.Dictionary<(int, bool, bool), System.Collections.Generic.Stack<ChunkData>>();

        // Sample scratch per thread (the workers, and the main thread when it builds a chunk right away).
        [ThreadStatic] private static Vector3d[] _dirs, _parentNormals;
        [ThreadStatic] private static double[] _heights, _fields;
        [ThreadStatic] private static Color32[] _parentColors;

        /// <summary>Gives a chunk's arrays back for reuse. Only once nothing reads the chunk any more; null is ignored.</summary>
        public static void Release(ChunkData data)
        {
            if (data == null) return;
            lock (Pool)
            {
                if (data.Pooled) return;
                if (!Pool.TryGetValue(data.Layout, out var spare)) Pool[data.Layout] = spare = new System.Collections.Generic.Stack<ChunkData>();
                if (spare.Count >= PoolCap) return;
                data.Pooled = true;
                spare.Push(data);
            }
        }

        private static ChunkData Rent(int n, bool skirts, bool colors, int vcount)
        {
            lock (Pool)
                if (Pool.TryGetValue((n, skirts, colors), out var spare) && spare.Count > 0)
                {
                    var d = spare.Pop();
                    d.Pooled = false;
                    return d;
                }
            return new ChunkData
            {
                Layout = (n, skirts, colors),
                Positions = new Vector3[vcount],
                Normals = new Vector3[vcount],
                Colors = colors ? new Color32[vcount] : null,
                UV0 = new Vector2[vcount],
                UV1 = new Vector2[vcount],
                UV2 = colors ? new Vector4[vcount] : null,
                UV3 = colors ? new Vector4[vcount] : null,
            };
        }

        private static T[] Scratch<T>(ref T[] a, int length) => a != null && a.Length >= length ? a : (a = new T[length]);

        /// <summary>A unit vector in two numbers (octahedral mapping); TerrainMorph.hlsl decodes it.</summary>
        public static Vector2 Octahedral(Vector3d n)
        {
            double s = Math.Abs(n.x) + Math.Abs(n.y) + Math.Abs(n.z);
            double x = n.x / s, y = n.y / s;
            if (n.z < 0)
            {
                double fx = (1 - Math.Abs(y)) * (x >= 0 ? 1 : -1);
                y = (1 - Math.Abs(x)) * (y >= 0 ? 1 : -1);
                x = fx;
            }
            return new Vector2((float)x, (float)y);
        }

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

        /// <summary>The perimeter vertices of an n-quad grid, counter-clockwise. Cached and shared: never modify it.</summary>
        public static int[] Perimeter(int n)
        {
            lock (PerimCache)
            {
                if (PerimCache.TryGetValue(n, out var p)) return p;
                int row = n + 1;
                var list = new System.Collections.Generic.List<int>();
                for (int i = 0; i < n; i++) list.Add(0 * row + i);          // bottom (j=0) left->right
                for (int j = 0; j < n; j++) list.Add(j * row + n);          // right (i=n) bottom->top
                for (int i = n; i > 0; i--) list.Add(n * row + i);          // top (j=n) right->left
                for (int j = n; j > 0; j--) list.Add(j * row + 0);          // left (i=0) top->bottom
                return PerimCache[n] = list.ToArray();
            }
        }

        /// <summary>
        /// Generates chunk geometry. Thread-safe (pure math on the terrain generator). colors: false for collision chunks,
        /// which need no colours (their Colors stay empty).
        /// </summary>
        public static ChunkData Generate(TerrainGenerator gen, double radius, int face, double u0, double v0, double size, int n, bool ocean, bool skirts, bool colors = true)
        {
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            var data = Build(gen, radius, face, u0, v0, size, n, ocean, skirts, colors);
            System.Threading.Interlocked.Add(ref GenerateTicks, System.Diagnostics.Stopwatch.GetTimestamp() - start);
            System.Threading.Interlocked.Increment(ref Generated);
            return data;
        }

        /// <summary>Chunks generated on any thread, and the Stopwatch ticks spent in <see cref="Generate"/> (the benchmark reads them).</summary>
        public static long Generated, GenerateTicks;

        private static ChunkData Build(TerrainGenerator gen, double radius, int face, double u0, double v0, double size, int n, bool ocean, bool skirts, bool colors)
        {
            // A visual chunk below the root also carries its parent's surface for LOD morphing. The parent's grid is every
            // second vertex of ours (n is even), and its normals need samples two steps out: a 2-sample border.
            bool morph = colors && size < 2 - 1e-9;
            int border = morph ? 2 : 1;
            int g = n + 1 + 2 * border; // grid with a border for normals
            int nf = gen.FieldCount;
            var dirs = Scratch(ref _dirs, g * g);
            var heights = Scratch(ref _heights, g * g);
            var fields = Scratch(ref _fields, Math.Max(1, g * g * nf));
            for (int j = 0; j < g; j++)
                for (int i = 0; i < g; i++)
                {
                    double u = u0 + size * (i - border) / n;
                    double v = v0 + size * (j - border) / n;
                    var d = CubeSphere.ToSphere(face, u, v);
                    int k = j * g + i;
                    // The border only shapes normals; colours need the full sample (every field) at the vertices.
                    bool vertex = colors && i >= border && j >= border && i <= n + border && j <= n + border;
                    dirs[k] = d;
                    heights[k] = gen.Sample(d, fields, k * nf, vertex);
                }
            Vector3d cDir = CubeSphere.ToSphere(face, u0 + size * 0.5, v0 + size * 0.5);
            double ch = gen.Height(cDir);
            if (ocean && ch < 0) ch = 0;
            var center = cDir * (radius + ch);
            var origin = new Vector3d(Math.Floor(center.x / DetailPeriod) * DetailPeriod, Math.Floor(center.y / DetailPeriod) * DetailPeriod, Math.Floor(center.z / DetailPeriod) * DetailPeriod);

            int row = n + 1;
            var perim = skirts ? Perimeter(n) : Array.Empty<int>();
            int vcount = row * row + perim.Length;
            var data = Rent(n, skirts, colors, vcount);
            data.CenterBF = center;
            data.MinHeight = double.MaxValue;
            data.MaxHeight = double.MinValue;
            int K(int i, int j) => (j + border) * g + (i + border);
            Vector3d P(int i, int j)
            {
                int k = K(i, j);
                double h = heights[k];
                if (ocean && h < 0) h = 0;
                return dirs[k] * (radius + h);
            }
            // The normal at (i, j) from neighbours s steps away: s = 1 for this chunk, 2 for its parent.
            Vector3d Normal(int i, int j, int s)
            {
                int k = K(i, j);
                if (ocean && heights[k] < 0) return dirs[k];
                Vector3d nrm = Vector3d.Cross(P(i + s, j) - P(i - s, j), P(i, j + s) - P(i, j - s)).normalized;
                return Vector3d.Dot(nrm, dirs[k]) < 0 ? -nrm : nrm;
            }
            Vector3 bmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), bmax = -bmin;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    int k = K(i, j);
                    double hRaw = heights[k];
                    bool water = ocean && hRaw < 0;
                    Vector3d p = P(i, j);
                    Vector3d dir = dirs[k];
                    Vector3d nrm = Normal(i, j, 1);
                    int vi = j * row + i;
                    if (colors)
                    {
                        var col = gen.Colorize(dir, hRaw, fields, k * nf, Vector3d.Dot(nrm, dir));
                        col.a = water ? (byte)255 : (byte)0; // the shader's water flag
                        data.Colors[vi] = col;
                    }
                    Vector3 local = (Vector3)(p - center);
                    data.Positions[vi] = local;
                    data.Normals[vi] = (Vector3)nrm;
                    Vector3d dc = p - origin;
                    data.UV0[vi] = new Vector2((float)dc.x, (float)dc.y);
                    data.UV1[vi] = new Vector2((float)dc.z, (float)(water ? 0 : hRaw));
                    bmin = Vector3.Min(bmin, local);
                    bmax = Vector3.Max(bmax, local);
                    double hh = water ? 0 : hRaw;
                    if (hh < data.MinHeight) data.MinHeight = hh;
                    if (hh > data.MaxHeight) data.MaxHeight = hh;
                }
            if (morph)
            {
                // The parent's own normal and colour at its vertices (every second one of ours), as the parent computes them.
                var parentNormals = Scratch(ref _parentNormals, row * row);
                var parentColors = Scratch(ref _parentColors, row * row);
                for (int j = 0; j <= n; j += 2)
                    for (int i = 0; i <= n; i += 2)
                    {
                        int k = K(i, j), vi = j * row + i;
                        Vector3d pn = Normal(i, j, 2);
                        var col = gen.Colorize(dirs[k], heights[k], fields, k * nf, Vector3d.Dot(pn, dirs[k]));
                        col.a = ocean && heights[k] < 0 ? (byte)255 : (byte)0;
                        parentNormals[vi] = pn;
                        parentColors[vi] = col;
                    }
                // Every vertex's point on the parent's surface: a parent vertex, the middle of a parent edge, or the middle of
                // the diagonal (a, c) that splits each parent quad into the triangles (a, b, c) and (a, c, d).
                float parentArc = (float)(2 * size * Math.PI * 0.25 * radius);
                for (int j = 0; j <= n; j++)
                    for (int i = 0; i <= n; i++)
                    {
                        int ia = i - (i & 1), ja = j - (j & 1), ib = i + (i & 1), jb = j + (j & 1);
                        int a = ja * row + ia, b = jb * row + ib, vi = j * row + i;
                        Vector3 delta = (Vector3)((P(ia, ja) + P(ib, jb)) * 0.5 - P(i, j));
                        Vector3d pn = (parentNormals[a] + parentNormals[b]).normalized;
                        Color32 ca = parentColors[a], cb = parentColors[b];
                        double r = (ca.r + cb.r) >> 1, gr = (ca.g + cb.g) >> 1, bl = (ca.b + cb.b) >> 1;
                        data.UV2[vi] = new Vector4(delta.x, delta.y, delta.z, parentArc);
                        Vector2 oct = Octahedral(pn);
                        data.UV3[vi] = new Vector4(oct.x, oct.y, (float)(r + gr * 256 + bl * 65536), (ca.a + cb.a) / 510f);
                    }
            }
            else if (colors)
            {
                Array.Clear(data.UV2, 0, row * row);
                Array.Clear(data.UV3, 0, row * row);
            }
            if (skirts)
            {
                double depth = Math.Max(2.0, size * Math.PI * 0.25 * radius / n * 0.8);
                for (int k = 0; k < perim.Length; k++)
                {
                    int src = perim[k];
                    int i = src % row, j = src / row;
                    Vector3d p = P(i, j) - dirs[K(i, j)] * depth;
                    int vi = row * row + k;
                    Vector3 local = (Vector3)(p - center);
                    data.Positions[vi] = local;
                    data.Normals[vi] = data.Normals[src];
                    if (colors)
                    {
                        data.Colors[vi] = data.Colors[src];
                        data.UV2[vi] = data.UV2[src];
                        data.UV3[vi] = data.UV3[src];
                    }
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
