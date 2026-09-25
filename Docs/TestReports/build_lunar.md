# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 37.0 kPa
- [x] stable orbit reached: Pe 80.9 km, Ap 82.8 km, e 0.0014
- [x] TLI encounter found by patched-conic search: node in 1437 s, 870.0 m/s prograde, Luma Pe 39.9 km
- [x] planned trajectory shows Luma encounter: Pe 39.9 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 113.2 km (planned 39.9 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.7 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 2.6 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.1 s (AGL 0.7 m, 14.2 m from the lander axis, grounded True on TerrainCollider (5, 988, 2006), speed 1.80 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (5, 988, 2006), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.69 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 270.6 m/s in 1807 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 35.3 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 799 K (HS-125 Ablative Heat Shield), max 6.4 g

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
    [AutoTest      247.2] circularization: 534 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      251.2] circularization: 480 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      255.2] circularization: 425 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      259.2] circularization: 370 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      263.3] circularization: 313 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      267.3] circularization: 255 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      271.3] circularization: 196 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest      275.3] circularization: 136 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest      279.3] circularization: 75 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest      283.4] circularization: 13 m/s to go, 0.1° off, throttle 0.74, spin 0.004 rad/s, SAS Maneuver
    [AutoTest      287.0] circularization: burn complete, residual 0.30 m/s after 55.9 s
    [AutoTest      287.0] CHECK PASS: stable orbit reached — Pe 80.9 km, Ap 82.8 km, e 0.0014
    [AutoTest      287.0] in orbit 285 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      287.0] screenshot: lunar_029_orbit.png
    [AutoTest      287.0] PHASE plan TLI
    [AutoTest      287.0] CHECK PASS: TLI encounter found by patched-conic search — node in 1437 s, 870.0 m/s prograde, Luma Pe 39.9 km
    [AutoTest      287.5] CHECK PASS: planned trajectory shows Luma encounter — Pe 39.9 km
    [AutoTest      287.5] PHASE TLI burn
    [AutoTest      287.5] TLI: dv 870.0 m/s, burn 47.9 s, in 1437 s
    [AutoTest     1700.1] TLI: 870 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1704.1] TLI: 809 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1708.1] TLI: 744 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1712.1] TLI: 677 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1716.2] TLI: 608 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1720.2] TLI: 538 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1724.2] TLI: 467 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1724.6] staging during burn
    [AutoTest     1728.2] TLI: 438 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1732.2] TLI: 412 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1736.3] TLI: 385 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1740.3] TLI: 359 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1744.3] TLI: 332 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1748.3] TLI: 305 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1752.3] TLI: 278 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1756.4] TLI: 251 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1760.4] TLI: 224 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1764.4] TLI: 196 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1768.4] TLI: 168 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest     1772.4] TLI: 141 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1776.5] TLI: 112 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1780.5] TLI: 84 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1784.5] TLI: 56 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1788.5] TLI: 27 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     1792.5] TLI: 2 m/s to go, 0.2° off, throttle 0.31, spin 0.008 rad/s, SAS Maneuver
    [AutoTest     1794.5] TLI: burn complete, residual 0.29 m/s after 94.4 s
    [AutoTest     1794.5] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 113.2 km (planned 39.9 km)
    [AutoTest     1794.5] delta-v left after TLI: 3012 m/s vac (3012)
    [AutoTest     1794.5] PHASE mid-course correction
    [AutoTest     1794.5] correction: pro -3.00 rad -5.95 nrm 0.35 m/s (score 334)
    [AutoTest     1794.5] MCC: dv 6.7 m/s, burn 0.9 s, in 1800 s
    [AutoTest     3594.0] MCC: 7 m/s to go, 0.0° off, throttle 0.79, spin 0.001 rad/s, SAS Maneuver
    [AutoTest     3597.5] MCC: burn complete, residual 0.15 m/s after 3.5 s
    [AutoTest     3597.5] PHASE coast to Luma
    [AutoTest    19033.2] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    19033.2] Luma periapsis 36.7 km: no correction needed
    [AutoTest    19033.2] PHASE capture burn
    [AutoTest    19033.2] Luma periapsis 36.7 km in 6328 s
    [AutoTest    19033.2] Luma orbit insertion: dv 282.2 m/s, burn 36.7 s, in 6328 s
    [AutoTest    25343.3] Luma orbit insertion: 282 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    25347.3] Luma orbit insertion: 254 m/s to go, 0.0° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    25351.4] Luma orbit insertion: 224 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25355.4] Luma orbit insertion: 193 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25359.4] Luma orbit insertion: 163 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25363.4] Luma orbit insertion: 132 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25367.4] Luma orbit insertion: 101 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25371.5] Luma orbit insertion: 69 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    25375.5] Luma orbit insertion: 37 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    25379.5] Luma orbit insertion: 6 m/s to go, 0.1° off, throttle 0.66, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    25382.3] Luma orbit insertion: burn complete, residual 0.30 m/s after 39.0 s
    [AutoTest    25382.3] screenshot: lunar_085_luma_orbit.png
    [AutoTest    25382.3] CHECK PASS: captured into Luma orbit — Pe 36.7 km, Ap 37.3 km
    [AutoTest    25382.3] delta-v left in Luma orbit: 2724 m/s vac (2724)
    [AutoTest    25384.4] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    25384.4] PHASE deorbit
    [AutoTest    25387.7] PHASE descent
    [AutoTest    26101.3] suicide burn start at 12256 m AGL, 589 m/s
    [AutoTest    26262.0] final descent from 150 m AGL at 41.3 m/s (vertical -34.5 m/s)
    [AutoTest    26262.6] landing site: 1.7° slope 24 m away (ground below: 8.0°)
    [AutoTest    26308.6] screenshot: lunar_094_luma_landed.png
    [AutoTest    26308.6] CHECK PASS: landed on Luma — situation Landed, tilt 2.6 deg, legs intact 4/4, crew 1
    [AutoTest    26308.6] delta-v left on the surface: 1940 m/s vac (1940)
    [AutoTest    26308.6] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    26308.6] PHASE EVA
    [AutoTest    26308.6] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
    [AutoTest    26308.6] lander watch: tilt 2.6 deg, surface speed 0.05 m/s, unity v 0.35, w 0.04, parts 17, legs [contact 0.03 m 4.1 kN] [contact 0.02 m 3.8 kN] [contact 0.01 m 2.1 kN] [contact 0.03 m 5.3 kN], frame v 8.45
    [AutoTest    26309.1] lander watch: tilt 2.4 deg, surface speed 0.03 m/s, unity v 0.82, w 0.05, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.02 m 1.6 kN] [contact 0.03 m 3.2 kN], frame v 8.48
    [AutoTest    26309.7] lander watch: tilt 2.3 deg, surface speed 0.01 m/s, unity v 1.62, w 0.05, parts 17, legs [contact 0.02 m 1.6 kN] [contact 0.03 m 3.1 kN] [contact 0.02 m 1.6 kN] [contact 0.03 m 3.1 kN], frame v 8.60
    [AutoTest    26310.2] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 2.43, w 0.05, parts 17, legs [contact 0.02 m 2.1 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 1.2 kN] [contact 0.03 m 3.4 kN], frame v 8.79
    [AutoTest    26310.7] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 3.25, w 0.05, parts 17, legs [contact 0.02 m 2.0 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 1.3 kN] [contact 0.03 m 3.4 kN], frame v 9.05
    [AutoTest    26311.2] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 4.08, w 0.05, parts 17, legs [contact 0.02 m 1.8 kN] [contact 0.03 m 3.0 kN] [contact 0.02 m 1.4 kN] [contact 0.03 m 3.3 kN], frame v 9.38
    [AutoTest    26311.7] lander watch: tilt 2.4 deg, surface speed 0.67 m/s, unity v 0.83, w 0.13, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.41
    [AutoTest    26312.3] lander watch: tilt 3.0 deg, surface speed 0.16 m/s, unity v 1.36, w 0.13, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.52
    [AutoTest    26312.8] lander watch: tilt 3.2 deg, surface speed 0.23 m/s, unity v 1.67, w 0.09, parts 17, legs [contact 0.07 m 11.0 kN] [contact 0.03 m 9.0 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 9.8 kN], frame v 7.07
    [AutoTest    26313.3] lander watch: tilt 2.1 deg, surface speed 0.12 m/s, unity v 1.69, w 0.05, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 2.9 kN] [contact 0.02 m 3.1 kN] [contact 0.02 m 2.3 kN], frame v 7.05
    [AutoTest    26313.8] lander watch: tilt 1.8 deg, surface speed 0.07 m/s, unity v 1.86, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 3.0 kN] [contact 0.03 m 2.9 kN] [contact 0.02 m 3.3 kN], frame v 7.05
    [AutoTest    26314.3] lander watch: tilt 2.4 deg, surface speed 0.06 m/s, unity v 1.85, w 0.05, parts 17, legs [contact 0.02 m 3.1 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 0.0 kN] [contact 0.03 m 3.2 kN], frame v 7.05
    [AutoTest    26314.9] lander watch: tilt 2.5 deg, surface speed 0.04 m/s, unity v 1.75, w 0.05, parts 17, legs [contact 0.03 m 2.5 kN] [contact 0.03 m 3.3 kN] [contact 0.00 m 0.6 kN] [contact 0.03 m 3.2 kN], frame v 7.06
    [AutoTest    26315.4] lander watch: tilt 2.2 deg, surface speed 0.02 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.01 m 1.2 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.9 kN] [contact 0.03 m 3.2 kN], frame v 7.06
    [AutoTest    26315.9] lander watch: tilt 2.2 deg, surface speed 0.02 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.01 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.2 kN], frame v 7.07
    [AutoTest    26316.4] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.02 m 2.1 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.2 kN], frame v 7.06
    [AutoTest    26316.9] lander watch: tilt 2.3 deg, surface speed 0.01 m/s, unity v 1.78, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.1 kN], frame v 7.06
    [AutoTest    26317.5] lander watch: tilt 2.3 deg, surface speed 0.00 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.2 kN], frame v 7.07
    [AutoTest    26318.0] lander watch: tilt 2.3 deg, surface speed 0.01 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.8 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.2 kN], frame v 7.05
    [AutoTest    26318.5] lander watch: tilt 2.3 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.02 m 1.9 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.2 kN], frame v 7.05
    [AutoTest    26318.7] CHECK PASS: walking in low gravity — 12.0 m in 7.1 s (AGL 0.7 m, 14.2 m from the lander axis, grounded True on TerrainCollider (5, 988, 2006), speed 1.80 m/s, jetpack 10.00 kg)
    [AutoTest    26318.7] CHECK PASS: EVA on the surface — standing on TerrainCollider (5, 988, 2006), gravity 1.60 m/s²
    [AutoTest    26319.0] lander watch: tilt 2.6 deg, surface speed 0.13 m/s, unity v 2.50, w 0.09, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.42
    [AutoTest    26319.5] lander watch: tilt 2.9 deg, surface speed 0.13 m/s, unity v 2.06, w 0.03, parts 17, legs [contact 0.05 m 6.2 kN] [contact 0.04 m 5.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 6.2 kN], frame v 7.25
    [AutoTest    26320.1] lander watch: tilt 2.2 deg, surface speed 0.07 m/s, unity v 1.66, w 0.04, parts 17, legs [contact 0.01 m 0.0 kN] [contact 0.03 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.04 m 3.7 kN], frame v 7.16
    [AutoTest    26320.6] lander watch: tilt 2.1 deg, surface speed 0.05 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.04 m 3.3 kN] [contact 0.02 m 1.7 kN] [contact 0.04 m 3.5 kN], frame v 7.17
    [AutoTest    26321.1] lander watch: tilt 2.5 deg, surface speed 0.02 m/s, unity v 2.17, w 0.04, parts 17, legs [contact 0.02 m 2.4 kN] [contact 0.04 m 3.6 kN] [contact 0.01 m 0.2 kN] [contact 0.04 m 3.2 kN], frame v 7.28
    [AutoTest    26321.5] CHECK PASS: jump in low gravity — apex 1.69 m
    [AutoTest    26321.6] lander watch: tilt 2.4 deg, surface speed 0.33 m/s, unity v 1.12, w 0.08, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.02 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 0.0 kN], frame v 7.80
    [AutoTest    26322.1] lander watch: tilt 1.9 deg, surface speed 0.17 m/s, unity v 0.25, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 5.6 kN] [contact 0.03 m 4.7 kN] [contact 0.02 m 4.9 kN], frame v 8.73
    [AutoTest    26322.7] lander watch: tilt 2.6 deg, surface speed 0.06 m/s, unity v 0.05, w 0.04, parts 17, legs [contact 0.02 m 2.8 kN] [contact 0.02 m 1.6 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN], frame v 8.87
    [AutoTest    26323.2] lander watch: tilt 2.6 deg, surface speed 0.05 m/s, unity v 0.10, w 0.04, parts 17, legs [contact 0.03 m 2.5 kN] [contact 0.02 m 2.9 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.8 kN], frame v 8.88
    [AutoTest    26323.5] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    26323.5] screenshot: lunar_133_eva_flag.png
    [AutoTest    26323.5] PHASE return to hatch
    [AutoTest    26323.5] screenshot: lunar_135_lander_before_return.png
    [AutoTest    26323.7] lander watch: tilt 2.2 deg, surface speed 0.02 m/s, unity v 0.38, w 0.04, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.03 m 4.0 kN] [contact 0.02 m 2.0 kN] [contact 0.03 m 2.6 kN], frame v 9.19
    [AutoTest    26324.2] lander watch: tilt 2.3 deg, surface speed 0.04 m/s, unity v 1.37, w 0.04, parts 17, legs [contact 0.01 m 1.6 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 1.3 kN] [contact 0.03 m 3.6 kN], frame v 10.21
    [AutoTest    26324.7] lander watch: tilt 2.5 deg, surface speed 0.00 m/s, unity v 1.83, w 0.04, parts 17, legs [contact 0.02 m 2.2 kN] [contact 0.03 m 2.7 kN] [contact 0.01 m 0.7 kN] [contact 0.03 m 3.9 kN], frame v 10.62
    [AutoTest    26325.3] lander watch: tilt 2.4 deg, surface speed 0.02 m/s, unity v 1.84, w 0.02, parts 17, legs [contact 0.01 m 1.2 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 2.7 kN], frame v 10.63
    [AutoTest    26325.8] lander watch: tilt 2.3 deg, surface speed 0.00 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.01 m 1.5 kN] [contact 0.03 m 3.4 kN] [contact 0.01 m 1.3 kN] [contact 0.03 m 3.3 kN], frame v 10.63
    [AutoTest    26326.3] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.02 m 1.8 kN] [contact 0.03 m 3.3 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.63
    [AutoTest    26326.8] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.8 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.6 kN], frame v 10.62
    [AutoTest    26327.3] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.5 kN], frame v 10.61
    [AutoTest    26327.9] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.79, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.5 kN], frame v 10.61
    [AutoTest    26328.4] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26328.9] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26329.4] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26329.9] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.63
    [AutoTest    26330.5] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26331.0] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.81, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26331.5] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26332.0] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 10.62
    [AutoTest    26332.5] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.01 m 0.9 kN] [contact 0.03 m 2.3 kN] [contact 0.01 m 0.4 kN] [contact 0.03 m 2.7 kN], frame v 10.61
    [AutoTest    26333.1] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.34, w 0.04, parts 17, legs [contact 0.01 m 1.7 kN] [contact 0.03 m 3.0 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.5 kN], frame v 10.14
    [AutoTest    26333.6] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.18, w 0.04, parts 17, legs [contact 0.01 m 1.6 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.4 kN], frame v 9.65
    [AutoTest    26334.1] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 1.73, w 0.04, parts 17, legs [contact 0.01 m 1.7 kN] [contact 0.02 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 9.52
    [AutoTest    26334.6] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 2.44, w 0.04, parts 17, legs [contact 0.01 m 1.8 kN] [contact 0.02 m 3.1 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.6 kN], frame v 9.53
    [AutoTest    26335.1] lander watch: tilt 2.4 deg, surface speed 0.01 m/s, unity v 2.39, w 0.05, parts 17, legs [contact 0.02 m 1.8 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.6 kN], frame v 9.41
    [AutoTest    26335.7] lander watch: tilt 2.4 deg, surface speed 0.00 m/s, unity v 1.78, w 0.04, parts 17, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN], frame v 9.21
    [AutoTest    26335.9] screenshot: lunar_160_before_boarding.png
    [AutoTest    26335.9] boarding: lander AGL 4.14 m, situation Landed, legs [contact 0.02 m 1.7 kN] [contact 0.03 m 3.1 kN] [contact 0.01 m 1.1 kN] [contact 0.03 m 3.5 kN]
    [AutoTest    26336.2] lander watch: tilt 2.4 deg, surface speed 0.03 m/s, unity v 0.00, w 0.04, parts 17, legs [contact 0.02 m 1.9 kN] [contact 0.03 m 2.5 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 3.8 kN], frame v 8.86
    [AutoTest    26336.5] screenshot: lunar_163_boarded.png
    [AutoTest    26336.5] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    26336.5] PHASE lunar ascent
    [AutoTest    26342.5] PHASE lunar gravity turn
    [AutoTest    26367.6] Luma orbit circularization: dv 401.9 m/s, burn 34.0 s, in 172 s
    [AutoTest    26370.4] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    26522.5] Luma orbit circularization: 401 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26526.5] Luma orbit circularization: 358 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26530.5] Luma orbit circularization: 312 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26534.5] Luma orbit circularization: 266 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    26538.5] Luma orbit circularization: 219 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    26542.6] Luma orbit circularization: 171 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    26546.6] Luma orbit circularization: 123 m/s to go, 0.0° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    26550.6] Luma orbit circularization: 74 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    26554.6] Luma orbit circularization: 24 m/s to go, 0.1° off, throttle 1.00, spin 0.003 rad/s, SAS Maneuver
    [AutoTest    26558.6] Luma orbit circularization: 1 m/s to go, 0.2° off, throttle 0.07, spin 0.005 rad/s, SAS Maneuver
    [AutoTest    26559.0] Luma orbit circularization: burn complete, residual 0.29 m/s after 36.5 s
    [AutoTest    26559.0] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    26559.0] delta-v left in Luma orbit after ascent: 1209 m/s vac (1209)
    [AutoTest    26559.0] PHASE plan return
    [AutoTest    26559.0] CHECK PASS: return trajectory to Tellus found — burn 270.6 m/s in 1807 s, Tellus Pe 32.0 km
    [AutoTest    26559.0] PHASE trans-Tellus injection
    [AutoTest    26559.0] TTI: dv 270.6 m/s, burn 20.7 s, in 1807 s
    [AutoTest    28356.1] TTI: 271 m/s to go, 0.1° off, throttle 1.00, spin 0.000 rad/s, SAS Maneuver
    [AutoTest    28360.1] TTI: 222 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28364.1] TTI: 170 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28368.1] TTI: 118 m/s to go, 0.1° off, throttle 1.00, spin 0.001 rad/s, SAS Maneuver
    [AutoTest    28372.1] TTI: 65 m/s to go, 0.1° off, throttle 1.00, spin 0.002 rad/s, SAS Maneuver
    [AutoTest    28376.2] TTI: 11 m/s to go, 0.1° off, throttle 0.73, spin 0.004 rad/s, SAS Maneuver
    [AutoTest    28379.6] TTI: burn complete, residual 0.19 m/s after 23.5 s
    [AutoTest    28379.6] delta-v left after trans-Tellus injection: 939 m/s vac (939)
    [AutoTest    28379.6] PHASE coast home
    [AutoTest    35420.5] Tellus periapsis after SOI exit 35.3 km
    [AutoTest    35420.5] CHECK PASS: return periapsis in reentry corridor — Pe 35.3 km
    [AutoTest    55468.9] PHASE reentry
    [AutoTest    55582.0] screenshot: lunar_198_reentry.png
    [AutoTest    55774.7] arming the parachute at 15.6 km, 401 m/s
    [AutoTest    56104.1] screenshot: lunar_200_touchdown.png
    [AutoTest    56104.1] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 799 K (HS-125 Ablative Heat Shield), max 6.4 g
