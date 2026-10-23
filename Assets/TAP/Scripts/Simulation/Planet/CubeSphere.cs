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
        public static Vector3d ToSphere(int face, double u, double v) => FromTangents(face, Tangent(u), Tangent(v));

        /// <summary>The equi-angular mapping's tangent of a face coordinate in [-1, 1].</summary>
        public static double Tangent(double u) => Math.Tan(u * Math.PI * 0.25);

        /// <summary>Unit sphere direction for the tangents of u and v (<see cref="Tangent"/>): a grid needs one per row and column.</summary>
        public static Vector3d FromTangents(int face, double tu, double tv)
        {
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
        /// <summary>Visual chunks: every vertex packed for the GPU (<see cref="TerrainVertex"/>), what PlanetTerrain uploads.</summary>
        public TerrainVertex[] Vertices;
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
        [ThreadStatic] private static Vector3d[] _dirs, _points, _parentNormals;
        [ThreadStatic] private static double[] _heights, _fields, _tanU, _tanV;
        [ThreadStatic] private static Color32[] _parentColors, _colorOut, _parentOut;
        // Batches for the terrain (TerrainGenerator.SampleMany, ColorizeMany): which samples are full, and the colour requests.
        [ThreadStatic] private static bool[] _full;
        [ThreadStatic] private static int[] _colorSamples, _parentSamples;
        [ThreadStatic] private static double[] _cosSlopes, _parentCosSlopes;

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
                Vertices = colors ? new TerrainVertex[vcount] : null,
            };
        }

        private static T[] Scratch<T>(ref T[] a, int length) => a != null && a.Length >= length ? a : (a = new T[length]);

        /// <summary>A unit vector in two numbers (octahedral mapping); TerrainMorph.hlsl decodes it.</summary>
        public static Vector2 Octahedral(Vector3d n) => ChunkGeometry.Octahedral(n);

        /// <summary>A float as an IEEE half, the same bits with Burst and without (the packed vertices' morph data).</summary>
        public static ushort Half(float v) => ChunkGeometry.Half(v);

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

        private static unsafe ChunkData Build(TerrainGenerator gen, double radius, int face, double u0, double v0, double size, int n, bool ocean, bool skirts, bool colors)
        {
            // A visual chunk below the root also carries its parent's surface for LOD morphing. The parent's grid is every
            // second vertex of ours (n is even), and its normals need samples two steps out: a 2-sample border.
            bool morph = colors && size < 2 - 1e-9;
            int border = morph ? 2 : 1;
            int g = n + 1 + 2 * border; // grid with a border for normals
            int nf = gen.FieldCount;
            bool burst = LayeredTerrain.UseBurst;
            var dirs = Scratch(ref _dirs, g * g);
            var points = Scratch(ref _points, g * g);
            var heights = Scratch(ref _heights, g * g);
            var fields = Scratch(ref _fields, Math.Max(1, g * g * nf));
            var full = Scratch(ref _full, g * g);
            var tanU = Scratch(ref _tanU, g);
            var tanV = Scratch(ref _tanV, g);
            for (int i = 0; i < g; i++)
            {
                tanU[i] = CubeSphere.Tangent(u0 + size * (i - border) / n);
                tanV[i] = CubeSphere.Tangent(v0 + size * (i - border) / n);
            }
            int row = n + 1, parents = (n / 2 + 1) * (n / 2 + 1);
            var perim = skirts ? Perimeter(n) : Array.Empty<int>();
            var data = Rent(n, skirts, colors, row * row + perim.Length);
            var colorSamples = Scratch(ref _colorSamples, row * row);
            var cosSlopes = Scratch(ref _cosSlopes, row * row);
            var colorOut = Scratch(ref _colorOut, row * row);
            var parentSamples = Scratch(ref _parentSamples, parents);
            var parentCosSlopes = Scratch(ref _parentCosSlopes, parents);
            var parentOut = Scratch(ref _parentOut, parents);
            var parentNormals = Scratch(ref _parentNormals, row * row);
            var parentColors = Scratch(ref _parentColors, row * row);
            Vector3d cDir = CubeSphere.ToSphere(face, u0 + size * 0.5, v0 + size * 0.5);
            fixed (double* tu = tanU) fixed (double* tv = tanV) fixed (Vector3d* d = dirs) fixed (Vector3d* pts = points) fixed (bool* fl = full)
            fixed (double* h = heights) fixed (Vector3* pos = data.Positions) fixed (Vector3* nrm = data.Normals) fixed (Vector2* uv0 = data.UV0)
            fixed (Vector2* uv1 = data.UV1) fixed (Vector4* uv2 = data.UV2) fixed (Vector4* uv3 = data.UV3) fixed (Color32* col = data.Colors)
            fixed (int* cs = colorSamples) fixed (double* cc = cosSlopes) fixed (Color32* co = colorOut) fixed (int* ps = parentSamples)
            fixed (double* pc = parentCosSlopes) fixed (Color32* po = parentOut) fixed (Vector3d* pn = parentNormals) fixed (Color32* pcol = parentColors)
            fixed (int* per = perim) fixed (TerrainVertex* packed = data.Vertices)
            {
                var c = new ChunkGrid
                {
                    N = n, Border = border, G = g, Row = row, Radius = radius, Ocean = ocean, Colors = colors, Morph = morph,
                    FaceNormal = CubeSphere.Normals[face], AxisA = CubeSphere.AxisA[face], AxisB = CubeSphere.AxisB[face], TanU = tu, TanV = tv,
                    Dirs = d, Points = pts, Full = fl, Heights = h, Positions = pos, Normals = nrm, UV0 = uv0, UV1 = uv1, UV2 = uv2, UV3 = uv3,
                    Colors32 = col, ColorSamples = cs, CosSlopes = cc, ColorOut = co, ParentSamples = ps, ParentCosSlopes = pc, ParentOut = po,
                    ParentNormals = pn, ParentColors = pcol, ParentArc = (float)(2 * size * Math.PI * 0.25 * radius), Perimeter = per,
                    PerimeterCount = perim.Length, SkirtDepth = Math.Max(2.0, size * Math.PI * 0.25 * radius / n * 0.8), Vertices = packed,
                };
                if (burst) ChunkGeometry.DirectionsBurst(&c); else ChunkGeometry.Directions(&c);
                gen.SampleMany(dirs, full, heights, fields, g * g);
                double ch = gen.Height(cDir);
                if (ocean && ch < 0) ch = 0;
                c.Center = cDir * (radius + ch);
                c.Origin = new Vector3d(Math.Floor(c.Center.x / DetailPeriod) * DetailPeriod, Math.Floor(c.Center.y / DetailPeriod) * DetailPeriod, Math.Floor(c.Center.z / DetailPeriod) * DetailPeriod);
                if (burst) ChunkGeometry.VerticesBurst(&c); else ChunkGeometry.Vertices(&c);
                if (colors) gen.ColorizeMany(dirs, heights, fields, colorSamples, cosSlopes, colorOut, row * row);
                if (morph) gen.ColorizeMany(dirs, heights, fields, parentSamples, parentCosSlopes, parentOut, c.ParentRequests);
                if (burst) ChunkGeometry.FinishBurst(&c); else ChunkGeometry.Finish(&c);
                data.CenterBF = c.Center;
                data.MinHeight = c.MinHeight;
                data.MaxHeight = c.MaxHeight;
                data.BoundsMin = c.BoundsMin;
                data.BoundsMax = c.BoundsMax;
            }
            data.Triangles = Triangles(n, CubeSphere.FaceIsRightHanded(face), skirts);
            return data;
        }
    }
}
