# Performance

## Budgets (FND-13)

| Budget | Limit | Where it is checked |
|---|---|---|
| A launch of 150 parts | 60 fps | GTX 1660 / Ryzen 5 class PC, 1080p (a windowed benchmark run) |
| A launch of 300 parts | 30 fps | the same |
| Main menu to the space center | under 20 s | any run (`boot` + `startup` rows) |
| Memory | under 4 GB | any run (`working_set_mb`) |

A run without a window can only show that the CPU's share of a frame fits in the budget (16.7 ms and 33.3 ms); the GPU's share needs the windowed run.

## Running the benchmark

```
TAP.exe -batchmode -benchmark -benchmark-out results/benchmark.csv            # no window: the CPU's share of each frame
TAP.exe -batchmode -nographics -benchmark                                       # a machine without a GPU (the CI Linux runner)
TAP.exe -benchmark -screen-width 1920 -screen-height 1080 -window-mode windowed # with the window: the real frame rate
```

Options: `-benchmark-scenarios launch-150,map-40` picks scenarios, `-benchmark-seconds 10` sets how long each is measured (3 s of warm-up first),
`-benchmark-out` sets the file (default `benchmark.csv` in the user folder). In the editor the same options are PlayerPrefs, `TAP.AutoTest` = `bench`
and `TAP.benchmark-seconds`, and so on.

| Scenario | What |
|---|---|
| `boot`, `startup` | Seconds from starting the program to the main menu, and from the main menu to the space center (a new game). |
| `launch-50`, `launch-150`, `launch-300` | A stack of that many parts on the pad, engines lit, climbing. |
| `base-10` | Ten landed vessels side by side on the launch complex's plain. |
| `map-40` | Forty vessels in orbit of Tellus, the map open. |
| `warp-max` | Ten vessels in orbit, the active one at the highest time warp (100,000×). |
| `descent` | Terrain streaming: a probe in an 80 km orbit of Tellus, and the camera diving at 30° from 80 km to 100 m above the launch site in the first 60% of the measurement (6 s), then hovering. The dive loses the same fraction of its height every second, so the quadtree must deliver one level about every 0.6 s. |
| `low-pass` | Terrain streaming: the camera flying east from the launch site 1 km above the ground at 2 km/s. |

In `descent` and `low-pass` the camera flies the path alone (the flight camera is switched off) and the probe stays in orbit: a vessel that low and fast
would be braked by the air within seconds. Before the measurement the camera waits at the start of the path (at least the 3 s warm-up) until the ground
under it is drawn at the detail the LOD wants, so the measurement starts from a settled quadtree.

The CSV has one row per scenario: frames, average, median, 95th and 99th percentile and longest frame in ms, the average frame rate, the milliseconds a frame
spends in each timed step, the memory the process holds (`working_set_mb`), the managed heap and Unity's allocations, and the terrain streaming columns below.

### The timed steps

`SimTimers` (TAP.Core) wraps each step of a frame. A scope appears in the Unity profiler as `TAP.<step>` (editor and development builds) and adds to a
stopwatch in every build, which is where the CSV's columns come from.

| Step | What |
|---|---|
| `PreStep` | before the physics step: ground colliders, pad holds, each vessel's forces |
| `PostStep` | after it: each vessel's state, heat, structure, the floating origin |
| `Rails` | moving every vessel along its orbit under time warp |
| `RenderUpdate` | per-frame part visuals and module updates |
| `Planets` | terrain chunks, colliders, atmosphere and sky |
| `Trajectory`, `Map` | path prediction, and the map's lines and markers |
| `Hud` | the flight display's text and widgets |
| `Effects` | exhaust, smoke and sparks |

### Terrain streaming

Every row has these; the last three are measured in `descent` and `low-pass` only (empty elsewhere). Chunks are counted for every body and every kind:
surface chunks, collider chunks and the far view's.

| Column | What |
|---|---|
| `chunks` | Terrain chunks generated during the measurement, on any thread. |
| `worker_ms_per_chunk` | Average time inside `TerrainChunkGenerator.Generate` per chunk (summed with thread-safe counters on the worker threads). |
| `pending_avg`, `pending_max` | `TerrainWorkers.Pending`, the jobs waiting for a worker, sampled every frame. |
| `uploads_per_frame`, `uploads_max` | Chunk meshes filled in `PlanetTerrain.BuildCompleted` per frame; it stops at `MeshBuildBudget` (12) or 6 ms. |
| `complete_s` | Seconds from the flight scene being up to `PlanetTerrain.Complete`: every root chunk facing the camera has its mesh. |
| `max_level_s` | Seconds from the start of the measurement until the chunk under the camera is at `MaxLevel` (13 on Tellus: 115 m chunks, wanted below about 680 m at the default terrain detail). Empty if never: 1 km up, level 12 is the finest the LOD asks for. |
| `detail_lag_s` | The longest time the chunk under the camera stayed coarser than the LOD wants (its four children not all made yet). |
| `gc_alloc_mb` | Managed memory allocated during the measurement: Unity's `GC Allocated In Frame` counter, which only the editor and development builds record (empty in a release player). |
| `gc_collections` | Garbage collections during the measurement (`GC.CollectionCount(0)`). |

No `SimTimers` step was added for them: the mesh uploads already run inside `Planets`, and the chunk generation is on worker threads, off the frame.

## Comparing

`tools/ci/compare-benchmark.ps1 -Current results/benchmark.csv -Baseline Docs/benchmarks/windows-batch.csv` prints, for every scenario, the average and 99th
percentile frame time and the memory against the baseline (for `descent` and `low-pass` also `planets_ms`, `worker_ms_per_chunk` and `detail_lag_s`),
marks what is more than 10% **slower** (or faster), and checks the budgets. An empty cell, or a column an older file does not have, is skipped. In a GitHub Actions
job it writes the same table to the job summary (`ci.yml` runs it on Linux with `-nographics`, `nightly.yml` on the developer's PC, with and without a window).
With `-Fail` it exits 1 on a regression. Timing noise between two runs of the same build is 10 to 15% on a laptop, so a single mark is a hint, a repeated one
is a regression.

Baselines are in `Docs/benchmarks/`: `windows-batch.csv` (the FND-17 run below), and `windows-gpu.csv` and `linux-ci.csv`, which do not exist yet (see
"Not measured").

## Baseline: 2026-10-18, after FND-17 (garbage-free terrain chunks)

The same player set-up as the run below, after FND-17. Terrain and collision chunks now reuse their arrays: workers fill pooled `ChunkData` with
per-thread sample scratch, and a chunk goes back to the pool once its mesh holds the data.

| Scenario | Parts | Vessels | Frames | Average | 99% under | Longest | Pre-step | Post-step | Planets | Memory | Chunks | GCs |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| launch-50 | 50 | 1 | 6,834 | 1.46 ms | 2.56 ms | 22.0 ms | 0.013 | 0.007 | 0.392 | 323 MB | 228 | 13 |
| launch-150 | 150 | 1 | 5,567 | 1.80 ms | 4.41 ms | 19.0 ms | 0.032 | 0.032 | 0.447 | 356 MB | 199 | 11 |
| launch-300 | 300 | 1 | 4,532 | 2.21 ms | 6.84 ms | 65.8 ms | 0.086 | 0.089 | 0.474 | 348 MB | 160 | 16 |
| base-10 | 90 | 10 | 6,072 | 1.65 ms | 3.18 ms | 5.5 ms | 0.026 | 0.014 | 0.420 | 355 MB | 200 | 10 |
| map-40 | 360 | 40 | 5,075 | 1.97 ms | 20.59 ms | 164.7 ms | 0.007 | 0.002 | 0.042 | 367 MB | 0 | 54 |
| warp-max | 90 | 10 | 7,338 | 1.36 ms | 2.12 ms | 5.7 ms | 0.000 | 0.000 | 0.127 | 378 MB | 1,032 | 9 |
| descent | 9 | 1 | 5,746 | 1.74 ms | 3.40 ms | 6.2 ms | 0.005 | 0.001 | 0.353 | 389 MB | 1,452 | 7 |
| low-pass | 9 | 1 | 4,221 | 2.37 ms | 4.47 ms | 6.7 ms | 0.007 | 0.002 | 0.416 | 388 MB | 1,764 | 6 |

Start to the main menu 1.05 s, main menu to the space center 0.08 s. A second run of `descent`, `low-pass` and the pad scenarios gave 13 and 6
collections against 10 to 12 on the pad. In the terrain columns, the worker time per chunk fell from 3.8 to 3.2 ms. The rest is unchanged: the queue,
uploads (0.25 / 8 and 0.42 / 11), MaxLevel at 4.30 s, and lag 0.00 s.

The editor (play mode, same day):

| Editor | Frames | Average | 99% under | Planets | Chunks | Worker per chunk | Uploads a frame (avg / max) | `max_level_s` | `detail_lag_s` | Allocated | GCs |
|---|---|---|---|---|---|---|---|---|---|---|---|
| descent | 492 | 20.3 ms | 28.2 ms | 1.49 | 1,392 | 8.59 ms | 2.83 / 12 | 4.35 | 0.06 | 18.2 MB | 0 |
| low-pass | 409 | 24.4 ms | 32.0 ms | 1.72 | 1,712 | 8.05 ms | 4.17 / 12 | - | 0.00 | 18.8 MB | 0 |
| launch-150 | 391 | 25.5 ms | 48.7 ms | 1.28 | 8 | 6.94 ms | 0.02 / 4 | - | - | 28.3 MB | 0 |

What changed:

- **Collections while streaming:**
  - 89 → 7 in `descent`, 106 → 6 in `low-pass`, 72 → 9 in `warp-max`.
  - The pad scenarios went from 28–31 to 10–16, since they stream chunks too.
  - Streaming now collects no more often than sitting on the pad.
- **Allocations:**
  - The editor's counter fell from 223 to 18 MB in `descent` and from 269 to 19 MB in `low-pass`, less than `launch-150` allocates on the pad.
  - A chunk made and given back allocates nothing: `GC.GetTotalMemory` over 20 chunks with no collection in between shows 0 B per chunk, against
    64 KB per chunk without the pool.
  - What is left per chunk is its quadtree node, mesh object and work item: a few hundred bytes.
- **Spikes:** the longest frame while streaming fell from 15.6 to 6.2 ms (`descent`) and from 9.7 to 6.7 ms (`low-pass`). 99% of frames are now under
  3.4 and 4.5 ms (were 5.0 and 6.4).
- **Unchanged:** `map-40` is still slow: 99% under 20.6 ms, longest 165 ms. It makes no chunks, so the cause is elsewhere (#31).

## Baseline: 2026-10-18, no window, with terrain streaming

Windows player (`-batchmode`), AMD Ryzen 7 5800H, RTX 3070 Laptop, 64 GB, version 0.9.0, 10 s per scenario: the first run with `descent` and
`low-pass` (step 1 of the plan in `TERRAIN_LOD_RESEARCH.md`). As before, the player draws nothing in batch mode, so these are the CPU's share of a frame.

| Scenario | Parts | Vessels | Frames | Average | 99% under | Longest | Pre-step | Post-step | Planets | Memory | Chunks | GCs |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| launch-50 | 50 | 1 | 7,158 | 1.40 ms | 2.40 ms | 25.6 ms | 0.012 | 0.007 | 0.351 | 321 MB | 240 | 30 |
| launch-150 | 150 | 1 | 5,966 | 1.68 ms | 4.31 ms | 19.7 ms | 0.029 | 0.029 | 0.399 | 342 MB | 204 | 29 |
| launch-300 | 300 | 1 | 4,620 | 2.16 ms | 6.86 ms | 68.1 ms | 0.087 | 0.090 | 0.439 | 343 MB | 168 | 31 |
| base-10 | 90 | 10 | 6,256 | 1.60 ms | 3.26 ms | 7.4 ms | 0.024 | 0.014 | 0.385 | 342 MB | 208 | 28 |
| map-40 | 360 | 40 | 4,356 | 2.29 ms | 29.73 ms | 168.6 ms | 0.040 | 0.003 | 0.093 | 373 MB | 0 | 49 |
| warp-max | 90 | 10 | 6,976 | 1.43 ms | 4.50 ms | 10.5 ms | 0.000 | 0.000 | 0.130 | 356 MB | 1,040 | 72 |
| descent | 9 | 1 | 5,518 | 1.81 ms | 5.01 ms | 15.6 ms | 0.007 | 0.001 | 0.349 | 373 MB | 1,452 | 89 |
| low-pass | 9 | 1 | 4,013 | 2.49 ms | 6.44 ms | 9.7 ms | 0.008 | 0.002 | 0.405 | 374 MB | 1,760 | 106 |

Start to the main menu 1.6 s, main menu to the space center 0.13 s. The terrain columns of the two new scenarios:

| Scenario | Worker per chunk | Queue (avg / max) | Uploads a frame (avg / max) | `complete_s` | `max_level_s` | `detail_lag_s` |
|---|---|---|---|---|---|---|
| descent | 3.91 ms | 0.1 / 10 | 0.26 / 12 | 0.01 | 4.31 | 0.01 |
| low-pass | 3.78 ms | 0.0 / 10 | 0.44 / 11 | 0.02 | - | 0.00 |

A second run of the two gave 1.74 and 2.65 ms average, 3.74 and 3.73 ms per chunk, MaxLevel at 4.30 s and the same lag. `gc_alloc_mb` is empty in the
release player; the editor (play mode, same machine, same day) measured it:

| Editor | Frames | Average | 99% under | Planets | Chunks | Worker per chunk | Uploads a frame (avg / max) | `max_level_s` | `detail_lag_s` | Allocated | GCs |
|---|---|---|---|---|---|---|---|---|---|---|---|
| descent | 462 | 21.6 ms | 36.3 ms | 1.58 | 1,392 | 9.21 ms | 3.01 / 12 | 4.37 | 0.05 | 223 MB | 2 |
| low-pass | 390 | 25.6 ms | 37.7 ms | 1.81 | 1,712 | 8.67 ms | 4.39 / 12 | - | 0.00 | 269 MB | 2 |

What the numbers say:

- **The streaming keeps up on both paths.** On the dive the chunk under the camera reaches MaxLevel 4.3 s in, about when the dive gets low enough to
  want it (680 m up, 4.3 s into this path), and it is never more than a frame or two coarser than the LOD wants. The proposed budgets of the research
  (terrain under 2 ms of a main-thread frame, full detail under the camera within 1 s) hold: 0.35 to 0.41 ms and 0.01 s.
- **The workers are mostly idle.** Six workers at 3.8 ms a chunk can make about 1,500 chunks a second; the paths ask for 145 (descent) and 176
  (low pass), and the queue is nearly always empty. The first-in-first-out order (gap 2) costs nothing on these paths yet.
- **Uploads come in bursts.** On average a fraction of a chunk a frame at 400 to 550 frames a second (about 3 a frame at 60 fps), but bursts hit the
  cap of 12 a frame.
- **The garbage is the cost that shows.** The release player's managed heap is small (about 20 MB), so Mono collects 89 to 106 times in 10 s while
  streaming, against 28 to 31 on the pad; every collection stops the main thread. The editor's allocation counter gives 22 to 27 MB a second while
  streaming, at most 160 KB per chunk made (the counter also has everything else a frame allocates). This is gap 4.
- **`complete_s` is near zero** because `Complete` only covers the six root chunks, which are ready within the frames the runner waits after the scene
  loads. `max_level_s` and `detail_lag_s` are the numbers that say how the ground under the camera does.
- The editor is about 2.3 times slower per chunk than the player and draws its game view, so its frame times are not comparable with the table above.

Against the 2026-10-12 run, `map-40` (0.76 to 2.29 ms average, 99% under 1.7 to 29.7 ms), `warp-max` (0.75 to 1.43 ms), `launch-50`, `launch-150` and
`base-10` (11 to 21%) are slower, and the start to the main menu went from 1.0 to 1.6 s. The home system has grown to 22 bodies since; this was not
investigated here.

## Baseline: 2026-10-12, no window

Windows player (`-batchmode`), AMD Ryzen 7 5800H, RTX 3070 Laptop, 64 GB, version 0.9.0, 10 s per scenario. The player draws nothing in batch mode, so these are the
CPU's share of a frame (simulation, planets, UI), not the frame rate.

| Scenario | Parts | Vessels | Frames | Average | 99% under | Longest | Pre-step | Post-step | Planets | Memory |
|---|---|---|---|---|---|---|---|---|---|---|
| launch-50 | 50 | 1 | 8,508 | 1.17 ms | 2.53 ms | 82.6 ms | 0.011 | 0.007 | 0.427 | 329 MB |
| launch-150 | 150 | 1 | 6,648 | 1.50 ms | 4.24 ms | 84.7 ms | 0.029 | 0.029 | 0.479 | 334 MB |
| launch-300 | 300 | 1 | 4,234 | 2.36 ms | 8.95 ms | 75.7 ms | 0.123 | 0.127 | 0.592 | 361 MB |
| base-10 | 90 | 10 | 7,551 | 1.32 ms | 3.07 ms | 48.1 ms | 0.022 | 0.013 | 0.448 | 364 MB |
| map-40 | 360 | 40 | 13,108 | 0.76 ms | 1.67 ms | 48.6 ms | 0.003 | 0.001 | 0.013 | 370 MB |
| warp-max | 90 | 10 | 13,337 | 0.75 ms | 1.71 ms | 78.5 ms | 0.000 | 0.000 | 0.089 | 380 MB |

(Step columns are milliseconds per frame; the map's own step is 0.23 ms in `map-40`, rails 0.07 ms in `warp-max`.) Start to the main menu 1.0 s, main menu to the
space center 0.1 s. The longest frames (50 to 80 ms) are the first frames after a scene loads.

What the numbers say: the CPU's share is 1 to 2.4 ms for 300 parts, a tenth of a 16.7 ms frame; the planets (terrain chunks and sky) cost more per frame than
all 300 parts' physics; 40 vessels in the map cost less than one on the pad. Memory is 330 to 380 MB, a tenth of the budget.

### Not measured

- **The frame rate with a GPU.** Windowed runs launched from the automation shell reached only 5 fps at any setting (even `-safe-mode`), which says the shell's
  process is throttled, not what the game does (the editor's game view, 900×460, runs the same scenarios at 90 to 120 fps). Run the third command above on a
  desktop session, on the development PC and on the reference PC, and keep the CSV as `windows-gpu.csv`.
- **The reference PC** (GTX 1660 / Ryzen 5) and the Linux runner: no numbers yet.
