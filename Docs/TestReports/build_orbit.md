# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 39.8 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = -6.94E-017 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.57 m/s, Δa 55 m, Pe 81.2 km
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
    [AutoTest      149.2] apoapsis 80.0 km reached at 47.5 km: holding it until clear of the air
    [AutoTest      185.6] MECO: Ap 81.5 km, max Q 39.8 kPa, delta-v left 705 m/s vac (705)
    [AutoTest      185.6] CHECK PASS: max dynamic pressure survivable — 39.8 kPa
    [AutoTest      185.6] PHASE coast to space
    [AutoTest      225.8] PHASE circularize
    [AutoTest      225.8] circularization: dv 352.4 m/s, burn 13.5 s, in 101 s
    [AutoTest      336.0] circularization: burn complete, residual 0.30 m/s after 16.3 s
    [AutoTest      336.0] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      336.0] in orbit 335 s after launch with 353 m/s vac (353) left
    [AutoTest      336.0] screenshot: orbit_015_orbit.png
    [AutoTest      336.0] PHASE orbit stability
    [AutoTest      396.1] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4156.4] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = -6.94E-017 after 2 orbits
    [AutoTest     4156.4] PHASE orbital EVA
    [AutoTest     4156.4] screenshot: orbit_020_orbital_eva.png
    [AutoTest     4156.4] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.57 m/s, Δa 55 m, Pe 81.2 km
    [AutoTest     4161.6] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4165.5] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4165.5] PHASE deorbit
    [AutoTest     4173.9] PHASE reentry
    [AutoTest     4659.1] screenshot: orbit_026_reentry.png
    [AutoTest     4810.8] arming the parachute at 15.1 km, 383 m/s
    [AutoTest     5137.0] screenshot: orbit_028_touchdown.png
    [AutoTest     5137.0] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g
