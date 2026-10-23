using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TAP.Construction;
using TAP.Core;
using TAP.Game;
using TAP.Parts;
using TAP.Persistence;
using TAP.Simulation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace TAP.App
{
    /// <summary>
    /// <c>-benchmark</c> (FND-13): times the game in the situations that cost most and writes one CSV row for each. Launches of 50, 150 and 300
    /// parts, a landed base of ten vessels, the map with forty vessels, maximum time warp, the time from the main menu to the hub, and the
    /// terrain streaming in under a camera diving to the ground and flying low and fast.
    /// <c>-benchmark-out &lt;file&gt;</c> sets the file, <c>-benchmark-scenarios a,b</c> picks some, <c>-benchmark-seconds n</c> sets the length of
    /// each measurement. Run it with <c>-batchmode</c> (add <c>-nographics</c> on a machine without a GPU: the numbers are then the CPU's alone).
    /// </summary>
    public sealed class BenchmarkRunner : MonoBehaviour
    {
        public static readonly string[] AllScenarios = { "startup", "launch-50", "launch-150", "launch-300", "base-10", "map-40", "warp-max", "descent", "low-pass", "chunks" };

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
            // terrain streaming: chunks made by the workers and their time, the work queue, mesh uploads, garbage
            public long Chunks;
            public double WorkerMs, PendingAvg, UploadsAvg, GcAllocMb = double.NaN;
            public int PendingMax, UploadsMax, GcCollections;
            // seconds from the flight scene to PlanetTerrain.Complete, and from the start of the measurement to the
            // chunk under the camera at MaxLevel; the longest time the ground under the camera stayed coarser than the LOD wants
            public double CompleteS = double.NaN, MaxLevelS = double.NaN, DetailLagS = double.NaN;
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
                case "descent":
                case "low-pass": return Terrain(db, name);
                case "chunks": return Chunks();
                default: throw new ArgumentException("Unknown scenario " + name);
            }
        }

        // ------------------------------------------------------------------ scenarios

        /// <summary>
        /// Building terrain chunks alone (FND-19): chunks of the home planet near the launch site, made one after another on this
        /// thread, in turn with the Burst kernels and with the classes. Short rounds alternate, and each way's fastest round counts, so
        /// other programs' load hardly shows. Two rows, chunks (Burst) and chunks-managed, with the time per chunk in worker_ms_per_chunk.
        /// </summary>
        private IEnumerator Chunks()
        {
            var sys = CelestialSystem.Default;
            var body = sys.HomeBody;
            CubeSphere.FromSphere(sys.Def.launchSite.UpBF, out int face, out double u0, out double v0);
            bool was = LayeredTerrain.UseBurst;
            int seq = 0;
            double Round(bool burst, int n)
            {
                LayeredTerrain.UseBurst = burst;
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < n; i++, seq++)
                    TerrainChunkGenerator.Release(TerrainChunkGenerator.Generate(body.Terrain, body.Radius, face, u0 + (seq % 40) * 0.002, v0 + (seq % 7) * 0.003,
                        1.0 / 512, 32, body.HasOcean, true));
                return sw.Elapsed.TotalMilliseconds / n;
            }
            Round(true, 4);
            Round(false, 2);
            double burstBest = double.MaxValue, managedBest = double.MaxValue;
            int rounds = 0;
            float end = Time.realtimeSinceStartup + _seconds;
            while (Time.realtimeSinceStartup < end)
            {
                managedBest = Math.Min(managedBest, Round(false, 4));
                burstBest = Math.Min(burstBest, Round(true, 12));
                rounds++;
                yield return null;
            }
            LayeredTerrain.UseBurst = was;
            _rows.Add(new Row { Scenario = "chunks", Seconds = _seconds, Chunks = rounds * 12, WorkerMs = burstBest });
            _rows.Add(new Row { Scenario = "chunks-managed", Seconds = _seconds, Chunks = rounds * 4, WorkerMs = managedBest });
            Debug.Log($"[Benchmark] chunks: Burst {burstBest:F2} ms, managed {managedBest:F2} ms a chunk (best of {rounds} rounds), x{managedBest / burstBest:F1}");
        }

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
                double phase = i * 0.61;
                AddOrbiter(design, db, body, new Vector3d(Math.Cos(phase), 0, Math.Sin(phase)), body.Radius + 300000 + (i % 20) * 15000, (i % 8) * 10, "Orbiter " + (i + 1));
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

        /// <summary>Adds a vessel in a circular, prograde orbit to the save: over <paramref name="up"/> (inertial), radius <paramref name="r"/>.</summary>
        private static void AddOrbiter(CraftDesign design, PartDatabase db, CelestialBody body, Vector3d up, double r, double inclinationDeg, string name)
        {
            var rec = LaunchService.CreateLaunchRecord(design, db, GameSession.Save.ut);
            double inc = inclinationDeg * MathD.Deg2Rad;
            var dir = Geo.East(up) * Math.Cos(inc) + Geo.North(up) * Math.Sin(inc);
            var pos = up * r;
            var vel = dir * Math.Sqrt(body.GM / r);
            var rot = Quaternion.LookRotation(-(Vector3)up, (Vector3)dir);
            rec.name = name;
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

        /// <summary>
        /// Terrain streaming over the launch site: a probe in an 80 km orbit of Tellus and the camera flying free on a set path, so the
        /// ground streams in under it as it would for a player. <c>descent</c> dives at 30 degrees from 80 km to 100 m above the ground in
        /// the first 60% of the measurement, losing the same fraction of its height every second (a quadtree level about every 0.6 s
        /// with 10 s), then hovers. <c>low-pass</c> flies east 1 km above the ground at 2 km/s. A vessel that low and fast would be
        /// braked by the air within seconds, so the camera flies the path alone. Before the measurement the camera waits at the
        /// start of the path until the ground under it is drawn at the detail the LOD wants.
        /// </summary>
        private IEnumerator Terrain(PartDatabase db, string name)
        {
            const double DiveFrom = 80000, DiveTo = 100, Low = 1000, Fast = 2000;
            var design = BenchmarkCraft.Lander(db);
            GameSession.NewGame("Benchmark");
            var sys = CelestialSystem.Default;
            var body = sys.HomeBody;
            Vector3d site = sys.Def.launchSite.UpBF, east = Geo.East(site);
            bool descent = name == "descent";
            double dive = _seconds * 0.6;

            // body-fixed, t seconds in: the direction under the camera and its height above the ground
            (Vector3d dir, double height) Track(double t)
            {
                double h = descent ? DiveFrom * Math.Pow(DiveTo / DiveFrom, Math.Min(t, dive) / dive) : Low;
                double x = descent ? -(h - DiveTo) * Math.Sqrt(3) : Fast * t; // along the ground; the 30 degree dive ends over the site
                double a = x / body.Radius;
                return (site * Math.Cos(a) + east * Math.Sin(a), h);
            }
            // body-fixed: the point on the path, or (above >= 0) that many metres above the ground under it
            Vector3d Point(double t, double above)
            {
                var (dir, h) = Track(t);
                double ground = body.Terrain.Height(dir);
                if (body.HasOcean && ground < 0) ground = 0;
                return dir * (body.Radius + ground + (above < 0 ? h : above));
            }

            AddOrbiter(design, db, body, body.RotationAtUT(GameSession.Save.ut) * Track(0).dir, body.Radius + DiveFrom, 0, "Benchmark probe");
            GameSession.Save.activeVesselId = GameSession.Save.vessels[0].id;
            GameSession.Save.scene = "flight";
            GameSession.ResumeFlight();
            yield return WaitFlight();
            // the terrain detail scale (the Ultra setting is 3 / 2.2 = 1.36): more chunks, to fill the queue (FND-20)
            if (float.TryParse(Arg("-benchmark-terrain-detail"), NumberStyles.Float, CultureInfo.InvariantCulture, out float detail)) PlanetTerrain.DetailScale = detail;
            float ready = Time.realtimeSinceStartup;
            var scene = FlightSceneController.Instance;
            if (!scene.Sim.Planets.Terrains.TryGetValue(body, out var terrain)) throw new InvalidOperationException(body.Name + " has no terrain");
            scene.Camera.enabled = false; // the camera flies the path instead of following the probe
            var cam = scene.Camera.Cam.transform;
            void Fly(float t)
            {
                var sim = scene.Sim;
                double rut = sim.RenderUT;
                var rot = body.RotationAtUT(rut);
                Vector3d centre = sim.Frame.BodyPosition(body, rut) - sim.Frame.RenderOrigin(sim.RenderAlpha);
                var pos = (Vector3)(centre + rot * Point(t, -1));
                // looking at the ground two seconds ahead, and a little beyond so a hover never looks straight down
                var ahead = (Vector3)(centre + rot * (Point(t + 2, 0) + east * 500));
                cam.SetPositionAndRotation(pos, Quaternion.LookRotation(ahead - pos, (Vector3)(rot * Track(t).dir)));
            }

            double complete = double.NaN;
            while (true)
            {
                float waited = Time.realtimeSinceStartup - ready;
                if (double.IsNaN(complete) && terrain.Complete) complete = waited;
                if (waited >= Warmup && terrain.Complete && terrain.LevelUnderCamera >= 0 && !terrain.DetailBehind) break;
                if (waited > 120f) { Debug.LogWarning($"[Benchmark] {name}: the ground under the camera is not drawn after 120 s"); break; }
                Fly(0);
                yield return null;
            }
            yield return Measure(name, design.parts.Count, 1, Fly, terrain, complete);
            scene.Camera.enabled = true;
        }

        // ------------------------------------------------------------------ measuring

        /// <summary>
        /// Measures the frames for <see cref="_seconds"/>. <paramref name="fly"/> is called every frame with the seconds since the start (it
        /// moves the camera); with <paramref name="terrain"/> the row also gets the time to MaxLevel under the camera and the detail lag.
        /// </summary>
        private IEnumerator Measure(string scenario, int parts, int vessels, Action<float> fly = null, PlanetTerrain terrain = null, double completeS = double.NaN)
        {
            var times = new List<float>();
            SimTimers.Reset();
            long chunks = Interlocked.Read(ref TerrainChunkGenerator.Generated), ticks = Interlocked.Read(ref TerrainChunkGenerator.GenerateTicks);
            long uploads = PlanetTerrain.MeshUploads, lastUploads = uploads, pendingSum = 0, dropped = TerrainWorkers.Dropped;
            int gc = GC.CollectionCount(0), pendingMax = 0, uploadsMax = 0;
            // per frame in the editor and development builds; a release player does not record it (the column stays empty)
            using var allocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            double allocBytes = 0, maxLevelS = double.NaN, lag = 0, behindSince = -1;
            float start = Time.realtimeSinceStartup;
            fly?.Invoke(0);
            yield return null;
            while (Time.realtimeSinceStartup - start < _seconds)
            {
                yield return null;
                float t = Time.realtimeSinceStartup - start;
                times.Add(Time.unscaledDeltaTime * 1000f);
                fly?.Invoke(t);
                int pending = TerrainWorkers.Pending;
                pendingSum += pending;
                pendingMax = Math.Max(pendingMax, pending);
                uploadsMax = Math.Max(uploadsMax, (int)(PlanetTerrain.MeshUploads - lastUploads));
                lastUploads = PlanetTerrain.MeshUploads;
                if (allocated.Valid) allocBytes += allocated.LastValue;
                if (terrain != null)
                {
                    if (double.IsNaN(maxLevelS) && terrain.LevelUnderCamera == terrain.MaxLevel) maxLevelS = t;
                    if (terrain.LevelUnderCamera >= 0 && !terrain.DetailBehind) behindSince = -1;
                    else if (behindSince < 0) behindSince = t;
                    if (behindSince >= 0) lag = Math.Max(lag, t - behindSince);
                }
            }
            var row = new Row { Scenario = scenario, Parts = parts, Vessels = vessels, Frames = times.Count, Seconds = Time.realtimeSinceStartup - start };
            row.Chunks = Interlocked.Read(ref TerrainChunkGenerator.Generated) - chunks;
            if (row.Chunks > 0) row.WorkerMs = (Interlocked.Read(ref TerrainChunkGenerator.GenerateTicks) - ticks) * 1000.0 / Stopwatch.Frequency / row.Chunks;
            row.PendingMax = pendingMax;
            row.UploadsMax = uploadsMax;
            row.GcCollections = GC.CollectionCount(0) - gc;
            if (allocated.Valid) row.GcAllocMb = allocBytes / 1048576.0;
            if (terrain != null) { row.CompleteS = completeS; row.MaxLevelS = maxLevelS; row.DetailLagS = lag; }
            if (times.Count > 0)
            {
                var sorted = times.OrderBy(t => t).ToList();
                row.Avg = times.Average();
                row.P50 = sorted[sorted.Count / 2];
                row.P95 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.95))];
                row.P99 = sorted[Math.Min(sorted.Count - 1, (int)(sorted.Count * 0.99))];
                row.Max = sorted[sorted.Count - 1];
                for (int i = 0; i < row.Steps.Length; i++) row.Steps[i] = SimTimers.Ms((SimStep)i) / times.Count;
                row.PendingAvg = (double)pendingSum / times.Count;
                row.UploadsAvg = (double)(PlanetTerrain.MeshUploads - uploads) / times.Count;
            }
            Memory(row);
            _rows.Add(row);
            Debug.Log($"[Benchmark] {scenario}: {row.Frames} frames, {row.Avg:F2} ms average ({(row.Avg > 0 ? 1000 / row.Avg : 0):F0} fps), 99% under {row.P99:F2} ms, {row.WorkingSetMb:F0} MB; " +
                $"{row.Chunks} chunks at {row.WorkerMs:F2} ms, queue {row.PendingAvg:F0} (max {row.PendingMax}), {row.UploadsAvg:F2} uploads a frame (max {row.UploadsMax}), " +
                $"{TerrainWorkers.Dropped - dropped} requests dropped, {TerrainWorkers.Count} workers{(TerrainWorkers.Fifo ? " first in first out" : "")}, lag {row.DetailLagS:F2} s, {row.GcCollections} GCs");
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
            sb.AppendLine(",working_set_mb,managed_mb,unity_mb,chunks,worker_ms_per_chunk,pending_avg,pending_max,uploads_per_frame,uploads_max,complete_s,max_level_s,detail_lag_s,gc_alloc_mb,gc_collections");
            foreach (var r in _rows)
            {
                sb.Append(string.Join(",", r.Scenario, r.Parts, r.Vessels, r.Frames, r.Seconds.ToString("F2", inv), r.Avg.ToString("F3", inv), r.P50.ToString("F3", inv),
                    r.P95.ToString("F3", inv), r.P99.ToString("F3", inv), r.Max.ToString("F3", inv), (r.Avg > 0 && r.Frames > 0 ? 1000 / r.Avg : 0).ToString("F1", inv)));
                foreach (var v in r.Steps) sb.Append(',').Append(v.ToString("F3", inv));
                sb.Append($",{r.WorkingSetMb.ToString("F0", inv)},{r.ManagedMb.ToString("F0", inv)},{r.UnityMb.ToString("F0", inv)}");
                sb.AppendLine(string.Join(",", "", r.Chunks, r.WorkerMs.ToString("F3", inv), r.PendingAvg.ToString("F1", inv), r.PendingMax, r.UploadsAvg.ToString("F2", inv), r.UploadsMax,
                    Opt(r.CompleteS), Opt(r.MaxLevelS), Opt(r.DetailLagS), Opt(r.GcAllocMb), r.GcCollections));
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_out)));
            File.WriteAllText(_out, sb.ToString());
        }

        /// <summary>A number, or an empty cell when it was not measured (NaN).</summary>
        private static string Opt(double v) => double.IsNaN(v) ? "" : v.ToString("F2", CultureInfo.InvariantCulture);

        private static string Snake(string pascal) => string.Concat(pascal.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));
    }
}
