# The Astraea Program automated mission report: orbit
Result: PASSED
Checks: 9/9 passed

- [x] liftoff: vertical speed 5.9 m/s
- [x] max dynamic pressure survivable: 40.2 kPa
- [x] stable orbit reached: Pe 81.1 km, Ap 81.5 km, e 0.0003
- [x] physics coast: no drift in semi-major axis: da = 0.000 m after 60 s
- [x] rails warp: orbit preserved: da = 0.000 m, de = -1.37E-015 after 2 orbits
- [x] EVA in orbit: rocketeer inherits the vessel's orbit: Ada Kestrel outside, relative speed 0.56 m/s, Δa 50 m, Pe 81.2 km
- [x] jetpack manoeuvre in orbit: moved 7.1 m from the hatch
- [x] crew boarded the vessel in orbit: Ada Kestrel back aboard Meridian Orbiter
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.6] CHECK PASS: liftoff — vertical speed 5.9 m/s
    [AutoTest        8.3] PHASE pitch-over
    [AutoTest        8.3] pitch-over to 9.0° (TWR 1.92)
    [AutoTest       13.5] PHASE gravity turn
    [AutoTest       58.3] staging at 13.0 km, 686 m/s
    [AutoTest      150.3] apoapsis 80.0 km reached at 46.7 km: holding it until clear of the air
    [AutoTest      191.3] MECO: Ap 81.5 km, max Q 40.2 kPa, delta-v left 665 m/s vac (665)
    [AutoTest      191.3] CHECK PASS: max dynamic pressure survivable — 40.2 kPa
    [AutoTest      191.3] PHASE coast to space
    [AutoTest      233.7] PHASE circularize
    [AutoTest      233.7] circularization: dv 315.0 m/s, burn 12.0 s, in 106 s
    [AutoTest      333.7] circularization: 315 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      337.7] circularization: 216 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      341.8] circularization: 111 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      345.8] circularization: 12 m/s to go, 0.1° off, throttle 0.39, spin 0.005 rad/s, SAS Maneuver
    [AutoTest      348.5] circularization: burn complete, residual 0.25 m/s after 14.8 s
    [AutoTest      348.6] CHECK PASS: stable orbit reached — Pe 81.1 km, Ap 81.5 km, e 0.0003
    [AutoTest      348.6] in orbit 347 s after launch with 350 m/s vac (350) left
    [AutoTest      348.6] screenshot: orbit_019_orbit.png
    [AutoTest      348.6] PHASE orbit stability
    [AutoTest      408.7] CHECK PASS: physics coast: no drift in semi-major axis — da = 0.000 m after 60 s
    [AutoTest     4169.2] CHECK PASS: rails warp: orbit preserved — da = 0.000 m, de = -1.37E-015 after 2 orbits
    [AutoTest     4169.2] PHASE orbital EVA
    [AutoTest     4169.2] screenshot: orbit_024_orbital_eva.png
    [AutoTest     4169.2] CHECK PASS: EVA in orbit: rocketeer inherits the vessel's orbit — Ada Kestrel outside, relative speed 0.56 m/s, Δa 50 m, Pe 81.2 km
    [AutoTest     4174.4] CHECK PASS: jetpack manoeuvre in orbit — moved 7.1 m from the hatch
    [AutoTest     4178.3] CHECK PASS: crew boarded the vessel in orbit — Ada Kestrel back aboard Meridian Orbiter
    [AutoTest     4178.3] PHASE deorbit
    [AutoTest     4186.7] PHASE reentry
    [AutoTest     4672.1] screenshot: orbit_030_reentry.png
    [AutoTest     4823.8] arming the parachute at 15.1 km, 383 m/s
    [AutoTest     5149.9] screenshot: orbit_032_touchdown.png
    [AutoTest     5149.9] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 668 K (HS-125 Ablative Heat Shield), max 6.3 g
