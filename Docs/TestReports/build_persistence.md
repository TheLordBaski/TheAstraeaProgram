# The Astraea Program automated mission report: persistence
Result: PASSED
Checks: 10/10 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.4 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] quicksave file written: C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
- [x] quickload restores the universe time: UT 324.62: 2.52 s after the saved 322.10, 2.51 s after the reload
- [x] quickload restores position and velocity: Δr 0.000 m, Δv 0.0000 m/s
- [x] quickload restores the vessel: Meridian Orbiter: 6 parts, crew 1
- [x] quickload restores resources: largest difference 0.0007 (Electric)
- [x] quickload restores all vessels and milestones: 1 vessels, 3 milestones
- [x] orbit continues seamlessly after quickload + warp: Δr 0.00 m after one more orbit

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.2 km, 686 m/s
    [AutoTest      147.6] apoapsis 80.0 km reached at 48.5 km: holding it until clear of the air
    [AutoTest      179.2] MECO: Ap 81.5 km, max Q 39.4 kPa, delta-v left 760 m/s vac (760)
    [AutoTest      179.2] CHECK PASS: max dynamic pressure survivable — 39.4 kPa
    [AutoTest      179.2] PHASE coast to space
    [AutoTest      216.9] PHASE circularize
    [AutoTest      216.9] circularization: dv 403.9 m/s, burn 15.6 s, in 95 s
    [AutoTest      303.6] circularization: 404 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      307.7] circularization: 308 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      311.7] circularization: 206 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      315.7] circularization: 100 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      319.7] circularization: 9 m/s to go, 0.1° off, throttle 0.29, spin 0.004 rad/s, SAS Maneuver
    [AutoTest      322.1] circularization: burn complete, residual 0.30 m/s after 18.5 s
    [AutoTest      322.1] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      322.1] in orbit 321 s after launch with 356 m/s vac (356) left
    [AutoTest      322.1] screenshot: persistence_020_orbit.png
    [AutoTest      322.1] PHASE quicksave
    [AutoTest      322.1] CHECK PASS: quicksave file written — C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
    [AutoTest     1074.1] warped to UT 1074.1; quickloading
    [AutoTest      323.6] PHASE verify quickload
    [AutoTest      324.6] CHECK PASS: quickload restores the universe time — UT 324.62: 2.52 s after the saved 322.10, 2.51 s after the reload
    [AutoTest      324.6] CHECK PASS: quickload restores position and velocity — Δr 0.000 m, Δv 0.0000 m/s
    [AutoTest      324.6] CHECK PASS: quickload restores the vessel — Meridian Orbiter: 6 parts, crew 1
    [AutoTest      324.6] CHECK PASS: quickload restores resources — largest difference 0.0007 (Electric)
    [AutoTest      324.6] CHECK PASS: quickload restores all vessels and milestones — 1 vessels, 3 milestones
    [AutoTest     2206.8] CHECK PASS: orbit continues seamlessly after quickload + warp — Δr 0.00 m after one more orbit
