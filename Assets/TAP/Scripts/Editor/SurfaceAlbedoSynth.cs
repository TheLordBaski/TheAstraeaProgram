using System;
using System.Collections.Generic;
using System.IO;
using TAP.Core;
using UnityEngine;

namespace TAP.Editor
{
    /// <summary>
    /// Orbital albedo that keeps a body's own geography: its terrain colour rules decide what grows where, and each rule
    /// is painted with the texture of a matching real landscape (tiling satellite exemplars, NASA Blue Marble, public
    /// domain) instead of a flat colour. Exemplars are high-passed, so they add structure, not their region's layout.
    /// Editor bake only; the source images stay outside Assets.
    /// </summary>
    public static class SurfaceAlbedoSynth
    {
        public const string Folder = "Art/PlanetVisuals/BlueMarble/Exemplars";
        public static readonly string[] Classes = { "forest", "grass", "steppe", "scrub", "sand", "rock" };
        /// <summary>Metres per exemplar texel on the planet (the sources are 500 m per pixel).</summary>
        public const double MetresPerTexel = 420;
        /// <summary>How far a rule's mean colour moves from the terrain palette towards the real landscape's.</summary>
        public const float Realism = .55f;

        public sealed class Exemplar { public int Size; public float[] R, G, B; public Vector3 Mean; }
        public sealed class Set { public Exemplar[][] ByClass = new Exemplar[Classes.Length][]; public bool Any; }

        /// <summary>Decodes the exemplars (main thread). An empty set leaves the flat terrain colours.</summary>
        public static Set Load()
        {
            var set = new Set();
            for (int c = 0; c < Classes.Length; c++)
            {
                var list = new List<Exemplar>();
                for (int i = 0; i < 8; i++)
                {
                    string path = Path.Combine(Folder, Classes[c] + "_" + i + ".png");
                    if (!File.Exists(path)) break;
                    var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
                    try
                    {
                        if (!t.LoadImage(File.ReadAllBytes(path))) continue;
                        var px = t.GetPixels32(); int n = t.width;
                        if (t.height != n) continue;
                        var e = new Exemplar { Size = n, R = new float[n * n], G = new float[n * n], B = new float[n * n] };
                        double sr = 0, sg = 0, sb = 0;
                        for (int k = 0; k < px.Length; k++)
                        {
                            e.R[k] = ToLinear(px[k].r); e.G[k] = ToLinear(px[k].g); e.B[k] = ToLinear(px[k].b);
                            sr += e.R[k]; sg += e.G[k]; sb += e.B[k];
                        }
                        e.Mean = new Vector3((float)(sr / px.Length), (float)(sg / px.Length), (float)(sb / px.Length));
                        list.Add(e);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(t); }
                }
                set.ByClass[c] = list.ToArray();
                set.Any |= list.Count > 0;
            }
            return set;
        }

        static float ToLinear(byte v) { float c = v / 255f; return c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f); }

        /// <summary>Exemplar class of a colour rule from its note; -1 flat colour (water, unknown), -2 snow and ice.</summary>
        public static int ClassOf(string note)
        {
            note = (note ?? "").ToLowerInvariant();
            if (note.Contains("water") || note.Contains("sea") || note.Contains("ocean") || note.Contains("lake")) return -1;
            if (note.Contains("snow") || note.Contains("ice") || note.Contains("frost") || note.Contains("glacier")) return -2;
            if (note.Contains("wood") || note.Contains("forest") || note.Contains("jungle") || note.Contains("taiga")) return 0;
            if (note.Contains("dry") || note.Contains("steppe") || note.Contains("savanna")) return 2;
            if (note.Contains("grass") || note.Contains("meadow") || note.Contains("field")) return 1;
            if (note.Contains("scrub") || note.Contains("highland") || note.Contains("upland") || note.Contains("heath") || note.Contains("tundra")) return 3;
            if (note.Contains("beach") || note.Contains("sand") || note.Contains("dune") || note.Contains("desert")) return 4;
            if (note.Contains("rock") || note.Contains("cliff") || note.Contains("mountain") || note.Contains("stone") || note.Contains("scree")) return 5;
            return -1;
        }

        static Vector3 Bilinear(Exemplar e, double u, double v)
        {
            int n = e.Size;
            u -= Math.Floor(u / n) * n; v -= Math.Floor(v / n) * n;
            int x0 = (int)u, y0 = (int)v; float fx = (float)(u - x0), fy = (float)(v - y0);
            if (x0 >= n) x0 = 0; if (y0 >= n) y0 = 0;
            int x1 = x0 + 1 == n ? 0 : x0 + 1, y1 = y0 + 1 == n ? 0 : y0 + 1;
            int i00 = y0 * n + x0, i10 = y0 * n + x1, i01 = y1 * n + x0, i11 = y1 * n + x1;
            float w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
            return new Vector3(e.R[i00] * w00 + e.R[i10] * w10 + e.R[i01] * w01 + e.R[i11] * w11,
                               e.G[i00] * w00 + e.G[i10] * w10 + e.G[i01] * w01 + e.G[i11] * w11,
                               e.B[i00] * w00 + e.B[i10] * w10 + e.B[i01] * w01 + e.B[i11] * w11);
        }

        /// <summary>Texture ratio (sample / exemplar mean) by sharp triplanar projection of a body-fixed position (m).</summary>
        static Vector3 Ratio(Exemplar e, Vector3d p, Vector3d n, int salt)
        {
            double ax = Math.Abs(n.x), ay = Math.Abs(n.y), az = Math.Abs(n.z);
            double wx = ax * ax * ax * ax * ax * ax, wy = ay * ay * ay * ay * ay * ay, wz = az * az * az * az * az * az;
            double sum = wx + wy + wz; wx /= sum; wy /= sum; wz /= sum;
            double s = 1 / MetresPerTexel, o = salt * 337.17;
            var c = Vector3.zero;
            if (wx > .01) c += Bilinear(e, p.y * s + o, p.z * s - o * .7) * (float)wx;
            if (wy > .01) c += Bilinear(e, p.z * s + o * 1.3, p.x * s + o * .4) * (float)wy;
            if (wz > .01) c += Bilinear(e, p.x * s - o * .9, p.y * s + o * 1.7) * (float)wz;
            return new Vector3(c.x / Mathf.Max(e.Mean.x, 1e-4f), c.y / Mathf.Max(e.Mean.y, 1e-4f), c.z / Mathf.Max(e.Mean.z, 1e-4f));
        }

        /// <summary>Linear albedo of one colour rule at a body-fixed surface point, before mixing the rules.</summary>
        public static Vector3 Paint(Set set, int cls, Color32 ruleColor, Vector3d p, Vector3d n, Noise3D noise, double radius)
        {
            var rule = new Vector3(ToLinear(ruleColor.r), ToLinear(ruleColor.g), ToLinear(ruleColor.b));
            if (cls == -1) return rule;
            // Low-frequency variety inside a biome: a few percent in brightness, never a pattern of its own.
            var q = p / radius;
            float vary = (float)(1 + .09 * noise.Fbm(q * 7.3 + new Vector3d(17, 3, 41), 3));
            if (cls == -2 || set.ByClass[cls] == null || set.ByClass[cls].Length == 0)
            {
                float snow = (float)(1 + .035 * noise.Fbm(q * 40 + new Vector3d(5, 51, 9), 3));
                return rule * (cls == -2 ? snow : vary);
            }
            var variants = set.ByClass[cls];
            // Domain warp hides the projection's straight blend seams and any repetition of a tile. Kept gentle (strain
            // ~0.3): a stronger warp smears the landscape into streaks.
            var warp = new Vector3d(noise.Fbm(q * 2 + new Vector3d(31, 17, 83), 2), noise.Fbm(q * 2 + new Vector3d(73, 5, 42), 2), noise.Fbm(q * 2 + new Vector3d(11, 92, 26), 2)) * (radius * .022);
            var wp = p + warp;
            Vector3 ratio, mean;
            if (variants.Length == 1) { ratio = Ratio(variants[0], wp, n, cls); mean = variants[0].Mean; }
            else
            {
                // Two landscapes per class, crossing over at a regional scale with a variance-preserving blend.
                double sel = noise.Fbm(q * 2.2 + new Vector3d(cls * 13.1, 7, 3), 3);
                float t = Mathf.SmoothStep(0, 1, (float)((sel + .25) / .5));
                var a = Ratio(variants[0], wp, n, cls * 2 + 1); var b = Ratio(variants[1], wp, n, cls * 2 + 2);
                float wa = 1 - t, wb = t, norm = 1 / Mathf.Sqrt(wa * wa + wb * wb);
                ratio = Vector3.one + ((a - Vector3.one) * wa + (b - Vector3.one) * wb) * norm;
                mean = variants[0].Mean * wa + variants[1].Mean * wb;
            }
            // Narrow beach bands read as a bright outline from orbit at full sand brightness.
            var target = Vector3.Lerp(rule, mean, Realism) * vary * (cls == 4 ? .82f : 1);
            return new Vector3(target.x * Mathf.Max(0, ratio.x), target.y * Mathf.Max(0, ratio.y), target.z * Mathf.Max(0, ratio.z));
        }

        public static Color32 ToSrgb(Vector3 linear)
        {
            return new Color32(Enc(linear.x), Enc(linear.y), Enc(linear.z), 255);
            static byte Enc(float c)
            {
                c = Mathf.Clamp01(c);
                float s = c <= 0.0031308f ? c * 12.92f : 1.055f * Mathf.Pow(c, 1 / 2.4f) - 0.055f;
                return (byte)Mathf.Clamp(Mathf.RoundToInt(s * 255), 0, 255);
            }
        }
    }
}
