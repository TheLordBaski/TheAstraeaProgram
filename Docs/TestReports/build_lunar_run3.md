# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 939.0 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 297.5 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.4 km
- [x] landed on Luma: situation Landed, tilt 0.1 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.38 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.1 s (AGL 0.7 m, 14.6 m from the lander axis, grounded True on TerrainCollider (1, 2286, 1990), speed 1.82 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2286, 1990), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.66 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 350.4 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.3 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 1007 K (HS-125 Ablative Heat Shield), max 7.8 g

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
    [AutoTest      286.1] circularization: burn complete, residual 0.27 m/s after 57.0 s
    [AutoTest      286.1] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.1] in orbit 285 s after launch with 3885 m/s vac (409 + 3476) left
    [AutoTest      286.1] screenshot: lunar_015_orbit.png
    [AutoTest      286.1] PHASE plan TLI
    [AutoTest      286.1] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 939.0 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.6] PHASE TLI burn
    [AutoTest      286.6] TLI: dv 939.0 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1698.4] staging during burn
    [AutoTest     1777.9] TLI: burn complete, residual 0.30 m/s after 104.0 s
    [AutoTest     1777.9] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 297.5 km (planned 40.0 km)
    [AutoTest     1777.9] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.9] PHASE mid-course correction
    [AutoTest     1777.9] correction: pro -18.00 rad 18.00 nrm 1.24 m/s (score 1275)
    [AutoTest     1777.9] MCC: dv 25.5 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3582.2] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.2] PHASE coast to Luma
    [AutoTest    11469.3] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11469.3] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11469.3] PHASE capture burn
    [AutoTest    11469.3] Luma periapsis 36.5 km in 3498 s
    [AutoTest    11469.3] Luma orbit insertion: dv 505.6 m/s, burn 62.0 s, in 3498 s
    [AutoTest    15001.0] Luma orbit insertion: burn complete, residual 0.30 m/s after 64.4 s
    [AutoTest    15001.0] screenshot: lunar_036_luma_orbit.png
    [AutoTest    15001.0] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.4 km
    [AutoTest    15001.0] delta-v left in Luma orbit: 2410 m/s vac (2410)
    [AutoTest    15003.1] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    15003.1] PHASE deorbit
    [AutoTest    15006.3] PHASE descent
    [AutoTest    15723.6] suicide burn start at 12330 m AGL, 589 m/s
    [AutoTest    15886.7] final descent from 150 m AGL at 43.1 m/s (vertical -35.1 m/s)
    [AutoTest    15887.3] landing site: 1.0° slope 12 m away (ground below: 5.8°)
    [AutoTest    15935.7] screenshot: lunar_045_luma_landed.png
    [AutoTest    15935.7] CHECK PASS: landed on Luma — situation Landed, tilt 0.1 deg, legs intact 4/4, crew 1
    [AutoTest    15935.7] delta-v left on the surface: 1625 m/s vac (1625)
    [AutoTest    15935.7] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15935.7] PHASE EVA
    [AutoTest    15935.7] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.38 m/s rel. lander
    [AutoTest    15935.7] lander watch: tilt 0.2 deg, surface speed 0.02 m/s, unity v 0.38, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 2.8 kN] [contact 0.00 m 0.0 kN] [contact 0.03 m 4.2 kN], frame v 8.48
    [AutoTest    15936.2] lander watch: tilt 0.2 deg, surface speed 0.03 m/s, unity v 0.90, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.01 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 8.51
    [AutoTest    15936.8] lander watch: tilt 0.4 deg, surface speed 0.03 m/s, unity v 1.68, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.9 kN], frame v 8.63
    [AutoTest    15937.3] lander watch: tilt 0.7 deg, surface speed 0.03 m/s, unity v 2.50, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.4 kN], frame v 8.83
    [AutoTest    15937.8] lander watch: tilt 1.1 deg, surface speed 0.05 m/s, unity v 3.32, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 9.10
    [AutoTest    15938.4] lander watch: tilt 1.6 deg, surface speed 0.07 m/s, unity v 4.25, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 9.47
    [AutoTest    15938.9] lander watch: tilt 1.8 deg, surface speed 0.30 m/s, unity v 0.58, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.43
    [AutoTest    15939.4] lander watch: tilt 1.5 deg, surface speed 0.47 m/s, unity v 1.48, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 7.9 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.72
    [AutoTest    15939.9] lander watch: tilt 1.7 deg, surface speed 0.08 m/s, unity v 1.79, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.1 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.2 kN], frame v 7.39
    [AutoTest    15940.5] lander watch: tilt 1.9 deg, surface speed 0.05 m/s, unity v 1.80, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN], frame v 7.37
    [AutoTest    15941.0] lander watch: tilt 2.1 deg, surface speed 0.03 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.4 kN], frame v 7.38
    [AutoTest    15941.5] lander watch: tilt 2.2 deg, surface speed 0.01 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.01 m 1.6 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN], frame v 7.37
    [AutoTest    15942.0] lander watch: tilt 2.1 deg, surface speed 0.03 m/s, unity v 1.79, w 0.03, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 7.35
    [AutoTest    15942.5] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.34
    [AutoTest    15943.1] lander watch: tilt 1.9 deg, surface speed 0.00 m/s, unity v 1.83, w 0.00, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.34
    [AutoTest    15943.6] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.34
    [AutoTest    15944.1] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.00 m 0.6 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 7.34
    [AutoTest    15944.6] lander watch: tilt 2.1 deg, surface speed 0.00 m/s, unity v 1.84, w 0.02, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 7.34
    [AutoTest    15945.1] lander watch: tilt 2.1 deg, surface speed 0.01 m/s, unity v 1.83, w 0.01, parts 17, legs [contact 0.01 m 0.6 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 7.34
    [AutoTest    15945.7] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 7.35
    [AutoTest    15945.8] CHECK PASS: walking in low gravity — 12.0 m in 7.1 s (AGL 0.7 m, 14.6 m from the lander axis, grounded True on TerrainCollider (1, 2286, 1990), speed 1.82 m/s, jetpack 10.00 kg)
    [AutoTest    15945.8] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2286, 1990), gravity 1.60 m/s²
    [AutoTest    15946.2] lander watch: tilt 0.5 deg, surface speed 0.12 m/s, unity v 2.46, w 0.08, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.65
    [AutoTest    15946.7] lander watch: tilt 0.1 deg, surface speed 0.04 m/s, unity v 1.88, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.05 m 3.7 kN] [contact 0.02 m 1.2 kN] [contact 0.04 m 4.1 kN], frame v 7.51
    [AutoTest    15947.2] lander watch: tilt 0.1 deg, surface speed 0.04 m/s, unity v 1.71, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.1 kN] [contact 0.04 m 4.4 kN], frame v 7.46
    [AutoTest    15947.7] lander watch: tilt 0.5 deg, surface speed 0.05 m/s, unity v 1.90, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.51
    [AutoTest    15948.3] lander watch: tilt 0.9 deg, surface speed 0.05 m/s, unity v 2.39, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.64
    [AutoTest    15948.6] CHECK PASS: jump in low gravity — apex 1.66 m
    [AutoTest    15948.8] lander watch: tilt 1.3 deg, surface speed 0.16 m/s, unity v 1.36, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 0.1 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 0.0 kN], frame v 7.83
    [AutoTest    15949.3] lander watch: tilt 1.8 deg, surface speed 0.09 m/s, unity v 0.48, w 0.02, parts 17, legs [contact 0.00 m 1.6 kN] [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN], frame v 8.49
    [AutoTest    15949.8] lander watch: tilt 2.3 deg, surface speed 0.04 m/s, unity v 0.07, w 0.04, parts 17, legs [contact 0.01 m 1.7 kN] [contact 0.03 m 3.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.5 kN], frame v 8.78
    [AutoTest    15950.3] lander watch: tilt 2.4 deg, surface speed 0.02 m/s, unity v 0.02, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.3 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.5 kN], frame v 8.79
    [AutoTest    15950.7] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15950.7] screenshot: lunar_084_eva_flag.png
    [AutoTest    15950.7] PHASE return to hatch
    [AutoTest    15950.7] screenshot: lunar_086_lander_before_return.png
    [AutoTest    15950.9] lander watch: tilt 2.1 deg, surface speed 0.05 m/s, unity v 0.34, w 0.02, parts 17, legs [contact 0.01 m 0.5 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN], frame v 9.06
    [AutoTest    15951.4] lander watch: tilt 1.8 deg, surface speed 0.03 m/s, unity v 1.19, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN], frame v 9.81
    [AutoTest    15951.9] lander watch: tilt 1.7 deg, surface speed 0.02 m/s, unity v 1.79, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 10.35
    [AutoTest    15952.4] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.80, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 10.37
    [AutoTest    15952.9] lander watch: tilt 1.8 deg, surface speed 0.03 m/s, unity v 1.78, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 10.37
    [AutoTest    15953.5] lander watch: tilt 2.0 deg, surface speed 0.03 m/s, unity v 1.76, w 0.02, parts 17, legs [contact 0.00 m 0.7 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 10.36
    [AutoTest    15954.0] lander watch: tilt 2.2 deg, surface speed 0.01 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.01 m 1.1 kN] [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN], frame v 10.36
    [AutoTest    15954.5] lander watch: tilt 2.2 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN], frame v 10.35
    [AutoTest    15955.0] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 10.34
    [AutoTest    15955.5] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 1.77, w 0.00, parts 17, legs [contact 0.00 m 0.1 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 10.33
    [AutoTest    15956.1] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 1.76, w 0.00, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN], frame v 10.33
    [AutoTest    15956.6] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.76, w 0.01, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 10.33
    [AutoTest    15957.1] lander watch: tilt 2.1 deg, surface speed 0.01 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 10.33
    [AutoTest    15957.6] lander watch: tilt 2.1 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 10.33
    [AutoTest    15958.1] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.79, w 0.01, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 10.33
    [AutoTest    15958.7] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.01 m 0.4 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 10.34
    [AutoTest    15959.2] lander watch: tilt 2.0 deg, surface speed 0.00 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 10.37
    [AutoTest    15959.7] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 10.37
    [AutoTest    15960.2] lander watch: tilt 2.0 deg, surface speed 0.00 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.00 m 0.6 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 10.37
    [AutoTest    15960.7] lander watch: tilt 2.1 deg, surface speed 0.01 m/s, unity v 1.49, w 0.01, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 10.08
    [AutoTest    15961.3] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 1.07, w 0.01, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN], frame v 9.56
    [AutoTest    15961.8] lander watch: tilt 2.0 deg, surface speed 0.00 m/s, unity v 1.50, w 0.00, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.2 kN], frame v 9.40
    [AutoTest    15962.3] lander watch: tilt 2.0 deg, surface speed 0.01 m/s, unity v 2.19, w 0.01, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN], frame v 9.41
    [AutoTest    15962.8] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 2.58, w 0.01, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.41
    [AutoTest    15963.3] lander watch: tilt 2.0 deg, surface speed 0.00 m/s, unity v 1.99, w 0.01, parts 17, legs [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 9.21
    [AutoTest    15963.8] screenshot: lunar_112_before_boarding.png
    [AutoTest    15963.8] boarding: lander AGL 4.24 m, situation Landed, legs [contact 0.01 m 0.5 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN]
    [AutoTest    15963.9] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 0.00, w 0.03, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.04 m 5.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.9 kN], frame v 8.82
    [AutoTest    15964.3] screenshot: lunar_115_boarded.png
    [AutoTest    15964.3] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15964.3] PHASE lunar ascent
    [AutoTest    15970.0] PHASE lunar gravity turn
    [AutoTest    15992.5] Luma orbit circularization: dv 411.5 m/s, burn 31.7 s, in 174 s
    [AutoTest    15996.0] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16184.9] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.3 s
    [AutoTest    16184.9] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16184.9] delta-v left in Luma orbit after ascent: 886 m/s vac (886)
    [AutoTest    16184.9] PHASE plan return
    [AutoTest    16184.9] CHECK PASS: return trajectory to Tellus found — burn 350.4 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16184.9] PHASE trans-Tellus injection
    [AutoTest    16184.9] TTI: dv 350.4 m/s, burn 24.1 s, in 1168 s
    [AutoTest    17367.7] TTI: burn complete, residual 0.19 m/s after 26.9 s
    [AutoTest    17367.7] delta-v left after trans-Tellus injection: 536 m/s vac (536)
    [AutoTest    17367.7] PHASE coast home
    [AutoTest    22512.1] Tellus periapsis after SOI exit 31.3 km
    [AutoTest    22512.1] CHECK PASS: return periapsis in reentry corridor — Pe 31.3 km
    [AutoTest    34780.2] PHASE reentry
    [AutoTest    34890.0] screenshot: lunar_134_reentry.png
    [AutoTest    35365.5] screenshot: lunar_135_touchdown.png
    [AutoTest    35365.5] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 1007 K (HS-125 Ablative Heat Shield), max 7.8 g
