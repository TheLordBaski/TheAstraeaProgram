# The Astraea Program automated mission report: persistence
Result: PASSED
Checks: 10/10 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.3 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] quicksave file written: C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
- [x] quickload restores the universe time: UT 323.34: 2.52 s after the saved 320.82, 2.50 s after the reload
- [x] quickload restores position and velocity: Δr 0.000 m, Δv 0.0000 m/s
- [x] quickload restores the vessel: Meridian Orbiter: 6 parts, crew 1
- [x] quickload restores resources: largest difference 0.0005 (Electric)
- [x] quickload restores all vessels and milestones: 1 vessels, 3 milestones
- [x] orbit continues seamlessly after quickload + warp: Δr 0.00 m after one more orbit

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.2 km, 686 m/s
    [AutoTest      147.5] apoapsis 80.0 km reached at 48.6 km: holding it until clear of the air
    [AutoTest      178.6] MECO: Ap 81.5 km, max Q 39.3 kPa, delta-v left 765 m/s vac (765)
    [AutoTest      178.6] CHECK PASS: max dynamic pressure survivable — 39.3 kPa
    [AutoTest      178.6] PHASE coast to space
    [AutoTest      216.1] PHASE circularize
    [AutoTest      216.1] circularization: dv 409.2 m/s, burn 15.8 s, in 94 s
    [AutoTest      302.1] circularization: 409 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      306.2] circularization: 314 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      310.2] circularization: 212 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      314.2] circularization: 106 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      318.2] circularization: 10 m/s to go, 0.1° off, throttle 0.35, spin 0.003 rad/s, SAS Maneuver
    [AutoTest      320.8] circularization: burn complete, residual 0.29 m/s after 18.7 s
    [AutoTest      320.8] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      320.8] in orbit 319 s after launch with 356 m/s vac (356) left
    [AutoTest      320.8] screenshot: persistence_020_orbit.png
    [AutoTest      320.8] PHASE quicksave
    [AutoTest      320.8] CHECK PASS: quicksave file written — C:/Users/jakub/AppData/LocalLow/Astraea Works/The Astraea Program\TAP\Saves\AutoTest\quicksave.json
    [AutoTest     1072.9] warped to UT 1072.9; quickloading
    [AutoTest      322.3] PHASE verify quickload
    [AutoTest      323.3] CHECK PASS: quickload restores the universe time — UT 323.34: 2.52 s after the saved 320.82, 2.50 s after the reload
    [AutoTest      323.3] CHECK PASS: quickload restores position and velocity — Δr 0.000 m, Δv 0.0000 m/s
    [AutoTest      323.3] CHECK PASS: quickload restores the vessel — Meridian Orbiter: 6 parts, crew 1
    [AutoTest      323.3] CHECK PASS: quickload restores resources — largest difference 0.0005 (Electric)
    [AutoTest      323.3] CHECK PASS: quickload restores all vessels and milestones — 1 vessels, 3 milestones
    [AutoTest     2205.5] CHECK PASS: orbit continues seamlessly after quickload + warp — Δr 0.00 m after one more orbit
