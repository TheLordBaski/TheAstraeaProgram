# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 6.0 m/s
- [x] max dynamic pressure survivable: 40.8 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = -1.43E-015 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.56 m/s, Δa 53 m, Pe 81.2 km
- [x] jetpack manoeuvre in orbit: moved 7.1 m from the hatch
- [x] crew boarded the vessel in orbit: Ada Kestrel back aboard Meridian Orbiter
- [x] capsule landed safely with crew: Splashed at 1.0 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.2 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.6] CHECK PASS: liftoff — vertical speed 6.0 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 12.9 km, 686 m/s
    [AutoTest      152.3] apoapsis 80.0 km reached at 45.1 km: holding it until clear of the air
    [AutoTest      202.5] MECO: Ap 81.5 km, max Q 40.8 kPa, delta-v left 598 m/s vac (598)
    [AutoTest      202.5] CHECK PASS: max dynamic pressure survivable — 40.8 kPa
    [AutoTest      202.5] PHASE coast to space
    [AutoTest      249.5] PHASE circularize
    [AutoTest      249.5] circularization: dv 256.0 m/s, burn 9.6 s, in 117 s
    [AutoTest      374.1] circularization: burn complete, residual 0.28 m/s after 12.5 s
    [AutoTest      374.1] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      374.1] in orbit 373 s after launch with 342 m/s vac (342) left
    [AutoTest      374.1] screenshot: orbit_015_orbit.png
    [AutoTest      374.1] PHASE orbit stability
    [AutoTest      434.1] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4194.4] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = -1.43E-015 after 2 orbits
    [AutoTest     4194.4] PHASE orbital EVA
    [AutoTest     4194.5] screenshot: orbit_020_orbital_eva.png
    [AutoTest     4194.5] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.56 m/s, Δa 53 m, Pe 81.2 km
    [AutoTest     4199.6] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4203.6] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4203.6] PHASE deorbit
    [AutoTest     4212.0] PHASE reentry
    [AutoTest     4699.6] screenshot: orbit_026_reentry.png
    [AutoTest     4851.9] arming the parachute at 14.8 km, 374 m/s
    [AutoTest     5175.9] screenshot: orbit_028_touchdown.png
    [AutoTest     5175.9] CHECK PASS: capsule landed safely with crew — Splashed at 1.0 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.2 g
