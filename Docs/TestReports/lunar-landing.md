# The Astraea Program automated mission report: lunar-landing
Result: PASSED
Checks: 12/12 passed

- [x] resumed in Luma orbit: Luma Pathfinder: Orbiting at Luma, delta-v 2410 m/s vac (2410)
- [x] landed on Luma: situation Landed, tilt 1.0 deg, legs intact 4/4, crew 1
- [x] EVA started: Ada Kestrel outside, inherited velocity 0.40 m/s rel. lander
- [x] walking in low gravity: 12.0 m in 7.0 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2308, 1990), speed 1.83 m/s, jetpack 10.00 kg)
- [x] EVA on the surface: standing on TerrainCollider (1, 2308, 1990), gravity 1.60 m/s²
- [x] jump in low gravity: apex 1.54 m
- [x] flag planted on Luma: flags: 1
- [x] crew boarded the lander: Ada Kestrel back aboard
- [x] lunar orbit after ascent: Pe 22.3 km
- [x] return trajectory to Tellus found: burn 368.3 m/s in 1168 s, Tellus Pe 32.0 km
- [x] return periapsis in reentry corridor: Pe 31.0 km
- [x] capsule landed safely with crew: Landed at 0.0 m/s, crew 1, max skin 1007 K (HS-125 Ablative Heat Shield), max 7.8 g

## Log
    [AutoTest    15004.6] PHASE resume in Luma orbit
    [AutoTest    15006.6] CHECK PASS: resumed in Luma orbit — Luma Pathfinder: Orbiting at Luma, delta-v 2410 m/s vac (2410)
    [AutoTest    15006.6] PHASE deorbit
    [AutoTest    15009.9] PHASE descent
    [AutoTest    15727.6] suicide burn start at 12326 m AGL, 589 m/s
    [AutoTest    15890.8] final descent from 150 m AGL at 43.0 m/s (vertical -35.1 m/s)
    [AutoTest    15891.4] landing site: 1.0° slope 12 m away (ground below: 2.9°)
    [AutoTest    15938.9] screenshot: lunar-landing_007_luma_landed.png
    [AutoTest    15938.9] CHECK PASS: landed on Luma — situation Landed, tilt 1.0 deg, legs intact 4/4, crew 1
    [AutoTest    15938.9] delta-v left on the surface: 1626 m/s vac (1626)
    [AutoTest    15938.9] snapshot (landed on Luma) saved: autotest_luma_landed.json
    [AutoTest    15938.9] PHASE EVA
    [AutoTest    15938.9] CHECK PASS: EVA started — Ada Kestrel outside, inherited velocity 0.40 m/s rel. lander
    [AutoTest    15938.9] lander watch: tilt 1.0 deg, surface speed 0.00 m/s, unity v 0.40, w 0.01, parts 17, legs [contact 0.04 m 3.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 8.46
    [AutoTest    15939.5] lander watch: tilt 1.2 deg, surface speed 0.03 m/s, unity v 0.95, w 0.01, parts 17, legs [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.51
    [AutoTest    15940.0] lander watch: tilt 1.3 deg, surface speed 0.02 m/s, unity v 1.73, w 0.01, parts 17, legs [contact 0.05 m 4.8 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 8.63
    [AutoTest    15940.5] lander watch: tilt 1.4 deg, surface speed 0.04 m/s, unity v 2.55, w 0.01, parts 17, legs [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 8.83
    [AutoTest    15941.0] lander watch: tilt 1.7 deg, surface speed 0.05 m/s, unity v 3.37, w 0.01, parts 17, legs [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 9.11
    [AutoTest    15941.5] lander watch: tilt 2.1 deg, surface speed 0.07 m/s, unity v 4.20, w 0.02, parts 17, legs [contact 0.05 m 4.6 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 9.44
    [AutoTest    15942.1] lander watch: tilt 2.6 deg, surface speed 0.30 m/s, unity v 0.69, w 0.02, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 8.10
    [AutoTest    15942.6] lander watch: tilt 3.1 deg, surface speed 0.28 m/s, unity v 1.50, w 0.03, parts 17, legs [contact 0.03 m 8.3 kN] [free 0.00 m 0.0 kN] [contact 0.03 m 7.7 kN] [free 0.00 m 0.0 kN], frame v 7.44
    [AutoTest    15943.1] lander watch: tilt 3.9 deg, surface speed 0.09 m/s, unity v 1.72, w 0.04, parts 17, legs [contact 0.03 m 2.7 kN] [contact 0.01 m 1.6 kN] [contact 0.03 m 3.0 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15943.6] lander watch: tilt 4.2 deg, surface speed 0.01 m/s, unity v 1.78, w 0.05, parts 17, legs [contact 0.03 m 2.7 kN] [contact 0.02 m 2.7 kN] [contact 0.03 m 3.2 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15944.1] lander watch: tilt 4.0 deg, surface speed 0.06 m/s, unity v 1.83, w 0.04, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.02 m 1.7 kN] [contact 0.03 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 7.29
    [AutoTest    15944.7] lander watch: tilt 3.6 deg, surface speed 0.06 m/s, unity v 1.84, w 0.02, parts 17, legs [contact 0.04 m 4.4 kN] [contact 0.01 m 0.3 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15945.2] lander watch: tilt 3.3 deg, surface speed 0.02 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15945.7] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15946.2] lander watch: tilt 3.4 deg, surface speed 0.04 m/s, unity v 1.79, w 0.01, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.00 m 0.0 kN] [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15946.7] lander watch: tilt 3.7 deg, surface speed 0.04 m/s, unity v 1.80, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15947.3] lander watch: tilt 3.9 deg, surface speed 0.01 m/s, unity v 1.83, w 0.04, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.02 m 1.7 kN] [contact 0.03 m 3.6 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15947.8] lander watch: tilt 3.8 deg, surface speed 0.03 m/s, unity v 1.85, w 0.03, parts 17, legs [contact 0.03 m 3.7 kN] [contact 0.01 m 1.2 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15948.3] lander watch: tilt 3.6 deg, surface speed 0.03 m/s, unity v 1.85, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15948.8] lander watch: tilt 3.5 deg, surface speed 0.01 m/s, unity v 1.84, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 7.28
    [AutoTest    15948.9] CHECK PASS: walking in low gravity — 12.0 m in 7.0 s (AGL 0.6 m, 14.4 m from the lander axis, grounded True on TerrainCollider (1, 2308, 1990), speed 1.83 m/s, jetpack 10.00 kg)
    [AutoTest    15948.9] CHECK PASS: EVA on the surface — standing on TerrainCollider (1, 2308, 1990), gravity 1.60 m/s²
    [AutoTest    15949.3] lander watch: tilt 2.1 deg, surface speed 0.22 m/s, unity v 2.41, w 0.07, parts 17, legs [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN] [free 0.00 m 0.0 kN], frame v 7.54
    [AutoTest    15949.9] lander watch: tilt 1.8 deg, surface speed 0.05 m/s, unity v 1.83, w 0.01, parts 17, legs [contact 0.06 m 5.3 kN] [free 0.00 m 0.0 kN] [contact 0.05 m 4.5 kN] [free 0.00 m 0.0 kN], frame v 7.42
    [AutoTest    15950.4] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.68, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 7.39
    [AutoTest    15950.9] lander watch: tilt 1.7 deg, surface speed 0.01 m/s, unity v 1.94, w 0.00, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN], frame v 7.45
    [AutoTest    15951.4] lander watch: tilt 1.8 deg, surface speed 0.03 m/s, unity v 2.46, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 7.61
    [AutoTest    15951.6] CHECK PASS: jump in low gravity — apex 1.54 m
    [AutoTest    15951.9] lander watch: tilt 2.1 deg, surface speed 0.09 m/s, unity v 0.78, w 0.01, parts 17, legs [contact 0.02 m 3.6 kN] [free 0.00 m 0.0 kN] [contact 0.02 m 3.4 kN] [free 0.00 m 0.0 kN], frame v 8.13
    [AutoTest    15952.5] lander watch: tilt 2.5 deg, surface speed 0.07 m/s, unity v 0.05, w 0.02, parts 17, legs [contact 0.05 m 4.9 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 8.78
    [AutoTest    15953.0] lander watch: tilt 3.1 deg, surface speed 0.10 m/s, unity v 0.12, w 0.03, parts 17, legs [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 8.83
    [AutoTest    15953.5] lander watch: tilt 3.9 deg, surface speed 0.10 m/s, unity v 0.11, w 0.05, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 2.1 kN] [contact 0.03 m 2.9 kN] [free 0.00 m 0.0 kN], frame v 8.83
    [AutoTest    15953.6] CHECK PASS: flag planted on Luma — flags: 1
    [AutoTest    15953.6] screenshot: lunar-landing_046_eva_flag.png
    [AutoTest    15953.6] PHASE return to hatch
    [AutoTest    15953.6] screenshot: lunar-landing_048_lander_before_return.png
    [AutoTest    15954.0] lander watch: tilt 4.3 deg, surface speed 0.02 m/s, unity v 0.80, w 0.05, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.03 m 2.9 kN] [contact 0.03 m 2.3 kN] [free 0.00 m 0.0 kN], frame v 9.52
    [AutoTest    15954.5] lander watch: tilt 4.1 deg, surface speed 0.06 m/s, unity v 1.66, w 0.04, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.02 m 1.7 kN] [contact 0.03 m 2.9 kN] [free 0.00 m 0.0 kN], frame v 10.33
    [AutoTest    15955.1] lander watch: tilt 3.6 deg, surface speed 0.06 m/s, unity v 1.78, w 0.02, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.1 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 10.43
    [AutoTest    15955.6] lander watch: tilt 3.3 deg, surface speed 0.02 m/s, unity v 1.80, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.3 kN] [free 0.00 m 0.0 kN], frame v 10.44
    [AutoTest    15956.1] lander watch: tilt 3.2 deg, surface speed 0.01 m/s, unity v 1.81, w 0.00, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.43
    [AutoTest    15956.6] lander watch: tilt 3.4 deg, surface speed 0.05 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.04 m 4.4 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.41
    [AutoTest    15957.1] lander watch: tilt 3.8 deg, surface speed 0.04 m/s, unity v 1.80, w 0.03, parts 17, legs [contact 0.03 m 3.5 kN] [contact 0.01 m 1.4 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 10.38
    [AutoTest    15957.7] lander watch: tilt 4.0 deg, surface speed 0.01 m/s, unity v 1.77, w 0.04, parts 17, legs [contact 0.03 m 3.3 kN] [contact 0.02 m 1.8 kN] [contact 0.03 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 10.38
    [AutoTest    15958.2] lander watch: tilt 3.9 deg, surface speed 0.03 m/s, unity v 1.75, w 0.03, parts 17, legs [contact 0.03 m 3.6 kN] [contact 0.01 m 1.3 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 10.37
    [AutoTest    15958.7] lander watch: tilt 3.7 deg, surface speed 0.03 m/s, unity v 1.75, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.01 m 0.6 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 10.37
    [AutoTest    15959.2] lander watch: tilt 3.5 deg, surface speed 0.01 m/s, unity v 1.76, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.00 m 0.3 kN] [contact 0.04 m 4.2 kN] [free 0.00 m 0.0 kN], frame v 10.38
    [AutoTest    15959.7] lander watch: tilt 3.6 deg, surface speed 0.02 m/s, unity v 1.78, w 0.01, parts 17, legs [contact 0.04 m 4.1 kN] [contact 0.00 m 0.5 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.38
    [AutoTest    15960.3] lander watch: tilt 3.7 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 3.8 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 10.40
    [AutoTest    15960.8] lander watch: tilt 3.8 deg, surface speed 0.01 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.03 m 3.9 kN] [contact 0.01 m 1.5 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 10.40
    [AutoTest    15961.3] lander watch: tilt 3.8 deg, surface speed 0.02 m/s, unity v 1.79, w 0.02, parts 17, legs [contact 0.04 m 3.5 kN] [contact 0.01 m 0.9 kN] [contact 0.03 m 3.5 kN] [free 0.00 m 0.0 kN], frame v 10.41
    [AutoTest    15961.8] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 1.80, w 0.02, parts 17, legs [contact 0.04 m 3.9 kN] [contact 0.01 m 0.8 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 10.43
    [AutoTest    15962.3] lander watch: tilt 3.6 deg, surface speed 0.01 m/s, unity v 1.81, w 0.01, parts 17, legs [contact 0.04 m 4.3 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 10.43
    [AutoTest    15962.9] lander watch: tilt 3.6 deg, surface speed 0.01 m/s, unity v 1.82, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 10.44
    [AutoTest    15963.4] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 1.13, w 0.02, parts 17, legs [contact 0.03 m 3.8 kN] [contact 0.00 m 0.8 kN] [contact 0.03 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 9.73
    [AutoTest    15963.9] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 1.36, w 0.02, parts 17, legs [contact 0.03 m 3.7 kN] [contact 0.00 m 1.0 kN] [contact 0.03 m 3.9 kN] [free 0.00 m 0.0 kN], frame v 9.46
    [AutoTest    15964.4] lander watch: tilt 3.7 deg, surface speed 0.00 m/s, unity v 2.01, w 0.02, parts 17, legs [contact 0.03 m 3.8 kN] [contact 0.01 m 1.0 kN] [contact 0.03 m 3.8 kN] [free 0.00 m 0.0 kN], frame v 9.43
    [AutoTest    15964.9] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 2.65, w 0.01, parts 17, legs [contact 0.03 m 3.7 kN] [contact 0.00 m 0.7 kN] [contact 0.03 m 3.7 kN] [free 0.00 m 0.0 kN], frame v 9.49
    [AutoTest    15965.5] lander watch: tilt 3.7 deg, surface speed 0.01 m/s, unity v 2.18, w 0.02, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.8 kN] [contact 0.04 m 4.1 kN] [free 0.00 m 0.0 kN], frame v 9.30
    [AutoTest    15966.0] lander watch: tilt 3.6 deg, surface speed 0.00 m/s, unity v 1.58, w 0.01, parts 17, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN], frame v 9.12
    [AutoTest    15966.1] screenshot: lunar-landing_073_before_boarding.png
    [AutoTest    15966.1] boarding: lander AGL 4.25 m, situation Landed, legs [contact 0.04 m 4.0 kN] [contact 0.01 m 0.7 kN] [contact 0.04 m 4.0 kN] [free 0.00 m 0.0 kN]
    [AutoTest    15966.5] lander watch: tilt 3.1 deg, surface speed 0.15 m/s, unity v 0.00, w 0.03, parts 17, legs [contact 0.04 m 3.7 kN] [free 0.00 m 0.0 kN] [contact 0.04 m 4.8 kN] [free 0.00 m 0.0 kN], frame v 8.95
    [AutoTest    15966.6] screenshot: lunar-landing_076_boarded.png
    [AutoTest    15966.6] CHECK PASS: crew boarded the lander — Ada Kestrel back aboard
    [AutoTest    15966.6] PHASE lunar ascent
    [AutoTest    15972.3] PHASE lunar gravity turn
    [AutoTest    15994.8] Luma orbit circularization: dv 410.6 m/s, burn 31.7 s, in 174 s
    [AutoTest    16006.5] Luma orbit circularization warp: rails warp unavailable (Altitude too low for 5x), continuing at physics speed
    [AutoTest    16187.0] Luma orbit circularization: burn complete, residual 0.29 m/s after 34.2 s
    [AutoTest    16187.0] CHECK PASS: lunar orbit after ascent — Pe 22.3 km
    [AutoTest    16187.0] delta-v left in Luma orbit after ascent: 888 m/s vac (888)
    [AutoTest    16187.0] PHASE plan return
    [AutoTest    16187.0] CHECK PASS: return trajectory to Tellus found — burn 368.3 m/s in 1168 s, Tellus Pe 32.0 km
    [AutoTest    16187.0] PHASE trans-Tellus injection
    [AutoTest    16187.0] TTI: dv 368.3 m/s, burn 25.3 s, in 1168 s
    [AutoTest    17370.3] TTI: burn complete, residual 0.20 m/s after 28.1 s
    [AutoTest    17370.3] delta-v left after trans-Tellus injection: 520 m/s vac (520)
    [AutoTest    17370.3] PHASE coast home
    [AutoTest    22258.4] Tellus periapsis after SOI exit 31.0 km
    [AutoTest    22258.4] CHECK PASS: return periapsis in reentry corridor — Pe 31.0 km
    [AutoTest    34006.6] PHASE reentry
    [AutoTest    34116.0] screenshot: lunar-landing_095_reentry.png
    [AutoTest    34586.8] screenshot: lunar-landing_096_touchdown.png
    [AutoTest    34586.8] CHECK PASS: capsule landed safely with crew — Landed at 0.0 m/s, crew 1, max skin 1007 K (HS-125 Ablative Heat Shield), max 7.8 g
