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

The CSV has one row per scenario: frames, average, median, 95th and 99th percentile and longest frame in ms, the average frame rate, the milliseconds a frame
spends in each timed step, and the memory the process holds (`working_set_mb`), the managed heap and Unity's allocations.

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

## Comparing

`tools/ci/compare-benchmark.ps1 -Current results/benchmark.csv -Baseline Docs/benchmarks/windows-batch.csv` prints, for every scenario, the average and 99th
percentile frame time and the memory against the baseline, marks what is more than 10% **slower** (or faster), and checks the budgets. In a GitHub Actions
job it writes the same table to the job summary (`ci.yml` runs it on Linux with `-nographics`, `nightly.yml` on the developer's PC, with and without a window).
With `-Fail` it exits 1 on a regression. Timing noise between two runs of the same build is 10 to 15% on a laptop, so a single mark is a hint, a repeated one
is a regression.

Baselines are in `Docs/benchmarks/`: `windows-batch.csv` (below), and `windows-gpu.csv` and `linux-ci.csv`, which do not exist yet (see "Not measured").

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
