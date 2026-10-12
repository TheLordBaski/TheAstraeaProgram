using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace TAP.App
{
    /// <summary>
    /// <c>-benchmark</c> (FND-13): times the game in the situations that cost most and writes one CSV row for each. Launches of 50, 150 and 300
    /// parts, a landed base of ten vessels, the map with forty vessels, maximum time warp, and the time from the main menu to the hub.
    /// <c>-benchmark-out &lt;file&gt;</c> sets the file, <c>-benchmark-scenarios a,b</c> picks some, <c>-benchmark-seconds n</c> sets the length of
    /// each measurement. Run it with <c>-batchmode</c> (add <c>-nographics</c> on a machine without a GPU: the numbers are then the CPU's alone).
    /// </summary>
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        public static readonly string[] AllScenarios = { "startup", "launch-50", "launch-150", "launch-300", "base-10", "map-40", "warp-max" };

        private const float Warmup = 3f;
        private float _seconds = 10f;
        private string _out;
        private readonly List<Row> _rows = new List<Row>();

        private sealed class Row
        {
            public string Scenario;
            public int Parts, Vessels, Frames;
            public double Seconds, Avg, P50, P95, P99, Max;
            public double[] Steps = new double[Enum.GetValues(typeof(SimStep)).Length];
            public double WorkingSetMb, ManagedMb, UnityMb;
        }

        public static void Launch()
        {
            var go = new GameObject("Benchmark");
            DontDestroyOnLoad(go);
            var b = go.AddComponent<BenchmarkRunner>();
            b.StartCoroutine(b.Run());
        }

        private static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            // in the editor the same options are PlayerPrefs, "TAP.benchmark-seconds"
            string pref = PlayerPrefs.GetString("TAP." + name.TrimStart('-'), "");
            return pref.Length > 0 ? pref : null;
        }

        private IEnumerator Run()
        {
            string root = Arg("-benchmark-out");
            _out = root ?? Path.Combine(SaveStorage.Root, "benchmark.csv");
            if (float.TryParse(Arg("-benchmark-seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out float s)) _seconds = s;
            var wanted = (Arg("-benchmark-scenarios") ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var list = wanted.Length > 0 ? wanted : AllScenarios;
            // The frame time is the work, not the wait for the display.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var db = PartDatabase.Instance;
            bool failed = false;
            foreach (var name in list)
            {
                Debug.Log("[Benchmark] " + name);
                IEnumerator scenario = Scenario(name, db);
                while (true)
                {
                    try { if (!scenario.MoveNext()) break; }
                    catch (Exception e) { Debug.LogError($"[Benchmark] {name} failed: {e}"); failed = true; break; }
                    yield return scenario.Current;
                }
            }
            Write();
            Debug.Log("[Benchmark] written to " + _out);
            if (!Application.isEditor) Application.Quit(failed ? 1 : 0);
        }

        private IEnumerator Scenario(string name, PartDatabase db)
        {
            switch (name)
            {
                case "startup": return Startup();
                case "launch-50": return Launch(db, 50);
                case "launch-150": return Launch(db, 150);
                case "launch-300": return Launch(db, 300);
                case "base-10": return Base(db, 10);
                case "map-40": return Orbits(db, 40, name, map: true, warp: false);
                case "warp-max": return Orbits(db, 10, name, map: false, warp: true);
                default: throw new ArgumentException("Unknown scenario " + name);
            }
        }

        // ------------------------------------------------------------------ scenarios

        /// <summary>Main menu to hub: a new game and the space center on screen (budget: under 20 s).</summary>
        private IEnumerator Startup()
        {
            yield return WaitScene(GameSession.MenuScene, () => true);
            float t0 = Time.realtimeSinceStartup;
            // from starting the program to the main menu being up
            _rows.Add(new Row { Scenario = "boot", Seconds = t0, Avg = t0 * 1000.0 });
            GameSession.NewGame("Benchmark");
            GameSession.WritePersistent();
            GameSession.GoToHub();
            yield return WaitScene(GameSession.HubScene, () => UiDriver.Find("bld-assembly") != null);
            float seconds = Time.realtimeSinceStartup - t0;
            var row = new Row { Scenario = "startup", Seconds = seconds, Avg = seconds * 1000.0, Frames = 0 };
            Memory(row);
            _rows.Add(row);
        }

        private IEnumerator Launch(PartDatabase db, int parts)
        {
            var design = BenchmarkCraft.Rocket(db, parts);
            GameSession.NewGame("Benchmark");
            GameSession.Launch(design);
            yield return WaitFlight();
            yield return new WaitForSeconds(Warmup);
            var v = FlightSceneController.Instance.Sim.ActiveVessel;
            v.Ctrl.Throttle = 1f;
            v.ActivateNextStage();
            yield return Measure("launch-" + parts, design.parts.Count, 1);
        }

        private IEnumerator Base(PartDatabase db, int count)
        {
            var design = BenchmarkCraft.Lander(db);
            GameSession.NewGame("Benchmark");
            var sys = CelestialSystem.Default;
            var site = sys.Def.launchSite;
            var east = Geo.East(site.UpBF);
            for (int i = 0; i < count; i++)
            {
                var rec = LaunchService.CreateLaunchRecord(design, db, GameSession.Save.ut);
                rec.name = "Base " + (i + 1);
                rec.situation = Situation.Landed;
                rec.launchUT = GameSession.Save.ut;
                var p = new Vector3d(rec.landedPos[0], rec.landedPos[1], rec.landedPos[2]) + east * (i * 16.0);
                rec.landedPos = new[] { p.x, p.y, p.z };
                GameSession.Save.vessels.Add(rec);
            }
            GameSession.Save.activeVesselId = GameSession.Save.vessels[0].id;
            GameSession.Save.scene = "flight";
            GameSession.ResumeFlight();
            yield return WaitFlight();
            yield return new WaitForSeconds(Warmup);
            yield return Measure("base-10", design.parts.Count * count, count);
        }

        private IEnumerator Orbits(PartDatabase db, int count, string name, bool map, bool warp)
        {
            var design = BenchmarkCraft.Lander(db);
            GameSession.NewGame("Benchmark");
            var body = CelestialSystem.Default.HomeBody;
            for (int i = 0; i < count; i++)
            {
                var rec = LaunchService.CreateLaunchRecord(design, db, GameSession.Save.ut);
                double r = body.Radius + 300000 + (i % 20) * 15000;
                double phase = i * 0.61, inc = (i % 8) * 10 * MathD.Deg2Rad;
                var up = new Vector3d(Math.Cos(phase), 0, Math.Sin(phase));
                var dir = Geo.East(up) * Math.Cos(inc) + Geo.North(up) * Math.Sin(inc);
                var pos = up * r;
                var vel = dir * Math.Sqrt(body.GM / r);
                var rot = Quaternion.LookRotation(-(Vector3)up, (Vector3)dir);
                rec.name = "Orbiter " + (i + 1);
                rec.landed = false;
                rec.landedPos = null;
                rec.landedRot = null;
                rec.situation = Situation.Orbiting;
                rec.orbitPos = new[] { pos.x, pos.y, pos.z };
                rec.orbitVel = new[] { vel.x, vel.y, vel.z };
                rec.orbitEpoch = GameSession.Save.ut;
                rec.launchUT = GameSession.Save.ut;
                rec.rotation = new[] { rot.x, rot.y, rot.z, rot.w };
                GameSession.Save.vessels.Add(rec);
            }
            GameSession.Save.activeVesselId = GameSession.Save.vessels[0].id;
            GameSession.Save.scene = "flight";
            GameSession.ResumeFlight();
            yield return WaitFlight();
            var scene = FlightSceneController.Instance;
            if (map) scene.SetMap(true);
            if (warp) scene.Sim.SetRailsWarp(99);
            yield return new WaitForSeconds(2f);
            yield return Measure(name, design.parts.Count * count, count);
            if (map) scene.SetMap(false);
            scene.Sim.StopWarp();
        }

        // ------------------------------------------------------------------ measuring

        private IEnumerator Measure(string scenario, int parts, int vessels)
        {
            var times = new List<float>();
            SimTimers.Reset();
            float start = Time.realtimeSinceStartup;
            yield return null;
            while (Time.realtimeSinceStartup - start < _seconds)
            {
                yield return null;
                times.Add(Time.unscaledDeltaTime * 1000f);
            }
            var row = new Row { Scenario = scenario, Parts = parts, Vessels = vessels, Frames = times.Count, Seconds = Time.realtimeSinceStartup - start };
            if (times.Count > 0)
            {
                var sorted = times.OrderBy(t => t).ToList();
                row.Avg = times.Average();
                row.P50 = sorted[sorted.Count / 2];
                row.P95 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.95))];
                row.P99 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))];
                row.Max = sorted[sorted.Count - 1];
                for (int i = 0; i < row.Steps.Length; i++) row.Steps[i] = SimTimers.Ms((SimStep)i) / times.Count;
            }
            Memory(row);
            _rows.Add(row);
            Debug.Log($"[Benchmark] {scenario}: {row.Frames} frames, {row.Avg:F2} ms average ({(row.Avg > 0 ? 1000 / row.Avg : 0):F0} fps), 99% under {row.P99:F2} ms, {row.WorkingSetMb:F0} MB");
        }

        private static void Memory(Row row)
        {
            var process = Process.GetCurrentProcess();
            process.Refresh();
            // the memory the process holds; where the system does not say, what Unity has reserved plus the managed heap
            row.WorkingSetMb = process.WorkingSet64 > 0 ? process.WorkingSet64 / 1048576.0 : (Profiler.GetTotalReservedMemoryLong() + GC.GetTotalMemory(false)) / 1048576.0;
            row.ManagedMb = GC.GetTotalMemory(false) / 1048576.0;
            row.UnityMb = Profiler.GetTotalAllocatedMemoryLong() / 1048576.0;
        }

        private IEnumerator WaitFlight()
        {
            yield return WaitScene(GameSession.FlightScene, () =>
                FlightSceneController.Instance != null && FlightSceneController.Instance.Sim != null && FlightSceneController.Instance.Sim.ActiveVessel != null);
        }

        private IEnumerator WaitScene(string scene, Func<bool> ready)
        {
            float deadline = Time.realtimeSinceStartup + 180f;
            while (!(SceneManager.GetActiveScene().name == scene && ready()))
            {
                if (Time.realtimeSinceStartup > deadline) throw new TimeoutException($"{scene} did not start in 180 s");
                yield return null;
            }
            for (int i = 0; i < 5; i++) yield return null;
        }

        // ------------------------------------------------------------------ the file

        private void Write()
        {
            var sb = new StringBuilder();
            var inv = CultureInfo.InvariantCulture;
            sb.AppendLine($"# version={Application.version}");
            sb.AppendLine($"# unity={Application.unityVersion} platform={Application.platform}");
            sb.AppendLine($"# machine={SystemInfo.processorType.Trim()} / {SystemInfo.graphicsDeviceName} / {SystemInfo.systemMemorySize} MB");
            sb.AppendLine($"# seconds_per_scenario={_seconds.ToString(inv)}");
            sb.Append("scenario,parts,vessels,frames,seconds,avg_ms,p50_ms,p95_ms,p99_ms,max_ms,avg_fps");
            foreach (SimStep step in Enum.GetValues(typeof(SimStep))) sb.Append(',').Append(Snake(step.ToString())).Append("_ms");
            sb.AppendLine(",working_set_mb,managed_mb,unity_mb");
            foreach (var r in _rows)
            {
                sb.Append(string.Join(",", r.Scenario, r.Parts, r.Vessels, r.Frames, r.Seconds.ToString("F2", inv), r.Avg.ToString("F3", inv), r.P50.ToString("F3", inv),
                    r.P95.ToString("F3", inv), r.P99.ToString("F3", inv), r.Max.ToString("F3", inv), (r.Avg > 0 && r.Frames > 0 ? 1000 / r.Avg : 0).ToString("F1", inv)));
                foreach (var v in r.Steps) sb.Append(',').Append(v.ToString("F3", inv));
                sb.AppendLine($",{r.WorkingSetMb.ToString("F0", inv)},{r.ManagedMb.ToString("F0", inv)},{r.UnityMb.ToString("F0", inv)}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_out)));
            File.WriteAllText(_out, sb.ToString());
        }

        private static string Snake(string pascal) => string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
