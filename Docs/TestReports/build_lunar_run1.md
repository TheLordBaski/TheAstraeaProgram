# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 938.9 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 297.5 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 7.4 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.37 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 6.9 s (AGL 0.6 m, 14.6 m from the lander axis, grounded True on TerrainCollider (1, 2271, 1981), speed 1.79 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2271, 1981), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.78 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.4 km
- [x] return trajectory to Tellus found: burn 280.0 m/s in 1529 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 36.7 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 944 K (HS-125 Ablative Heat Shield), max 8.8 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.5 km, 1194 m/s
    [AutoTest      163.0] MECO: Ap 82.0 km, max Q 36.9 kPa, delta-v left 4636 m/s vac (1160 + 3476)
    [AutoTest      163.0] CHECK PASS: max dynamic pressure survivable — 36.9 kPa
    [AutoTest      163.0] PHASE coast to space
    [AutoTest      182.1] PHASE circularize
    [AutoTest      182.1] circularization: dv 750.0 m/s, burn 53.9 s, in 74 s
    [AutoTest      286.0] circularization: burn complete, residual 0.29 m/s after 56.8 s
    [AutoTest      286.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.0] in orbit 285 s after launch with 3885 m/s vac (409 + 3476) left
    [AutoTest      286.0] screenshot: lunar_015_orbit.png
    [AutoTest      286.0] PHASE plan TLI
    [AutoTest      286.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 938.9 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.6] PHASE TLI burn
    [AutoTest      286.6] TLI: dv 938.9 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1698.3] staging during burn
    [AutoTest     1777.8] TLI: burn complete, residual 0.29 m/s after 104.0 s
    [AutoTest     1777.8] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 297.5 km (planned 40.0 km)
    [AutoTest     1777.8] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.8] PHASE mid-course correction
    [AutoTest     1777.8] correction: pro -18.00 rad 18.00 nrm 1.50 m/s (score 1277)
    [AutoTest     1777.8] MCC: dv 25.5 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3582.1] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.1] PHASE coast to Luma
    [AutoTest    11472.6] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11472.6] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11472.6] PHASE capture burn
    [AutoTest    11472.6] Luma periapsis 36.5 km in 3500 s
    [AutoTest    11472.6] Luma orbit insertion: dv 505.3 m/s, burn 61.9 s, in 3500 s
    [AutoTest    15005.9] Luma orbit insertion: burn complete, residual 0.29 m/s after 64.4 s
    [AutoTest    15005.9] screenshot: lunar_036_luma_orbit.png
    [AutoTest    15005.9] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    15005.9] delta-v left in Luma orbit: 2410 m/s vac (2410)
    [AutoTest    15005.9] PHASE deorbit
    [AutoTest    15009.1] PHASE descent
    [AutoTest    15725.3] suicide burn start at 12322 m AGL, 589 m/s
    [AutoTest    15888.9] final descent from 149 m AGL at 43.0 m/s (vertical -35.0 m/s)
    [AutoTest    15915.3] screenshot: lunar_043_luma_landed.png
    [AutoTest    15915.3] CHECK PASS: landed on Luma — situation Landed, tilt 7.4 deg, legs intact 4/4, crew 1
    [AutoTest    15915.3] delta-v left on the surface: 1664 m/s vac (1664)
    [AutoTest    15915.3] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15915.3] PHASE EVA
    [AutoTest    15915.3] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.37 m/s rel. lander
    [AutoTest    15915.3] lander watch: tilt 7.4 deg, surface speed 0.04 m/s, unity v 0.37, w 0.01, parts 17, legs [contact 0.05 m 5.4 kN] [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 2.7 kN], frame v 8.53
    [AutoTest    15915.9] lander watch: tilt 8.5 deg, surface speed 0.22 m/s, unity v 0.85, w 0.05, parts 17, legs [contact 0.02 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 3.2 kN] [contact 0.04 m 4.2 kN], frame v 8.57
    [AutoTest    15916.4] lander watch: tilt 9.7 deg, surface speed 0.10 m/s, unity v 1.72, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 1.2 kN] [contact 0.05 m 5.6 kN] [contact 0.02 m 1.3 kN], frame v 8.69
    [AutoTest    15916.9] lander watch: tilt 9.8 deg, surface speed 0.06 m/s, unity v 2.56, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 1.8 kN] [contact 0.05 m 5.1 kN] [contact 0.02 m 1.5 kN], frame v 8.89
    [AutoTest    15917.4] lander watch: tilt 9.1 deg, surface speed 0.13 m/s, unity v 3.38, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.03 m 2.2 kN] [contact 0.03 m 3.2 kN], frame v 9.16
    [AutoTest    15918.0] lander watch: tilt 8.5 deg, surface speed 0.01 m/s, unity v 4.24, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 3.4 kN], frame v 9.51
    [AutoTest    15918.5] lander watch: tilt 8.7 deg, surface speed 0.66 m/s, unity v 1.20, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.45
    [AutoTest    15919.0] lander watch: tilt 9.1 deg, surface speed 0.27 m/s, unity v 1.60, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.72
    [AutoTest    15919.6] lander watch: tilt 9.5 deg, surface speed 0.14 m/s, unity v 1.76, w 0.19, parts 17, legs [contact 0.00 m 3.1 kN] [contact 0.04 m 6.2 kN] [contact 0.08 m 11.8 kN] [contact 0.06 m 10.9 kN], frame v 7.65
    [AutoTest    15920.1] lander watch: tilt 9.5 deg, surface speed 0.07 m/s, unity v 1.85, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 4.0 kN] [contact 0.02 m 1.9 kN], frame v 7.65
    [AutoTest    15920.6] lander watch: tilt 8.8 deg, surface speed 0.10 m/s, unity v 1.89, w 0.06, parts 17, legs [contact 0.01 m 1.7 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 1.9 kN] [contact 0.03 m 2.9 kN], frame v 7.65
    [AutoTest    15921.1] lander watch: tilt 8.6 deg, surface speed 0.04 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.02 m 2.5 kN] [contact 0.01 m 1.6 kN] [contact 0.03 m 3.1 kN], frame v 7.63
    [AutoTest    15921.6] lander watch: tilt 8.9 deg, surface speed 0.04 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.02 m 2.0 kN] [contact 0.03 m 3.2 kN] [contact 0.03 m 3.5 kN], frame v 7.63
    [AutoTest    15922.2] lander watch: tilt 9.0 deg, surface speed 0.02 m/s, unity v 1.84, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 1.6 kN] [contact 0.03 m 2.5 kN] [contact 0.03 m 2.8 kN], frame v 7.63
    [AutoTest    15922.7] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 1.85, w 0.05, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.1 kN], frame v 7.62
    [AutoTest    15923.2] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 1.83, w 0.05, parts 17, legs [contact 0.01 m 1.2 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 3.0 kN] [contact 0.03 m 3.8 kN], frame v 7.62
    [AutoTest    15923.7] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.84, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.2 kN] [contact 0.02 m 2.7 kN] [contact 0.03 m 3.3 kN], frame v 7.62
    [AutoTest    15924.2] lander watch: tilt 8.9 deg, surface speed 0.03 m/s, unity v 1.81, w 0.03, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.02 m 1.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.2 kN], frame v 7.65
    [AutoTest    15924.8] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.02 m 2.3 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.3 kN], frame v 7.65
    [AutoTest    15925.2] CHECK PASS: walking in low gravity — 12.0 m in 6.9 s (AGL 0.6 m, 14.6 m from the lander axis, grounded True on TerrainCollider (1, 2271, 1981), speed 1.79 m/s, jetpack 10.00 kg)
    [AutoTest    15925.2] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2271, 1981), gravity 1.60 m/s²
    [AutoTest    15925.3] lander watch: tilt 9.0 deg, surface speed 0.53 m/s, unity v 2.51, w 0.09, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN] [contact 0.02 m 0.0 kN] [contact 0.02 m 0.0 kN], frame v 8.08
    [AutoTest    15925.8] lander watch: tilt 11.0 deg, surface speed 0.21 m/s, unity v 2.41, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 7.87
    [AutoTest    15926.3] lander watch: tilt 10.3 deg, surface speed 0.20 m/s, unity v 1.92, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 5.5 kN] [contact 0.03 m 2.8 kN], frame v 7.75
    [AutoTest    15926.9] lander watch: tilt 8.7 deg, surface speed 0.23 m/s, unity v 1.83, w 0.06, parts 17, legs [contact 0.01 m 2.8 kN] [contact 0.04 m 5.2 kN] [contact 0.03 m 0.9 kN] [contact 0.03 m 2.5 kN], frame v 7.72
    [AutoTest    15927.4] lander watch: tilt 8.1 deg, surface speed 0.06 m/s, unity v 1.93, w 0.05, parts 17, legs [contact 0.03 m 2.6 kN] [contact 0.04 m 3.4 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 1.9 kN], frame v 7.78
    [AutoTest    15927.9] lander watch: tilt 8.8 deg, surface speed 0.12 m/s, unity v 2.40, w 0.06, parts 17, legs [contact 0.01 m 0.0 kN] [contact 0.03 m 1.6 kN] [contact 0.02 m 3.1 kN] [contact 0.04 m 4.4 kN], frame v 7.92
    [AutoTest    15928.2] CHECK PASS: jump in low gravity — apex 1.78 m
    [AutoTest    15928.4] lander watch: tilt 9.2 deg, surface speed 0.21 m/s, unity v 1.48, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN] [contact 0.00 m 0.0 kN], frame v 7.96
    [AutoTest    15928.9] lander watch: tilt 9.1 deg, surface speed 0.04 m/s, unity v 0.68, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.3 kN] [contact 0.04 m 4.7 kN] [contact 0.03 m 4.3 kN], frame v 8.45
    [AutoTest    15929.5] lander watch: tilt 8.9 deg, surface speed 0.05 m/s, unity v 0.12, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 2.1 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.9 kN], frame v 8.79
    [AutoTest    15930.0] lander watch: tilt 8.7 deg, surface speed 0.01 m/s, unity v 0.04, w 0.05, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.7 kN] [contact 0.03 m 3.4 kN], frame v 8.81
    [AutoTest    15930.3] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15930.3] screenshot: lunar_082_eva_flag.png
    [AutoTest    15930.3] PHASE return to hatch
    [AutoTest    15930.3] screenshot: lunar_084_lander_before_return.png
    [AutoTest    15930.5] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 0.38, w 0.05, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 3.3 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 2.8 kN], frame v 9.10
    [AutoTest    15931.0] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.20, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.1 kN], frame v 9.73
    [AutoTest    15931.6] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.76, w 0.05, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.02 m 2.8 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 3.2 kN], frame v 10.13
    [AutoTest    15932.1] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.1 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.0 kN], frame v 10.15
    [AutoTest    15932.6] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 2.9 kN], frame v 10.16
    [AutoTest    15933.1] lander watch: tilt 8.8 deg, surface speed 0.00 m/s, unity v 1.82, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.1 kN], frame v 10.17
    [AutoTest    15933.6] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 3.0 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.1 kN], frame v 10.16
    [AutoTest    15934.2] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.0 kN], frame v 10.16
    [AutoTest    15934.7] lander watch: tilt 8.8 deg, surface speed 0.00 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.1 kN], frame v 10.16
    [AutoTest    15935.2] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.1 kN], frame v 10.16
    [AutoTest    15935.7] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.76, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.7 kN], frame v 10.13
    [AutoTest    15936.2] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.75, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.12
    [AutoTest    15936.8] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.12
    [AutoTest    15937.3] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.12
    [AutoTest    15937.8] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.13
    [AutoTest    15938.3] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.12
    [AutoTest    15938.9] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.03 m 2.5 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.5 kN], frame v 10.14
    [AutoTest    15939.5] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.1 kN], frame v 10.14
    [AutoTest    15940.0] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.0 kN], frame v 10.13
    [AutoTest    15940.5] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.45, w 0.04, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.2 kN] [contact 0.03 m 3.5 kN], frame v 9.84
    [AutoTest    15941.1] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.22, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 3.2 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.0 kN], frame v 9.42
    [AutoTest    15941.6] lander watch: tilt 8.8 deg, surface speed 0.00 m/s, unity v 1.75, w 0.04, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 3.1 kN], frame v 9.35
    [AutoTest    15942.1] lander watch: tilt 8.8 deg, surface speed 0.00 m/s, unity v 2.45, w 0.05, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.02 m 3.1 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.2 kN], frame v 9.40
    [AutoTest    15942.6] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 2.53, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.03 m 3.1 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.1 kN], frame v 9.34
    [AutoTest    15943.1] lander watch: tilt 8.8 deg, surface speed 0.01 m/s, unity v 1.89, w 0.05, parts 17, legs [contact 0.01 m 0.2 kN] [contact 0.03 m 3.1 kN] [contact 0.03 m 2.5 kN] [contact 0.03 m 3.0 kN], frame v 9.14
    [AutoTest    15943.5] screenshot: lunar_110_before_boarding.png
    [AutoTest    15943.5] boarding: lander AGL 4.24 m, situation Landed, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.0 kN]
    [AutoTest    15943.7] lander watch: tilt 8.7 deg, surface speed 0.08 m/s, unity v 0.00, w 0.04, parts 17, legs [contact 0.01 m 2.0 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 0.9 kN] [contact 0.03 m 3.2 kN], frame v 8.82
    [AutoTest    15944.0] screenshot: lunar_113_boarded.png
    [AutoTest    15944.0] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15944.0] PHASE lunar ascent
    [AutoTest    15949.8] PHASE lunar gravity turn
    [AutoTest    15972.7] Luma orbit circularization: dv 408.8 m/s, burn 31.9 s, in 174 s
    [AutoTest    15986.0] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16165.5] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.4 s
    [AutoTest    16165.5] CHECK PASS: lunar orbit after ascent — Pe 22.4 km
    [AutoTest    16165.5] delta-v left in Luma orbit after ascent: 927 m/s vac (927)
    [AutoTest    16165.5] PHASE plan return
    [AutoTest    16165.5] CHECK PASS: return trajectory to Tellus found — burn 280.0 m/s in 1529 s, Tellus Pe 32.0 km
    [AutoTest    16165.5] PHASE trans-Tellus injection
    [AutoTest    16165.5] TTI: dv 280.0 m/s, burn 19.7 s, in 1529 s
    [AutoTest    17707.4] TTI: burn complete, residual 0.20 m/s after 22.5 s
    [AutoTest    17707.4] delta-v left after trans-Tellus injection: 647 m/s vac (647)
    [AutoTest    17707.4] PHASE coast home
    [AutoTest    24413.8] Tellus periapsis after SOI exit 36.7 km
    [AutoTest    24413.8] CHECK PASS: return periapsis in reentry corridor — Pe 36.7 km
    [AutoTest    52454.9] PHASE reentry
    [AutoTest    52571.6] screenshot: lunar_132_reentry.png
    [AutoTest    53150.7] screenshot: lunar_133_touchdown.png
    [AutoTest    53150.7] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 944 K (HS-125 Ablative Heat Shield), max 8.8 g
