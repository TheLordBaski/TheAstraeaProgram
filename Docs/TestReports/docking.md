# The Astraea Program automated mission report: docking
Result: PASSED
Checks: 5/5 passed

- [x] docking vessels in orbit: Dock Chaser (7 parts, 1.25 t, crew 1) and Dock Target (3 parts, 0.72 t), ports 9.8 m apart
- [x] docking ports capture and merge the vessels: captured 23 s after the start at 0.25 m/s and 0.3° (last sample 0.59 m apart); Dock Chaser: 10 parts, 1.96 t, crew 1
- [x] docked stack holds together: 10 parts after 10 s, mass 1.96 t (sum 1.97 t), crew 1
- [x] undocking splits the stack and pushes the vessels apart: Dock Chaser (7 parts) and Dock Chaser (undocked) (3 parts) separating at 0.34 m/s
- [x] ports re-arm only after the vessels are clear of each other: ports 3.5 m apart after 9 s, states Ready / Ready

## Log
    [AutoTest        1.5] PHASE docking setup
    [AutoTest        2.5] CHECK PASS: docking vessels in orbit — Dock Chaser (7 parts, 1.25 t, crew 1) and Dock Target (3 parts, 0.72 t), ports 9.8 m apart
    [AutoTest        2.5] screenshot: docking_002_docking_start.png
    [AutoTest        2.5] PHASE docking approach
    [AutoTest        2.5] approach: 9.81 m to go, 0.04 m off-axis, closing 0.00 m/s (want 0.50), RCS (-0.17, 1.00, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest        4.6] approach: 8.88 m to go, 0.00 m off-axis, closing 0.49 m/s (want 0.50), RCS (-0.15, 0.04, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest        6.6] approach: 7.90 m to go, 0.01 m off-axis, closing 0.48 m/s (want 0.50), RCS (0.08, 0.05, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest        8.6] approach: 6.91 m to go, 0.01 m off-axis, closing 0.49 m/s (want 0.50), RCS (0.07, 0.04, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       10.6] approach: 5.94 m to go, 0.01 m off-axis, closing 0.49 m/s (want 0.50), RCS (0.07, 0.04, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       12.6] approach: 4.95 m to go, 0.01 m off-axis, closing 0.49 m/s (want 0.50), RCS (-0.15, 0.02, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       14.7] approach: 4.03 m to go, 0.01 m off-axis, closing 0.41 m/s (want 0.40), RCS (0.08, -0.04, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       16.7] approach: 3.26 m to go, 0.01 m off-axis, closing 0.34 m/s (want 0.33), RCS (0.07, -0.04, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       18.7] approach: 2.63 m to go, 0.01 m off-axis, closing 0.28 m/s (want 0.26), RCS (0.07, -0.06, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       20.7] approach: 2.03 m to go, 0.00 m off-axis, closing 0.28 m/s (want 0.20), RCS (0.15, -0.22, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       22.7] approach: 1.46 m to go, 0.00 m off-axis, closing 0.26 m/s (want 0.15), RCS (-0.04, -0.34, 0.00), spin 0.00 / 0.01 rad/s
    [AutoTest       24.8] approach: 0.92 m to go, 0.00 m off-axis, closing 0.26 m/s (want 0.15), RCS (0.03, -0.34, 0.00), spin 0.00 / 0.00 rad/s
    [AutoTest       26.0] CHECK PASS: docking ports capture and merge the vessels — captured 23 s after the start at 0.25 m/s and 0.3° (last sample 0.59 m apart); Dock Chaser: 10 parts, 1.96 t, crew 1
    [AutoTest       26.0] screenshot: docking_017_docked.png
    [AutoTest       36.0] CHECK PASS: docked stack holds together — 10 parts after 10 s, mass 1.96 t (sum 1.97 t), crew 1
    [AutoTest       36.0] PHASE undocking
    [AutoTest       37.1] CHECK PASS: undocking splits the stack and pushes the vessels apart — Dock Chaser (7 parts) and Dock Chaser (undocked) (3 parts) separating at 0.34 m/s
    [AutoTest       46.2] CHECK PASS: ports re-arm only after the vessels are clear of each other — ports 3.5 m apart after 9 s, states Ready / Ready
    [AutoTest       46.2] screenshot: docking_022_undocked.png
