# The Astraea Program automated mission report: lunar
Result: FAILED
Checks: 8/9 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 938.4 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 296.9 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [ ] landed on Luma: situation Landed, tilt 29.3 deg, legs intact 4/4, crew 1

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.5 km, 1195 m/s
    [AutoTest      163.1] MECO: Ap 82.0 km, max Q 36.9 kPa, delta-v left 4637 m/s vac (1161 + 3476)
    [AutoTest      163.1] CHECK PASS: max dynamic pressure survivable — 36.9 kPa
    [AutoTest      163.1] PHASE coast to space
    [AutoTest      182.0] PHASE circularize
    [AutoTest      182.0] circularization: dv 751.0 m/s, burn 53.9 s, in 74 s
    [AutoTest      286.0] circularization: burn complete, residual 0.29 m/s after 56.9 s
    [AutoTest      286.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.0] in orbit 284 s after launch with 3885 m/s vac (409 + 3476) left
    [AutoTest      286.0] screenshot: lunar_015_orbit.png
    [AutoTest      286.0] PHASE plan TLI
    [AutoTest      286.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 938.4 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.5] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.5] PHASE TLI burn
    [AutoTest      286.5] TLI: dv 938.4 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1698.2] staging during burn
    [AutoTest     1777.7] TLI: burn complete, residual 0.30 m/s after 103.9 s
    [AutoTest     1777.7] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 296.9 km (planned 40.0 km)
    [AutoTest     1777.7] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.7] PHASE mid-course correction
    [AutoTest     1777.7] correction: pro -18.00 rad 18.00 nrm 3.32 m/s (score 1285)
    [AutoTest     1777.7] MCC: dv 25.7 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3582.0] MCC: burn complete, residual 0.14 m/s after 6.1 s
    [AutoTest     3582.1] PHASE coast to Luma
    [AutoTest    11491.1] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11491.1] Luma periapsis 36.4 km: no correction needed
    [AutoTest    11491.1] PHASE capture burn
    [AutoTest    11491.1] Luma periapsis 36.4 km in 3509 s
    [AutoTest    11491.1] Luma orbit insertion: dv 503.9 m/s, burn 61.8 s, in 3509 s
    [AutoTest    15033.3] Luma orbit insertion: burn complete, residual 0.29 m/s after 64.2 s
    [AutoTest    15033.3] screenshot: lunar_036_luma_orbit.png
    [AutoTest    15033.3] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    15033.3] delta-v left in Luma orbit: 2412 m/s vac (2412)
    [AutoTest    15033.3] PHASE deorbit
    [AutoTest    15036.4] PHASE descent
    [AutoTest    15751.7] suicide burn start at 12352 m AGL, 589 m/s
    [AutoTest    15916.5] final descent from 149 m AGL at 43.2 m/s (vertical -35.5 m/s)
    [AutoTest    15939.4] screenshot: lunar_043_luma_landed.png
    [AutoTest    15939.4] CHECK FAIL: landed on Luma — situation Landed, tilt 29.3 deg, legs intact 4/4, crew 1
    [AutoTest    15939.4] delta-v left on the surface: 1671 m/s vac (1671)
