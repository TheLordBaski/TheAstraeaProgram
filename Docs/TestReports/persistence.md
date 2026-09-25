# The Astraea Program automated mission report: persistence
Result: PASSED
Checks: 10/10 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.9 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] quicksave file written: C:/Users/jakub/AppData/LocalLow/Starwright Works/Starwright\Starwright\Saves\AutoTest\quicksave.json
- [x] quickload restores the universe time: UT 340.28: 2.54 s after the saved 337.74, 2.53 s after the reload
- [x] quickload restores position and velocity: Δr 0.000 m, Δv 0.0000 m/s
- [x] quickload restores the vessel: Meridian Orbiter: 6 parts, crew 1
- [x] quickload restores resources: largest difference 0.0006 (Electric)
- [x] quickload restores all vessels and milestones: 1 vessels, 3 milestones
- [x] orbit continues seamlessly after quickload + warp: Δr 0.00 m after one more orbit

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.1 km, 686 m/s
    [AutoTest      149.4] apoapsis 80.0 km reached at 47.4 km: holding it until clear of the air
    [AutoTest      186.4] MECO: Ap 81.5 km, max Q 39.9 kPa, delta-v left 699 m/s vac (699)
    [AutoTest      186.4] CHECK PASS: max dynamic pressure survivable — 39.9 kPa
    [AutoTest      186.4] PHASE coast to space
    [AutoTest      226.9] PHASE circularize
    [AutoTest      226.9] circularization: dv 347.1 m/s, burn 13.3 s, in 101 s
    [AutoTest      337.7] circularization: burn complete, residual 0.28 m/s after 16.1 s
    [AutoTest      337.7] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      337.7] in orbit 336 s after launch with 353 m/s vac (353) left
    [AutoTest      337.7] screenshot: persistence_015_orbit.png
    [AutoTest      337.7] PHASE quicksave
    [AutoTest      337.7] CHECK PASS: quicksave file written — C:/Users/jakub/AppData/LocalLow/Starwright Works/Starwright\Starwright\Saves\AutoTest\quicksave.json
    [AutoTest     1089.8] warped to UT 1089.8; quickloading
    [AutoTest      339.3] PHASE verify quickload
    [AutoTest      340.3] CHECK PASS: quickload restores the universe time — UT 340.28: 2.54 s after the saved 337.74, 2.53 s after the reload
    [AutoTest      340.3] CHECK PASS: quickload restores position and velocity — Δr 0.000 m, Δv 0.0000 m/s
    [AutoTest      340.3] CHECK PASS: quickload restores the vessel — Meridian Orbiter: 6 parts, crew 1
    [AutoTest      340.3] CHECK PASS: quickload restores resources — largest difference 0.0006 (Electric)
    [AutoTest      340.3] CHECK PASS: quickload restores all vessels and milestones — 1 vessels, 3 milestones
    [AutoTest     2222.5] CHECK PASS: orbit continues seamlessly after quickload + warp — Δr 0.00 m after one more orbit
