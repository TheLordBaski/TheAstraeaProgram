using System;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TAP.Editor
{
    /// <summary>
    /// A BC7 texture array built on import from the image files a small JSON manifest lists. The repository keeps only
    /// the source images and the manifest; the array itself lives in the import cache, never as hex text in an asset.
    /// </summary>
    [ScriptedImporter(1, "texarray")]
    public sealed class TextureArrayImporter : ScriptedImporter
    {
        [Serializable]
        public sealed class Manifest
        {
            /// <summary>Linear data (normals, masks, ratios); false for sRGB colour.</summary>
            public bool linear;
            public int size = 2048;
            public int aniso = 4;
            /// <summary>Project-relative image paths (PNG or JPEG), one per layer.</summary>
            public string[] layers;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ctx.assetPath));
            if (m == null || m.layers == null || m.layers.Length == 0) { ctx.LogImportError("Texture array manifest lists no layers."); return; }
            int n = m.size > 0 ? m.size : 2048;
            var array = new Texture2DArray(n, n, m.layers.Length, TextureFormat.BC7, true, m.linear)
            { name = Path.GetFileNameWithoutExtension(ctx.assetPath), wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = m.aniso };
            for (int i = 0; i < m.layers.Length; i++)
            {
                string path = m.layers[i];
                ctx.DependsOnSourceAsset(path);
                var pixels = File.Exists(path) ? Decode(path, n) : null;
                if (pixels == null)
                {
                    ctx.LogImportError("Missing or unreadable texture array layer " + path);
                    pixels = new Color32[n * n];
                    for (int k = 0; k < pixels.Length; k++) pixels[k] = new Color32(128, 128, 128, 255);
                }
                var t = new Texture2D(n, n, TextureFormat.RGBA32, true, m.linear);
                try
                {
                    t.SetPixels32(pixels); t.Apply(true, false);
                    EditorUtility.CompressTexture(t, TextureFormat.BC7, TextureCompressionQuality.Normal);
                    for (int mip = 0; mip < t.mipmapCount; mip++) array.SetPixelData(t.GetPixelData<byte>(mip), mip, i);
                }
                finally { DestroyImmediate(t); }
            }
            array.Apply(false, true);
            ctx.AddObjectToAsset("array", array);
            ctx.SetMainObject(array);
        }

        /// <summary>Raw 8-bit texels of an image file (no colour conversion), bilinearly resized to n x n when needed.</summary>
        static Color32[] Decode(string path, int n)
        {
            var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!t.LoadImage(File.ReadAllBytes(path))) return null;
                var src = t.GetPixels32(); int w = t.width, h = t.height;
                if (w == n && h == n) return src;
                var dst = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float u = (x + .5f) * w / n - .5f, v = (y + .5f) * h / n - .5f;
                        int x0 = Mathf.Clamp((int)Mathf.Floor(u), 0, w - 1), y0 = Mathf.Clamp((int)Mathf.Floor(v), 0, h - 1);
                        int x1 = Mathf.Min(w - 1, x0 + 1), y1 = Mathf.Min(h - 1, y0 + 1);
                        float fx = Mathf.Clamp01(u - x0), fy = Mathf.Clamp01(v - y0);
                        dst[y * n + x] = Color32.Lerp(Color32.Lerp(src[y0 * w + x0], src[y0 * w + x1], fx), Color32.Lerp(src[y1 * w + x0], src[y1 * w + x1], fx), fy);
                    }
                return dst;
            }
            finally { DestroyImmediate(t); }
        }
    }

    /// <summary>Tiling 3D cloud noise generated on import from a seed (<see cref="CloudNoiseBaker"/>): nothing stored.</summary>
    [ScriptedImporter(CloudNoiseBaker.Version, "cloudnoise")]
    public sealed class CloudNoiseImporter : ScriptedImporter
    {
        [Serializable]
        public sealed class Manifest
        {
            /// <summary>"shape" (128-cubed Perlin-Worley + Worley) or "detail" (64-cubed Worley erosion).</summary>
            public string kind = "shape";
            public int seed = 1931;
        }

        public override void OnImportAsset(AssetImportContext ctx)
        {
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(ctx.assetPath)) ?? new Manifest();
            bool detail = m.kind == "detail";
            var pixels = detail ? CloudNoiseBaker.Detail(m.seed) : CloudNoiseBaker.Shape(m.seed);
            var texture = CloudNoiseBaker.ToTexture(pixels, detail ? CloudNoiseBaker.DetailSize : CloudNoiseBaker.ShapeSize, Path.GetFileNameWithoutExtension(ctx.assetPath));
            texture.Apply(false, true);
            ctx.AddObjectToAsset("noise", texture);
            ctx.SetMainObject(texture);
        }
    }
}
