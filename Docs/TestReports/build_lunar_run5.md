# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 938.6 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 297.0 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.2 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.37 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.0 s (AGL 0.6 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2284, 1926), speed 1.81 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2284, 1926), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.87 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 280.0 m/s in 1528 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 36.7 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 783 K (HS-125 Ablative Heat Shield), max 6.3 g

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
    [AutoTest      286.0] circularization: burn complete, residual 0.30 m/s after 56.9 s
    [AutoTest      286.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.0] in orbit 284 s after launch with 3885 m/s vac (409 + 3476) left
    [AutoTest      286.0] screenshot: lunar_015_orbit.png
    [AutoTest      286.0] PHASE plan TLI
    [AutoTest      286.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 938.6 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.5] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.5] PHASE TLI burn
    [AutoTest      286.5] TLI: dv 938.6 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1698.3] staging during burn
    [AutoTest     1777.7] TLI: burn complete, residual 0.29 m/s after 103.9 s
    [AutoTest     1777.7] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 297.0 km (planned 40.0 km)
    [AutoTest     1777.7] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.7] PHASE mid-course correction
    [AutoTest     1777.7] correction: pro -18.00 rad 18.00 nrm 2.81 m/s (score 1281)
    [AutoTest     1777.7] MCC: dv 25.6 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3582.1] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.1] PHASE coast to Luma
    [AutoTest    11485.1] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11485.1] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11485.1] PHASE capture burn
    [AutoTest    11485.1] Luma periapsis 36.5 km in 3503 s
    [AutoTest    11485.1] Luma orbit insertion: dv 504.5 m/s, burn 61.9 s, in 3503 s
    [AutoTest    15021.5] Luma orbit insertion: burn complete, residual 0.30 m/s after 64.3 s
    [AutoTest    15021.5] screenshot: lunar_036_luma_orbit.png
    [AutoTest    15021.5] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    15021.5] delta-v left in Luma orbit: 2411 m/s vac (2411)
    [AutoTest    15023.5] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    15023.5] PHASE deorbit
    [AutoTest    15026.7] PHASE descent
    [AutoTest    15745.1] suicide burn start at 12260 m AGL, 589 m/s
    [AutoTest    15906.8] final descent from 149 m AGL at 43.0 m/s (vertical -35.2 m/s)
    [AutoTest    15907.5] landing site: 1.1° slope 12 m away (ground below: 6.1°)
    [AutoTest    15955.3] screenshot: lunar_045_luma_landed.png
    [AutoTest    15955.3] CHECK PASS: landed on Luma — situation Landed, tilt 0.2 deg, legs intact 4/4, crew 1
    [AutoTest    15955.3] delta-v left on the surface: 1627 m/s vac (1627)
    [AutoTest    15955.3] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15955.3] PHASE EVA
    [AutoTest    15955.3] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.37 m/s rel. lander
    [AutoTest    15955.3] lander watch: tilt 0.2 deg, surface speed 0.04 m/s, unity v 0.37, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 6.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN], frame v 8.47
    [AutoTest    15955.8] lander watch: tilt 0.3 deg, surface speed 0.02 m/s, unity v 0.86, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.05 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.9 kN], frame v 8.51
    [AutoTest    15956.3] lander watch: tilt 0.3 deg, surface speed 0.01 m/s, unity v 1.65, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.5 kN], frame v 8.62
    [AutoTest    15956.8] lander watch: tilt 0.4 deg, surface speed 0.01 m/s, unity v 2.46, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 8.82
    [AutoTest    15957.4] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 3.29, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 9.08
    [AutoTest    15957.9] lander watch: tilt 0.8 deg, surface speed 0.03 m/s, unity v 4.12, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN], frame v 9.41
    [AutoTest    15958.4] lander watch: tilt 1.0 deg, surface speed 0.38 m/s, unity v 0.82, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN], frame v 8.40
    [AutoTest    15958.9] lander watch: tilt 0.9 deg, surface speed 0.34 m/s, unity v 1.58, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 7.0 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 7.7 kN], frame v 7.56
    [AutoTest    15959.4] lander watch: tilt 1.4 deg, surface speed 0.09 m/s, unity v 1.75, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.05 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.6 kN], frame v 7.37
    [AutoTest    15960.0] lander watch: tilt 1.9 deg, surface speed 0.09 m/s, unity v 1.76, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 7.35
    [AutoTest    15960.5] lander watch: tilt 2.7 deg, surface speed 0.11 m/s, unity v 1.74, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 7.35
    [AutoTest    15961.0] lander watch: tilt 3.4 deg, surface speed 0.08 m/s, unity v 1.75, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.3 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 2.7 kN], frame v 7.35
    [AutoTest    15961.5] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 1.81, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.3 kN] [contact 0.02 m 2.7 kN] [contact 0.03 m 2.5 kN], frame v 7.35
    [AutoTest    15962.0] lander watch: tilt 3.4 deg, surface speed 0.07 m/s, unity v 1.85, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN] [contact 0.02 m 1.4 kN] [contact 0.03 m 3.4 kN], frame v 7.34
    [AutoTest    15962.6] lander watch: tilt 2.9 deg, surface speed 0.06 m/s, unity v 1.84, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN], frame v 7.34
    [AutoTest    15963.1] lander watch: tilt 2.6 deg, surface speed 0.03 m/s, unity v 1.82, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.34
    [AutoTest    15963.6] lander watch: tilt 2.5 deg, surface speed 0.00 m/s, unity v 1.81, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 7.34
    [AutoTest    15964.1] lander watch: tilt 2.6 deg, surface speed 0.03 m/s, unity v 1.79, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.34
    [AutoTest    15964.6] lander watch: tilt 3.0 deg, surface speed 0.06 m/s, unity v 1.77, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.8 kN] [contact 0.04 m 4.1 kN], frame v 7.34
    [AutoTest    15965.2] lander watch: tilt 3.3 deg, surface speed 0.03 m/s, unity v 1.79, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.4 kN] [contact 0.01 m 1.7 kN] [contact 0.03 m 3.9 kN], frame v 7.34
    [AutoTest    15965.3] CHECK PASS: walking in low gravity — 12.0 m in 7.0 s (AGL 0.6 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2284, 1926), speed 1.81 m/s, jetpack 10.00 kg)
    [AutoTest    15965.3] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2284, 1926), gravity 1.60 m/s²
    [AutoTest    15965.7] lander watch: tilt 2.3 deg, surface speed 0.12 m/s, unity v 2.49, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.61
    [AutoTest    15966.2] lander watch: tilt 1.7 deg, surface speed 0.03 m/s, unity v 1.91, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.06 m 5.7 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 5.7 kN], frame v 7.47
    [AutoTest    15966.7] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.70, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 7.41
    [AutoTest    15967.2] lander watch: tilt 1.9 deg, surface speed 0.03 m/s, unity v 1.86, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 7.45
    [AutoTest    15967.8] lander watch: tilt 2.2 deg, surface speed 0.05 m/s, unity v 2.32, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 7.59
    [AutoTest    15968.3] lander watch: tilt 2.7 deg, surface speed 0.08 m/s, unity v 2.95, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 7.81
    [AutoTest    15968.4] CHECK PASS: jump in low gravity — apex 1.87 m
    [AutoTest    15968.8] lander watch: tilt 3.1 deg, surface speed 0.15 m/s, unity v 1.18, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.01 m 3.5 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 3.3 kN], frame v 7.72
    [AutoTest    15969.3] lander watch: tilt 3.4 deg, surface speed 0.03 m/s, unity v 0.37, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.02 m 1.7 kN] [contact 0.03 m 3.4 kN], frame v 8.46
    [AutoTest    15969.8] lander watch: tilt 3.4 deg, surface speed 0.02 m/s, unity v 0.05, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.4 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.9 kN], frame v 8.77
    [AutoTest    15970.4] lander watch: tilt 3.1 deg, surface speed 0.03 m/s, unity v 0.04, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN], frame v 8.78
    [AutoTest    15970.4] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15970.4] screenshot: lunar_085_eva_flag.png
    [AutoTest    15970.4] PHASE return to hatch
    [AutoTest    15970.4] screenshot: lunar_087_lander_before_return.png
    [AutoTest    15970.9] lander watch: tilt 3.0 deg, surface speed 0.02 m/s, unity v 0.76, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.2 kN] [contact 0.04 m 4.2 kN], frame v 9.45
    [AutoTest    15971.4] lander watch: tilt 2.9 deg, surface speed 0.01 m/s, unity v 1.61, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.9 kN] [contact 0.00 m 1.0 kN] [contact 0.04 m 5.1 kN], frame v 10.19
    [AutoTest    15971.9] lander watch: tilt 3.0 deg, surface speed 0.03 m/s, unity v 1.78, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.00 m 0.5 kN] [contact 0.04 m 4.0 kN], frame v 10.32
    [AutoTest    15972.4] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.77, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.8 kN], frame v 10.33
    [AutoTest    15973.0] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.76, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 0.6 kN] [contact 0.03 m 3.4 kN], frame v 10.33
    [AutoTest    15973.5] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.76, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 4.0 kN], frame v 10.34
    [AutoTest    15974.0] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.77, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN], frame v 10.34
    [AutoTest    15974.5] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 1.78, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.00 m 0.5 kN] [contact 0.04 m 4.1 kN], frame v 10.36
    [AutoTest    15975.0] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.79, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.2 kN], frame v 10.37
    [AutoTest    15975.6] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN], frame v 10.37
    [AutoTest    15976.1] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 4.0 kN], frame v 10.37
    [AutoTest    15976.6] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN], frame v 10.37
    [AutoTest    15977.1] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.79, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.1 kN], frame v 10.39
    [AutoTest    15977.6] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.80, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN], frame v 10.38
    [AutoTest    15978.2] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.01 m 1.2 kN] [contact 0.04 m 4.6 kN], frame v 10.38
    [AutoTest    15978.7] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN], frame v 10.39
    [AutoTest    15979.2] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN], frame v 10.39
    [AutoTest    15979.7] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN], frame v 10.39
    [AutoTest    15980.2] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN], frame v 10.39
    [AutoTest    15980.8] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.42, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN] [contact 0.00 m 0.6 kN] [contact 0.04 m 4.2 kN], frame v 10.04
    [AutoTest    15981.3] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.20, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN] [contact 0.00 m 0.7 kN] [contact 0.03 m 3.9 kN], frame v 9.56
    [AutoTest    15981.8] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.71, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN] [contact 0.00 m 0.9 kN] [contact 0.03 m 4.0 kN], frame v 9.43
    [AutoTest    15982.3] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 2.42, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.00 m 0.8 kN] [contact 0.03 m 4.1 kN], frame v 9.46
    [AutoTest    15982.8] lander watch: tilt 3.1 deg, surface speed 0.02 m/s, unity v 2.48, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 4.1 kN], frame v 9.38
    [AutoTest    15983.4] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.86, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.1 kN], frame v 9.18
    [AutoTest    15983.7] screenshot: lunar_113_before_boarding.png
    [AutoTest    15983.7] boarding: lander AGL 4.22 m, situation Landed, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 3.9 kN]
    [AutoTest    15983.9] lander watch: tilt 3.0 deg, surface speed 0.07 m/s, unity v 0.00, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.9 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 8.80
    [AutoTest    15984.2] screenshot: lunar_116_boarded.png
    [AutoTest    15984.2] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15984.2] PHASE lunar ascent
    [AutoTest    15989.9] PHASE lunar gravity turn
    [AutoTest    16012.5] Luma orbit circularization: dv 410.6 m/s, burn 31.7 s, in 174 s
    [AutoTest    16015.9] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16204.9] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.2 s
    [AutoTest    16204.9] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16204.9] delta-v left in Luma orbit after ascent: 888 m/s vac (888)
    [AutoTest    16204.9] PHASE plan return
    [AutoTest    16204.9] CHECK PASS: return trajectory to Tellus found — burn 280.0 m/s in 1528 s, Tellus Pe 32.0 km
    [AutoTest    16204.9] PHASE trans-Tellus injection
    [AutoTest    16204.9] TTI: dv 280.0 m/s, burn 19.5 s, in 1528 s
    [AutoTest    17745.8] TTI: burn complete, residual 0.19 m/s after 22.3 s
    [AutoTest    17745.8] delta-v left after trans-Tellus injection: 609 m/s vac (609)
    [AutoTest    17745.8] PHASE coast home
    [AutoTest    24454.9] Tellus periapsis after SOI exit 36.7 km
    [AutoTest    24454.9] CHECK PASS: return periapsis in reentry corridor — Pe 36.7 km
    [AutoTest    52302.9] PHASE reentry
    [AutoTest    52417.7] screenshot: lunar_135_reentry.png
    [AutoTest    52660.7] arming the parachute at 15.1 km, 385 m/s
    [AutoTest    52986.7] screenshot: lunar_137_touchdown.png
    [AutoTest    52986.7] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 783 K (HS-125 Ablative Heat Shield), max 6.3 g
