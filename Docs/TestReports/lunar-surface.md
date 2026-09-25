# The Astraea Program automated mission report: lunar-surface
Result: PASSED
Checks: 12/12 passed

- [x] resumed landed on Luma: Luma Pathfinder: Landed on Luma, crew 1, delta-v 1664 m/s vac (1664)
- [x] reloaded lander stands on its legs: 4/4 legs in contact, tilt 8.5 deg, legs [contact 0.01 m 1.3 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 3.5 kN]
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 6.9 s (AGL 0.6 m, 14.8 m from the lander axis, grounded True on TerrainCollider (1, 2271, 1981), speed 1.78 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2271, 1981), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.67 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 280.0 m/s in 1527 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 36.6 km
- [x] capsule landed safely with crew: Splashed at 0.9 m/s, crew 1, max skin 950 K (HS-125 Ablative Heat Shield), max 8.7 g

## Log
    [AutoTest    15916.8] PHASE resume on Luma
    [AutoTest    15918.9] CHECK PASS: resumed landed on Luma — Luma Pathfinder: Landed on Luma, crew 1, delta-v 1664 m/s vac (1664)
    [AutoTest    15920.9] CHECK PASS: reloaded lander stands on its legs — 4/4 legs in contact, tilt 8.5 deg, legs [contact 0.01 m 1.3 kN] [contact 0.03 m 3.2 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 3.5 kN]
    [AutoTest    15920.9] PHASE EVA
    [AutoTest    15920.9] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.35 m/s rel. lander
    [AutoTest    15920.9] lander watch: tilt 8.5 deg, surface speed 0.05 m/s, unity v 0.35, w 0.04, parts 17, legs [contact 0.01 m 2.6 kN] [contact 0.03 m 4.5 kN] [contact 0.01 m 2.3 kN] [contact 0.03 m 5.0 kN], frame v 8.52
    [AutoTest    15921.4] lander watch: tilt 8.8 deg, surface speed 0.04 m/s, unity v 0.88, w 0.05, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 2.4 kN] [contact 0.04 m 3.8 kN], frame v 8.56
    [AutoTest    15921.9] lander watch: tilt 9.0 deg, surface speed 0.02 m/s, unity v 1.68, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 2.2 kN] [contact 0.03 m 2.8 kN] [contact 0.03 m 3.5 kN], frame v 8.68
    [AutoTest    15922.5] lander watch: tilt 9.0 deg, surface speed 0.02 m/s, unity v 2.50, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 2.4 kN] [contact 0.03 m 2.6 kN] [contact 0.03 m 3.5 kN], frame v 8.87
    [AutoTest    15923.0] lander watch: tilt 8.8 deg, surface speed 0.03 m/s, unity v 3.32, w 0.04, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 2.0 kN] [contact 0.04 m 3.8 kN], frame v 9.14
    [AutoTest    15923.5] lander watch: tilt 8.7 deg, surface speed 0.00 m/s, unity v 4.18, w 0.04, parts 17, legs [contact 0.01 m 0.3 kN] [contact 0.03 m 3.2 kN] [contact 0.02 m 1.7 kN] [contact 0.04 m 3.4 kN], frame v 9.48
    [AutoTest    15924.0] lander watch: tilt 8.3 deg, surface speed 0.58 m/s, unity v 1.19, w 0.08, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.51
    [AutoTest    15924.6] lander watch: tilt 6.4 deg, surface speed 0.29 m/s, unity v 1.67, w 0.08, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.78
    [AutoTest    15925.1] lander watch: tilt 7.7 deg, surface speed 0.49 m/s, unity v 1.57, w 0.11, parts 17, legs [contact 0.06 m 4.8 kN] [contact 0.04 m 5.9 kN] [free 0.00 m 0.0 kN] [contact 0.01 m 5.0 kN], frame v 7.70
    [AutoTest    15925.6] lander watch: tilt 10.6 deg, surface speed 0.33 m/s, unity v 1.62, w 0.07, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.06 m 7.5 kN] [contact 0.02 m 0.5 kN], frame v 7.70
    [AutoTest    15926.1] lander watch: tilt 12.0 deg, surface speed 0.12 m/s, unity v 1.83, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.07 m 7.6 kN] [free 0.00 m 0.0 kN], frame v 7.70
    [AutoTest    15926.6] lander watch: tilt 11.9 deg, surface speed 0.14 m/s, unity v 1.95, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.7 kN] [contact 0.02 m 2.8 kN], frame v 7.69
    [AutoTest    15927.2] lander watch: tilt 10.8 deg, surface speed 0.21 m/s, unity v 1.93, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.9 kN] [contact 0.04 m 5.0 kN], frame v 7.68
    [AutoTest    15927.7] lander watch: tilt 9.1 deg, surface speed 0.35 m/s, unity v 1.88, w 0.08, parts 17, legs [contact 0.00 m 3.9 kN] [contact 0.01 m 5.1 kN] [contact 0.03 m 2.9 kN] [contact 0.04 m 3.9 kN], frame v 7.68
    [AutoTest    15928.2] lander watch: tilt 8.3 deg, surface speed 0.03 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.02 m 2.1 kN] [contact 0.05 m 5.2 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.67
    [AutoTest    15928.7] lander watch: tilt 9.0 deg, surface speed 0.17 m/s, unity v 1.84, w 0.05, parts 17, legs [contact 0.01 m 0.0 kN] [contact 0.03 m 1.0 kN] [contact 0.03 m 3.2 kN] [contact 0.02 m 3.5 kN], frame v 7.68
    [AutoTest    15929.2] lander watch: tilt 9.4 deg, surface speed 0.02 m/s, unity v 1.82, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.00 m 0.2 kN] [contact 0.04 m 4.0 kN] [contact 0.04 m 4.0 kN], frame v 7.67
    [AutoTest    15929.8] lander watch: tilt 9.1 deg, surface speed 0.08 m/s, unity v 1.83, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 2.3 kN] [contact 0.02 m 1.9 kN], frame v 7.70
    [AutoTest    15930.3] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.01 m 1.4 kN] [contact 0.03 m 2.9 kN] [contact 0.02 m 1.8 kN] [contact 0.02 m 2.5 kN], frame v 7.70
    [AutoTest    15930.8] lander watch: tilt 9.0 deg, surface speed 0.04 m/s, unity v 1.76, w 0.03, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.02 m 1.2 kN] [contact 0.02 m 1.9 kN] [contact 0.03 m 2.2 kN], frame v 7.72
    [AutoTest    15930.8] CHECK PASS: walking in low gravity — 12.0 m in 6.9 s (AGL 0.6 m, 14.8 m from the lander axis, grounded True on TerrainCollider (1, 2271, 1981), speed 1.78 m/s, jetpack 10.00 kg)
    [AutoTest    15930.8] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2271, 1981), gravity 1.60 m/s²
    [AutoTest    15931.3] lander watch: tilt 11.0 deg, surface speed 0.14 m/s, unity v 2.37, w 0.01, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 2.5 kN] [free 0.00 m 0.0 kN], frame v 7.97
    [AutoTest    15931.8] lander watch: tilt 10.5 deg, surface speed 0.17 m/s, unity v 1.98, w 0.04, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.00 m 1.0 kN] [contact 0.08 m 8.3 kN] [contact 0.03 m 3.5 kN], frame v 7.84
    [AutoTest    15932.4] lander watch: tilt 8.9 deg, surface speed 0.22 m/s, unity v 1.82, w 0.06, parts 17, legs [contact 0.00 m 2.9 kN] [contact 0.03 m 3.6 kN] [contact 0.03 m 0.9 kN] [contact 0.03 m 3.6 kN], frame v 7.80
    [AutoTest    15932.9] lander watch: tilt 8.2 deg, surface speed 0.04 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.03 m 2.9 kN] [contact 0.03 m 2.3 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 2.7 kN], frame v 7.84
    [AutoTest    15933.4] lander watch: tilt 8.9 deg, surface speed 0.12 m/s, unity v 2.27, w 0.06, parts 17, legs [contact 0.01 m 0.0 kN] [contact 0.03 m 2.8 kN] [contact 0.03 m 3.5 kN] [contact 0.03 m 3.0 kN], frame v 7.98
    [AutoTest    15933.8] CHECK PASS: jump in low gravity — apex 1.67 m
    [AutoTest    15933.9] lander watch: tilt 9.3 deg, surface speed 0.25 m/s, unity v 1.54, w 0.03, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.00 m 0.0 kN] [contact 0.02 m 0.0 kN] [contact 0.01 m 0.0 kN], frame v 8.00
    [AutoTest    15934.4] lander watch: tilt 9.1 deg, surface speed 0.05 m/s, unity v 0.70, w 0.07, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.03 m 4.2 kN] [contact 0.03 m 4.6 kN] [contact 0.03 m 3.8 kN], frame v 8.45
    [AutoTest    15935.0] lander watch: tilt 8.9 deg, surface speed 0.03 m/s, unity v 0.11, w 0.05, parts 17, legs [contact 0.00 m 0.7 kN] [contact 0.02 m 2.5 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 3.3 kN], frame v 8.79
    [AutoTest    15935.5] lander watch: tilt 8.8 deg, surface speed 0.02 m/s, unity v 0.03, w 0.05, parts 17, legs [contact 0.01 m 0.7 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.1 kN] [contact 0.03 m 2.9 kN], frame v 8.81
    [AutoTest    15935.8] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15935.8] screenshot: lunar-surface_038_eva_flag.png
    [AutoTest    15935.8] PHASE return to hatch
    [AutoTest    15935.8] screenshot: lunar-surface_040_lander_before_return.png
    [AutoTest    15936.0] lander watch: tilt 9.0 deg, surface speed 0.01 m/s, unity v 0.24, w 0.05, parts 17, legs [contact 0.00 m 0.0 kN] [contact 0.03 m 3.2 kN] [contact 0.02 m 2.8 kN] [contact 0.03 m 2.6 kN], frame v 9.00
    [AutoTest    15936.5] lander watch: tilt 9.0 deg, surface speed 0.01 m/s, unity v 1.07, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.02 m 2.8 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 3.1 kN], frame v 9.62
    [AutoTest    15937.0] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.74, w 0.05, parts 17, legs [contact 0.01 m 0.6 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 2.2 kN] [contact 0.03 m 3.1 kN], frame v 10.09
    [AutoTest    15937.6] lander watch: tilt 8.9 deg, surface speed 0.02 m/s, unity v 1.80, w 0.04, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.03 m 3.3 kN] [contact 0.02 m 2.8 kN] [contact 0.03 m 3.0 kN], frame v 10.13
    [AutoTest    15938.1] lander watch: tilt 9.0 deg, surface speed 0.00 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.00 m 0.2 kN] [contact 0.03 m 2.9 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 2.8 kN], frame v 10.14
    [AutoTest    15938.6] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.82, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.8 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.0 kN], frame v 10.14
    [AutoTest    15939.1] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.81, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.0 kN], frame v 10.13
    [AutoTest    15939.6] lander watch: tilt 8.9 deg, surface speed 0.02 m/s, unity v 1.82, w 0.04, parts 17, legs [contact 0.00 m 0.1 kN] [contact 0.03 m 2.8 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 2.7 kN], frame v 10.13
    [AutoTest    15940.2] lander watch: tilt 8.9 deg, surface speed 0.03 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.3 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.4 kN], frame v 10.12
    [AutoTest    15940.7] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.80, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.4 kN], frame v 10.13
    [AutoTest    15941.2] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 3.0 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 3.0 kN], frame v 10.09
    [AutoTest    15941.7] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.76, w 0.04, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.0 kN], frame v 10.09
    [AutoTest    15942.2] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.6 kN] [contact 0.03 m 3.0 kN], frame v 10.09
    [AutoTest    15942.8] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.77, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 2.9 kN], frame v 10.09
    [AutoTest    15943.3] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 2.9 kN], frame v 10.09
    [AutoTest    15943.8] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 2.9 kN], frame v 10.09
    [AutoTest    15944.3] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.79, w 0.04, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.3 kN] [contact 0.02 m 2.3 kN] [contact 0.03 m 3.3 kN], frame v 10.11
    [AutoTest    15944.8] lander watch: tilt 8.9 deg, surface speed 0.00 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 3.4 kN], frame v 10.11
    [AutoTest    15945.4] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.79, w 0.05, parts 17, legs [contact 0.00 m 0.3 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 2.9 kN], frame v 10.10
    [AutoTest    15945.9] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.65, w 0.05, parts 17, legs [contact 0.00 m 0.7 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.3 kN], frame v 9.97
    [AutoTest    15946.4] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.18, w 0.05, parts 17, legs [free 0.00 m 0.0 kN] [contact 0.02 m 3.1 kN] [contact 0.02 m 2.7 kN] [contact 0.02 m 2.9 kN], frame v 9.47
    [AutoTest    15946.9] lander watch: tilt 8.9 deg, surface speed 0.00 m/s, unity v 1.58, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.4 kN] [contact 0.02 m 2.9 kN], frame v 9.34
    [AutoTest    15947.5] lander watch: tilt 8.9 deg, surface speed 0.00 m/s, unity v 2.26, w 0.05, parts 17, legs [contact 0.00 m 0.5 kN] [contact 0.02 m 3.0 kN] [contact 0.02 m 2.5 kN] [contact 0.03 m 3.0 kN], frame v 9.37
    [AutoTest    15948.0] lander watch: tilt 8.9 deg, surface speed 0.02 m/s, unity v 2.66, w 0.05, parts 17, legs [contact 0.00 m 0.4 kN] [contact 0.02 m 2.9 kN] [contact 0.02 m 2.4 kN] [contact 0.03 m 2.9 kN], frame v 9.39
    [AutoTest    15948.5] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 2.05, w 0.05, parts 17, legs [contact 0.01 m 0.3 kN] [contact 0.03 m 2.9 kN] [contact 0.03 m 2.6 kN] [contact 0.03 m 2.9 kN], frame v 9.19
    [AutoTest    15949.0] lander watch: tilt 8.9 deg, surface speed 0.01 m/s, unity v 1.48, w 0.05, parts 17, legs [contact 0.01 m 0.3 kN] [contact 0.03 m 2.9 kN] [contact 0.03 m 2.6 kN] [contact 0.03 m 2.9 kN], frame v 9.04
    [AutoTest    15949.0] screenshot: lunar-surface_067_before_boarding.png
    [AutoTest    15949.0] boarding: lander AGL 4.25 m, situation Landed, legs [contact 0.01 m 0.3 kN] [contact 0.03 m 2.9 kN] [contact 0.03 m 2.6 kN] [contact 0.03 m 2.9 kN]
    [AutoTest    15949.5] lander watch: tilt 8.7 deg, surface speed 0.03 m/s, unity v 0.00, w 0.05, parts 17, legs [contact 0.01 m 1.8 kN] [contact 0.03 m 2.8 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.0 kN], frame v 8.82
    [AutoTest    15949.5] screenshot: lunar-surface_070_boarded.png
    [AutoTest    15949.5] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15949.5] PHASE lunar ascent
    [AutoTest    15955.2] PHASE lunar gravity turn
    [AutoTest    15978.4] Luma orbit circularization: dv 403.5 m/s, burn 31.5 s, in 175 s
    [AutoTest    15986.1] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16171.2] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.0 s
    [AutoTest    16171.2] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16171.2] delta-v left in Luma orbit after ascent: 930 m/s vac (930)
    [AutoTest    16171.2] PHASE plan return
    [AutoTest    16171.2] CHECK PASS: return trajectory to Tellus found — burn 280.0 m/s in 1527 s, Tellus Pe 32.0 km
    [AutoTest    16171.2] PHASE trans-Tellus injection
    [AutoTest    16171.2] TTI: dv 280.0 m/s, burn 19.7 s, in 1527 s
    [AutoTest    17710.7] TTI: burn complete, residual 0.19 m/s after 22.5 s
    [AutoTest    17710.7] delta-v left after trans-Tellus injection: 650 m/s vac (650)
    [AutoTest    17710.7] PHASE coast home
    [AutoTest    24417.3] Tellus periapsis after SOI exit 36.6 km
    [AutoTest    24417.3] CHECK PASS: return periapsis in reentry corridor — Pe 36.6 km
    [AutoTest    52492.3] PHASE reentry
    [AutoTest    52608.8] screenshot: lunar-surface_089_reentry.png
    [AutoTest    53180.2] screenshot: lunar-surface_090_touchdown.png
    [AutoTest    53180.2] CHECK PASS: capsule landed safely with crew — Splashed at 0.9 m/s, crew 1, max skin 950 K (HS-125 Ablative Heat Shield), max 8.7 g
