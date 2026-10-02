using System;
using System.Threading.Tasks;
using UnityEngine;

namespace TAP.Editor
{
    /// <summary>
    /// Tiling 3D cloud noise (Schneider, "The real-time volumetric cloudscapes of Horizon Zero Dawn"): a 128-cubed shape
    /// texture with Perlin-Worley in R and Worley octaves in GBA, and a 64-cubed erosion texture of Worley octaves.
    /// Generated off the main thread; deterministic for a seed.
    /// </summary>
    public static class CloudNoiseBaker
    {
        public const int ShapeSize = 128, DetailSize = 64;
        /// <summary>Bump when the generator changes: .cloudnoise assets then import again.</summary>
        public const int Version = 1;

        static uint Hash(int x, int y, int z, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + z * 1274126177 + seed * 144665 + 1013904223);
                h = (h ^ (h >> 13)) * 1103515245u; h ^= h >> 16; h *= 2246822519u; h ^= h >> 13;
                return h;
            }
        }
        static float Hash01(int x, int y, int z, int seed) => (Hash(x, y, z, seed) & 0xffffff) / 16777216f;
        static int Wrap(int v, int p) { v %= p; return v < 0 ? v + p : v; }

        static readonly Vector3[] Gradients =
        {
            new Vector3(1,1,0), new Vector3(-1,1,0), new Vector3(1,-1,0), new Vector3(-1,-1,0),
            new Vector3(1,0,1), new Vector3(-1,0,1), new Vector3(1,0,-1), new Vector3(-1,0,-1),
            new Vector3(0,1,1), new Vector3(0,-1,1), new Vector3(0,1,-1), new Vector3(0,-1,-1),
        };

        /// <summary>Periodic gradient noise, period <paramref name="p"/> lattice cells, roughly [-1,1].</summary>
        static float Perlin(Vector3 q, int p, int seed)
        {
            int x0 = Mathf.FloorToInt(q.x), y0 = Mathf.FloorToInt(q.y), z0 = Mathf.FloorToInt(q.z);
            Vector3 f = new Vector3(q.x - x0, q.y - y0, q.z - z0);
            Vector3 u = new Vector3(Fade(f.x), Fade(f.y), Fade(f.z));
            float n000 = Corner(x0, y0, z0, 0, 0, 0), n100 = Corner(x0, y0, z0, 1, 0, 0), n010 = Corner(x0, y0, z0, 0, 1, 0), n110 = Corner(x0, y0, z0, 1, 1, 0);
            float n001 = Corner(x0, y0, z0, 0, 0, 1), n101 = Corner(x0, y0, z0, 1, 0, 1), n011 = Corner(x0, y0, z0, 0, 1, 1), n111 = Corner(x0, y0, z0, 1, 1, 1);
            float a = Mathf.Lerp(Mathf.Lerp(n000, n100, u.x), Mathf.Lerp(n010, n110, u.x), u.y);
            float b = Mathf.Lerp(Mathf.Lerp(n001, n101, u.x), Mathf.Lerp(n011, n111, u.x), u.y);
            return Mathf.Lerp(a, b, u.z);

            float Corner(int x, int y, int z, int dx, int dy, int dz)
            {
                var g = Gradients[Hash(Wrap(x + dx, p), Wrap(y + dy, p), Wrap(z + dz, p), seed) % 12];
                return Vector3.Dot(g, new Vector3(f.x - dx, f.y - dy, f.z - dz));
            }
        }
        static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);

        /// <summary>Inverted periodic cellular noise: 1 at feature points, falling to 0 a cell away.</summary>
        static float Worley(Vector3 unit, int cells, int seed)
        {
            Vector3 q = unit * cells;
            int cx = Mathf.FloorToInt(q.x), cy = Mathf.FloorToInt(q.y), cz = Mathf.FloorToInt(q.z);
            float best = 9;
            for (int dz = -1; dz <= 1; dz++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = cx + dx, y = cy + dy, z = cz + dz;
                        int wx = Wrap(x, cells), wy = Wrap(y, cells), wz = Wrap(z, cells);
                        var feature = new Vector3(x + Hash01(wx, wy, wz, seed), y + Hash01(wx, wy, wz, seed + 7919), z + Hash01(wx, wy, wz, seed + 15473));
                        float d = (feature - q).sqrMagnitude;
                        if (d < best) best = d;
                    }
            return 1 - Mathf.Clamp01(Mathf.Sqrt(best));
        }

        static float Fbm(float a, float b, float c) => a * .625f + b * .25f + c * .125f;

        public static Color32[] Shape(int seed)
        {
            int n = ShapeSize; var pixels = new Color32[n * n * n];
            Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, z =>
            {
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = new Vector3((x + .5f) / n, (y + .5f) / n, (z + .5f) / n);
                        float w4 = Worley(p, 4, seed + 11), w8 = Worley(p, 8, seed + 23), w16 = Worley(p, 16, seed + 37), w32 = Worley(p, 32, seed + 41), w64 = Worley(p, 64, seed + 53);
                        float perlin = 0, amp = 1, total = 0;
                        for (int o = 0, f = 4; o < 4; o++, f *= 2) { perlin += Perlin(p * f, f, seed + o * 101) * amp; total += amp; amp *= .5f; }
                        perlin = Mathf.Clamp01(perlin / total * .9f + .5f);
                        float worley = Fbm(w4, w8, w16);
                        // Perlin-Worley: Perlin billows dilated by cellular structure.
                        float pw = Mathf.Clamp01(worley + perlin * (1 - worley));
                        pixels[(z * n + y) * n + x] = new Color32(B(pw), B(worley), B(Fbm(w8, w16, w32)), B(Fbm(w16, w32, w64)));
                    }
            });
            return pixels;
        }

        public static Color32[] Detail(int seed)
        {
            int n = DetailSize; var pixels = new Color32[n * n * n];
            Parallel.For(0, n, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 2) }, z =>
            {
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = new Vector3((x + .5f) / n, (y + .5f) / n, (z + .5f) / n);
                        float w4 = Worley(p, 4, seed + 3), w8 = Worley(p, 8, seed + 5), w16 = Worley(p, 16, seed + 9), w32 = Worley(p, 32, seed + 13);
                        pixels[(z * n + y) * n + x] = new Color32(B(Fbm(w4, w8, w16)), B(Fbm(w8, w16, w32)), B(Fbm(w16, w32, w32)), 255);
                    }
            });
            return pixels;
        }

        static byte B(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255), 0, 255);

        public static Texture3D ToTexture(Color32[] pixels, int size, string name)
        {
            var t = new Texture3D(size, size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
            t.SetPixels32(pixels); t.Apply(true, false);
            return t;
        }
    }
}
