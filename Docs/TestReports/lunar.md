# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.9 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 939.1 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 297.6 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 2.4 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.40 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 6.9 s (AGL 0.7 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2270, 2017), speed 1.83 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2270, 2017), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.64 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 299.6 m/s in 1198 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 32.6 km
- [x] capsule landed safely with crew: Landed at 0.1 m/s, crew 1, max skin 1011 K (HS-125 Ablative Heat Shield), max 7.8 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.6] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.5 km, 1195 m/s
    [AutoTest      163.2] MECO: Ap 82.0 km, max Q 36.9 kPa, delta-v left 4637 m/s vac (1162 + 3476)
    [AutoTest      163.2] CHECK PASS: max dynamic pressure survivable — 36.9 kPa
    [AutoTest      163.2] PHASE coast to space
    [AutoTest      182.0] PHASE circularize
    [AutoTest      182.0] circularization: dv 751.3 m/s, burn 54.0 s, in 74 s
    [AutoTest      286.1] circularization: burn complete, residual 0.30 m/s after 57.0 s
    [AutoTest      286.1] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.9 km, e 0.0015
    [AutoTest      286.1] in orbit 285 s after launch with 3885 m/s vac (409 + 3476) left
    [AutoTest      286.1] screenshot: lunar_015_orbit.png
    [AutoTest      286.1] PHASE plan TLI
    [AutoTest      286.1] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 939.1 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.7] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.7] PHASE TLI burn
    [AutoTest      286.7] TLI: dv 939.1 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1698.3] staging during burn
    [AutoTest     1777.9] TLI: burn complete, residual 0.30 m/s after 104.0 s
    [AutoTest     1777.9] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 297.6 km (planned 40.0 km)
    [AutoTest     1777.9] delta-v left after TLI: 2940 m/s vac (2940)
    [AutoTest     1777.9] PHASE mid-course correction
    [AutoTest     1777.9] correction: pro -18.00 rad 18.00 nrm 0.52 m/s (score 1273)
    [AutoTest     1777.9] MCC: dv 25.5 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3582.2] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.2] PHASE coast to Luma
    [AutoTest    11467.1] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11467.1] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11467.1] PHASE capture burn
    [AutoTest    11467.1] Luma periapsis 36.5 km in 3497 s
    [AutoTest    11467.1] Luma orbit insertion: dv 505.8 m/s, burn 62.0 s, in 3497 s
    [AutoTest    14997.7] Luma orbit insertion: burn complete, residual 0.30 m/s after 64.4 s
    [AutoTest    14997.7] screenshot: lunar_036_luma_orbit.png
    [AutoTest    14997.7] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    14997.7] delta-v left in Luma orbit: 2409 m/s vac (2409)
    [AutoTest    14997.7] PHASE deorbit
    [AutoTest    15000.9] PHASE descent
    [AutoTest    15717.8] suicide burn start at 12328 m AGL, 589 m/s
    [AutoTest    15880.2] final descent from 149 m AGL at 42.9 m/s (vertical -35.0 m/s)
    [AutoTest    15907.4] screenshot: lunar_043_luma_landed.png
    [AutoTest    15907.4] CHECK PASS: landed on Luma — situation Landed, tilt 2.4 deg, legs intact 4/4, crew 1
    [AutoTest    15907.4] delta-v left on the surface: 1661 m/s vac (1661)
    [AutoTest    15907.4] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15907.4] PHASE EVA
    [AutoTest    15907.5] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.40 m/s rel. lander
    [AutoTest    15907.5] lander watch: tilt 2.5 deg, surface speed 0.04 m/s, unity v 0.40, w 0.02, parts 17, legs [contact 0.04 m 3.0 kN] [contact 0.02 m 2.3 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 1.3 kN], frame v 8.40
    [AutoTest    15908.1] lander watch: tilt 3.3 deg, surface speed 0.11 m/s, unity v 1.00, w 0.05, parts 17, legs [contact 0.01 m 0.0 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 3.0 kN] [contact 0.03 m 2.9 kN], frame v 8.45
    [AutoTest    15908.6] lander watch: tilt 3.6 deg, surface speed 0.02 m/s, unity v 1.82, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 2.7 kN] [contact 0.03 m 3.3 kN] [contact 0.03 m 2.5 kN], frame v 8.59
    [AutoTest    15909.1] lander watch: tilt 3.3 deg, surface speed 0.06 m/s, unity v 2.64, w 0.05, parts 17, legs [contact 0.01 m 1.5 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 1.4 kN] [contact 0.03 m 3.0 kN], frame v 8.80
    [AutoTest    15909.6] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 3.49, w 0.05, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 2.9 kN], frame v 9.09
    [AutoTest    15910.2] lander watch: tilt 3.3 deg, surface speed 0.03 m/s, unity v 4.32, w 0.05, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.03 m 2.7 kN] [contact 0.02 m 2.0 kN] [contact 0.03 m 3.3 kN], frame v 9.44
    [AutoTest    15910.7] lander watch: tilt 3.3 deg, surface speed 0.54 m/s, unity v 1.23, w 0.16, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.72
    [AutoTest    15911.2] lander watch: tilt 3.2 deg, surface speed 0.33 m/s, unity v 1.75, w 0.16, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.04
    [AutoTest    15911.7] lander watch: tilt 3.3 deg, surface speed 0.17 m/s, unity v 1.66, w 0.12, parts 17, legs [contact 0.06 m 5.2 kN] [contact 0.02 m 2.9 kN] [contact 0.07 m 7.8 kN] [contact 0.06 m 5.2 kN], frame v 7.00
    [AutoTest    15912.3] lander watch: tilt 3.5 deg, surface speed 0.08 m/s, unity v 1.75, w 0.05, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.02 m 3.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.02
    [AutoTest    15912.8] lander watch: tilt 3.2 deg, surface speed 0.07 m/s, unity v 1.87, w 0.05, parts 17, legs [contact 0.02 m 2.8 kN] [contact 0.03 m 2.5 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN], frame v 7.02
    [AutoTest    15913.3] lander watch: tilt 3.2 deg, surface speed 0.07 m/s, unity v 1.85, w 0.05, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 2.2 kN], frame v 7.02
    [AutoTest    15913.8] lander watch: tilt 3.3 deg, surface speed 0.04 m/s, unity v 1.76, w 0.04, parts 17, legs [contact 0.02 m 2.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.02 m 1.9 kN], frame v 7.03
    [AutoTest    15914.3] lander watch: tilt 3.3 deg, surface speed 0.03 m/s, unity v 1.77, w 0.04, parts 17, legs [contact 0.03 m 2.7 kN] [contact 0.01 m 1.7 kN] [contact 0.04 m 4.1 kN] [contact 0.01 m 0.3 kN], frame v 7.03
    [AutoTest    15914.9] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.82, w 0.05, parts 17, legs [contact 0.03 m 3.2 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.9 kN] [contact 0.01 m 0.9 kN], frame v 7.02
    [AutoTest    15915.4] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.03 m 3.1 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 1.3 kN], frame v 7.02
    [AutoTest    15915.9] lander watch: tilt 3.3 deg, surface speed 0.01 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.03 m 2.9 kN] [contact 0.01 m 1.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 1.0 kN], frame v 7.01
    [AutoTest    15916.4] lander watch: tilt 3.3 deg, surface speed 0.00 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.03 m 3.0 kN] [contact 0.01 m 1.2 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN], frame v 7.00
    [AutoTest    15917.0] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.83, w 0.03, parts 17, legs [contact 0.03 m 2.7 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.5 kN] [contact 0.01 m 0.8 kN], frame v 7.01
    [AutoTest    15917.4] CHECK PASS: walking in low gravity — 12.0 m in 6.9 s (AGL 0.7 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2270, 2017), speed 1.83 m/s, jetpack 10.00 kg)
    [AutoTest    15917.4] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2270, 2017), gravity 1.60 m/s²
    [AutoTest    15917.5] lander watch: tilt 3.4 deg, surface speed 0.43 m/s, unity v 2.34, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.38
    [AutoTest    15918.0] lander watch: tilt 4.3 deg, surface speed 0.35 m/s, unity v 2.31, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 6.4 kN] [free 0.00 m 0.0 kN], frame v 7.20
    [AutoTest    15918.5] lander watch: tilt 3.4 deg, surface speed 0.19 m/s, unity v 1.92, w 0.05, parts 17, legs [contact 0.02 m 3.1 kN] [contact 0.03 m 0.5 kN] [contact 0.04 m 2.1 kN] [contact 0.02 m 2.3 kN], frame v 7.11
    [AutoTest    15919.0] lander watch: tilt 2.9 deg, surface speed 0.04 m/s, unity v 1.74, w 0.05, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN] [contact 0.02 m 1.2 kN] [contact 0.03 m 2.8 kN], frame v 7.12
    [AutoTest    15919.6] lander watch: tilt 3.3 deg, surface speed 0.07 m/s, unity v 2.13, w 0.05, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.02 m 2.4 kN] [contact 0.04 m 4.0 kN] [contact 0.02 m 0.7 kN], frame v 7.23
    [AutoTest    15920.1] lander watch: tilt 3.4 deg, surface speed 0.04 m/s, unity v 2.80, w 0.05, parts 17, legs [contact 0.02 m 2.0 kN] [contact 0.02 m 2.1 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 1.1 kN], frame v 7.43
    [AutoTest    15920.1] CHECK PASS: jump in low gravity — apex 1.64 m
    [AutoTest    15920.6] lander watch: tilt 3.3 deg, surface speed 0.24 m/s, unity v 0.76, w 0.17, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.17
    [AutoTest    15921.1] lander watch: tilt 3.1 deg, surface speed 0.09 m/s, unity v 0.11, w 0.04, parts 17, legs [contact 0.04 m 3.4 kN] [contact 0.01 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 1.5 kN], frame v 8.79
    [AutoTest    15921.7] lander watch: tilt 3.1 deg, surface speed 0.02 m/s, unity v 0.02, w 0.04, parts 17, legs [contact 0.03 m 2.7 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN] [contact 0.02 m 2.0 kN], frame v 8.83
    [AutoTest    15922.2] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15922.2] screenshot: lunar_081_eva_flag.png
    [AutoTest    15922.2] PHASE return to hatch
    [AutoTest    15922.2] screenshot: lunar_083_lander_before_return.png
    [AutoTest    15922.3] lander watch: tilt 3.2 deg, surface speed 0.06 m/s, unity v 0.26, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.00 m 1.2 kN] [contact 0.04 m 4.5 kN] [contact 0.01 m 0.2 kN], frame v 9.01
    [AutoTest    15922.8] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.15, w 0.03, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 1.4 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 9.97
    [AutoTest    15923.3] lander watch: tilt 3.1 deg, surface speed 0.03 m/s, unity v 1.76, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.01 m 0.4 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.8 kN], frame v 10.59
    [AutoTest    15923.8] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.00 m 0.2 kN] [contact 0.04 m 4.4 kN] [contact 0.01 m 0.9 kN], frame v 10.62
    [AutoTest    15924.3] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.4 kN], frame v 10.60
    [AutoTest    15924.9] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.4 kN], frame v 10.60
    [AutoTest    15925.4] lander watch: tilt 3.1 deg, surface speed 0.02 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.03 m 3.1 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [contact 0.00 m 0.2 kN], frame v 10.58
    [AutoTest    15925.9] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.00 m 0.6 kN] [contact 0.04 m 4.3 kN] [contact 0.01 m 0.6 kN], frame v 10.57
    [AutoTest    15926.4] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.03 m 3.0 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 3.9 kN] [contact 0.00 m 0.1 kN], frame v 10.58
    [AutoTest    15927.0] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.3 kN] [contact 0.01 m 0.6 kN], frame v 10.59
    [AutoTest    15927.5] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.60
    [AutoTest    15928.0] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.79, w 0.04, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.01 m 1.1 kN] [contact 0.04 m 4.6 kN] [contact 0.01 m 0.9 kN], frame v 10.61
    [AutoTest    15928.5] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.5 kN], frame v 10.62
    [AutoTest    15929.0] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.62
    [AutoTest    15929.6] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.62
    [AutoTest    15930.1] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.00 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.61
    [AutoTest    15930.6] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.61
    [AutoTest    15931.1] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.61
    [AutoTest    15931.7] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.5 kN], frame v 10.59
    [AutoTest    15932.2] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.30, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.00 m 0.5 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.5 kN], frame v 10.04
    [AutoTest    15932.7] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.36, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.00 m 0.7 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.5 kN], frame v 9.63
    [AutoTest    15933.2] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.97, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.00 m 0.5 kN] [contact 0.03 m 4.2 kN] [contact 0.00 m 0.6 kN], frame v 9.53
    [AutoTest    15933.7] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 2.66, w 0.02, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.00 m 0.5 kN] [contact 0.03 m 4.1 kN] [contact 0.00 m 0.5 kN], frame v 9.56
    [AutoTest    15934.3] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 2.29, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.5 kN], frame v 9.36
    [AutoTest    15934.8] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.67, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.5 kN], frame v 9.17
    [AutoTest    15935.0] screenshot: lunar_109_before_boarding.png
    [AutoTest    15935.0] boarding: lander AGL 4.18 m, situation Landed, legs [contact 0.03 m 3.4 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.5 kN]
    [AutoTest    15935.3] lander watch: tilt 2.8 deg, surface speed 0.08 m/s, unity v 0.00, w 0.02, parts 17, legs [contact 0.05 m 5.6 kN] [contact 0.01 m 0.6 kN] [contact 0.02 m 1.7 kN] [contact 0.00 m 0.3 kN], frame v 8.85
    [AutoTest    15935.5] screenshot: lunar_112_boarded.png
    [AutoTest    15935.5] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15935.5] PHASE lunar ascent
    [AutoTest    15941.2] PHASE lunar gravity turn
    [AutoTest    15963.9] Luma orbit circularization: dv 414.2 m/s, burn 32.3 s, in 174 s
    [AutoTest    15972.9] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16156.1] Luma orbit circularization: burn complete, residual 0.30 m/s after 34.8 s
    [AutoTest    16156.1] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16156.1] delta-v left in Luma orbit after ascent: 922 m/s vac (922)
    [AutoTest    16156.1] PHASE plan return
    [AutoTest    16156.1] CHECK PASS: return trajectory to Tellus found — burn 299.6 m/s in 1198 s, Tellus Pe 32.0 km
    [AutoTest    16156.1] PHASE trans-Tellus injection
    [AutoTest    16156.1] TTI: dv 299.6 m/s, burn 21.0 s, in 1198 s
    [AutoTest    17367.2] TTI: burn complete, residual 0.19 m/s after 23.8 s
    [AutoTest    17367.2] delta-v left after trans-Tellus injection: 622 m/s vac (622)
    [AutoTest    17367.2] PHASE coast home
    [AutoTest    23506.4] Tellus periapsis after SOI exit 32.6 km
    [AutoTest    23506.4] CHECK PASS: return periapsis in reentry corridor — Pe 32.6 km
    [AutoTest    38076.6] PHASE reentry
    [AutoTest    38188.1] screenshot: lunar_131_reentry.png
    [AutoTest    38610.9] screenshot: lunar_132_touchdown.png
    [AutoTest    38610.9] CHECK PASS: capsule landed safely with crew — Landed at 0.1 m/s, crew 1, max skin 1011 K (HS-125 Ablative Heat Shield), max 7.8 g
