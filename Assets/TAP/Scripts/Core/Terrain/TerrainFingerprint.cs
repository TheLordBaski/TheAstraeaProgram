using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;

namespace TAP.Core
{
    /// <summary>
    /// The heights and biomes of a terrain at fixed points, bit for bit, with a hash of the definition they came from.
    /// Recorded on one machine (TAP → Terrain → Record Fingerprint) and checked by a test on every other: the same data
    /// must give the same bits on Windows and Linux.
    /// </summary>
    public static class TerrainFingerprint
    {
        public const int Points = 300;

        /// <summary>Points spread evenly over the sphere (a golden-angle spiral), the same on every platform.</summary>
        public static Vector3d Point(int i, int n = Points)
        {
            double y = 1 - 2 * (i + 0.5) / n;
            double r = Math.Sqrt(Math.Max(0, 1 - y * y));
            double phi = i * 2.39996322972865332; // golden angle (rad)
            return new Vector3d(DetMath.Cos(phi) * r, y, DetMath.Sin(phi) * r);
        }

        /// <summary>
        /// FNV-1a hash of the definition as JSON: tells a data change from a determinism fault. Empty and zero entries are
        /// left out, so a new optional entry in the format doesn't count as a change.
        /// </summary>
        public static string Hash(TerrainDefinition def)
        {
            string json = JsonConvert.SerializeObject(def, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore,
            });
            ulong h = 14695981039346656037UL;
            foreach (byte b in Encoding.UTF8.GetBytes(json))
            {
                h ^= b;
                h *= 1099511628211UL;
            }
            return h.ToString("x16");
        }

        /// <summary>One block per body: "body &lt;system&gt;/&lt;id&gt; &lt;hash&gt;" then "&lt;i&gt; &lt;height bits&gt; &lt;biome&gt;" per point.</summary>
        public static string Record(IEnumerable<(string key, LayeredTerrain terrain)> bodies)
        {
            var sb = new StringBuilder();
            sb.Append("# TAP terrain fingerprint: heights (IEEE-754 bits) and biomes at fixed points. Record again with TAP > Terrain > Record Fingerprint after changing terrain data.\n");
            foreach (var (key, t) in bodies)
            {
                sb.Append("body ").Append(key).Append(' ').Append(Hash(t.Def)).Append('\n');
                for (int i = 0; i < Points; i++)
                {
                    var d = Point(i);
                    long bits = BitConverter.DoubleToInt64Bits(t.Height(d));
                    sb.Append(i).Append(' ').Append(bits.ToString("x16")).Append(' ').Append(t.BiomeAt(d).Id).Append('\n');
                }
            }
            return sb.ToString();
        }

        public sealed class Result
        {
            public int Checked, Mismatches;
            /// <summary>Bodies whose data changed since recording (their points are not checked).</summary>
            public readonly List<string> Changed = new List<string>();
            public readonly List<string> Missing = new List<string>();
            public readonly List<string> Details = new List<string>();
        }

        /// <summary>
        /// Compares the terrains with a recorded fingerprint. checkHash: false to skip the data check (another runtime may
        /// write the definition's numbers differently, while the data files are the same).
        /// </summary>
        public static Result Verify(string recorded, Func<string, LayeredTerrain> terrainFor, bool checkHash = true)
        {
            var res = new Result();
            LayeredTerrain t = null;
            bool skip = false;
            foreach (var raw in recorded.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var parts = line.Split(' ');
                if (parts[0] == "body")
                {
                    t = terrainFor(parts[1]);
                    skip = false;
                    if (t == null) { res.Missing.Add(parts[1]); skip = true; }
                    else if (checkHash && Hash(t.Def) != parts[2]) { res.Changed.Add(parts[1]); skip = true; }
                    continue;
                }
                if (skip || t == null) continue;
                int i = int.Parse(parts[0], CultureInfo.InvariantCulture);
                var d = Point(i);
                long bits = BitConverter.DoubleToInt64Bits(t.Height(d));
                string biome = t.BiomeAt(d).Id;
                res.Checked++;
                if (bits.ToString("x16") != parts[1] || biome != parts[2])
                {
                    res.Mismatches++;
                    if (res.Details.Count < 10)
                        res.Details.Add($"{t.BodyId} point {i}: height {BitConverter.Int64BitsToDouble(bits):R} (recorded {BitConverter.Int64BitsToDouble(long.Parse(parts[1], NumberStyles.HexNumber)):R}), biome {biome} (recorded {parts[2]})");
                }
            }
            return res;
        }
    }
}
