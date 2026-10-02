# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.3 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = 2.78E-016 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.56 m/s, Δa 53 m, Pe 81.2 km
- [x] jetpack manoeuvre in orbit: moved 7.1 m from the hatch
- [x] crew boarded the vessel in orbit: Ada Kestrel back aboard Meridian Orbiter
- [x] capsule landed safely with crew: Landed at 0.0 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.2 km, 686 m/s
    [AutoTest      147.5] apoapsis 80.0 km reached at 48.6 km: holding it until clear of the air
    [AutoTest      178.8] MECO: Ap 81.5 km, max Q 39.3 kPa, delta-v left 763 m/s vac (763)
    [AutoTest      178.8] CHECK PASS: max dynamic pressure survivable — 39.3 kPa
    [AutoTest      178.8] PHASE coast to space
    [AutoTest      216.4] PHASE circularize
    [AutoTest      216.4] circularization: dv 407.2 m/s, burn 15.7 s, in 94 s
    [AutoTest      302.7] circularization: 407 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      306.7] circularization: 312 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      310.7] circularization: 210 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      314.7] circularization: 104 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      318.8] circularization: 10 m/s to go, 0.1° off, throttle 0.32, spin 0.004 rad/s, SAS Maneuver
    [AutoTest      321.3] circularization: burn complete, residual 0.30 m/s after 18.6 s
    [AutoTest      321.3] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      321.3] in orbit 320 s after launch with 356 m/s vac (356) left
    [AutoTest      321.3] screenshot: orbit_020_orbit.png
    [AutoTest      321.3] PHASE orbit stability
    [AutoTest      381.3] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4141.6] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = 2.78E-016 after 2 orbits
    [AutoTest     4141.6] PHASE orbital EVA
    [AutoTest     4141.7] screenshot: orbit_025_orbital_eva.png
    [AutoTest     4141.7] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.56 m/s, Δa 53 m, Pe 81.2 km
    [AutoTest     4146.9] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4150.8] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4150.8] PHASE deorbit
    [AutoTest     4159.2] PHASE reentry
    [AutoTest     4644.5] screenshot: orbit_031_reentry.png
    [AutoTest     4796.2] arming the parachute at 15.1 km, 383 m/s
    [AutoTest     5038.1] screenshot: orbit_033_touchdown.png
    [AutoTest     5038.1] CHECK PASS: capsule landed safely with crew — Landed at 0.0 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g
