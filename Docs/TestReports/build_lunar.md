# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 37.0 kPa
- [x] stable orbit reached: Pe 80.8 km, Ap 82.8 km, e 0.0014
- [x] TLI encounter found by patched-conic search: node in 1437 s, 870.0 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 113.5 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.7 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 1.0 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.34 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.1 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (5, 990, 1987), speed 1.79 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (5, 990, 1987), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.85 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 370.0 m/s in 2365 s, Tellus Pe 32.1 km
- [x] return periapsis in reentry corridor: Pe 36.8 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 782 K (HS-125 Ablative Heat Shield), max 6.3 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.3 km, 1195 m/s
    [AutoTest      164.1] MECO: Ap 82.0 km, max Q 37.0 kPa, delta-v left 4624 m/s vac (1148 + 3476)
    [AutoTest      164.1] CHECK PASS: max dynamic pressure survivable — 37.0 kPa
    [AutoTest      164.1] PHASE coast to space
    [AutoTest      182.9] PHASE circularize
    [AutoTest      182.9] circularization: dv 736.9 m/s, burn 52.8 s, in 75 s
    [AutoTest      231.1] circularization: 737 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      235.1] circularization: 689 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      239.1] circularization: 638 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      243.2] circularization: 587 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      247.2] circularization: 534 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      251.2] circularization: 480 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      255.2] circularization: 425 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      259.2] circularization: 370 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      263.3] circularization: 313 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      267.3] circularization: 255 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      271.3] circularization: 196 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      275.3] circularization: 136 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      279.3] circularization: 75 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      283.4] circularization: 13 m/s to go, 0.1° off, throttle 0.74, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      287.0] circularization: burn complete, residual 0.30 m/s after 55.9 s
    [AutoTest      287.0] CHECK PASS: stable orbit reached — Pe 80.8 km, Ap 82.8 km, e 0.0014
    [AutoTest      287.0] in orbit 286 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      287.0] screenshot: lunar_029_orbit.png
    [AutoTest      287.0] PHASE plan TLI
    [AutoTest      287.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1437 s, 870.0 m/s prograde, Luma Pe 40.0 km
    [AutoTest      287.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      287.6] PHASE TLI burn
    [AutoTest      287.6] TLI: dv 870.0 m/s, burn 47.9 s, in 1436 s
    [AutoTest     1700.1] TLI: 870 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1704.1] TLI: 809 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1708.1] TLI: 744 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1712.1] TLI: 677 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1716.2] TLI: 608 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1720.2] TLI: 538 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1724.2] TLI: 467 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1724.6] staging during burn
    [AutoTest     1728.2] TLI: 438 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1732.2] TLI: 412 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1736.3] TLI: 385 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1740.3] TLI: 359 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1744.3] TLI: 332 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1748.3] TLI: 305 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1752.3] TLI: 278 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1756.4] TLI: 251 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1760.4] TLI: 224 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1764.4] TLI: 196 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1768.4] TLI: 168 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1772.4] TLI: 141 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1776.5] TLI: 112 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1780.5] TLI: 84 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1784.5] TLI: 56 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1788.5] TLI: 27 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest     1792.5] TLI: 2 m/s to go, 0.3° off, throttle 0.31, spin 0.009 rad/s, SAS Maneuver
    [AutoTest     1794.5] TLI: burn complete, residual 0.29 m/s after 94.4 s
    [AutoTest     1794.5] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 113.5 km (planned 40.0 km)
    [AutoTest     1794.5] delta-v left after TLI: 3012 m/s vac (3012)
    [AutoTest     1794.5] PHASE mid-course correction
    [AutoTest     1794.5] correction: pro -3.00 rad -5.81 nrm 0.69 m/s (score 330)
    [AutoTest     1794.5] MCC: dv 6.6 m/s, burn 0.9 s, in 1800 s
    [AutoTest     3594.0] MCC: 7 m/s to go, 0.1° off, throttle 0.77, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     3597.5] MCC: burn complete, residual 0.15 m/s after 3.5 s
    [AutoTest     3597.5] PHASE coast to Luma
    [AutoTest    19029.1] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    19029.1] Luma periapsis 36.8 km: no correction needed
    [AutoTest    19029.1] PHASE capture burn
    [AutoTest    19029.1] Luma periapsis 36.8 km in 6327 s
    [AutoTest    19029.1] Luma orbit insertion: dv 282.2 m/s, burn 36.7 s, in 6327 s
    [AutoTest    25338.3] Luma orbit insertion: 282 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    25342.3] Luma orbit insertion: 254 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    25346.3] Luma orbit insertion: 224 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    25350.4] Luma orbit insertion: 193 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25354.4] Luma orbit insertion: 163 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25358.4] Luma orbit insertion: 132 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25362.4] Luma orbit insertion: 101 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25366.4] Luma orbit insertion: 69 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25370.5] Luma orbit insertion: 37 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    25374.5] Luma orbit insertion: 6 m/s to go, 0.1° off, throttle 0.66, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    25377.3] Luma orbit insertion: burn complete, residual 0.29 m/s after 39.0 s
    [AutoTest    25377.3] screenshot: lunar_085_luma_orbit.png
    [AutoTest    25377.3] CHECK PASS: captured into Luma orbit — Pe 36.7 km, Ap 37.3 km
    [AutoTest    25377.3] delta-v left in Luma orbit: 2724 m/s vac (2724)
    [AutoTest    25379.4] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    25379.4] PHASE deorbit
    [AutoTest    25382.7] PHASE descent
    [AutoTest    26096.9] suicide burn start at 12284 m AGL, 589 m/s
    [AutoTest    26258.1] final descent from 150 m AGL at 41.2 m/s (vertical -34.4 m/s)
    [AutoTest    26258.7] landing site: 0.8° slope 12 m away (ground below: 6.9°)
    [AutoTest    26298.2] screenshot: lunar_094_luma_landed.png
    [AutoTest    26298.2] CHECK PASS: landed on Luma — situation Landed, tilt 1.0 deg, legs intact 4/4, crew 1
    [AutoTest    26298.2] delta-v left on the surface: 1950 m/s vac (1950)
    [AutoTest    26298.2] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    26298.2] PHASE EVA
    [AutoTest    26298.2] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.34 m/s rel. lander
    [AutoTest    26298.2] lander watch: tilt 1.0 deg, surface speed 0.05 m/s, unity v 0.34, w 0.06, parts 17, legs [contact 0.03 m 4.5 kN] [contact 0.02 m 4.0 kN] [contact 0.04 m 5.3 kN] [contact 0.00 m 1.5 kN], frame v 8.44
    [AutoTest    26298.7] lander watch: tilt 0.9 deg, surface speed 0.04 m/s, unity v 0.87, w 0.04, parts 17, legs [contact 0.04 m 3.5 kN] [contact 0.02 m 1.4 kN] [contact 0.04 m 3.5 kN] [contact 0.01 m 1.3 kN], frame v 8.48
    [AutoTest    26299.3] lander watch: tilt 0.8 deg, surface speed 0.00 m/s, unity v 1.63, w 0.04, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.1 kN] [contact 0.02 m 1.5 kN], frame v 8.59
    [AutoTest    26299.8] lander watch: tilt 0.8 deg, surface speed 0.02 m/s, unity v 2.45, w 0.04, parts 17, legs [contact 0.04 m 3.4 kN] [contact 0.02 m 1.5 kN] [contact 0.04 m 3.5 kN] [contact 0.02 m 1.1 kN], frame v 8.78
    [AutoTest    26300.3] lander watch: tilt 0.9 deg, surface speed 0.00 m/s, unity v 3.27, w 0.04, parts 17, legs [contact 0.04 m 3.3 kN] [contact 0.02 m 1.5 kN] [contact 0.04 m 3.6 kN] [contact 0.01 m 1.0 kN], frame v 9.05
    [AutoTest    26300.8] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 4.10, w 0.04, parts 17, legs [contact 0.04 m 3.5 kN] [contact 0.02 m 1.3 kN] [contact 0.04 m 3.4 kN] [contact 0.02 m 1.2 kN], frame v 9.38
    [AutoTest    26301.3] lander watch: tilt 0.8 deg, surface speed 0.66 m/s, unity v 0.85, w 0.12, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.28
    [AutoTest    26301.9] lander watch: tilt 1.1 deg, surface speed 0.17 m/s, unity v 1.45, w 0.12, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.38
    [AutoTest    26302.4] lander watch: tilt 1.1 deg, surface speed 0.16 m/s, unity v 1.75, w 0.18, parts 17, legs [contact 0.08 m 11.5 kN] [contact 0.04 m 8.2 kN] [contact 0.04 m 9.0 kN] [contact 0.02 m 6.1 kN], frame v 7.03
    [AutoTest    26302.9] lander watch: tilt 0.7 deg, surface speed 0.06 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.03 m 2.3 kN] [contact 0.01 m 0.4 kN] [contact 0.04 m 5.1 kN] [contact 0.01 m 0.9 kN], frame v 7.02
    [AutoTest    26303.4] lander watch: tilt 0.6 deg, surface speed 0.05 m/s, unity v 1.82, w 0.02, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.00 m 0.3 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 7.03
    [AutoTest    26303.9] lander watch: tilt 0.8 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 4.7 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.6 kN], frame v 7.04
    [AutoTest    26304.5] lander watch: tilt 0.8 deg, surface speed 0.02 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 7.04
    [AutoTest    26305.0] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.8 kN], frame v 7.07
    [AutoTest    26305.5] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.77, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 7.08
    [AutoTest    26306.0] lander watch: tilt 0.8 deg, surface speed 0.00 m/s, unity v 1.77, w 0.03, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.7 kN], frame v 7.08
    [AutoTest    26306.5] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 1.77, w 0.03, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 7.08
    [AutoTest    26307.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.77, w 0.03, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 7.08
    [AutoTest    26307.6] lander watch: tilt 0.8 deg, surface speed 0.00 m/s, unity v 1.77, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 1.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 7.07
    [AutoTest    26308.1] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 7.04
    [AutoTest    26308.3] CHECK PASS: walking in low gravity — 12.0 m in 7.1 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (5, 990, 1987), speed 1.79 m/s, jetpack 10.00 kg)
    [AutoTest    26308.3] CHECK PASS: EVA on the surface — standing on TerrainCollider (5, 990, 1987), gravity 1.60 m/s²
    [AutoTest    26308.6] lander watch: tilt 0.9 deg, surface speed 0.10 m/s, unity v 2.52, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.41
    [AutoTest    26309.1] lander watch: tilt 1.0 deg, surface speed 0.07 m/s, unity v 2.10, w 0.03, parts 17, legs [contact 0.05 m 6.5 kN] [contact 0.03 m 4.4 kN] [contact 0.05 m 6.1 kN] [contact 0.01 m 2.5 kN], frame v 7.24
    [AutoTest    26309.7] lander watch: tilt 0.7 deg, surface speed 0.03 m/s, unity v 1.73, w 0.02, parts 17, legs [contact 0.04 m 3.6 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 3.8 kN] [contact 0.02 m 1.5 kN], frame v 7.15
    [AutoTest    26310.2] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 1.75, w 0.03, parts 17, legs [contact 0.04 m 3.8 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 3.9 kN] [contact 0.02 m 1.1 kN], frame v 7.16
    [AutoTest    26310.7] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 2.15, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.02 m 1.4 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 0.5 kN], frame v 7.27
    [AutoTest    26311.2] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 2.76, w 0.03, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.02 m 1.1 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 0.8 kN], frame v 7.47
    [AutoTest    26311.3] CHECK PASS: jump in low gravity — apex 1.85 m
    [AutoTest    26311.7] lander watch: tilt 0.7 deg, surface speed 0.13 m/s, unity v 0.86, w 0.06, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.00
    [AutoTest    26312.3] lander watch: tilt 0.7 deg, surface speed 0.04 m/s, unity v 0.09, w 0.02, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 3.5 kN] [contact 0.01 m 0.8 kN], frame v 8.77
    [AutoTest    26312.8] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 0.02, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.6 kN] [contact 0.01 m 0.5 kN], frame v 8.83
    [AutoTest    26313.3] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 0.01, w 0.02, parts 17, legs [contact 0.04 m 3.8 kN] [contact 0.01 m 1.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN], frame v 8.84
    [AutoTest    26313.4] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    26313.4] screenshot: lunar_134_eva_flag.png
    [AutoTest    26313.4] PHASE return to hatch
    [AutoTest    26313.4] screenshot: lunar_136_lander_before_return.png
    [AutoTest    26313.8] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 0.82, w 0.02, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN], frame v 9.66
    [AutoTest    26314.3] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.70, w 0.02, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.9 kN], frame v 10.52
    [AutoTest    26314.9] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.6 kN], frame v 10.62
    [AutoTest    26315.4] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.03, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 1.0 kN] [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN], frame v 10.63
    [AutoTest    26315.9] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 10.63
    [AutoTest    26316.4] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 0.8 kN], frame v 10.63
    [AutoTest    26316.9] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.63
    [AutoTest    26317.5] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.63
    [AutoTest    26318.0] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 3.7 kN] [contact 0.01 m 0.7 kN], frame v 10.64
    [AutoTest    26318.5] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.8 kN] [contact 0.01 m 0.7 kN], frame v 10.65
    [AutoTest    26319.0] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.65
    [AutoTest    26319.5] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.83, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 10.65
    [AutoTest    26320.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.65
    [AutoTest    26320.6] lander watch: tilt 0.7 deg, surface speed 0.03 m/s, unity v 1.82, w 0.02, parts 17, legs [contact 0.04 m 3.3 kN] [contact 0.01 m 0.3 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.0 kN], frame v 10.64
    [AutoTest    26321.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.81, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 10.63
    [AutoTest    26321.6] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.81, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.63
    [AutoTest    26322.1] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.03 m 3.8 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 0.6 kN], frame v 10.62
    [AutoTest    26322.7] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 10.62
    [AutoTest    26323.2] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.61, w 0.03, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 3.8 kN] [contact 0.01 m 1.1 kN], frame v 10.43
    [AutoTest    26323.7] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.18, w 0.02, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.00 m 1.0 kN] [contact 0.03 m 3.9 kN] [contact 0.00 m 0.6 kN], frame v 9.77
    [AutoTest    26324.2] lander watch: tilt 0.7 deg, surface speed 0.01 m/s, unity v 1.61, w 0.02, parts 17, legs [contact 0.03 m 4.0 kN] [contact 0.00 m 0.9 kN] [contact 0.03 m 3.9 kN] [contact 0.00 m 0.7 kN], frame v 9.56
    [AutoTest    26324.7] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 2.30, w 0.02, parts 17, legs [contact 0.03 m 4.0 kN] [contact 0.00 m 0.8 kN] [contact 0.03 m 3.8 kN] [contact 0.00 m 0.8 kN], frame v 9.54
    [AutoTest    26325.3] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 2.54, w 0.03, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.9 kN] [contact 0.01 m 0.8 kN], frame v 9.48
    [AutoTest    26325.8] lander watch: tilt 0.7 deg, surface speed 0.00 m/s, unity v 1.93, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN], frame v 9.26
    [AutoTest    26326.2] screenshot: lunar_161_before_boarding.png
    [AutoTest    26326.2] boarding: lander AGL 4.14 m, situation Landed, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 0.7 kN]
    [AutoTest    26326.3] lander watch: tilt 0.8 deg, surface speed 0.03 m/s, unity v 0.00, w 0.06, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.01 m 1.9 kN] [contact 0.04 m 4.4 kN] [contact 0.01 m 0.8 kN], frame v 8.80
    [AutoTest    26326.7] screenshot: lunar_164_boarded.png
    [AutoTest    26326.7] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    26326.7] PHASE lunar ascent
    [AutoTest    26332.7] PHASE lunar gravity turn
    [AutoTest    26357.4] Luma orbit circularization: dv 417.5 m/s, burn 35.4 s, in 171 s
    [AutoTest    26360.3] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    26510.4] Luma orbit circularization: 417 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26514.5] Luma orbit circularization: 374 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26518.5] Luma orbit circularization: 328 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26522.5] Luma orbit circularization: 282 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26526.5] Luma orbit circularization: 235 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26530.5] Luma orbit circularization: 188 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26534.6] Luma orbit circularization: 140 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26538.6] Luma orbit circularization: 91 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    26542.6] Luma orbit circularization: 42 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    26546.6] Luma orbit circularization: 3 m/s to go, 0.2° off, throttle 0.21, spin 0.004 rad/s, SAS Maneuver
    [AutoTest    26548.4] Luma orbit circularization: burn complete, residual 0.29 m/s after 37.9 s
    [AutoTest    26548.4] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    26548.4] delta-v left in Luma orbit after ascent: 1209 m/s vac (1209)
    [AutoTest    26548.4] PHASE plan return
    [AutoTest    26548.4] CHECK PASS: return trajectory to Tellus found — burn 370.0 m/s in 2365 s, Tellus Pe 32.1 km
    [AutoTest    26548.4] PHASE trans-Tellus injection
    [AutoTest    26548.4] TTI: dv 370.0 m/s, burn 27.9 s, in 2365 s
    [AutoTest    28899.3] TTI: 370 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    28903.3] TTI: 321 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    28907.3] TTI: 270 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    28911.4] TTI: 217 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    28915.4] TTI: 164 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28919.4] TTI: 110 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28923.4] TTI: 55 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28927.4] TTI: 6 m/s to go, 0.1° off, throttle 0.36, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    28930.0] TTI: burn complete, residual 0.20 m/s after 30.7 s
    [AutoTest    28930.0] delta-v left after trans-Tellus injection: 839 m/s vac (839)
    [AutoTest    28930.0] PHASE coast home
    [AutoTest    33796.5] Tellus periapsis after SOI exit 36.8 km
    [AutoTest    33796.5] CHECK PASS: return periapsis in reentry corridor — Pe 36.8 km
    [AutoTest   108446.3] PHASE reentry
    [AutoTest   108560.2] screenshot: lunar_201_reentry.png
    [AutoTest   108811.0] arming the parachute at 15.1 km, 383 m/s
    [AutoTest   109136.7] screenshot: lunar_203_touchdown.png
    [AutoTest   109136.7] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 782 K (HS-125 Ablative Heat Shield), max 6.3 g
