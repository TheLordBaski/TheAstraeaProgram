# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 939.0 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 297.3 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 2.9 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.1 s (AGL 0.7 m, 14.3 m from the lander axis, grounded True on TerrainCollider (1, 2283, 1981), speed 1.78 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2283, 1981), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.84 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 338.9 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.5 km
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
    [AutoTest      228.9] circularization: 752 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      232.9] circularization: 705 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      236.9] circularization: 654 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      240.9] circularization: 603 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      244.9] circularization: 550 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      249.0] circularization: 497 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      253.0] circularization: 442 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      257.0] circularization: 387 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      261.0] circularization: 331 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      265.0] circularization: 273 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      269.1] circularization: 215 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      273.1] circularization: 155 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      277.1] circularization: 94 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      281.1] circularization: 32 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      285.1] circularization: 1 m/s to go, 0.2° off, throttle 0.07, spin 0.007 rad/s, SAS Maneuver
    [AutoTest      286.1] circularization: burn complete, residual 0.30 m/s after 57.2 s
    [AutoTest      286.1] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.1] in orbit 285 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      286.1] screenshot: lunar_030_orbit.png
    [AutoTest      286.1] PHASE plan TLI
    [AutoTest      286.1] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 939.0 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.6] PHASE TLI burn
    [AutoTest      286.6] TLI: dv 939.0 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1673.9] TLI: 939 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1677.9] TLI: 878 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1681.9] TLI: 813 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1685.9] TLI: 746 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1689.9] TLI: 678 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1694.0] TLI: 608 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1698.0] TLI: 536 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1698.3] staging during burn
    [AutoTest     1702.0] TLI: 507 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1706.0] TLI: 481 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1710.0] TLI: 455 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1714.1] TLI: 428 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1718.1] TLI: 402 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1722.1] TLI: 375 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1726.1] TLI: 348 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1730.1] TLI: 320 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1734.2] TLI: 293 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1738.2] TLI: 265 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1742.2] TLI: 238 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1746.2] TLI: 210 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1750.2] TLI: 182 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1754.3] TLI: 153 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1758.3] TLI: 125 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1762.3] TLI: 96 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1766.3] TLI: 67 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1770.3] TLI: 38 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1774.4] TLI: 9 m/s to go, 0.2° off, throttle 1.00, spin 0.006 rad/s, SAS Maneuver
    [AutoTest     1777.8] TLI: burn complete, residual 0.30 m/s after 103.9 s
    [AutoTest     1777.8] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 297.3 km (planned 40.0 km)
    [AutoTest     1777.8] delta-v left after TLI: 2941 m/s vac (2941)
    [AutoTest     1777.8] PHASE mid-course correction
    [AutoTest     1777.8] correction: pro -18.00 rad 18.00 nrm 1.17 m/s (score 1275)
    [AutoTest     1777.8] MCC: dv 25.5 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3576.1] MCC: 25 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3580.2] MCC: 2 m/s to go, 0.1° off, throttle 0.25, spin 0.005 rad/s, SAS Maneuver
    [AutoTest     3582.2] MCC: burn complete, residual 0.15 m/s after 6.0 s
    [AutoTest     3582.2] PHASE coast to Luma
    [AutoTest    11469.0] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11469.0] Luma periapsis 36.5 km: no correction needed
    [AutoTest    11469.0] PHASE capture burn
    [AutoTest    11469.0] Luma periapsis 36.5 km in 3495 s
    [AutoTest    11469.0] Luma orbit insertion: dv 505.8 m/s, burn 62.0 s, in 3495 s
    [AutoTest    14933.3] Luma orbit insertion: 506 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14937.3] Luma orbit insertion: 476 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14941.3] Luma orbit insertion: 446 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14945.4] Luma orbit insertion: 414 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14949.4] Luma orbit insertion: 383 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14953.4] Luma orbit insertion: 351 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14957.4] Luma orbit insertion: 319 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14961.4] Luma orbit insertion: 287 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    14965.5] Luma orbit insertion: 254 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14969.5] Luma orbit insertion: 221 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14973.5] Luma orbit insertion: 188 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14977.5] Luma orbit insertion: 154 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14981.5] Luma orbit insertion: 121 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14985.6] Luma orbit insertion: 86 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14989.6] Luma orbit insertion: 52 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    14993.6] Luma orbit insertion: 17 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    14997.6] Luma orbit insertion: 0 m/s to go, 0.1° off, throttle 0.06, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    14997.7] Luma orbit insertion: burn complete, residual 0.29 m/s after 64.4 s
    [AutoTest    14997.7] screenshot: lunar_096_luma_orbit.png
    [AutoTest    14997.7] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    14997.7] delta-v left in Luma orbit: 2410 m/s vac (2410)
    [AutoTest    14999.8] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    14999.8] PHASE deorbit
    [AutoTest    15003.0] PHASE descent
    [AutoTest    15722.1] suicide burn start at 12230 m AGL, 589 m/s
    [AutoTest    15883.1] final descent from 150 m AGL at 42.9 m/s (vertical -35.0 m/s)
    [AutoTest    15883.7] landing site: 1.6° slope 12 m away (ground below: 7.7°)
    [AutoTest    15931.9] screenshot: lunar_105_luma_landed.png
    [AutoTest    15931.9] CHECK PASS: landed on Luma — situation Landed, tilt 2.9 deg, legs intact 4/4, crew 1
    [AutoTest    15931.9] delta-v left on the surface: 1625 m/s vac (1625)
    [AutoTest    15931.9] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15931.9] PHASE EVA
    [AutoTest    15932.0] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
    [AutoTest    15932.0] lander watch: tilt 2.9 deg, surface speed 0.05 m/s, unity v 0.36, w 0.04, parts 17, legs [contact 0.00 m 1.8 kN] [contact 0.04 m 5.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.5 kN], frame v 8.67
    [AutoTest    15932.5] lander watch: tilt 2.9 deg, surface speed 0.01 m/s, unity v 0.85, w 0.01, parts 17, legs [contact 0.01 m 0.6 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 8.71
    [AutoTest    15933.0] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.63, w 0.01, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 8.82
    [AutoTest    15933.5] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 2.45, w 0.02, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.00
    [AutoTest    15934.0] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 3.27, w 0.02, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 9.26
    [AutoTest    15934.6] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 4.10, w 0.01, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.59
    [AutoTest    15935.1] lander watch: tilt 2.8 deg, surface speed 0.46 m/s, unity v 0.57, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.63
    [AutoTest    15935.6] lander watch: tilt 2.2 deg, surface speed 0.38 m/s, unity v 1.30, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.44
    [AutoTest    15936.1] lander watch: tilt 1.9 deg, surface speed 0.11 m/s, unity v 1.80, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.06 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 5.8 kN], frame v 8.37
    [AutoTest    15936.6] lander watch: tilt 1.9 deg, surface speed 0.03 m/s, unity v 1.78, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN], frame v 8.37
    [AutoTest    15937.2] lander watch: tilt 2.0 deg, surface speed 0.04 m/s, unity v 1.80, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 8.37
    [AutoTest    15937.7] lander watch: tilt 2.3 deg, surface speed 0.06 m/s, unity v 1.84, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 5.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN], frame v 8.38
    [AutoTest    15938.2] lander watch: tilt 2.8 deg, surface speed 0.08 m/s, unity v 1.86, w 0.02, parts 17, legs [contact 0.00 m 0.9 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 8.38
    [AutoTest    15938.7] lander watch: tilt 3.3 deg, surface speed 0.05 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.02 m 2.0 kN] [contact 0.03 m 3.5 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.1 kN], frame v 8.38
    [AutoTest    15939.2] lander watch: tilt 3.4 deg, surface speed 0.02 m/s, unity v 1.78, w 0.04, parts 17, legs [contact 0.02 m 1.9 kN] [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.0 kN], frame v 8.38
    [AutoTest    15939.8] lander watch: tilt 3.1 deg, surface speed 0.05 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN], frame v 8.38
    [AutoTest    15940.3] lander watch: tilt 2.8 deg, surface speed 0.03 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN], frame v 8.39
    [AutoTest    15940.8] lander watch: tilt 2.7 deg, surface speed 0.01 m/s, unity v 1.79, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 8.39
    [AutoTest    15941.3] lander watch: tilt 2.8 deg, surface speed 0.03 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 8.39
    [AutoTest    15941.8] lander watch: tilt 3.0 deg, surface speed 0.03 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 8.39
    [AutoTest    15942.1] CHECK PASS: walking in low gravity — 12.0 m in 7.1 s (AGL 0.7 m, 14.3 m from the lander axis, grounded True on TerrainCollider (1, 2283, 1981), speed 1.78 m/s, jetpack 10.00 kg)
    [AutoTest    15942.1] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2283, 1981), gravity 1.60 m/s²
    [AutoTest    15942.4] lander watch: tilt 2.1 deg, surface speed 0.03 m/s, unity v 2.64, w 0.08, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.65
    [AutoTest    15942.9] lander watch: tilt 1.2 deg, surface speed 0.03 m/s, unity v 2.07, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.06 m 6.3 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 6.5 kN], frame v 8.49
    [AutoTest    15943.4] lander watch: tilt 1.1 deg, surface speed 0.01 m/s, unity v 1.71, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 8.41
    [AutoTest    15943.9] lander watch: tilt 1.2 deg, surface speed 0.01 m/s, unity v 1.74, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 8.42
    [AutoTest    15944.4] lander watch: tilt 1.3 deg, surface speed 0.02 m/s, unity v 2.12, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN], frame v 8.50
    [AutoTest    15945.0] lander watch: tilt 1.5 deg, surface speed 0.04 m/s, unity v 2.72, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 8.67
    [AutoTest    15945.2] CHECK PASS: jump in low gravity — apex 1.84 m
    [AutoTest    15945.5] lander watch: tilt 1.9 deg, surface speed 0.07 m/s, unity v 1.31, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 2.1 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 1.8 kN], frame v 8.45
    [AutoTest    15946.0] lander watch: tilt 2.4 deg, surface speed 0.08 m/s, unity v 0.49, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.0 kN], frame v 8.67
    [AutoTest    15946.5] lander watch: tilt 3.1 deg, surface speed 0.09 m/s, unity v 0.13, w 0.04, parts 17, legs [contact 0.01 m 1.6 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 8.80
    [AutoTest    15947.0] lander watch: tilt 3.5 deg, surface speed 0.02 m/s, unity v 0.05, w 0.05, parts 17, legs [contact 0.02 m 2.6 kN] [contact 0.03 m 2.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN], frame v 8.80
    [AutoTest    15947.2] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15947.2] screenshot: lunar_145_eva_flag.png
    [AutoTest    15947.2] PHASE return to hatch
    [AutoTest    15947.2] screenshot: lunar_147_lander_before_return.png
    [AutoTest    15947.6] lander watch: tilt 3.4 deg, surface speed 0.05 m/s, unity v 0.52, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.6 kN], frame v 9.00
    [AutoTest    15948.1] lander watch: tilt 3.0 deg, surface speed 0.06 m/s, unity v 1.36, w 0.02, parts 17, legs [contact 0.01 m 0.4 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 9.35
    [AutoTest    15948.6] lander watch: tilt 2.7 deg, surface speed 0.03 m/s, unity v 1.79, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN], frame v 9.56
    [AutoTest    15949.1] lander watch: tilt 2.6 deg, surface speed 0.00 m/s, unity v 1.78, w 0.00, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN], frame v 9.57
    [AutoTest    15949.6] lander watch: tilt 2.8 deg, surface speed 0.04 m/s, unity v 1.75, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 9.58
    [AutoTest    15950.2] lander watch: tilt 3.0 deg, surface speed 0.04 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.6 kN], frame v 9.58
    [AutoTest    15950.7] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.81, w 0.03, parts 17, legs [contact 0.01 m 1.4 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.4 kN], frame v 9.58
    [AutoTest    15951.2] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.01 m 1.4 kN] [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN], frame v 9.60
    [AutoTest    15951.7] lander watch: tilt 3.0 deg, surface speed 0.02 m/s, unity v 1.83, w 0.01, parts 17, legs [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 9.59
    [AutoTest    15952.2] lander watch: tilt 2.9 deg, surface speed 0.01 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 9.59
    [AutoTest    15952.8] lander watch: tilt 2.9 deg, surface speed 0.02 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 9.59
    [AutoTest    15953.3] lander watch: tilt 3.0 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.59
    [AutoTest    15953.8] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.01 m 1.0 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN], frame v 9.59
    [AutoTest    15954.3] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN], frame v 9.58
    [AutoTest    15954.8] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.58
    [AutoTest    15955.4] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.01 m 0.5 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN], frame v 9.58
    [AutoTest    15955.9] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.01 m 1.1 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN], frame v 9.58
    [AutoTest    15956.4] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 9.57
    [AutoTest    15956.9] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 9.58
    [AutoTest    15957.4] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.66, w 0.02, parts 17, legs [contact 0.01 m 1.2 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 9.50
    [AutoTest    15958.0] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 1.10, w 0.01, parts 17, legs [contact 0.00 m 0.7 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN], frame v 9.19
    [AutoTest    15958.5] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 1.44, w 0.01, parts 17, legs [contact 0.00 m 0.6 kN] [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN], frame v 9.12
    [AutoTest    15959.0] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 2.12, w 0.01, parts 17, legs [contact 0.00 m 0.6 kN] [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.0 kN], frame v 9.20
    [AutoTest    15959.5] lander watch: tilt 3.0 deg, surface speed 0.02 m/s, unity v 2.61, w 0.01, parts 17, legs [contact 0.00 m 0.6 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN], frame v 9.30
    [AutoTest    15960.0] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 2.06, w 0.02, parts 17, legs [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN], frame v 9.13
    [AutoTest    15960.6] lander watch: tilt 3.0 deg, surface speed 0.00 m/s, unity v 1.49, w 0.02, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN], frame v 8.99
    [AutoTest    15960.6] screenshot: lunar_174_before_boarding.png
    [AutoTest    15960.6] boarding: lander AGL 4.22 m, situation Landed, legs [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN]
    [AutoTest    15961.1] lander watch: tilt 2.9 deg, surface speed 0.02 m/s, unity v 0.00, w 0.01, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN], frame v 8.80
    [AutoTest    15961.1] screenshot: lunar_177_boarded.png
    [AutoTest    15961.1] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15961.1] PHASE lunar ascent
    [AutoTest    15966.8] PHASE lunar gravity turn
    [AutoTest    15989.5] Luma orbit circularization: dv 403.2 m/s, burn 31.1 s, in 175 s
    [AutoTest    15991.9] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16148.6] Luma orbit circularization: 402 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16152.7] Luma orbit circularization: 355 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16156.7] Luma orbit circularization: 305 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16160.7] Luma orbit circularization: 254 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16164.7] Luma orbit circularization: 202 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16168.7] Luma orbit circularization: 149 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16172.8] Luma orbit circularization: 96 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16176.8] Luma orbit circularization: 41 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    16180.8] Luma orbit circularization: 2 m/s to go, 0.1° off, throttle 0.16, spin 0.004 rad/s, SAS Maneuver
    [AutoTest    16182.3] Luma orbit circularization: burn complete, residual 0.29 m/s after 33.6 s
    [AutoTest    16182.3] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16182.3] delta-v left in Luma orbit after ascent: 892 m/s vac (892)
    [AutoTest    16182.3] PHASE plan return
    [AutoTest    16182.3] CHECK PASS: return trajectory to Tellus found — burn 338.9 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16182.3] PHASE trans-Tellus injection
    [AutoTest    16182.3] TTI: dv 338.9 m/s, burn 23.4 s, in 1168 s
    [AutoTest    17338.5] TTI: 339 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    17342.6] TTI: 285 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17346.6] TTI: 229 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17350.6] TTI: 171 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17354.6] TTI: 112 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17358.6] TTI: 52 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    17362.7] TTI: 4 m/s to go, 0.1° off, throttle 0.23, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    17364.7] TTI: burn complete, residual 0.20 m/s after 26.2 s
    [AutoTest    17364.7] delta-v left after trans-Tellus injection: 553 m/s vac (553)
    [AutoTest    17364.7] PHASE coast home
    [AutoTest    22695.0] Tellus periapsis after SOI exit 31.5 km
    [AutoTest    22695.0] CHECK PASS: return periapsis in reentry corridor — Pe 31.5 km
    [AutoTest    35349.4] PHASE reentry
    [AutoTest    35457.6] screenshot: lunar_212_reentry.png
    [AutoTest    35595.9] arming the parachute at 16.1 km, 421 m/s
    [AutoTest    35929.7] screenshot: lunar_214_touchdown.png
    [AutoTest    35929.7] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 824 K (HS-125 Ablative Heat Shield), max 6.4 g
