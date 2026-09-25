# The Astraea Program automated mission report: lunar
Result: PASSED
Checks: 19/19 passed

- [x] liftoff: vertical speed 8.5 m/s
- [x] max dynamic pressure survivable: 37.0 kPa
- [x] stable orbit reached: Pe 80.9 km, Ap 82.8 km, e 0.0014
- [x] TLI encounter found by patched-conic search: node in 1437 s, 870.0 m/s prograde, Luma Pe 40.0 km
- [x] planned trajectory shows Luma encounter: Pe 40.0 km
- [x] actual post-burn trajectory encounters Luma: predicted Pe 112.6 km (planned 40.0 km)
- [x] entered Luma sphere of influence: frame body Luma
- [x] captured into Luma orbit: Pe 36.7 km, Ap 37.3 km
- [x] landed on Luma: situation Landed, tilt 0.8 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 6.9 s (AGL 0.7 m, 14.4 m from the lander axis, grounded True on TerrainCollider (5, 987, 1956), speed 1.82 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (5, 987, 1956), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.70 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 321.8 m/s in 1646 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.9 km
- [x] capsule landed safely with crew: Landed at 0.0 m/s, crew 1, max skin 822 K (HS-125 Ablative Heat Shield), max 6.4 g

## Log
    [AutoTest        1.5] PHASE ascent
    [AutoTest        2.5] CHECK PASS: liftoff — vertical speed 8.5 m/s
    [AutoTest        7.0] PHASE pitch-over
    [AutoTest        7.0] pitch-over to 9.2° (TWR 2.13)
    [AutoTest       13.1] PHASE gravity turn
    [AutoTest       45.5] staging at 9.9 km, 523 m/s
    [AutoTest      108.0] staging at 37.3 km, 1195 m/s
    [AutoTest      164.2] MECO: Ap 82.0 km, max Q 37.0 kPa, delta-v left 4623 m/s vac (1147 + 3476)
    [AutoTest      164.2] CHECK PASS: max dynamic pressure survivable — 37.0 kPa
    [AutoTest      164.2] PHASE coast to space
    [AutoTest      183.0] PHASE circularize
    [AutoTest      183.0] circularization: dv 736.3 m/s, burn 52.8 s, in 75 s
    [AutoTest      287.1] circularization: burn complete, residual 0.30 m/s after 55.9 s
    [AutoTest      287.1] CHECK PASS: stable orbit reached — Pe 80.9 km, Ap 82.8 km, e 0.0014
    [AutoTest      287.1] in orbit 286 s after launch with 3886 m/s vac (410 + 3476) left
    [AutoTest      287.1] screenshot: lunar_015_orbit.png
    [AutoTest      287.1] PHASE plan TLI
    [AutoTest      287.1] CHECK PASS: TLI encounter found by patched-conic search — node in 1437 s, 870.0 m/s prograde, Luma Pe 40.0 km
    [AutoTest      287.6] CHECK PASS: planned trajectory shows Luma encounter — Pe 40.0 km
    [AutoTest      287.6] PHASE TLI burn
    [AutoTest      287.6] TLI: dv 870.0 m/s, burn 47.9 s, in 1436 s
    [AutoTest     1724.5] staging during burn
    [AutoTest     1794.4] TLI: burn complete, residual 0.30 m/s after 94.4 s
    [AutoTest     1794.4] CHECK PASS: actual post-burn trajectory encounters Luma — predicted Pe 112.6 km (planned 40.0 km)
    [AutoTest     1794.4] delta-v left after TLI: 3012 m/s vac (3012)
    [AutoTest     1794.4] PHASE mid-course correction
    [AutoTest     1794.4] correction: pro -3.00 rad -6.00 nrm 1.20 m/s (score 343)
    [AutoTest     1794.4] MCC: dv 6.8 m/s, burn 0.9 s, in 1800 s
    [AutoTest     3597.4] MCC: burn complete, residual 0.15 m/s after 3.5 s
    [AutoTest     3597.4] PHASE coast to Luma
    [AutoTest    19029.6] CHECK PASS: entered Luma sphere of influence — frame body Luma
    [AutoTest    19029.6] Luma periapsis 36.7 km: no correction needed
    [AutoTest    19029.6] PHASE capture burn
    [AutoTest    19029.6] Luma periapsis 36.7 km in 6328 s
    [AutoTest    19029.6] Luma orbit insertion: dv 282.2 m/s, burn 36.7 s, in 6328 s
    [AutoTest    25378.3] Luma orbit insertion: burn complete, residual 0.30 m/s after 39.0 s
    [AutoTest    25378.3] screenshot: lunar_036_luma_orbit.png
    [AutoTest    25378.3] CHECK PASS: captured into Luma orbit — Pe 36.7 km, Ap 37.3 km
    [AutoTest    25378.3] delta-v left in Luma orbit: 2724 m/s vac (2724)
    [AutoTest    25380.3] snapshot (Luma orbit) saved: autotest_luma_orbit.json
    [AutoTest    25380.3] PHASE deorbit
    [AutoTest    25383.7] PHASE descent
    [AutoTest    26097.2] suicide burn start at 12322 m AGL, 589 m/s
    [AutoTest    26258.8] final descent from 150 m AGL at 41.2 m/s (vertical -34.5 m/s)
    [AutoTest    26259.4] landing site: 1.0° slope 12 m away (ground below: 3.7°)
    [AutoTest    26305.2] screenshot: lunar_045_luma_landed.png
    [AutoTest    26305.2] CHECK PASS: landed on Luma — situation Landed, tilt 0.8 deg, legs intact 4/4, crew 1
    [AutoTest    26305.2] delta-v left on the surface: 1940 m/s vac (1940)
    [AutoTest    26305.2] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    26305.2] PHASE EVA
    [AutoTest    26305.2] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.36 m/s rel. lander
    [AutoTest    26305.2] lander watch: tilt 0.8 deg, surface speed 0.04 m/s, unity v 0.36, w 0.04, parts 17, legs [contact 0.04 m 6.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 6.1 kN] [free 0.00 m 0.0 kN], frame v 8.44
    [AutoTest    26305.7] lander watch: tilt 0.8 deg, surface speed 0.01 m/s, unity v 0.85, w 0.00, parts 17, legs [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 8.47
    [AutoTest    26306.3] lander watch: tilt 0.9 deg, surface speed 0.01 m/s, unity v 1.63, w 0.00, parts 17, legs [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 8.59
    [AutoTest    26306.8] lander watch: tilt 1.0 deg, surface speed 0.02 m/s, unity v 2.44, w 0.01, parts 17, legs [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 8.78
    [AutoTest    26307.3] lander watch: tilt 1.1 deg, surface speed 0.03 m/s, unity v 3.27, w 0.01, parts 17, legs [contact 0.05 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 9.04
    [AutoTest    26307.8] lander watch: tilt 1.3 deg, surface speed 0.04 m/s, unity v 4.09, w 0.01, parts 17, legs [contact 0.05 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 9.37
    [AutoTest    26308.3] lander watch: tilt 1.3 deg, surface speed 0.52 m/s, unity v 1.10, w 0.07, parts 17, legs [contact 0.01 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.86
    [AutoTest    26308.9] lander watch: tilt 1.7 deg, surface speed 0.32 m/s, unity v 1.72, w 0.07, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.09
    [AutoTest    26309.4] lander watch: tilt 1.2 deg, surface speed 0.24 m/s, unity v 1.58, w 0.07, parts 17, legs [contact 0.05 m 3.5 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 4.3 kN] [contact 0.04 m 2.4 kN], frame v 7.02
    [AutoTest    26309.9] lander watch: tilt 0.3 deg, surface speed 0.18 m/s, unity v 1.68, w 0.05, parts 17, legs [contact 0.02 m 3.0 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 7.0 kN] [free 0.00 m 0.0 kN], frame v 7.04
    [AutoTest    26310.4] lander watch: tilt 1.8 deg, surface speed 0.21 m/s, unity v 1.74, w 0.05, parts 17, legs [contact 0.04 m 5.0 kN] [contact 0.00 m 1.8 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 7.03
    [AutoTest    26310.9] lander watch: tilt 2.8 deg, surface speed 0.08 m/s, unity v 1.75, w 0.02, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.03 m 4.0 kN] [contact 0.02 m 1.3 kN] [free 0.00 m 0.0 kN], frame v 7.03
    [AutoTest    26311.5] lander watch: tilt 2.9 deg, surface speed 0.07 m/s, unity v 1.84, w 0.05, parts 17, legs [contact 0.03 m 3.0 kN] [contact 0.03 m 3.5 kN] [contact 0.03 m 3.0 kN] [free 0.00 m 0.0 kN], frame v 7.03
    [AutoTest    26312.0] lander watch: tilt 2.1 deg, surface speed 0.12 m/s, unity v 1.91, w 0.03, parts 17, legs [contact 0.03 m 4.1 kN] [contact 0.01 m 0.5 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 7.03
    [AutoTest    26312.5] lander watch: tilt 1.3 deg, surface speed 0.10 m/s, unity v 1.91, w 0.03, parts 17, legs [contact 0.05 m 5.1 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 7.02
    [AutoTest    26313.0] lander watch: tilt 0.7 deg, surface speed 0.09 m/s, unity v 1.89, w 0.02, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 7.02
    [AutoTest    26313.5] lander watch: tilt 0.5 deg, surface speed 0.08 m/s, unity v 1.88, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [contact 0.00 m 1.1 kN], frame v 7.02
    [AutoTest    26314.1] lander watch: tilt 0.7 deg, surface speed 0.02 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 3.7 kN] [contact 0.01 m 1.7 kN], frame v 7.02
    [AutoTest    26314.6] lander watch: tilt 0.6 deg, surface speed 0.04 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 3.9 kN] [contact 0.01 m 1.0 kN], frame v 7.02
    [AutoTest    26315.1] lander watch: tilt 0.4 deg, surface speed 0.06 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [contact 0.00 m 0.0 kN], frame v 7.02
    [AutoTest    26315.1] CHECK PASS: walking in low gravity — 12.0 m in 6.9 s (AGL 0.7 m, 14.4 m from the lander axis, grounded True on TerrainCollider (5, 987, 1956), speed 1.82 m/s, jetpack 10.00 kg)
    [AutoTest    26315.1] CHECK PASS: EVA on the surface — standing on TerrainCollider (5, 987, 1956), gravity 1.60 m/s²
    [AutoTest    26315.6] lander watch: tilt 0.7 deg, surface speed 0.11 m/s, unity v 2.30, w 0.02, parts 17, legs [contact 0.04 m 5.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 5.3 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    26316.1] lander watch: tilt 1.1 deg, surface speed 0.07 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.05 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 7.16
    [AutoTest    26316.7] lander watch: tilt 1.7 deg, surface speed 0.09 m/s, unity v 1.66, w 0.02, parts 17, legs [contact 0.05 m 4.9 kN] [contact 0.00 m 0.5 kN] [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 7.13
    [AutoTest    26317.2] lander watch: tilt 2.2 deg, surface speed 0.05 m/s, unity v 1.95, w 0.02, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.02 m 1.8 kN] [contact 0.04 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 7.20
    [AutoTest    26317.7] lander watch: tilt 2.3 deg, surface speed 0.01 m/s, unity v 2.50, w 0.03, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.02 m 2.0 kN] [contact 0.04 m 3.4 kN] [free 0.00 m 0.0 kN], frame v 7.36
    [AutoTest    26317.9] CHECK PASS: jump in low gravity — apex 1.70 m
    [AutoTest    26318.2] lander watch: tilt 2.0 deg, surface speed 0.09 m/s, unity v 1.06, w 0.02, parts 17, legs [contact 0.00 m 0.3 kN] [free 0.00 m 0.0 kN] [contact 0.00 m 0.5 kN] [free 0.00 m 0.0 kN], frame v 7.85
    [AutoTest    26318.7] lander watch: tilt 1.5 deg, surface speed 0.06 m/s, unity v 0.14, w 0.02, parts 17, legs [contact 0.05 m 6.1 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 5.6 kN] [free 0.00 m 0.0 kN], frame v 8.73
    [AutoTest    26319.3] lander watch: tilt 1.1 deg, surface speed 0.05 m/s, unity v 0.04, w 0.01, parts 17, legs [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.84
    [AutoTest    26319.8] lander watch: tilt 0.8 deg, surface speed 0.04 m/s, unity v 0.03, w 0.01, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 8.85
    [AutoTest    26320.0] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    26320.0] screenshot: lunar_084_eva_flag.png
    [AutoTest    26320.0] PHASE return to hatch
    [AutoTest    26320.0] screenshot: lunar_086_lander_before_return.png
    [AutoTest    26320.3] lander watch: tilt 0.6 deg, surface speed 0.03 m/s, unity v 0.63, w 0.01, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 9.48
    [AutoTest    26320.8] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 1.60, w 0.01, parts 17, legs [contact 0.04 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 10.44
    [AutoTest    26321.3] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 10.62
    [AutoTest    26321.9] lander watch: tilt 0.4 deg, surface speed 0.02 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 10.63
    [AutoTest    26322.4] lander watch: tilt 0.5 deg, surface speed 0.01 m/s, unity v 1.79, w 0.00, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [contact 0.00 m 0.4 kN], frame v 10.61
    [AutoTest    26322.9] lander watch: tilt 0.5 deg, surface speed 0.01 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [contact 0.00 m 0.3 kN], frame v 10.61
    [AutoTest    26323.4] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 1.80, w 0.00, parts 17, legs [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 10.60
    [AutoTest    26323.9] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 1.79, w 0.00, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26324.5] lander watch: tilt 0.5 deg, surface speed 0.02 m/s, unity v 1.79, w 0.00, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26325.0] lander watch: tilt 0.6 deg, surface speed 0.02 m/s, unity v 1.79, w 0.01, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26325.5] lander watch: tilt 0.7 deg, surface speed 0.03 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26326.0] lander watch: tilt 0.9 deg, surface speed 0.04 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 10.58
    [AutoTest    26326.5] lander watch: tilt 1.1 deg, surface speed 0.05 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26327.1] lander watch: tilt 1.5 deg, surface speed 0.06 m/s, unity v 1.83, w 0.02, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 10.59
    [AutoTest    26327.6] lander watch: tilt 2.0 deg, surface speed 0.06 m/s, unity v 1.83, w 0.01, parts 17, legs [contact 0.04 m 4.2 kN] [contact 0.01 m 1.3 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.60
    [AutoTest    26328.1] lander watch: tilt 2.3 deg, surface speed 0.01 m/s, unity v 1.80, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.02 m 1.8 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 10.60
    [AutoTest    26328.6] lander watch: tilt 2.2 deg, surface speed 0.03 m/s, unity v 1.77, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 1.2 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.60
    [AutoTest    26329.1] lander watch: tilt 1.9 deg, surface speed 0.04 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.7 kN] [contact 0.00 m 0.2 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 10.61
    [AutoTest    26329.7] lander watch: tilt 1.6 deg, surface speed 0.03 m/s, unity v 1.43, w 0.01, parts 17, legs [contact 0.04 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 10.24
    [AutoTest    26330.2] lander watch: tilt 1.5 deg, surface speed 0.01 m/s, unity v 1.28, w 0.00, parts 17, legs [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 9.72
    [AutoTest    26330.7] lander watch: tilt 1.5 deg, surface speed 0.01 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.7 kN] [free 0.00 m 0.0 kN], frame v 9.55
    [AutoTest    26331.2] lander watch: tilt 1.6 deg, surface speed 0.03 m/s, unity v 2.51, w 0.01, parts 17, legs [contact 0.04 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.6 kN] [free 0.00 m 0.0 kN], frame v 9.53
    [AutoTest    26331.7] lander watch: tilt 1.9 deg, surface speed 0.04 m/s, unity v 2.42, w 0.01, parts 17, legs [contact 0.04 m 4.6 kN] [contact 0.01 m 0.9 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.38
    [AutoTest    26332.3] lander watch: tilt 2.1 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.01 m 1.2 kN] [contact 0.04 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 9.18
    [AutoTest    26332.6] screenshot: lunar_111_before_boarding.png
    [AutoTest    26332.6] boarding: lander AGL 4.03 m, situation Landed, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 1.1 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN]
    [AutoTest    26332.8] lander watch: tilt 2.0 deg, surface speed 0.06 m/s, unity v 0.00, w 0.02, parts 17, legs [contact 0.04 m 4.5 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 8.87
    [AutoTest    26333.1] screenshot: lunar_114_boarded.png
    [AutoTest    26333.1] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    26333.1] PHASE lunar ascent
    [AutoTest    26339.1] PHASE lunar gravity turn
    [AutoTest    26363.9] Luma orbit circularization: dv 411.7 m/s, burn 34.8 s, in 171 s
    [AutoTest    26367.1] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    26555.0] Luma orbit circularization: burn complete, residual 0.30 m/s after 37.3 s
    [AutoTest    26555.0] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    26555.0] delta-v left in Luma orbit after ascent: 1202 m/s vac (1202)
    [AutoTest    26555.0] PHASE plan return
    [AutoTest    26555.0] CHECK PASS: return trajectory to Tellus found — burn 321.8 m/s in 1646 s, Tellus Pe 32.0 km
    [AutoTest    26555.0] PHASE trans-Tellus injection
    [AutoTest    26555.0] TTI: dv 321.8 m/s, burn 24.4 s, in 1646 s
    [AutoTest    28216.3] TTI: burn complete, residual 0.20 m/s after 27.2 s
    [AutoTest    28216.3] delta-v left after trans-Tellus injection: 881 m/s vac (881)
    [AutoTest    28216.3] PHASE coast home
    [AutoTest    33860.0] Tellus periapsis after SOI exit 31.9 km
    [AutoTest    33860.0] CHECK PASS: return periapsis in reentry corridor — Pe 31.9 km
    [AutoTest    47208.2] PHASE reentry
    [AutoTest    47316.9] screenshot: lunar_133_reentry.png
    [AutoTest    47459.0] arming the parachute at 16.1 km, 421 m/s
    [AutoTest    47787.2] screenshot: lunar_135_touchdown.png
    [AutoTest    47787.2] CHECK PASS: capsule landed safely with crew — Landed at 0.0 m/s, crew 1, max skin 822 K (HS-125 Ablative Heat Shield), max 6.4 g
