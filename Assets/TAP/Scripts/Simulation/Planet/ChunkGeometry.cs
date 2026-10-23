using System;
using System.Runtime.InteropServices;
using TAP.Core;
using Unity.Burst;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// A terrain vertex as the GPU gets it (FND-21): 60 bytes instead of the 76 of separate channels. The LOD morph data is in half
    /// floats: the move to the parent's surface (m), the parent's arc (km, so it fits a half), the parent's normal (octahedral), and the
    /// parent's colour with the water flag in bytes. Terrain.shader and TerrainMorph.hlsl read this layout.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct TerrainVertex
    {
        public Vector3 Position, Normal;
        public Color32 Color;
        public Vector2 UV0, UV1;
        public ushort MoveX, MoveY, MoveZ, ParentArcKm, ParentNormalX, ParentNormalY;
        public Color32 Parent;
    }

    /// <summary>One chunk being built: its grid (with a border of samples for normals), the scratch and the arrays it fills.</summary>
    internal unsafe struct ChunkGrid
    {
        public int N, Border, G, Row;
        public double Radius;
        public bool Ocean, Colors, Morph;
        public Vector3d FaceNormal, AxisA, AxisB;
        public double* TanU, TanV;
        public Vector3d* Dirs, Points;
        public bool* Full;
        public double* Heights;
        public Vector3d Center, Origin;
        public Vector3* Positions, Normals;
        public Vector2* UV0, UV1;
        public Vector4* UV2, UV3;
        public Color32* Colors32;
        // colour requests: one per vertex, and one per parent vertex (every second one)
        public int* ColorSamples, ParentSamples;
        public double* CosSlopes, ParentCosSlopes;
        public Color32* ColorOut, ParentOut;
        public Vector3d* ParentNormals;
        public Color32* ParentColors;
        public int ParentRequests;
        public float ParentArc;
        public int* Perimeter;
        public int PerimeterCount;
        public double SkirtDepth;
        public Vector3 BoundsMin, BoundsMax;
        public double MinHeight, MaxHeight;
        public TerrainVertex* Vertices;
    }

    /// <summary>
    /// The geometry of a terrain chunk (FND-19): directions, positions, normals, UVs, LOD morph data and skirts. One implementation:
    /// the *Burst entries compile it with Burst in strict float mode, the others run it as plain C#, so the two give the same bits
    /// (LayeredTerrain.UseBurst picks). The terrain's own values come from TerrainGenerator.SampleMany and ColorizeMany in between.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
    internal static unsafe class ChunkGeometry
    {
        [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public static void DirectionsBurst(ChunkGrid* c) => Directions(c);

        [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public static void VerticesBurst(ChunkGrid* c) => Vertices(c);

        [BurstCompile(FloatMode = FloatMode.Strict, FloatPrecision = FloatPrecision.Standard, CompileSynchronously = true)]
        public static void FinishBurst(ChunkGrid* c) => Finish(c);

        /// <summary>The grid's directions (CubeSphere.FromTangents) and which samples need every field (the vertices, for colours).</summary>
        public static void Directions(ChunkGrid* c)
        {
            int g = c->G, b = c->Border, n = c->N;
            for (int j = 0; j < g; j++)
                for (int i = 0; i < g; i++)
                {
                    int k = j * g + i;
                    c->Dirs[k] = Normalize(c->FaceNormal + c->AxisA * c->TanU[i] + c->AxisB * c->TanV[j]);
                    // The border only shapes normals; colours need the full sample (every field) at the vertices.
                    c->Full[k] = c->Colors && i >= b && j >= b && i <= n + b && j <= n + b;
                }
        }

        private static int K(ChunkGrid* c, int i, int j) => (j + c->Border) * c->G + (i + c->Border);

        private static Vector3d P(ChunkGrid* c, int i, int j) => c->Points[K(c, i, j)];

        /// <summary>The normal at (i, j) from neighbours s steps away: s = 1 for this chunk, 2 for its parent.</summary>
        private static Vector3d Normal(ChunkGrid* c, int i, int j, int s)
        {
            int k = K(c, i, j);
            if (c->Ocean && c->Heights[k] < 0) return c->Dirs[k];
            Vector3d nrm = Normalize(Vector3d.Cross(P(c, i + s, j) - P(c, i - s, j), P(c, i, j + s) - P(c, i, j - s)));
            return Vector3d.Dot(nrm, c->Dirs[k]) < 0 ? -nrm : nrm;
        }

        /// <summary>Points, the vertices (position, normal, UVs, bounds, heights) and the colour requests.</summary>
        public static void Vertices(ChunkGrid* c)
        {
            int g = c->G, n = c->N, row = c->Row;
            // Every grid point's position, worked out once (the normals and the LOD morph read each several times).
            for (int k = 0; k < g * g; k++)
            {
                double h = c->Heights[k];
                if (c->Ocean && h < 0) h = 0;
                c->Points[k] = c->Dirs[k] * (c->Radius + h);
            }
            Vector3 bmin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue), bmax = -bmin;
            double minH = double.MaxValue, maxH = double.MinValue;
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    int k = K(c, i, j);
                    double hRaw = c->Heights[k];
                    bool water = c->Ocean && hRaw < 0;
                    Vector3d p = P(c, i, j);
                    Vector3d dir = c->Dirs[k];
                    Vector3d nrm = Normal(c, i, j, 1);
                    int vi = j * row + i;
                    if (c->Colors)
                    {
                        c->ColorSamples[vi] = k;
                        c->CosSlopes[vi] = Vector3d.Dot(nrm, dir);
                    }
                    Vector3 local = (Vector3)(p - c->Center);
                    c->Positions[vi] = local;
                    c->Normals[vi] = (Vector3)nrm;
                    Vector3d dc = p - c->Origin;
                    c->UV0[vi] = new Vector2((float)dc.x, (float)dc.y);
                    c->UV1[vi] = new Vector2((float)dc.z, (float)(water ? 0 : hRaw));
                    bmin = Min(bmin, local);
                    bmax = Max(bmax, local);
                    double hh = water ? 0 : hRaw;
                    if (hh < minH) minH = hh;
                    if (hh > maxH) maxH = hh;
                }
            c->BoundsMin = bmin;
            c->BoundsMax = bmax;
            c->MinHeight = minH;
            c->MaxHeight = maxH;
            c->ParentRequests = 0;
            if (!c->Morph) return;
            // The parent's own normal at its vertices (every second one of ours), as the parent computes it, and its colour request.
            for (int j = 0; j <= n; j += 2)
                for (int i = 0; i <= n; i += 2)
                {
                    int k = K(c, i, j);
                    Vector3d pn = Normal(c, i, j, 2);
                    c->ParentNormals[j * row + i] = pn;
                    c->ParentSamples[c->ParentRequests] = k;
                    c->ParentCosSlopes[c->ParentRequests++] = Vector3d.Dot(pn, c->Dirs[k]);
                }
        }

        /// <summary>The colours (with the shader's water flag), the LOD morph data and the skirts.</summary>
        public static void Finish(ChunkGrid* c)
        {
            int n = c->N, row = c->Row;
            if (c->Colors)
                for (int vi = 0; vi < row * row; vi++)
                {
                    Color32 col = c->ColorOut[vi];
                    col.a = c->Ocean && c->Heights[c->ColorSamples[vi]] < 0 ? (byte)255 : (byte)0; // the shader's water flag
                    c->Colors32[vi] = col;
                }
            if (c->Morph)
            {
                int requests = 0;
                for (int j = 0; j <= n; j += 2)
                    for (int i = 0; i <= n; i += 2)
                    {
                        int k = c->ParentSamples[requests];
                        Color32 col = c->ParentOut[requests++];
                        col.a = c->Ocean && c->Heights[k] < 0 ? (byte)255 : (byte)0;
                        c->ParentColors[j * row + i] = col;
                    }
                // Every vertex's point on the parent's surface: a parent vertex, the middle of a parent edge, or the middle of
                // the diagonal (a, c) that splits each parent quad into the triangles (a, b, c) and (a, c, d).
                for (int j = 0; j <= n; j++)
                    for (int i = 0; i <= n; i++)
                    {
                        int ia = i - (i & 1), ja = j - (j & 1), ib = i + (i & 1), jb = j + (j & 1);
                        int a = ja * row + ia, b = jb * row + ib, vi = j * row + i;
                        Vector3 delta = (Vector3)((P(c, ia, ja) + P(c, ib, jb)) * 0.5 - P(c, i, j));
                        Vector3d pn = Normalize(c->ParentNormals[a] + c->ParentNormals[b]);
                        Color32 ca = c->ParentColors[a], cb = c->ParentColors[b];
                        double r = (ca.r + cb.r) >> 1, gr = (ca.g + cb.g) >> 1, bl = (ca.b + cb.b) >> 1;
                        c->UV2[vi] = new Vector4(delta.x, delta.y, delta.z, c->ParentArc);
                        Vector2 oct = Octahedral(pn);
                        c->UV3[vi] = new Vector4(oct.x, oct.y, (float)(r + gr * 256 + bl * 65536), (ca.a + cb.a) / 510f);
                    }
            }
            else if (c->Colors)
                for (int vi = 0; vi < row * row; vi++)
                {
                    c->UV2[vi] = default;
                    c->UV3[vi] = default;
                }
            Vector3 bmin = c->BoundsMin, bmax = c->BoundsMax;
            for (int k = 0; k < c->PerimeterCount; k++)
            {
                int src = c->Perimeter[k];
                int i = src % row, j = src / row;
                Vector3d p = P(c, i, j) - c->Dirs[K(c, i, j)] * c->SkirtDepth;
                int vi = row * row + k;
                Vector3 local = (Vector3)(p - c->Center);
                c->Positions[vi] = local;
                c->Normals[vi] = c->Normals[src];
                if (c->Colors)
                {
                    c->Colors32[vi] = c->Colors32[src];
                    c->UV2[vi] = c->UV2[src];
                    c->UV3[vi] = c->UV3[src];
                }
                c->UV0[vi] = c->UV0[src];
                c->UV1[vi] = c->UV1[src];
                bmin = Min(bmin, local);
                bmax = Max(bmax, local);
            }
            c->BoundsMin = bmin;
            c->BoundsMax = bmax;
            if (!c->Colors) return;
            // Pack every vertex for the GPU. UV3.z holds the parent's RGB as r + 256 g + 65536 b (an integer, exact in a float).
            for (int vi = 0, count = row * row + c->PerimeterCount; vi < count; vi++)
            {
                Vector4 move = c->UV2[vi], parent = c->UV3[vi];
                int rgb = (int)parent.z;
                c->Vertices[vi] = new TerrainVertex
                {
                    Position = c->Positions[vi], Normal = c->Normals[vi], Color = c->Colors32[vi], UV0 = c->UV0[vi], UV1 = c->UV1[vi],
                    MoveX = Half(move.x), MoveY = Half(move.y), MoveZ = Half(move.z), ParentArcKm = Half(move.w * 0.001f),
                    ParentNormalX = Half(parent.x), ParentNormalY = Half(parent.y),
                    Parent = new Color32((byte)(rgb & 255), (byte)((rgb >> 8) & 255), (byte)(rgb >> 16), (byte)(parent.w * 255 + 0.5f)),
                };
            }
        }

        /// <summary>
        /// A float as an IEEE half (round to nearest, ties to even), in integer arithmetic only: Burst flushes subnormal floats to zero,
        /// so a conversion through float maths (math.f32tof16) gives other bits for tiny values than plain C# does.
        /// </summary>
        internal static ushort Half(float v)
        {
            uint x = *(uint*)&v;
            uint sign = (x >> 16) & 0x8000;
            int exp = (int)((x >> 23) & 0xff) - 112; // rebias 127 → 15
            uint mant = x & 0x7fffff;
            if (exp >= 31) return (ushort)(sign | (exp == 143 && mant != 0 ? 0x7e00u : 0x7c00u)); // too big: infinity (NaN stays NaN)
            if (exp <= 0)
            {
                if (exp < -10) return (ushort)sign; // below half's smallest subnormal
                mant |= 0x800000;
                int shift = 14 - exp;
                uint sub = mant >> shift, rest = mant & ((1u << shift) - 1), mid = 1u << (shift - 1);
                if (rest > mid || (rest == mid && (sub & 1) != 0)) sub++;
                return (ushort)(sign | sub);
            }
            uint h = ((uint)exp << 10) | (mant >> 13), r = mant & 0x1fff;
            if (r > 0x1000 || (r == 0x1000 && (h & 1) != 0)) h++; // a carry into the exponent is right (up to infinity)
            return (ushort)(sign | h);
        }

        /// <summary>Vector3d.normalized, without its static zero.</summary>
        internal static Vector3d Normalize(Vector3d v)
        {
            double m = v.magnitude;
            return m > 1e-300 ? new Vector3d(v.x / m, v.y / m, v.z / m) : new Vector3d(0, 0, 0);
        }

        // Vector3.Min and Max (Mathf.Min: a < b ? a : b), without UnityEngine's statics.
        private static Vector3 Min(Vector3 a, Vector3 b) => new Vector3(a.x < b.x ? a.x : b.x, a.y < b.y ? a.y : b.y, a.z < b.z ? a.z : b.z);
        private static Vector3 Max(Vector3 a, Vector3 b) => new Vector3(a.x > b.x ? a.x : b.x, a.y > b.y ? a.y : b.y, a.z > b.z ? a.z : b.z);

        /// <summary>A unit vector in two numbers (octahedral mapping); TerrainMorph.hlsl decodes it.</summary>
        internal static Vector2 Octahedral(Vector3d n)
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
    }
}
