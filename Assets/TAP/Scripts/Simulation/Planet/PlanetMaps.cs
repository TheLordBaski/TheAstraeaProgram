using System;
using System.Threading.Tasks;
using TAP.Core;
using UnityEngine;

namespace TAP.Simulation
{
    /// <summary>
    /// Flat (equirectangular) maps of a body, laid out as the map view's sphere reads them: longitude −180°…180° from left
    /// to right, and rows from the terrain's latitude −90° (geographic north) at row 0 to +90° at the top row. Pure
    /// computation, safe on any thread; <see cref="ToTexture"/> must run on the main thread.
    /// </summary>
    public static class PlanetMaps
    {
        public static Vector3d DirectionOf(int x, int y, int w, int h) =>
            TerrainGenerator.DirectionFromLatLon(((y + 0.5) / h - 0.5) * 180.0, (x + 0.5) / w * 360.0 - 180.0);

        /// <summary>The surface colours, lit by a soft hillshade.</summary>
        public static Color32[] Surface(TerrainGenerator gen, int w, int h)
        {
            var heights = new float[w * h];
            var cols = new Color32[w * h];
            Parallel.For(0, h, y =>
            {
                var f = new double[Math.Max(1, gen.FieldCount)];
                for (int x = 0; x < w; x++)
                {
                    var dir = DirectionOf(x, y, w, h);
                    double hh = gen.Sample(dir, f, 0, true);
                    heights[y * w + x] = (float)hh;
                    cols[y * w + x] = gen.Colorize(dir, hh, f, 0, 1);
                }
            });
            float metersPerPx = (float)(Math.PI * 2 * gen.Radius / w);
            var result = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    float hC = heights[i];
                    float hE = heights[y * w + (x + 1) % w];
                    float hN = heights[Mathf.Min(h - 1, y + 1) * w + x];
                    float shade = 1f;
                    if (!(gen.HasOcean && hC < 0))
                    {
                        float dx = (hE - hC) / metersPerPx, dy = (hN - hC) / metersPerPx;
                        shade = Mathf.Clamp(1f - (dx * 0.6f - dy * 0.8f) * 8f, 0.55f, 1.35f);
                    }
                    Color c = cols[i];
                    c *= shade;
                    c.a = 1;
                    result[i] = c;
                }
            return result;
        }

        /// <summary>The biome under every pixel (indices into the terrain's biome list).</summary>
        public static int[] BiomeIndices(TerrainGenerator gen, int w, int h)
        {
            var idx = new int[w * h];
            Parallel.For(0, h, y =>
            {
                for (int x = 0; x < w; x++) idx[y * w + x] = gen.BiomeAt(DirectionOf(x, y, w, h)).Index;
            });
            return idx;
        }

        /// <summary>
        /// The biome map: each biome's colour, shaded by the surface map (when given) so the land's shapes show through,
        /// with darker lines along the borders.
        /// </summary>
        public static Color32[] Biomes(TerrainGenerator gen, int[] idx, int w, int h, Color32[] surface = null)
        {
            var biomes = gen.Biomes;
            var result = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    Color c = biomes[idx[i]].Color;
                    if (surface != null)
                    {
                        Color s = surface[i];
                        float lum = 0.3f * s.r + 0.59f * s.g + 0.11f * s.b;
                        c *= Mathf.Lerp(0.65f, 1.25f, Mathf.Clamp01(lum * 1.4f));
                    }
                    bool border = idx[i] != idx[y * w + (x + 1) % w] || (y + 1 < h && idx[i] != idx[(y + 1) * w + x]);
                    if (border) c *= 0.45f;
                    c.a = 1;
                    result[i] = c;
                }
            return result;
        }

        /// <summary>Rows flipped so geographic north is at the top, for pictures meant for people.</summary>
        public static Color32[] NorthUp(Color32[] map, int w, int h)
        {
            var r = new Color32[map.Length];
            for (int y = 0; y < h; y++) Array.Copy(map, y * w, r, (h - 1 - y) * w, w);
            return r;
        }

        public static Texture2D ToTexture(Color32[] pixels, int w, int h, bool mipmaps = true)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, mipmaps) { wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(pixels);
            t.Apply(mipmaps);
            return t;
        }
    }
}
