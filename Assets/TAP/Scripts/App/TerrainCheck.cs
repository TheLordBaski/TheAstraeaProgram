using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TAP.Core;
using TAP.Game;
using UnityEngine;

namespace TAP.App
{
    /// <summary>
    /// <c>-autotest terrain -fingerprint &lt;file&gt; [-report &lt;file&gt;]</c>: checks the recorded terrain fingerprint
    /// (Assets/TAP/Tests/EditMode/TerrainFingerprint.txt) in this build, with the classes and with the Burst kernels (FND-19), writes
    /// what it found and quits, with exit code 1 on any mismatch. The editor's tests do the same in the editor.
    /// </summary>
    public static class TerrainCheck
    {
        public static void Run()
        {
            var report = new StringBuilder();
            bool ok = true;
            try
            {
                string path = null;
                var args = Environment.GetCommandLineArgs();
                for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-fingerprint") path = args[i + 1];
                if (path == null || !File.Exists(path)) throw new FileNotFoundException("-fingerprint names no file: " + path);
                var terrains = new Dictionary<string, LayeredTerrain>();
                foreach (var (file, id) in new[] { ("Data/system", "home"), ("Data/system_debug", "debug") })
                    foreach (var b in CelestialSystem.LoadFromResources(file, id).Bodies)
                        if (b.Terrain is LayeredTerrain t) terrains[$"{id}/{b.Id}"] = t;
                string recorded = File.ReadAllText(path);
                report.AppendLine($"Terrain fingerprint check: {Application.platform}, {SystemInfo.processorType.Trim()}, Burst compiled: {LayeredTerrain.BurstCompiled}");
                bool was = LayeredTerrain.UseBurst;
                foreach (bool burst in new[] { false, true })
                {
                    LayeredTerrain.UseBurst = burst;
                    // The definitions' hashes are the editor test's job: another runtime may write their numbers differently.
                    var res = TerrainFingerprint.Verify(recorded, k => terrains.TryGetValue(k, out var t) ? t : null, checkHash: false, batch: burst);
                    bool pass = res.Mismatches == 0 && res.Missing.Count == 0 && res.Checked > 1000;
                    ok &= pass;
                    report.AppendLine($"- {(burst ? "Burst kernels" : "classes")}: {res.Checked} points, {res.Mismatches} mismatches, {res.Missing.Count} bodies missing: {(pass ? "PASS" : "FAIL")}");
                    foreach (var d in res.Details) report.AppendLine("  " + d);
                }
                LayeredTerrain.UseBurst = was;
            }
            catch (Exception e)
            {
                ok = false;
                report.AppendLine("FAIL: " + e);
            }
            Debug.Log("[TerrainCheck] " + report);
            if (!string.IsNullOrEmpty(GameSession.AutoTestReportPath)) File.WriteAllText(GameSession.AutoTestReportPath, report.ToString());
            Application.Quit(ok ? 0 : 1);
        }
    }
}
