# The Astraea Program automated mission report: persistence
Result: PASSED
Checks: 10/10 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.9 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] quicksave file written: C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
- [x] quickload restores the universe time: UT 341.98: 2.54 s after the saved 339.44, 2.51 s after the reload
- [x] quickload restores position and velocity: Δr 0.000 m, Δv 0.0000 m/s
- [x] quickload restores the vessel: Meridian Orbiter: 6 parts, crew 1
- [x] quickload restores resources: largest difference 0.0001 (Electric)
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
    [AutoTest      187.1] MECO: Ap 81.5 km, max Q 39.9 kPa, delta-v left 694 m/s vac (694)
    [AutoTest      187.1] CHECK PASS: max dynamic pressure survivable — 39.9 kPa
    [AutoTest      187.1] PHASE coast to space
    [AutoTest      228.0] PHASE circularize
    [AutoTest      228.0] circularization: dv 341.6 m/s, burn 13.1 s, in 102 s
    [AutoTest      323.5] circularization: 342 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      327.5] circularization: 244 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      331.6] circularization: 140 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      335.6] circularization: 32 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      339.4] circularization: burn complete, residual 0.29 m/s after 15.9 s
    [AutoTest      339.4] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      339.4] in orbit 338 s after launch with 352 m/s vac (352) left
    [AutoTest      339.4] screenshot: persistence_019_orbit.png
    [AutoTest      339.4] PHASE quicksave
    [AutoTest      339.4] CHECK PASS: quicksave file written — C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
    [AutoTest     1091.5] warped to UT 1091.5; quickloading
    [AutoTest      341.0] PHASE verify quickload
    [AutoTest      342.0] CHECK PASS: quickload restores the universe time — UT 341.98: 2.54 s after the saved 339.44, 2.51 s after the reload
    [AutoTest      342.0] CHECK PASS: quickload restores position and velocity — Δr 0.000 m, Δv 0.0000 m/s
    [AutoTest      342.0] CHECK PASS: quickload restores the vessel — Meridian Orbiter: 6 parts, crew 1
    [AutoTest      342.0] CHECK PASS: quickload restores resources — largest difference 0.0001 (Electric)
    [AutoTest      342.0] CHECK PASS: quickload restores all vessels and milestones — 1 vessels, 3 milestones
    [AutoTest     2224.1] CHECK PASS: orbit continues seamlessly after quickload + warp — Δr 0.00 m after one more orbit
