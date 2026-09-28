# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 938.6 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 296.9 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.7 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.0 s (AGL 0.7 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2287, 1921), speed 1.82 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2287, 1921), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.66 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 348.5 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.4 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 824 K (HS-125 Ablative Heat Shield), max 6.4 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.5 km, 1195 m/s
    [AutoTest      163.0] MECO: Ap 82.0 km, max Q 36.9 kPa, delta-v left 4639 m/s vac (1163 + 3476)
    [AutoTest      163.0] CHECK PASS: max dynamic pressure survivable — 36.9 kPa
    [AutoTest      163.0] PHASE coast to space
    [AutoTest      181.9] PHASE circularize
    [AutoTest      181.9] circularization: dv 752.4 m/s, burn 54.1 s, in 74 s
    [AutoTest      228.9] circularization: 752 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      232.9] circularization: 705 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      236.9] circularization: 654 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      240.9] circularization: 603 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      244.9] circularization: 550 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      249.0] circularization: 497 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      253.0] circularization: 442 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      257.0] circularization: 387 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      261.0] circularization: 331 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      265.0] circularization: 273 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      269.1] circularization: 215 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      273.1] circularization: 155 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      277.1] circularization: 94 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      281.1] circularization: 32 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      285.1] circularization: 1 m/s to go, 1.1° off, throttle 0.07, spin 0.023 rad/s, SAS Maneuver
    [AutoTest      286.0] circularization: burn complete, residual 0.29 m/s after 57.2 s
    [AutoTest      286.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.0] in orbit 285 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      286.0] screenshot: lunar_030_orbit.png
    [AutoTest      286.0] PHASE plan TLI
    [AutoTest      286.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 938.6 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.6] PHASE TLI burn
    [AutoTest      286.6] TLI: dv 938.6 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1673.8] TLI: 939 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1677.8] TLI: 878 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1681.9] TLI: 812 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1685.9] TLI: 746 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1689.9] TLI: 677 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1693.9] TLI: 607 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1697.9] TLI: 536 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1698.3] staging during burn
    [AutoTest     1702.0] TLI: 507 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1706.0] TLI: 481 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1710.0] TLI: 454 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1714.0] TLI: 428 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1718.0] TLI: 401 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1722.1] TLI: 374 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1726.1] TLI: 347 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1730.1] TLI: 320 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1734.1] TLI: 293 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1738.1] TLI: 265 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1742.2] TLI: 237 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1746.2] TLI: 209 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1750.2] TLI: 181 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1754.2] TLI: 153 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1758.2] TLI: 124 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1762.3] TLI: 96 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1766.3] TLI: 67 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1770.3] TLI: 38 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1774.3] TLI: 9 m/s to go, 0.2° off, throttle 1.00, spin 0.006 rad/s, SAS Maneuver
    [AutoTest     1777.7] TLI: burn complete, residual 0.29 m/s after 103.9 s
    [AutoTest     1777.7] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 296.9 km (planned 40.0 km)
    [AutoTest     1777.7] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.7] PHASE mid-course correction
    [AutoTest     1777.7] correction: pro -18.00 rad 18.00 nrm 2.70 m/s (score 1282)
    [AutoTest     1777.7] MCC: dv 25.6 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3576.0] MCC: 26 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3580.1] MCC: 2 m/s to go, 0.0° off, throttle 0.25, spin 0.002 rad/s, SAS Maneuver
    [AutoTest     3582.1] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.1] PHASE coast to Luma
    [AutoTest    11483.0] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11483.0] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11483.0] PHASE capture burn
    [AutoTest    11483.0] Luma periapsis 36.5 km in 3502 s
    [AutoTest    11483.0] Luma orbit insertion: dv 504.6 m/s, burn 61.9 s, in 3502 s
    [AutoTest    14954.2] Luma orbit insertion: 505 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14958.2] Luma orbit insertion: 475 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14962.2] Luma orbit insertion: 444 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14966.2] Luma orbit insertion: 413 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14970.2] Luma orbit insertion: 382 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14974.3] Luma orbit insertion: 350 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14978.3] Luma orbit insertion: 318 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14982.3] Luma orbit insertion: 286 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14986.3] Luma orbit insertion: 253 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14990.3] Luma orbit insertion: 220 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14994.4] Luma orbit insertion: 187 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14998.4] Luma orbit insertion: 153 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15002.4] Luma orbit insertion: 119 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15006.4] Luma orbit insertion: 85 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15010.4] Luma orbit insertion: 51 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    15014.5] Luma orbit insertion: 16 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    15018.5] Luma orbit insertion: burn complete, residual 0.30 m/s after 64.3 s
    [AutoTest    15018.5] screenshot: lunar_095_luma_orbit.png
    [AutoTest    15018.5] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    15018.5] delta-v left in Luma orbit: 2411 m/s vac (2411)
    [AutoTest    15020.5] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    15020.5] PHASE deorbit
    [AutoTest    15023.7] PHASE descent
    [AutoTest    15742.7] suicide burn start at 12226 m AGL, 589 m/s
    [AutoTest    15904.5] final descent from 150 m AGL at 43.1 m/s (vertical -35.2 m/s)
    [AutoTest    15905.1] landing site: 0.8° slope 36 m away (ground below: 4.8°)
    [AutoTest    15954.5] screenshot: lunar_104_luma_landed.png
    [AutoTest    15954.5] CHECK PASS: landed on Luma — situation Landed, tilt 0.7 deg, legs intact 4/4, crew 1
    [AutoTest    15954.5] delta-v left on the surface: 1624 m/s vac (1624)
    [AutoTest    15954.5] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15954.5] PHASE EVA
    [AutoTest    15954.5] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
    [AutoTest    15954.5] lander watch: tilt 0.7 deg, surface speed 0.05 m/s, unity v 0.35, w 0.05, parts 17, legs [contact 0.04 m 5.4 kN] [contact 0.00 m 1.6 kN] [contact 0.04 m 5.6 kN] [contact 0.00 m 1.7 kN], frame v 8.57
    [AutoTest    15955.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 0.86, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 4.3 kN] [contact 0.01 m 0.3 kN], frame v 8.60
    [AutoTest    15955.6] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.64, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 8.72
    [AutoTest    15956.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 2.46, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 8.91
    [AutoTest    15956.6] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 3.29, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 9.17
    [AutoTest    15957.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 4.12, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 9.50
    [AutoTest    15957.7] lander watch: tilt 0.7 deg, surface speed 0.73 m/s, unity v 0.85, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.48
    [AutoTest    15958.2] lander watch: tilt 0.8 deg, surface speed 0.11 m/s, unity v 1.36, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.01
    [AutoTest    15958.7] lander watch: tilt 0.9 deg, surface speed 0.58 m/s, unity v 1.91, w 0.06, parts 17, legs [contact 0.02 m 13.5 kN] [contact 0.02 m 12.3 kN] [contact 0.03 m 14.4 kN] [free 0.00 m 0.0 kN], frame v 7.80
    [AutoTest    15959.2] lander watch: tilt 0.7 deg, surface speed 0.10 m/s, unity v 1.88, w 0.03, parts 17, legs [contact 0.03 m 2.9 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.5 kN] [contact 0.01 m 1.6 kN], frame v 7.80
    [AutoTest    15959.7] lander watch: tilt 0.9 deg, surface speed 0.02 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.03 m 3.4 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 2.3 kN], frame v 7.80
    [AutoTest    15960.3] lander watch: tilt 0.7 deg, surface speed 0.08 m/s, unity v 1.74, w 0.04, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.00 m 1.3 kN] [contact 0.03 m 4.1 kN] [contact 0.01 m 0.4 kN], frame v 7.80
    [AutoTest    15960.8] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.82, w 0.03, parts 17, legs [contact 0.03 m 2.7 kN] [contact 0.01 m 1.3 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN], frame v 7.78
    [AutoTest    15961.3] lander watch: tilt 0.7 deg, surface speed 0.04 m/s, unity v 1.86, w 0.03, parts 17, legs [contact 0.03 m 3.7 kN] [contact 0.01 m 0.5 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.9 kN], frame v 7.78
    [AutoTest    15961.8] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.82, w 0.03, parts 17, legs [contact 0.03 m 3.7 kN] [contact 0.00 m 0.2 kN] [contact 0.03 m 3.6 kN] [contact 0.01 m 1.2 kN], frame v 7.78
    [AutoTest    15962.3] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 1.81, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 0.5 kN], frame v 7.78
    [AutoTest    15962.9] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.83, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 3.7 kN] [contact 0.00 m 0.5 kN], frame v 7.78
    [AutoTest    15963.4] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.83, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.01 m 0.6 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.8 kN], frame v 7.78
    [AutoTest    15963.9] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.82, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.01 m 0.6 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.7 kN], frame v 7.78
    [AutoTest    15964.4] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.82, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.6 kN], frame v 7.78
    [AutoTest    15964.5] CHECK PASS: walking in low gravity — 12.0 m in 7.0 s (AGL 0.7 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2287, 1921), speed 1.82 m/s, jetpack 10.00 kg)
    [AutoTest    15964.5] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2287, 1921), gravity 1.60 m/s²
    [AutoTest    15964.9] lander watch: tilt 1.0 deg, surface speed 0.02 m/s, unity v 2.40, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.00
    [AutoTest    15965.5] lander watch: tilt 1.0 deg, surface speed 0.10 m/s, unity v 1.99, w 0.09, parts 17, legs [contact 0.04 m 4.5 kN] [contact 0.04 m 3.1 kN] [contact 0.06 m 5.7 kN] [contact 0.02 m 2.7 kN], frame v 7.87
    [AutoTest    15966.0] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 1.73, w 0.04, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.01 m 0.3 kN] [contact 0.03 m 2.0 kN] [contact 0.02 m 1.9 kN], frame v 7.83
    [AutoTest    15966.5] lander watch: tilt 0.7 deg, surface speed 0.05 m/s, unity v 1.86, w 0.05, parts 17, legs [contact 0.04 m 3.1 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.4 kN] [contact 0.02 m 1.0 kN], frame v 7.87
    [AutoTest    15967.0] lander watch: tilt 0.8 deg, surface speed 0.00 m/s, unity v 2.39, w 0.04, parts 17, legs [contact 0.03 m 2.6 kN] [contact 0.02 m 1.4 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 8.00
    [AutoTest    15967.4] CHECK PASS: jump in low gravity — apex 1.66 m
    [AutoTest    15967.5] lander watch: tilt 0.8 deg, surface speed 0.36 m/s, unity v 1.70, w 0.16, parts 17, legs [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.90
    [AutoTest    15968.1] lander watch: tilt 0.7 deg, surface speed 0.18 m/s, unity v 0.77, w 0.12, parts 17, legs [contact 0.02 m 6.0 kN] [contact 0.01 m 4.4 kN] [contact 0.02 m 5.3 kN] [contact 0.02 m 5.6 kN], frame v 8.36
    [AutoTest    15968.6] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 0.05, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 1.6 kN], frame v 8.76
    [AutoTest    15969.1] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 0.02, w 0.05, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.02 m 2.0 kN] [contact 0.02 m 2.8 kN] [contact 0.01 m 1.7 kN], frame v 8.79
    [AutoTest    15969.5] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15969.5] screenshot: lunar_143_eva_flag.png
    [AutoTest    15969.5] PHASE return to hatch
    [AutoTest    15969.5] screenshot: lunar_145_lander_before_return.png
    [AutoTest    15969.6] lander watch: tilt 0.8 deg, surface speed 0.02 m/s, unity v 0.23, w 0.05, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 1.8 kN], frame v 8.96
    [AutoTest    15970.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.11, w 0.05, parts 17, legs [contact 0.02 m 2.5 kN] [contact 0.02 m 1.7 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.0 kN], frame v 9.54
    [AutoTest    15970.7] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.75, w 0.05, parts 17, legs [contact 0.02 m 2.5 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 1.9 kN], frame v 9.98
    [AutoTest    15971.2] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.02 m 2.0 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 1.7 kN], frame v 10.02
    [AutoTest    15971.7] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.03
    [AutoTest    15972.2] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 1.9 kN], frame v 10.02
    [AutoTest    15972.7] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.02
    [AutoTest    15973.3] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.02 m 2.3 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 1.7 kN], frame v 10.00
    [AutoTest    15973.8] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.8 kN], frame v 10.00
    [AutoTest    15974.3] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 1.9 kN], frame v 10.00
    [AutoTest    15974.8] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.06, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.8 kN], frame v 10.00
    [AutoTest    15975.3] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.01
    [AutoTest    15975.9] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.00
    [AutoTest    15976.4] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.00
    [AutoTest    15976.9] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.8 kN], frame v 10.00
    [AutoTest    15977.4] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.00
    [AutoTest    15977.9] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 1.79, w 0.04, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.02 m 1.7 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 1.7 kN], frame v 10.01
    [AutoTest    15978.5] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 1.8 kN], frame v 10.02
    [AutoTest    15979.0] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 10.02
    [AutoTest    15979.5] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.74, w 0.05, parts 17, legs [contact 0.02 m 2.6 kN] [contact 0.02 m 2.0 kN] [contact 0.02 m 2.8 kN] [contact 0.02 m 2.1 kN], frame v 9.96
    [AutoTest    15980.0] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.10, w 0.05, parts 17, legs [contact 0.02 m 2.3 kN] [contact 0.01 m 1.9 kN] [contact 0.02 m 2.7 kN] [contact 0.01 m 1.7 kN], frame v 9.44
    [AutoTest    15980.5] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.39, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.01 m 1.9 kN] [contact 0.02 m 2.6 kN] [contact 0.01 m 1.8 kN], frame v 9.29
    [AutoTest    15981.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 2.05, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.01 m 1.8 kN] [contact 0.02 m 2.5 kN] [contact 0.01 m 1.9 kN], frame v 9.31
    [AutoTest    15981.6] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 2.62, w 0.05, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.01 m 1.7 kN] [contact 0.02 m 2.4 kN] [contact 0.01 m 1.7 kN], frame v 9.37
    [AutoTest    15982.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 2.10, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.9 kN] [contact 0.03 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 9.19
    [AutoTest    15982.6] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.52, w 0.05, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.03 m 2.6 kN] [contact 0.02 m 1.9 kN], frame v 9.04
    [AutoTest    15982.7] screenshot: lunar_172_before_boarding.png
    [AutoTest    15982.7] boarding: lander AGL 4.26 m, situation Landed, legs [contact 0.02 m 2.4 kN] [contact 0.02 m 1.8 kN] [contact 0.03 m 2.6 kN] [contact 0.02 m 1.9 kN]
    [AutoTest    15983.1] lander watch: tilt 0.8 deg, surface speed 0.00 m/s, unity v 0.00, w 0.03, parts 17, legs [contact 0.02 m 2.3 kN] [contact 0.02 m 1.6 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 2.0 kN], frame v 8.80
    [AutoTest    15983.2] screenshot: lunar_175_boarded.png
    [AutoTest    15983.2] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15983.2] PHASE lunar ascent
    [AutoTest    15988.9] PHASE lunar gravity turn
    [AutoTest    16011.5] Luma orbit circularization: dv 410.4 m/s, burn 31.6 s, in 174 s
    [AutoTest    16013.9] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16169.9] Luma orbit circularization: 410 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16173.9] Luma orbit circularization: 362 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16177.9] Luma orbit circularization: 312 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16182.0] Luma orbit circularization: 261 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16186.0] Luma orbit circularization: 209 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16190.0] Luma orbit circularization: 156 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16194.0] Luma orbit circularization: 103 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16198.0] Luma orbit circularization: 49 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16202.1] Luma orbit circularization: 4 m/s to go, 0.1° off, throttle 0.25, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    16204.1] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.2 s
    [AutoTest    16204.1] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16204.1] delta-v left in Luma orbit after ascent: 885 m/s vac (885)
    [AutoTest    16204.1] PHASE plan return
    [AutoTest    16204.1] CHECK PASS: return trajectory to Tellus found — burn 348.5 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16204.1] PHASE trans-Tellus injection
    [AutoTest    16204.1] TTI: dv 348.5 m/s, burn 24.0 s, in 1168 s
    [AutoTest    17360.0] TTI: 348 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17364.0] TTI: 295 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17368.1] TTI: 238 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17372.1] TTI: 180 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17376.1] TTI: 121 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17380.1] TTI: 61 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    17384.1] TTI: 6 m/s to go, 0.1° off, throttle 0.38, spin 0.004 rad/s, SAS Maneuver
    [AutoTest    17386.8] TTI: burn complete, residual 0.20 m/s after 26.8 s
    [AutoTest    17386.8] delta-v left after trans-Tellus injection: 537 m/s vac (537)
    [AutoTest    17386.8] PHASE coast home
    [AutoTest    22562.5] Tellus periapsis after SOI exit 31.4 km
    [AutoTest    22562.5] CHECK PASS: return periapsis in reentry corridor — Pe 31.4 km
    [AutoTest    34881.5] PHASE reentry
    [AutoTest    34989.5] screenshot: lunar_210_reentry.png
    [AutoTest    35126.6] arming the parachute at 16.1 km, 422 m/s
    [AutoTest    35460.7] screenshot: lunar_212_touchdown.png
    [AutoTest    35460.7] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 824 K (HS-125 Ablative Heat Shield), max 6.4 g
