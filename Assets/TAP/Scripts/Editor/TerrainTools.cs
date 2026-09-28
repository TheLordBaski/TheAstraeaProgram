using System.Collections.Generic;
using System.IO;
using TAP.Core;
using TAP.Simulation;
using UnityEditor;
using UnityEngine;

namespace TAP.EditorTools
{
    /// <summary>Terrain upkeep: the cross-platform fingerprint and the flat maps for reviewing worlds and biomes.</summary>
    public static class TerrainTools
    {
        public const string FingerprintPath = "Assets/TAP/Tests/EditMode/TerrainFingerprint.txt";

        /// <summary>Every body with a surface in the home and debug systems, as "system/id".</summary>
        public static IEnumerable<(string key, LayeredTerrain terrain)> AllTerrains()
        {
            foreach (var (file, id) in new[] { ("Data/system", "home"), ("Data/system_debug", "debug") })
            {
                var sys = CelestialSystem.LoadFromResources(file, id);
                foreach (var b in sys.Bodies)
                    if (b.Terrain is LayeredTerrain t) yield return ($"{id}/{b.Id}", t);
            }
        }

        [MenuItem("TAP/Terrain/Record Fingerprint")]
        public static void RecordFingerprint()
        {
            File.WriteAllText(FingerprintPath, TerrainFingerprint.Record(AllTerrains()));
            AssetDatabase.ImportAsset(FingerprintPath);
            Debug.Log("[TerrainTools] Fingerprint recorded: " + FingerprintPath);
        }

        [MenuItem("TAP/Terrain/Export Maps")]
        public static void ExportMaps() => ExportMaps("Screenshots/Terrain");

        /// <summary>
        /// Writes, per body, a flat surface map and a biome map with its legend (north up, 1024×512), plus each biome's
        /// share of the surface in the log.
        /// </summary>
        public static string ExportMaps(string folder, int w = 1024, int h = 512)
        {
            Directory.CreateDirectory(folder);
            var log = new System.Text.StringBuilder();
            foreach (var (key, t) in AllTerrains())
            {
                string name = key.Replace('/', '_');
                var surface = PlanetMaps.Surface(t, w, h);
                var idx = PlanetMaps.BiomeIndices(t, w, h);
                var biomes = PlanetMaps.Biomes(t, idx, w, h, surface);
                Save(PlanetMaps.NorthUp(surface, w, h), w, h, Path.Combine(folder, name + "_surface.png"));

                // Shares weighted by area (rows near the poles cover less).
                var area = new double[t.Biomes.Count];
                double total = 0;
                for (int y = 0; y < h; y++)
                {
                    double wgt = System.Math.Cos(((y + 0.5) / h - 0.5) * System.Math.PI);
                    for (int x = 0; x < w; x++) { area[idx[y * w + x]] += wgt; total += wgt; }
                }
                log.Append(key).Append(':');
                for (int i = 0; i < area.Length; i++) log.Append($" {t.Biomes[i].Name} {100 * area[i] / total:F1}%,");
                log.Append('\n');

                // The biome map above a legend: a swatch, the name and the share of each biome, in as many columns as fit.
                var labels = new string[t.Biomes.Count];
                float widest = 0;
                for (int i = 0; i < labels.Length; i++)
                {
                    labels[i] = $"{t.Biomes[i].Name} {Share(100 * area[i] / total)}";
                    widest = Mathf.Max(widest, TexturePainter.TextWidth(labels[i], 2));
                }
                int cols = Mathf.Clamp((int)((w - 24) / (widest + 48)), 1, 4), rowH = 22, legendH = 16 + rowH * ((t.Biomes.Count + cols - 1) / cols);
                var p = new TexturePainter(w, h + legendH, new Color(0.08f, 0.08f, 0.1f, 1));
                var up = PlanetMaps.NorthUp(biomes, w, h);
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++) p.Px[(y + legendH) * w + x] = up[y * w + x];
                for (int i = 0; i < t.Biomes.Count; i++)
                {
                    int col = i % cols, row = i / cols;
                    float x0 = 12 + col * (w / cols), y0 = legendH - 8 - rowH * (row + 1) + 4;
                    p.RectFill(x0, y0, x0 + 14, y0 + 14, t.Biomes[i].Color);
                    p.Text(labels[i], x0 + 22, y0 + 0.5f, 2, Color.white);
                }
                Save(p, Path.Combine(folder, name + "_biomes.png"));
            }
            Debug.Log("[TerrainTools] Maps exported to " + folder + "\n" + log);
            return log.ToString();
        }

        private static string Share(double pct) =>
            pct <= 0 ? "0%" : pct < 0.1 ? "<0.1%" : pct < 10 ? $"{pct:F1}%" : $"{pct:F0}%";

        private static void Save(Color32[] px, int w, int h, string path)
        {
            var tex = PlanetMaps.ToTexture(px, w, h, false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static void Save(TexturePainter p, string path)
        {
            var tex = p.ToTexture(false);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
