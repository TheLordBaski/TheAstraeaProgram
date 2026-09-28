# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.9 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1414 s, 939.9 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 298.3 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.5 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.38 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.0 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2286, 2141), speed 1.81 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2286, 2141), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.85 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 349.4 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.3 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 824 K (HS-125 Ablative Heat Shield), max 6.4 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.5 km, 1195 m/s
    [AutoTest      162.9] MECO: Ap 82.0 km, max Q 36.9 kPa, delta-v left 4639 m/s vac (1163 + 3476)
    [AutoTest      162.9] CHECK PASS: max dynamic pressure survivable — 36.9 kPa
    [AutoTest      162.9] PHASE coast to space
    [AutoTest      181.9] PHASE circularize
    [AutoTest      181.9] circularization: dv 752.8 m/s, burn 54.1 s, in 74 s
    [AutoTest      228.8] circularization: 753 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      232.8] circularization: 705 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      236.8] circularization: 655 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      240.9] circularization: 603 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      244.9] circularization: 551 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      248.9] circularization: 497 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      252.9] circularization: 443 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      256.9] circularization: 388 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      261.0] circularization: 331 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      265.0] circularization: 274 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      269.0] circularization: 215 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      273.0] circularization: 156 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      277.0] circularization: 95 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      281.1] circularization: 33 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      285.1] circularization: 1 m/s to go, 1.0° off, throttle 0.07, spin 0.027 rad/s, SAS Maneuver
    [AutoTest      285.9] circularization: burn complete, residual 0.29 m/s after 57.1 s
    [AutoTest      285.9] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.9 km, e 0.0015
    [AutoTest      285.9] in orbit 284 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      285.9] screenshot: lunar_030_orbit.png
    [AutoTest      285.9] PHASE plan TLI
    [AutoTest      285.9] CHECK PASS: TLI encounter found by patched-conic search — node in 1414 s, 939.9 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.5] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.5] PHASE TLI burn
    [AutoTest      286.5] TLI: dv 939.9 m/s, burn 51.2 s, in 1413 s
    [AutoTest     1674.0] TLI: 940 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1678.1] TLI: 879 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1682.1] TLI: 814 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1686.1] TLI: 747 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1690.1] TLI: 678 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1694.1] TLI: 608 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1698.2] TLI: 537 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1698.5] staging during burn
    [AutoTest     1702.2] TLI: 508 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1706.2] TLI: 482 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1710.2] TLI: 456 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1714.2] TLI: 429 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1718.3] TLI: 402 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1722.3] TLI: 376 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1726.3] TLI: 349 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1730.3] TLI: 321 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1734.3] TLI: 294 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1738.4] TLI: 266 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1742.4] TLI: 239 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1746.4] TLI: 211 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1750.4] TLI: 182 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1754.4] TLI: 154 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1758.5] TLI: 126 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1762.5] TLI: 97 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1766.5] TLI: 68 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1770.5] TLI: 39 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest     1774.5] TLI: 10 m/s to go, 0.2° off, throttle 1.00, spin 0.005 rad/s, SAS Maneuver
    [AutoTest     1778.1] TLI: burn complete, residual 0.30 m/s after 104.1 s
    [AutoTest     1778.1] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 298.3 km (planned 40.0 km)
    [AutoTest     1778.1] delta-v left after TLI: 2940 m/s vac (2940)
    [AutoTest     1778.1] PHASE mid-course correction
    [AutoTest     1778.1] correction: pro -18.38 rad 18.00 nrm -2.89 m/s (score 1297)
    [AutoTest     1778.1] MCC: dv 25.9 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3576.4] MCC: 26 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3580.4] MCC: 2 m/s to go, 0.0° off, throttle 0.26, spin 0.002 rad/s, SAS Maneuver
    [AutoTest     3582.5] MCC: burn complete, residual 0.15 m/s after 6.1 s
    [AutoTest     3582.5] PHASE coast to Luma
    [AutoTest    11444.2] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11444.2] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11444.2] PHASE capture burn
    [AutoTest    11444.2] Luma periapsis 36.5 km in 3483 s
    [AutoTest    11444.2] Luma orbit insertion: dv 507.7 m/s, burn 62.2 s, in 3483 s
    [AutoTest    14896.6] Luma orbit insertion: 508 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14900.6] Luma orbit insertion: 478 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14904.6] Luma orbit insertion: 448 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14908.7] Luma orbit insertion: 416 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14912.7] Luma orbit insertion: 385 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14916.7] Luma orbit insertion: 353 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14920.7] Luma orbit insertion: 321 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14924.7] Luma orbit insertion: 289 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14928.8] Luma orbit insertion: 256 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14932.8] Luma orbit insertion: 223 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14936.8] Luma orbit insertion: 190 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14940.8] Luma orbit insertion: 156 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14944.8] Luma orbit insertion: 122 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14948.9] Luma orbit insertion: 88 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14952.9] Luma orbit insertion: 53 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    14956.9] Luma orbit insertion: 18 m/s to go, 0.0° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    14960.9] Luma orbit insertion: 0 m/s to go, 0.1° off, throttle 0.08, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    14961.2] Luma orbit insertion: burn complete, residual 0.29 m/s after 64.6 s
    [AutoTest    14961.2] screenshot: lunar_096_luma_orbit.png
    [AutoTest    14961.2] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    14961.2] delta-v left in Luma orbit: 2406 m/s vac (2406)
    [AutoTest    14963.3] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    14963.3] PHASE deorbit
    [AutoTest    14966.4] PHASE descent
    [AutoTest    15687.5] suicide burn start at 12259 m AGL, 589 m/s
    [AutoTest    15848.8] final descent from 150 m AGL at 43.1 m/s (vertical -35.2 m/s)
    [AutoTest    15849.4] landing site: 0.7° slope 24 m away (ground below: 2.8°)
    [AutoTest    15900.1] screenshot: lunar_105_luma_landed.png
    [AutoTest    15900.1] CHECK PASS: landed on Luma — situation Landed, tilt 0.5 deg, legs intact 4/4, crew 1
    [AutoTest    15900.1] delta-v left on the surface: 1618 m/s vac (1618)
    [AutoTest    15900.1] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15900.1] PHASE EVA
    [AutoTest    15900.2] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.38 m/s rel. lander
    [AutoTest    15900.2] lander watch: tilt 0.5 deg, surface speed 0.04 m/s, unity v 0.38, w 0.04, parts 17, legs [contact 0.02 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 5.3 kN] [contact 0.02 m 3.4 kN], frame v 8.98
    [AutoTest    15900.7] lander watch: tilt 0.9 deg, surface speed 0.08 m/s, unity v 0.84, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.5 kN], frame v 9.01
    [AutoTest    15901.2] lander watch: tilt 1.5 deg, surface speed 0.09 m/s, unity v 1.64, w 0.02, parts 17, legs [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 9.12
    [AutoTest    15901.7] lander watch: tilt 2.1 deg, surface speed 0.07 m/s, unity v 2.48, w 0.04, parts 17, legs [contact 0.04 m 3.6 kN] [contact 0.02 m 1.6 kN] [contact 0.04 m 3.3 kN] [free 0.00 m 0.0 kN], frame v 9.30
    [AutoTest    15902.2] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 3.29, w 0.04, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 3.0 kN] [free 0.00 m 0.0 kN], frame v 9.55
    [AutoTest    15902.8] lander watch: tilt 2.2 deg, surface speed 0.05 m/s, unity v 4.11, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.02 m 1.1 kN] [contact 0.04 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 9.87
    [AutoTest    15903.3] lander watch: tilt 1.2 deg, surface speed 0.54 m/s, unity v 0.93, w 0.13, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 9.28
    [AutoTest    15903.8] lander watch: tilt 2.7 deg, surface speed 0.25 m/s, unity v 1.68, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 8.4 kN], frame v 9.65
    [AutoTest    15904.3] lander watch: tilt 1.8 deg, surface speed 0.31 m/s, unity v 1.63, w 0.07, parts 17, legs [contact 0.01 m 2.8 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 2.2 kN] [contact 0.09 m 8.5 kN], frame v 9.74
    [AutoTest    15904.8] lander watch: tilt 1.3 deg, surface speed 0.40 m/s, unity v 1.60, w 0.10, parts 17, legs [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN], frame v 9.74
    [AutoTest    15905.4] lander watch: tilt 3.6 deg, surface speed 0.22 m/s, unity v 1.72, w 0.05, parts 17, legs [contact 0.01 m 0.2 kN] [contact 0.05 m 7.3 kN] [contact 0.01 m 0.2 kN] [free 0.00 m 0.0 kN], frame v 9.74
    [AutoTest    15905.9] lander watch: tilt 4.1 deg, surface speed 0.10 m/s, unity v 1.90, w 0.02, parts 17, legs [contact 0.01 m 1.3 kN] [contact 0.07 m 7.3 kN] [contact 0.00 m 0.8 kN] [free 0.00 m 0.0 kN], frame v 9.74
    [AutoTest    15906.4] lander watch: tilt 2.7 deg, surface speed 0.24 m/s, unity v 1.99, w 0.07, parts 17, legs [contact 0.03 m 4.0 kN] [contact 0.02 m 1.2 kN] [contact 0.02 m 3.4 kN] [free 0.00 m 0.0 kN], frame v 9.74
    [AutoTest    15906.9] lander watch: tilt 1.1 deg, surface speed 0.22 m/s, unity v 1.98, w 0.05, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.01 m 2.4 kN], frame v 9.74
    [AutoTest    15907.4] lander watch: tilt 0.7 deg, surface speed 0.06 m/s, unity v 1.88, w 0.04, parts 17, legs [contact 0.02 m 2.5 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 1.6 kN] [contact 0.03 m 3.9 kN], frame v 9.74
    [AutoTest    15908.0] lander watch: tilt 0.6 deg, surface speed 0.11 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.02 m 3.3 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 2.2 kN], frame v 9.73
    [AutoTest    15908.5] lander watch: tilt 1.2 deg, surface speed 0.15 m/s, unity v 1.74, w 0.04, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 9.73
    [AutoTest    15909.0] lander watch: tilt 2.2 deg, surface speed 0.14 m/s, unity v 1.74, w 0.03, parts 17, legs [contact 0.03 m 3.0 kN] [contact 0.01 m 1.9 kN] [contact 0.03 m 3.0 kN] [free 0.00 m 0.0 kN], frame v 9.72
    [AutoTest    15909.5] lander watch: tilt 2.8 deg, surface speed 0.02 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.03 m 3.4 kN] [contact 0.02 m 2.9 kN] [free 0.00 m 0.0 kN], frame v 9.72
    [AutoTest    15910.0] lander watch: tilt 2.6 deg, surface speed 0.08 m/s, unity v 1.86, w 0.05, parts 17, legs [contact 0.03 m 3.1 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN], frame v 9.72
    [AutoTest    15910.2] CHECK PASS: walking in low gravity — 12.0 m in 7.0 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2286, 2141), speed 1.81 m/s, jetpack 10.00 kg)
    [AutoTest    15910.2] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2286, 2141), gravity 1.60 m/s²
    [AutoTest    15910.6] lander watch: tilt 0.9 deg, surface speed 0.19 m/s, unity v 2.55, w 0.09, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 9.81
    [AutoTest    15911.1] lander watch: tilt 0.8 deg, surface speed 0.07 m/s, unity v 1.87, w 0.05, parts 17, legs [contact 0.03 m 2.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.0 kN] [contact 0.04 m 3.3 kN], frame v 9.70
    [AutoTest    15911.6] lander watch: tilt 0.9 deg, surface speed 0.15 m/s, unity v 1.62, w 0.04, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.2 kN], frame v 9.66
    [AutoTest    15912.1] lander watch: tilt 1.8 deg, surface speed 0.15 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.05 m 4.1 kN] [contact 0.01 m 1.5 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 9.69
    [AutoTest    15912.6] lander watch: tilt 2.6 deg, surface speed 0.06 m/s, unity v 2.35, w 0.05, parts 17, legs [contact 0.03 m 3.1 kN] [contact 0.03 m 3.1 kN] [contact 0.03 m 2.2 kN] [free 0.00 m 0.0 kN], frame v 9.80
    [AutoTest    15913.2] lander watch: tilt 2.6 deg, surface speed 0.06 m/s, unity v 3.01, w 0.05, parts 17, legs [contact 0.03 m 3.4 kN] [contact 0.03 m 2.4 kN] [contact 0.03 m 2.7 kN] [free 0.00 m 0.0 kN], frame v 9.97
    [AutoTest    15913.2] CHECK PASS: jump in low gravity — apex 1.85 m
    [AutoTest    15913.7] lander watch: tilt 1.4 deg, surface speed 0.22 m/s, unity v 1.35, w 0.08, parts 17, legs [contact 0.01 m 3.5 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 9.50
    [AutoTest    15914.2] lander watch: tilt 0.8 deg, surface speed 0.04 m/s, unity v 0.53, w 0.03, parts 17, legs [contact 0.03 m 2.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [contact 0.02 m 1.5 kN], frame v 9.08
    [AutoTest    15914.7] lander watch: tilt 0.9 deg, surface speed 0.05 m/s, unity v 0.05, w 0.02, parts 17, legs [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 1.0 kN], frame v 8.85
    [AutoTest    15915.2] lander watch: tilt 1.3 deg, surface speed 0.07 m/s, unity v 0.05, w 0.02, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.84
    [AutoTest    15915.3] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15915.3] screenshot: lunar_145_eva_flag.png
    [AutoTest    15915.3] PHASE return to hatch
    [AutoTest    15915.3] screenshot: lunar_147_lander_before_return.png
    [AutoTest    15915.8] lander watch: tilt 1.8 deg, surface speed 0.09 m/s, unity v 0.75, w 0.02, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.00 m 1.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.58
    [AutoTest    15916.3] lander watch: tilt 2.2 deg, surface speed 0.04 m/s, unity v 1.51, w 0.04, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.01 m 1.9 kN] [contact 0.03 m 2.9 kN] [free 0.00 m 0.0 kN], frame v 8.35
    [AutoTest    15916.8] lander watch: tilt 2.2 deg, surface speed 0.03 m/s, unity v 1.74, w 0.03, parts 17, legs [contact 0.03 m 3.8 kN] [contact 0.02 m 1.6 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN], frame v 8.28
    [AutoTest    15917.3] lander watch: tilt 1.9 deg, surface speed 0.05 m/s, unity v 1.73, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.4 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.27
    [AutoTest    15917.8] lander watch: tilt 1.6 deg, surface speed 0.04 m/s, unity v 1.74, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.24
    [AutoTest    15918.4] lander watch: tilt 1.4 deg, surface speed 0.02 m/s, unity v 1.76, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.23
    [AutoTest    15918.9] lander watch: tilt 1.3 deg, surface speed 0.01 m/s, unity v 1.78, w 0.00, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 8.22
    [AutoTest    15919.4] lander watch: tilt 1.3 deg, surface speed 0.00 m/s, unity v 1.79, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.22
    [AutoTest    15919.9] lander watch: tilt 1.4 deg, surface speed 0.02 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.22
    [AutoTest    15920.5] lander watch: tilt 1.6 deg, surface speed 0.03 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.22
    [AutoTest    15921.0] lander watch: tilt 1.8 deg, surface speed 0.04 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.00 m 0.6 kN] [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 8.22
    [AutoTest    15921.5] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 1.78, w 0.03, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 8.23
    [AutoTest    15922.0] lander watch: tilt 2.0 deg, surface speed 0.02 m/s, unity v 1.76, w 0.02, parts 17, legs [contact 0.03 m 4.0 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN], frame v 8.24
    [AutoTest    15922.6] lander watch: tilt 1.9 deg, surface speed 0.03 m/s, unity v 1.74, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 8.25
    [AutoTest    15923.1] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.75, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.24
    [AutoTest    15923.6] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.76, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.24
    [AutoTest    15924.1] lander watch: tilt 1.8 deg, surface speed 0.02 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.25
    [AutoTest    15924.6] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 8.26
    [AutoTest    15925.2] lander watch: tilt 1.9 deg, surface speed 0.00 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.6 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 8.30
    [AutoTest    15925.7] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.04 m 5.1 kN] [contact 0.01 m 1.4 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 8.28
    [AutoTest    15926.2] lander watch: tilt 1.8 deg, surface speed 0.01 m/s, unity v 1.21, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.46
    [AutoTest    15926.7] lander watch: tilt 1.8 deg, surface speed 0.01 m/s, unity v 1.49, w 0.01, parts 17, legs [contact 0.03 m 4.2 kN] [contact 0.00 m 0.5 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.67
    [AutoTest    15927.2] lander watch: tilt 1.9 deg, surface speed 0.00 m/s, unity v 2.15, w 0.01, parts 17, legs [contact 0.03 m 4.1 kN] [contact 0.00 m 0.5 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 8.89
    [AutoTest    15927.8] lander watch: tilt 1.9 deg, surface speed 0.02 m/s, unity v 2.62, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.00 m 0.4 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 9.05
    [AutoTest    15928.3] lander watch: tilt 1.9 deg, surface speed 0.01 m/s, unity v 2.06, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 0.4 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.94
    [AutoTest    15928.8] lander watch: tilt 1.8 deg, surface speed 0.00 m/s, unity v 1.49, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.85
    [AutoTest    15928.8] screenshot: lunar_174_before_boarding.png
    [AutoTest    15928.8] boarding: lander AGL 4.19 m, situation Landed, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN]
    [AutoTest    15929.3] lander watch: tilt 1.2 deg, surface speed 0.14 m/s, unity v 0.00, w 0.03, parts 17, legs [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 5.1 kN] [free 0.00 m 0.0 kN], frame v 8.67
    [AutoTest    15929.3] screenshot: lunar_177_boarded.png
    [AutoTest    15929.3] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15929.3] PHASE lunar ascent
    [AutoTest    15935.0] PHASE lunar gravity turn
    [AutoTest    15957.5] Luma orbit circularization: dv 411.7 m/s, burn 31.7 s, in 174 s
    [AutoTest    15960.1] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16115.4] Luma orbit circularization: 411 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16119.4] Luma orbit circularization: 363 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16123.5] Luma orbit circularization: 313 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16127.5] Luma orbit circularization: 262 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16131.5] Luma orbit circularization: 210 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16135.5] Luma orbit circularization: 157 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16139.5] Luma orbit circularization: 104 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16143.6] Luma orbit circularization: 50 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    16147.6] Luma orbit circularization: 4 m/s to go, 0.1° off, throttle 0.27, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    16149.6] Luma orbit circularization: burn complete, residual 0.30 m/s after 34.2 s
    [AutoTest    16149.6] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16149.6] delta-v left in Luma orbit after ascent: 879 m/s vac (879)
    [AutoTest    16149.6] PHASE plan return
    [AutoTest    16149.6] CHECK PASS: return trajectory to Tellus found — burn 349.4 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16149.6] PHASE trans-Tellus injection
    [AutoTest    16149.6] TTI: dv 349.4 m/s, burn 24.0 s, in 1168 s
    [AutoTest    17305.6] TTI: 349 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17309.6] TTI: 295 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17313.6] TTI: 239 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17317.6] TTI: 181 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17321.6] TTI: 122 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17325.7] TTI: 62 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17329.7] TTI: 7 m/s to go, 0.0° off, throttle 0.39, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17332.4] TTI: burn complete, residual 0.19 m/s after 26.8 s
    [AutoTest    17332.4] delta-v left after trans-Tellus injection: 530 m/s vac (530)
    [AutoTest    17332.4] PHASE coast home
    [AutoTest    22494.0] Tellus periapsis after SOI exit 31.3 km
    [AutoTest    22494.0] CHECK PASS: return periapsis in reentry corridor — Pe 31.3 km
    [AutoTest    34783.2] PHASE reentry
    [AutoTest    34891.2] screenshot: lunar_212_reentry.png
    [AutoTest    35027.9] arming the parachute at 16.1 km, 422 m/s
    [AutoTest    35361.9] screenshot: lunar_214_touchdown.png
    [AutoTest    35361.9] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 824 K (HS-125 Ablative Heat Shield), max 6.4 g
