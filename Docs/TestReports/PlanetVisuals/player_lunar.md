# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1414 s, 939.8 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 298.3 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.5 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 6.9 s (AGL 0.6 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2267, 2145), speed 1.79 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2267, 2145), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.84 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 280.0 m/s in 1528 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 36.7 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 782 K (HS-125 Ablative Heat Shield), max 6.3 g

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
    [AutoTest      228.9] circularization: 752 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      232.9] circularization: 705 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      236.9] circularization: 654 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      240.9] circularization: 603 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      244.9] circularization: 550 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      249.0] circularization: 497 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      253.0] circularization: 442 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      257.0] circularization: 387 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      261.0] circularization: 331 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      265.0] circularization: 273 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      269.1] circularization: 215 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      273.1] circularization: 155 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      277.1] circularization: 94 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      281.1] circularization: 32 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest      285.1] circularization: 1 m/s to go, 1.6° off, throttle 0.07, spin 0.039 rad/s, SAS Maneuver
    [AutoTest      285.9] circularization: burn complete, residual 0.29 m/s after 57.1 s
    [AutoTest      285.9] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      285.9] in orbit 284 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      285.9] screenshot: lunar_030_orbit.png
    [AutoTest      285.9] PHASE plan TLI
    [AutoTest      285.9] CHECK PASS: TLI encounter found by patched-conic search — node in 1414 s, 939.8 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.4] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.4] PHASE TLI burn
    [AutoTest      286.4] TLI: dv 939.8 m/s, burn 51.2 s, in 1413 s
    [AutoTest     1674.0] TLI: 940 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1678.0] TLI: 879 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1682.1] TLI: 814 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1686.1] TLI: 747 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1690.1] TLI: 678 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1694.1] TLI: 608 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1698.1] TLI: 537 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1698.5] staging during burn
    [AutoTest     1702.2] TLI: 508 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1706.2] TLI: 482 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1710.2] TLI: 455 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1714.2] TLI: 429 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1718.2] TLI: 402 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1722.3] TLI: 376 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1726.3] TLI: 348 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1730.3] TLI: 321 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1734.3] TLI: 294 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1738.3] TLI: 266 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1742.4] TLI: 238 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1746.4] TLI: 211 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1750.4] TLI: 182 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1754.4] TLI: 154 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1758.4] TLI: 126 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1762.5] TLI: 97 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1766.5] TLI: 68 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1770.5] TLI: 39 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1774.5] TLI: 10 m/s to go, 0.2° off, throttle 1.00, spin 0.006 rad/s, SAS Maneuver
    [AutoTest     1778.1] TLI: burn complete, residual 0.29 m/s after 104.1 s
    [AutoTest     1778.1] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 298.3 km (planned 40.0 km)
    [AutoTest     1778.1] delta-v left after TLI: 2940 m/s vac (2940)
    [AutoTest     1778.1] PHASE mid-course correction
    [AutoTest     1778.1] correction: pro -18.38 rad 18.00 nrm -3.04 m/s (score 1296)
    [AutoTest     1778.1] MCC: dv 25.9 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3576.4] MCC: 26 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3580.4] MCC: 2 m/s to go, 0.1° off, throttle 0.26, spin 0.005 rad/s, SAS Maneuver
    [AutoTest     3582.4] MCC: burn complete, residual 0.15 m/s after 6.1 s
    [AutoTest     3582.4] PHASE coast to Luma
    [AutoTest    11445.1] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11445.1] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11445.1] PHASE capture burn
    [AutoTest    11445.1] Luma periapsis 36.5 km in 3484 s
    [AutoTest    11445.1] Luma orbit insertion: dv 507.7 m/s, burn 62.2 s, in 3484 s
    [AutoTest    14897.9] Luma orbit insertion: 508 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14901.9] Luma orbit insertion: 478 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14905.9] Luma orbit insertion: 447 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14910.0] Luma orbit insertion: 416 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14914.0] Luma orbit insertion: 385 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14918.0] Luma orbit insertion: 353 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14922.0] Luma orbit insertion: 321 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14926.0] Luma orbit insertion: 289 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14930.1] Luma orbit insertion: 256 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14934.1] Luma orbit insertion: 223 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14938.1] Luma orbit insertion: 190 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14942.1] Luma orbit insertion: 156 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14946.1] Luma orbit insertion: 122 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14950.2] Luma orbit insertion: 88 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14954.2] Luma orbit insertion: 53 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    14958.2] Luma orbit insertion: 18 m/s to go, 0.0° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    14962.2] Luma orbit insertion: 0 m/s to go, 0.1° off, throttle 0.07, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    14962.5] Luma orbit insertion: burn complete, residual 0.30 m/s after 64.6 s
    [AutoTest    14962.5] screenshot: lunar_096_luma_orbit.png
    [AutoTest    14962.5] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    14962.5] delta-v left in Luma orbit: 2407 m/s vac (2407)
    [AutoTest    14964.5] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    14964.5] PHASE deorbit
    [AutoTest    14967.7] PHASE descent
    [AutoTest    15685.8] suicide burn start at 12282 m AGL, 589 m/s
    [AutoTest    15847.1] final descent from 149 m AGL at 42.9 m/s (vertical -35.1 m/s)
    [AutoTest    15847.7] landing site: 1.0° slope 12 m away (ground below: 3.8°)
    [AutoTest    15896.7] screenshot: lunar_105_luma_landed.png
    [AutoTest    15896.7] CHECK PASS: landed on Luma — situation Landed, tilt 0.5 deg, legs intact 4/4, crew 1
    [AutoTest    15896.7] delta-v left on the surface: 1621 m/s vac (1621)
    [AutoTest    15896.7] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15896.7] PHASE EVA
    [AutoTest    15896.8] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
    [AutoTest    15896.8] lander watch: tilt 0.5 deg, surface speed 0.04 m/s, unity v 0.36, w 0.03, parts 17, legs [contact 0.04 m 5.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.8 kN] [free 0.00 m 0.0 kN], frame v 9.01
    [AutoTest    15897.3] lander watch: tilt 0.5 deg, surface speed 0.00 m/s, unity v 0.85, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.00 m 0.0 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 9.04
    [AutoTest    15897.8] lander watch: tilt 0.6 deg, surface speed 0.01 m/s, unity v 1.64, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 9.15
    [AutoTest    15898.3] lander watch: tilt 0.6 deg, surface speed 0.01 m/s, unity v 2.46, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 9.33
    [AutoTest    15898.8] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 3.28, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.00 m 0.0 kN], frame v 9.58
    [AutoTest    15899.4] lander watch: tilt 0.8 deg, surface speed 0.02 m/s, unity v 4.12, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 9.89
    [AutoTest    15899.9] lander watch: tilt 0.7 deg, surface speed 0.57 m/s, unity v 1.31, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 9.47
    [AutoTest    15900.4] lander watch: tilt 0.6 deg, surface speed 0.29 m/s, unity v 1.69, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 9.80
    [AutoTest    15900.9] lander watch: tilt 0.8 deg, surface speed 0.23 m/s, unity v 1.71, w 0.08, parts 17, legs [contact 0.06 m 4.1 kN] [contact 0.03 m 2.4 kN] [contact 0.04 m 5.1 kN] [free 0.00 m 0.0 kN], frame v 9.82
    [AutoTest    15901.4] lander watch: tilt 1.2 deg, surface speed 0.04 m/s, unity v 1.87, w 0.02, parts 17, legs [contact 0.01 m 1.2 kN] [contact 0.01 m 0.3 kN] [contact 0.06 m 6.4 kN] [free 0.00 m 0.0 kN], frame v 9.82
    [AutoTest    15902.0] lander watch: tilt 0.6 deg, surface speed 0.09 m/s, unity v 1.91, w 0.03, parts 17, legs [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.6 kN] [free 0.00 m 0.0 kN], frame v 9.82
    [AutoTest    15902.5] lander watch: tilt 0.3 deg, surface speed 0.05 m/s, unity v 1.81, w 0.03, parts 17, legs [contact 0.05 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.4 kN] [free 0.00 m 0.0 kN], frame v 9.82
    [AutoTest    15903.0] lander watch: tilt 0.7 deg, surface speed 0.05 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.1 kN] [free 0.00 m 0.0 kN], frame v 9.81
    [AutoTest    15903.5] lander watch: tilt 0.9 deg, surface speed 0.05 m/s, unity v 1.83, w 0.03, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 9.82
    [AutoTest    15904.0] lander watch: tilt 1.1 deg, surface speed 0.03 m/s, unity v 1.80, w 0.03, parts 17, legs [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.6 kN], frame v 9.81
    [AutoTest    15904.6] lander watch: tilt 1.2 deg, surface speed 0.02 m/s, unity v 1.76, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN] [contact 0.01 m 0.5 kN], frame v 9.81
    [AutoTest    15905.1] lander watch: tilt 1.1 deg, surface speed 0.02 m/s, unity v 1.76, w 0.04, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.01 m 0.7 kN], frame v 9.81
    [AutoTest    15905.6] lander watch: tilt 1.0 deg, surface speed 0.02 m/s, unity v 1.78, w 0.04, parts 17, legs [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 9.81
    [AutoTest    15906.1] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 1.78, w 0.03, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.81
    [AutoTest    15906.6] CHECK PASS: walking in low gravity — 12.0 m in 6.9 s (AGL 0.6 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2267, 2145), speed 1.79 m/s, jetpack 10.00 kg)
    [AutoTest    15906.6] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2267, 2145), gravity 1.60 m/s²
    [AutoTest    15906.6] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 2.98, w 0.03, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 10.09
    [AutoTest    15907.2] lander watch: tilt 0.7 deg, surface speed 0.08 m/s, unity v 2.38, w 0.01, parts 17, legs [contact 0.04 m 5.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN] [free 0.00 m 0.0 kN], frame v 9.88
    [AutoTest    15907.7] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 1.84, w 0.00, parts 17, legs [contact 0.05 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 9.78
    [AutoTest    15908.2] lander watch: tilt 0.6 deg, surface speed 0.01 m/s, unity v 1.68, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.75
    [AutoTest    15908.7] lander watch: tilt 0.6 deg, surface speed 0.01 m/s, unity v 1.92, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 9.79
    [AutoTest    15909.2] lander watch: tilt 0.6 deg, surface speed 0.01 m/s, unity v 2.42, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.90
    [AutoTest    15909.6] CHECK PASS: jump in low gravity — apex 1.84 m
    [AutoTest    15909.8] lander watch: tilt 0.7 deg, surface speed 0.07 m/s, unity v 1.06, w 0.00, parts 17, legs [contact 0.01 m 0.1 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 0.7 kN] [free 0.00 m 0.0 kN], frame v 9.48
    [AutoTest    15910.3] lander watch: tilt 0.7 deg, surface speed 0.03 m/s, unity v 0.14, w 0.01, parts 17, legs [contact 0.04 m 5.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.9 kN] [free 0.00 m 0.0 kN], frame v 8.90
    [AutoTest    15910.8] lander watch: tilt 1.0 deg, surface speed 0.04 m/s, unity v 0.06, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.82
    [AutoTest    15911.3] lander watch: tilt 1.2 deg, surface speed 0.04 m/s, unity v 0.05, w 0.01, parts 17, legs [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.7 kN], frame v 8.81
    [AutoTest    15911.6] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15911.6] screenshot: lunar_144_eva_flag.png
    [AutoTest    15911.6] PHASE return to hatch
    [AutoTest    15911.6] screenshot: lunar_146_lander_before_return.png
    [AutoTest    15911.8] lander watch: tilt 1.4 deg, surface speed 0.02 m/s, unity v 0.47, w 0.02, parts 17, legs [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 1.0 kN], frame v 8.61
    [AutoTest    15912.4] lander watch: tilt 1.4 deg, surface speed 0.01 m/s, unity v 1.50, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 0.7 kN], frame v 8.19
    [AutoTest    15912.9] lander watch: tilt 1.2 deg, surface speed 0.03 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.0 kN] [contact 0.00 m 1.1 kN], frame v 8.06
    [AutoTest    15913.4] lander watch: tilt 1.1 deg, surface speed 0.02 m/s, unity v 1.82, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 8.05
    [AutoTest    15913.9] lander watch: tilt 1.1 deg, surface speed 0.01 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.05
    [AutoTest    15914.4] lander watch: tilt 1.1 deg, surface speed 0.01 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.05
    [AutoTest    15915.0] lander watch: tilt 1.2 deg, surface speed 0.02 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.3 kN], frame v 8.05
    [AutoTest    15915.5] lander watch: tilt 1.3 deg, surface speed 0.01 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.00 m 0.6 kN], frame v 8.05
    [AutoTest    15916.0] lander watch: tilt 1.3 deg, surface speed 0.00 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.6 kN], frame v 8.05
    [AutoTest    15916.5] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.83, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.3 kN], frame v 8.04
    [AutoTest    15917.0] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.83, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.1 kN], frame v 8.04
    [AutoTest    15917.6] lander watch: tilt 1.2 deg, surface speed 0.00 m/s, unity v 1.82, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.1 kN], frame v 8.04
    [AutoTest    15918.1] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.2 kN], frame v 8.04
    [AutoTest    15918.6] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.4 kN], frame v 8.04
    [AutoTest    15919.1] lander watch: tilt 1.2 deg, surface speed 0.00 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.4 kN], frame v 8.04
    [AutoTest    15919.6] lander watch: tilt 1.2 deg, surface speed 0.02 m/s, unity v 1.79, w 0.00, parts 17, legs [contact 0.04 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [contact 0.00 m 0.0 kN], frame v 8.05
    [AutoTest    15920.2] lander watch: tilt 1.2 deg, surface speed 0.00 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN], frame v 8.05
    [AutoTest    15920.7] lander watch: tilt 1.2 deg, surface speed 0.00 m/s, unity v 1.77, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.2 kN], frame v 8.05
    [AutoTest    15921.2] lander watch: tilt 1.2 deg, surface speed 0.00 m/s, unity v 1.73, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.00 m 0.3 kN], frame v 8.06
    [AutoTest    15921.7] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.27, w 0.00, parts 17, legs [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 8.37
    [AutoTest    15922.2] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.66, w 0.00, parts 17, legs [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.62
    [AutoTest    15922.8] lander watch: tilt 1.3 deg, surface speed 0.00 m/s, unity v 2.34, w 0.00, parts 17, legs [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.1 kN] [contact 0.00 m 0.5 kN], frame v 8.87
    [AutoTest    15923.3] lander watch: tilt 1.2 deg, surface speed 0.02 m/s, unity v 2.62, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [contact 0.00 m 0.4 kN], frame v 9.01
    [AutoTest    15923.8] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.99, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [contact 0.01 m 0.3 kN], frame v 8.91
    [AutoTest    15924.3] screenshot: lunar_171_before_boarding.png
    [AutoTest    15924.3] boarding: lander AGL 4.06 m, situation Landed, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.2 kN]
    [AutoTest    15924.3] lander watch: tilt 1.2 deg, surface speed 0.02 m/s, unity v 0.00, w 0.02, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.5 kN] [contact 0.01 m 0.7 kN], frame v 8.81
    [AutoTest    15924.8] screenshot: lunar_174_boarded.png
    [AutoTest    15924.8] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15924.8] PHASE lunar ascent
    [AutoTest    15930.5] PHASE lunar gravity turn
    [AutoTest    15953.0] Luma orbit circularization: dv 411.1 m/s, burn 31.6 s, in 174 s
    [AutoTest    15955.5] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16111.0] Luma orbit circularization: 410 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16115.1] Luma orbit circularization: 363 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16119.1] Luma orbit circularization: 312 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16123.1] Luma orbit circularization: 261 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16127.1] Luma orbit circularization: 210 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16131.1] Luma orbit circularization: 157 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16135.2] Luma orbit circularization: 104 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16139.2] Luma orbit circularization: 49 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    16143.2] Luma orbit circularization: 4 m/s to go, 0.1° off, throttle 0.26, spin 0.004 rad/s, SAS Maneuver
    [AutoTest    16145.2] Luma orbit circularization: burn complete, residual 0.30 m/s after 34.2 s
    [AutoTest    16145.2] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16145.2] delta-v left in Luma orbit after ascent: 882 m/s vac (882)
    [AutoTest    16145.2] PHASE plan return
    [AutoTest    16145.2] CHECK PASS: return trajectory to Tellus found — burn 280.0 m/s in 1528 s, Tellus Pe 32.0 km
    [AutoTest    16145.2] PHASE trans-Tellus injection
    [AutoTest    16145.2] TTI: dv 280.0 m/s, burn 19.4 s, in 1528 s
    [AutoTest    17663.4] TTI: 280 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17667.4] TTI: 226 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17671.4] TTI: 169 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17675.4] TTI: 111 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17679.4] TTI: 52 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17683.5] TTI: 4 m/s to go, 0.0° off, throttle 0.25, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17685.6] TTI: burn complete, residual 0.20 m/s after 22.2 s
    [AutoTest    17685.6] delta-v left after trans-Tellus injection: 602 m/s vac (602)
    [AutoTest    17685.6] PHASE coast home
    [AutoTest    24394.4] Tellus periapsis after SOI exit 36.7 km
    [AutoTest    24394.4] CHECK PASS: return periapsis in reentry corridor — Pe 36.7 km
    [AutoTest    52352.5] PHASE reentry
    [AutoTest    52467.3] screenshot: lunar_208_reentry.png
    [AutoTest    52713.7] arming the parachute at 15.1 km, 384 m/s
    [AutoTest    53039.6] screenshot: lunar_210_touchdown.png
    [AutoTest    53039.6] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 782 K (HS-125 Ablative Heat Shield), max 6.3 g
