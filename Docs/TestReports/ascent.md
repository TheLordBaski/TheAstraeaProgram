# The Astraea Program automated mission report: ascent
Result: PASSED
Checks: 4/4 passed

- [x] liftoff: vertical speed 8.7 m/s
- [x] max dynamic pressure survivable: 37.1 kPa
- [x] stable orbit reached: Pe 81.0 km, Ap 82.7 km, e 0.0012
- [x] delta-v left in orbit: 3888 m/s vac (412 + 3476)

## Log
    [AutoTest        1.5] craft: Luma Pathfinder, 51 parts, 80.0 t, delta-v 7643 m/s vac (1332 + 1272 + 1564 + 3476)
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.6] CHECK PASS: liftoff — vertical speed 8.7 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.6] staging at 9.9 km, 522 m/s
    [AutoTest      108.0] staging at 36.9 km, 1195 m/s
    [AutoTest      167.4] MECO: Ap 82.0 km, max Q 37.1 kPa, delta-v left 4579 m/s vac (1103 + 3476)
    [AutoTest      167.4] CHECK PASS: max dynamic pressure survivable — 37.1 kPa
    [AutoTest      167.4] PHASE coast to space
    [AutoTest      186.1] PHASE circularize
    [AutoTest      186.1] circularization: dv 690.2 m/s, burn 49.1 s, in 77 s
    [AutoTest      238.1] circularization: 690 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      242.2] circularization: 641 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      246.3] circularization: 589 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      250.4] circularization: 535 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      254.4] circularization: 482 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      258.4] circularization: 427 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      262.5] circularization: 370 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      266.6] circularization: 313 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      270.6] circularization: 254 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      274.7] circularization: 195 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      278.8] circularization: 134 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      282.8] circularization: 72 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest      286.9] circularization: 11 m/s to go, 0.1° off, throttle 0.62, spin 0.005 rad/s, SAS Maneuver
    [AutoTest      289.9] circularization: burn complete, residual 0.26 m/s after 51.8 s
    [AutoTest      290.1] CHECK PASS: stable orbit reached — Pe 81.0 km, Ap 82.7 km, e 0.0012
    [AutoTest      290.1] in orbit 289 s after launch with 3888 m/s vac (412 + 3476) left
    [AutoTest      290.1] screenshot: ascent_029_orbit.png
    [AutoTest      290.1] CHECK PASS: delta-v left in orbit — 3888 m/s vac (412 + 3476)
