# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.9 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = -1.59E-015 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.56 m/s, Δa 51 m, Pe 81.2 km
- [x] jetpack manoeuvre in orbit: moved 7.1 m from the hatch
- [x] crew boarded the vessel in orbit: Ada Kestrel back aboard Meridian Orbiter
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g

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
    [AutoTest      323.5] circularization: 342 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      327.5] circularization: 244 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      331.6] circularization: 140 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      335.6] circularization: 32 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      339.4] circularization: burn complete, residual 0.29 m/s after 15.9 s
    [AutoTest      339.4] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      339.4] in orbit 338 s after launch with 352 m/s vac (352) left
    [AutoTest      339.4] screenshot: orbit_019_orbit.png
    [AutoTest      339.4] PHASE orbit stability
    [AutoTest      399.5] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4159.8] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = -1.59E-015 after 2 orbits
    [AutoTest     4159.8] PHASE orbital EVA
    [AutoTest     4159.8] screenshot: orbit_024_orbital_eva.png
    [AutoTest     4159.8] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.56 m/s, Δa 51 m, Pe 81.2 km
    [AutoTest     4165.0] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4168.9] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4168.9] PHASE deorbit
    [AutoTest     4177.4] PHASE reentry
    [AutoTest     4662.6] screenshot: orbit_030_reentry.png
    [AutoTest     4814.2] arming the parachute at 15.1 km, 383 m/s
    [AutoTest     5140.3] screenshot: orbit_032_touchdown.png
    [AutoTest     5140.3] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g
