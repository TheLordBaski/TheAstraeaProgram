# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 36.9 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0015
- [x] TLI encounter found by patched-conic search: node in 1413 s, 938.3 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 296.5 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.2 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.3 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.0 s (AGL 0.7 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2287, 1885), speed 1.83 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2287, 1885), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.81 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 376.8 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.0 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 827 K (HS-125 Ablative Heat Shield), max 6.4 g

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
    [AutoTest      232.9] circularization: 705 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      236.9] circularization: 654 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      240.9] circularization: 603 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      244.9] circularization: 550 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      249.0] circularization: 497 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      253.0] circularization: 442 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      257.0] circularization: 387 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      261.0] circularization: 331 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest      265.0] circularization: 273 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      269.1] circularization: 215 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      273.1] circularization: 155 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      277.1] circularization: 94 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      281.1] circularization: 32 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      285.1] circularization: 1 m/s to go, 1.5° off, throttle 0.07, spin 0.032 rad/s, SAS Maneuver
    [AutoTest      286.0] circularization: burn complete, residual 0.30 m/s after 57.1 s
    [AutoTest      286.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0015
    [AutoTest      286.0] in orbit 284 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      286.0] screenshot: lunar_030_orbit.png
    [AutoTest      286.0] PHASE plan TLI
    [AutoTest      286.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1413 s, 938.3 m/s prograde, Luma Pe 40.0 km
    [AutoTest      286.5] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      286.5] PHASE TLI burn
    [AutoTest      286.5] TLI: dv 938.3 m/s, burn 51.1 s, in 1413 s
    [AutoTest     1673.7] TLI: 938 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1677.8] TLI: 877 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1681.8] TLI: 812 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1685.8] TLI: 745 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1689.8] TLI: 677 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1693.8] TLI: 607 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1697.9] TLI: 535 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1698.2] staging during burn
    [AutoTest     1701.9] TLI: 506 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1705.9] TLI: 480 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1709.9] TLI: 454 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1713.9] TLI: 428 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1718.0] TLI: 401 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1722.0] TLI: 374 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1726.0] TLI: 347 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1730.0] TLI: 320 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1734.0] TLI: 292 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1738.1] TLI: 265 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1742.1] TLI: 237 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1746.1] TLI: 209 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1750.1] TLI: 181 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1754.1] TLI: 153 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1758.2] TLI: 124 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1762.2] TLI: 95 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1766.2] TLI: 67 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1770.2] TLI: 38 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest     1774.2] TLI: 8 m/s to go, 0.2° off, throttle 0.96, spin 0.006 rad/s, SAS Maneuver
    [AutoTest     1777.6] TLI: burn complete, residual 0.29 m/s after 103.9 s
    [AutoTest     1777.6] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 296.5 km (planned 40.0 km)
    [AutoTest     1777.6] delta-v left after TLI: 2942 m/s vac (2942)
    [AutoTest     1777.6] PHASE mid-course correction
    [AutoTest     1777.6] correction: pro -18.00 rad 18.00 nrm 3.55 m/s (score 1285)
    [AutoTest     1777.6] MCC: dv 25.7 m/s, burn 3.4 s, in 1800 s
    [AutoTest     3575.9] MCC: 26 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3579.9] MCC: 2 m/s to go, 0.1° off, throttle 0.26, spin 0.003 rad/s, SAS Maneuver
    [AutoTest     3582.0] MCC: burn complete, residual 0.15 m/s after 6.1 s
    [AutoTest     3582.0] PHASE coast to Luma
    [AutoTest    11495.7] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    11495.7] Luma periapsis 36.4 km: no correction needed
    [AutoTest    11495.7] PHASE capture burn
    [AutoTest    11495.7] Luma periapsis 36.4 km in 3508 s
    [AutoTest    11495.7] Luma orbit insertion: dv 503.6 m/s, burn 61.8 s, in 3508 s
    [AutoTest    14973.1] Luma orbit insertion: 504 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14977.1] Luma orbit insertion: 474 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14981.1] Luma orbit insertion: 443 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14985.1] Luma orbit insertion: 412 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14989.2] Luma orbit insertion: 381 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14993.2] Luma orbit insertion: 349 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    14997.2] Luma orbit insertion: 317 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15001.2] Luma orbit insertion: 285 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15005.2] Luma orbit insertion: 252 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15009.3] Luma orbit insertion: 219 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15013.3] Luma orbit insertion: 186 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15017.3] Luma orbit insertion: 152 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15021.3] Luma orbit insertion: 118 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15025.3] Luma orbit insertion: 84 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    15029.4] Luma orbit insertion: 50 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    15033.4] Luma orbit insertion: 15 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    15037.3] Luma orbit insertion: burn complete, residual 0.29 m/s after 64.2 s
    [AutoTest    15037.3] screenshot: lunar_095_luma_orbit.png
    [AutoTest    15037.3] CHECK PASS: captured into Luma orbit — Pe 36.2 km, Ap 37.3 km
    [AutoTest    15037.3] delta-v left in Luma orbit: 2412 m/s vac (2412)
    [AutoTest    15039.3] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    15039.3] PHASE deorbit
    [AutoTest    15042.5] PHASE descent
    [AutoTest    15760.6] suicide burn start at 12246 m AGL, 589 m/s
    [AutoTest    15923.2] final descent from 150 m AGL at 43.1 m/s (vertical -35.3 m/s)
    [AutoTest    15923.8] landing site: 1.1° slope 12 m away (ground below: 4.0°)
    [AutoTest    15971.8] screenshot: lunar_104_luma_landed.png
    [AutoTest    15971.8] CHECK PASS: landed on Luma — situation Landed, tilt 0.3 deg, legs intact 4/4, crew 1
    [AutoTest    15971.8] delta-v left on the surface: 1627 m/s vac (1627)
    [AutoTest    15971.8] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15971.8] PHASE EVA
    [AutoTest    15971.9] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
    [AutoTest    15971.9] lander watch: tilt 0.3 deg, surface speed 0.05 m/s, unity v 0.35, w 0.04, parts 17, legs [contact 0.03 m 4.7 kN] [contact 0.00 m 1.6 kN] [contact 0.05 m 6.5 kN] [free 0.00 m 0.0 kN], frame v 8.45
    [AutoTest    15972.4] lander watch: tilt 0.4 deg, surface speed 0.03 m/s, unity v 0.87, w 0.01, parts 17, legs [contact 0.04 m 4.6 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.48
    [AutoTest    15972.9] lander watch: tilt 0.5 deg, surface speed 0.01 m/s, unity v 1.64, w 0.00, parts 17, legs [contact 0.05 m 4.8 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 8.60
    [AutoTest    15973.4] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 2.46, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 8.79
    [AutoTest    15973.9] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 3.29, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.05
    [AutoTest    15974.5] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 4.12, w 0.01, parts 17, legs [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 9.39
    [AutoTest    15975.0] lander watch: tilt 0.8 deg, surface speed 0.32 m/s, unity v 0.59, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.34
    [AutoTest    15975.5] lander watch: tilt 1.0 deg, surface speed 0.28 m/s, unity v 1.42, w 0.01, parts 17, legs [contact 0.03 m 8.1 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 7.4 kN] [free 0.00 m 0.0 kN], frame v 7.61
    [AutoTest    15976.0] lander watch: tilt 1.3 deg, surface speed 0.08 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 7.30
    [AutoTest    15976.5] lander watch: tilt 1.7 deg, surface speed 0.07 m/s, unity v 1.85, w 0.02, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15977.1] lander watch: tilt 2.3 deg, surface speed 0.09 m/s, unity v 1.86, w 0.02, parts 17, legs [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15977.6] lander watch: tilt 3.0 deg, surface speed 0.12 m/s, unity v 1.88, w 0.03, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 1.3 kN], frame v 7.29
    [AutoTest    15978.1] lander watch: tilt 3.7 deg, surface speed 0.06 m/s, unity v 1.83, w 0.05, parts 17, legs [contact 0.03 m 2.6 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.02 m 2.6 kN], frame v 7.29
    [AutoTest    15978.6] lander watch: tilt 3.8 deg, surface speed 0.03 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.03 m 3.4 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.6 kN] [contact 0.03 m 2.6 kN], frame v 7.29
    [AutoTest    15979.1] lander watch: tilt 3.4 deg, surface speed 0.07 m/s, unity v 1.76, w 0.03, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.6 kN] [contact 0.01 m 1.0 kN], frame v 7.29
    [AutoTest    15979.7] lander watch: tilt 3.0 deg, surface speed 0.05 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15980.2] lander watch: tilt 2.7 deg, surface speed 0.02 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15980.7] lander watch: tilt 2.7 deg, surface speed 0.01 m/s, unity v 1.83, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15981.2] lander watch: tilt 2.9 deg, surface speed 0.04 m/s, unity v 1.85, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15981.7] lander watch: tilt 3.3 deg, surface speed 0.05 m/s, unity v 1.86, w 0.03, parts 17, legs [contact 0.04 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 1.3 kN], frame v 7.28
    [AutoTest    15981.8] CHECK PASS: walking in low gravity — 12.0 m in 7.0 s (AGL 0.7 m, 14.5 m from the lander axis, grounded True on TerrainCollider (1, 2287, 1885), speed 1.83 m/s, jetpack 10.00 kg)
    [AutoTest    15981.8] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2287, 1885), gravity 1.60 m/s²
    [AutoTest    15982.3] lander watch: tilt 2.1 deg, surface speed 0.22 m/s, unity v 2.46, w 0.08, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.52
    [AutoTest    15982.8] lander watch: tilt 1.9 deg, surface speed 0.05 m/s, unity v 1.84, w 0.00, parts 17, legs [contact 0.06 m 5.1 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 5.2 kN] [free 0.00 m 0.0 kN], frame v 7.39
    [AutoTest    15983.3] lander watch: tilt 2.1 deg, surface speed 0.04 m/s, unity v 1.75, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 7.36
    [AutoTest    15983.8] lander watch: tilt 2.4 deg, surface speed 0.06 m/s, unity v 2.01, w 0.02, parts 17, legs [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 7.42
    [AutoTest    15984.3] lander watch: tilt 3.0 deg, surface speed 0.09 m/s, unity v 2.53, w 0.02, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.8 kN], frame v 7.58
    [AutoTest    15984.7] CHECK PASS: jump in low gravity — apex 1.81 m
    [AutoTest    15984.9] lander watch: tilt 3.4 deg, surface speed 0.27 m/s, unity v 1.46, w 0.03, parts 17, legs [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.61
    [AutoTest    15985.4] lander watch: tilt 3.4 deg, surface speed 0.07 m/s, unity v 0.51, w 0.00, parts 17, legs [contact 0.04 m 5.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN] [contact 0.02 m 3.2 kN], frame v 8.37
    [AutoTest    15985.9] lander watch: tilt 3.3 deg, surface speed 0.03 m/s, unity v 0.01, w 0.02, parts 17, legs [contact 0.04 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.6 kN] [contact 0.01 m 0.8 kN], frame v 8.78
    [AutoTest    15986.4] lander watch: tilt 3.1 deg, surface speed 0.02 m/s, unity v 0.02, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [contact 0.00 m 0.4 kN], frame v 8.80
    [AutoTest    15986.8] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15986.8] screenshot: lunar_143_eva_flag.png
    [AutoTest    15986.8] PHASE return to hatch
    [AutoTest    15986.8] screenshot: lunar_145_lander_before_return.png
    [AutoTest    15986.9] lander watch: tilt 3.0 deg, surface speed 0.01 m/s, unity v 0.31, w 0.00, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.00 m 0.1 kN], frame v 9.07
    [AutoTest    15987.5] lander watch: tilt 3.1 deg, surface speed 0.02 m/s, unity v 1.23, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.00 m 0.5 kN], frame v 9.89
    [AutoTest    15988.0] lander watch: tilt 3.3 deg, surface speed 0.02 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.5 kN] [contact 0.01 m 1.1 kN], frame v 10.36
    [AutoTest    15988.5] lander watch: tilt 3.3 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 1.2 kN], frame v 10.38
    [AutoTest    15989.0] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN], frame v 10.38
    [AutoTest    15989.5] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.6 kN], frame v 10.36
    [AutoTest    15990.1] lander watch: tilt 3.1 deg, surface speed 0.00 m/s, unity v 1.77, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.00 m 0.5 kN], frame v 10.36
    [AutoTest    15990.6] lander watch: tilt 3.1 deg, surface speed 0.01 m/s, unity v 1.76, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.6 kN], frame v 10.36
    [AutoTest    15991.1] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 0.8 kN], frame v 10.37
    [AutoTest    15991.6] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.9 kN], frame v 10.36
    [AutoTest    15992.1] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 0.8 kN], frame v 10.36
    [AutoTest    15992.7] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 10.37
    [AutoTest    15993.2] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.79, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.6 kN], frame v 10.38
    [AutoTest    15993.7] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.79, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 10.37
    [AutoTest    15994.2] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.79, w 0.03, parts 17, legs [contact 0.04 m 5.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [contact 0.01 m 1.7 kN], frame v 10.36
    [AutoTest    15994.7] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.8 kN], frame v 10.37
    [AutoTest    15995.3] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN], frame v 10.37
    [AutoTest    15995.8] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 10.37
    [AutoTest    15996.3] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 10.37
    [AutoTest    15996.8] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.65, w 0.02, parts 17, legs [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 1.0 kN], frame v 10.23
    [AutoTest    15997.3] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 1.13, w 0.01, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN] [contact 0.00 m 0.7 kN], frame v 9.62
    [AutoTest    15997.9] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.49, w 0.01, parts 17, legs [contact 0.03 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.9 kN] [contact 0.00 m 0.7 kN], frame v 9.42
    [AutoTest    15998.4] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 2.17, w 0.02, parts 17, legs [contact 0.03 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN] [contact 0.00 m 0.7 kN], frame v 9.42
    [AutoTest    15998.9] lander watch: tilt 3.2 deg, surface speed 0.02 m/s, unity v 2.58, w 0.01, parts 17, legs [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.7 kN], frame v 9.41
    [AutoTest    15999.4] lander watch: tilt 3.2 deg, surface speed 0.00 m/s, unity v 2.00, w 0.02, parts 17, legs [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN], frame v 9.21
    [AutoTest    15999.9] screenshot: lunar_171_before_boarding.png
    [AutoTest    15999.9] boarding: lander AGL 4.19 m, situation Landed, legs [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN]
    [AutoTest    15999.9] lander watch: tilt 3.2 deg, surface speed 0.03 m/s, unity v 0.00, w 0.03, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.1 kN] [contact 0.01 m 1.0 kN], frame v 8.78
    [AutoTest    16000.4] screenshot: lunar_174_boarded.png
    [AutoTest    16000.4] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    16000.4] PHASE lunar ascent
    [AutoTest    16006.1] PHASE lunar gravity turn
    [AutoTest    16028.7] Luma orbit circularization: dv 411.3 m/s, burn 31.7 s, in 174 s
    [AutoTest    16031.2] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16187.4] Luma orbit circularization: 411 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16191.4] Luma orbit circularization: 363 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16195.4] Luma orbit circularization: 313 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16199.4] Luma orbit circularization: 262 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16203.4] Luma orbit circularization: 210 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    16207.5] Luma orbit circularization: 158 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16211.5] Luma orbit circularization: 104 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    16215.5] Luma orbit circularization: 50 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    16219.5] Luma orbit circularization: 4 m/s to go, 0.1° off, throttle 0.28, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    16221.6] Luma orbit circularization: burn complete, residual 0.30 m/s after 34.2 s
    [AutoTest    16221.6] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16221.6] delta-v left in Luma orbit after ascent: 887 m/s vac (887)
    [AutoTest    16221.6] PHASE plan return
    [AutoTest    16221.6] CHECK PASS: return trajectory to Tellus found — burn 376.8 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16221.6] PHASE trans-Tellus injection
    [AutoTest    16221.6] TTI: dv 376.8 m/s, burn 25.8 s, in 1168 s
    [AutoTest    17376.7] TTI: 377 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17380.7] TTI: 323 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17384.7] TTI: 266 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    17388.8] TTI: 208 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17392.8] TTI: 150 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17396.8] TTI: 90 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    17400.8] TTI: 29 m/s to go, 0.0° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    17404.8] TTI: 1 m/s to go, 0.1° off, throttle 0.06, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    17405.3] TTI: burn complete, residual 0.20 m/s after 28.6 s
    [AutoTest    17405.3] delta-v left after trans-Tellus injection: 511 m/s vac (511)
    [AutoTest    17405.3] PHASE coast home
    [AutoTest    22185.7] Tellus periapsis after SOI exit 31.0 km
    [AutoTest    22185.7] CHECK PASS: return periapsis in reentry corridor — Pe 31.0 km
    [AutoTest    33773.2] PHASE reentry
    [AutoTest    33880.5] screenshot: lunar_210_reentry.png
    [AutoTest    34014.4] arming the parachute at 16.1 km, 422 m/s
    [AutoTest    34348.5] screenshot: lunar_212_touchdown.png
    [AutoTest    34348.5] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 827 K (HS-125 Ablative Heat Shield), max 6.4 g
