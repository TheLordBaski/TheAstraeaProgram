# The Astraea Program automated mission report: persistence
Result: PASSED
Checks: 10/10 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.9 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] quicksave file written: C:/Users/jakub/AppData/LocalLow/Starwright Works/Starwright\Starwright\Saves\AutoTest\quicksave.json
- [x] quickload restores the universe time: UT 342.22: 2.54 s after the saved 339.68, 2.52 s after the reload
- [x] quickload restores position and velocity: Δr 0.000 m, Δv 0.0000 m/s
- [x] quickload restores the vessel: Meridian Orbiter: 6 parts, crew 1
- [x] quickload restores resources: largest difference 0.0012 (Electric)
- [x] quickload restores all vessels and milestones: 1 vessels, 3 milestones
- [x] orbit continues seamlessly after quickload + warp: Δr 0.00 m after one more orbit

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.1 km, 686 m/s
    [AutoTest      149.6] apoapsis 80.0 km reached at 47.3 km: holding it until clear of the air
    [AutoTest      187.2] MECO: Ap 81.5 km, max Q 39.9 kPa, delta-v left 693 m/s vac (693)
    [AutoTest      187.2] CHECK PASS: max dynamic pressure survivable — 39.9 kPa
    [AutoTest      187.2] PHASE coast to space
    [AutoTest      228.1] PHASE circularize
    [AutoTest      228.1] circularization: dv 341.1 m/s, burn 13.0 s, in 102 s
    [AutoTest      339.7] circularization: burn complete, residual 0.30 m/s after 15.9 s
    [AutoTest      339.7] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      339.7] in orbit 338 s after launch with 352 m/s vac (352) left
    [AutoTest      339.7] screenshot: persistence_015_orbit.png
    [AutoTest      339.7] PHASE quicksave
    [AutoTest      339.7] CHECK PASS: quicksave file written — C:/Users/jakub/AppData/LocalLow/Starwright Works/Starwright\Starwright\Saves\AutoTest\quicksave.json
    [AutoTest     1091.7] warped to UT 1091.7; quickloading
    [AutoTest      341.2] PHASE verify quickload
    [AutoTest      342.2] CHECK PASS: quickload restores the universe time — UT 342.22: 2.54 s after the saved 339.68, 2.52 s after the reload
    [AutoTest      342.2] CHECK PASS: quickload restores position and velocity — Δr 0.000 m, Δv 0.0000 m/s
    [AutoTest      342.2] CHECK PASS: quickload restores the vessel — Meridian Orbiter: 6 parts, crew 1
    [AutoTest      342.2] CHECK PASS: quickload restores resources — largest difference 0.0012 (Electric)
    [AutoTest      342.2] CHECK PASS: quickload restores all vessels and milestones — 1 vessels, 3 milestones
    [AutoTest     2224.4] CHECK PASS: orbit continues seamlessly after quickload + warp — Δr 0.00 m after one more orbit
