# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.4 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = -9.75E-016 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.56 m/s, Δa 55 m, Pe 81.2 km
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
    [AutoTest      147.6] apoapsis 80.0 km reached at 48.5 km: holding it until clear of the air
    [AutoTest      179.2] MECO: Ap 81.5 km, max Q 39.4 kPa, delta-v left 760 m/s vac (760)
    [AutoTest      179.2] CHECK PASS: max dynamic pressure survivable — 39.4 kPa
    [AutoTest      179.2] PHASE coast to space
    [AutoTest      216.9] PHASE circularize
    [AutoTest      216.9] circularization: dv 403.9 m/s, burn 15.6 s, in 95 s
    [AutoTest      303.6] circularization: 404 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      307.6] circularization: 308 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      311.7] circularization: 206 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      315.7] circularization: 100 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      319.7] circularization: 9 m/s to go, 0.1° off, throttle 0.29, spin 0.003 rad/s, SAS Maneuver
    [AutoTest      322.1] circularization: burn complete, residual 0.30 m/s after 18.5 s
    [AutoTest      322.1] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      322.1] in orbit 321 s after launch with 356 m/s vac (356) left
    [AutoTest      322.1] screenshot: orbit_020_orbit.png
    [AutoTest      322.1] PHASE orbit stability
    [AutoTest      382.1] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4142.4] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = -9.75E-016 after 2 orbits
    [AutoTest     4142.4] PHASE orbital EVA
    [AutoTest     4142.4] screenshot: orbit_025_orbital_eva.png
    [AutoTest     4142.4] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.56 m/s, Δa 55 m, Pe 81.2 km
    [AutoTest     4147.6] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4151.6] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4151.6] PHASE deorbit
    [AutoTest     4160.0] PHASE reentry
    [AutoTest     4645.2] screenshot: orbit_031_reentry.png
    [AutoTest     4797.0] arming the parachute at 15.1 km, 383 m/s
    [AutoTest     5118.2] screenshot: orbit_033_touchdown.png
    [AutoTest     5118.2] CHECK PASS: capsule landed safely with crew — Landed at 0.0 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g
