# Controls

The same list is available in game: **F1** in flight, **Help** in the assembly building, **Controls** in the main menu.
Most buttons and readouts show a tooltip when hovered.

## Flight

| Key | Action |
|---|---|
| **W / S** | Pitch down / up (on the pad: north / south) |
| **A / D** | Yaw left / right (on the pad, D tips the rocket east, as in KSP) |
| **Q / E** | Roll left / right |
| **Shift / Ctrl** | Throttle up / down |
| **Z / X** | Full throttle / cut throttle |
| **Space** | Activate the next stage (the first stage ignites the engines and releases the pad) |
| **T** | Toggle SAS (stability assist) |
| **F** (hold) | Temporarily invert SAS |
| **1 – 0** | SAS mode: hold, prograde, retrograde, normal, anti-normal, radial out, radial in, target, anti-target, manoeuvre |
| **R** | Toggle RCS |
| **H / N, I / K, J / L** | RCS translation: forward/back, up/down, left/right |
| **G** | Deploy / retract landing legs |
| **M** | Map view |
| **, / .** | Time warp slower / faster (on rails: 5×…100,000×, limited by altitude) |
| **Alt + .** | Physics warp (2×, 3×, 4×) — the simulation keeps running |
| **/** | Stop time warp |
| **V** | Camera mode (orbit / chase / locked) |
| **Right mouse drag, scroll** | Rotate and zoom the camera (Page Up / Page Down also zoom; zoom speed in the developer tools) |
| **[ / ]** | Switch to the previous / next nearby vessel |
| **F5 / F9** | Quicksave / quickload (hold F9) |
| **Esc** | Pause menu (resume, quicksave, quickload, revert to launch, revert to assembly, recover, leave to assembly / main menu) |
| **F1 / F2** | Control guide / hide the interface |
| **Alt + F12** or **`** | Developer tools (also in the pause menu): cheats (infinite propellant / electricity, no crash damage, ignore heat, unbreakable joints), refill tanks, teleport to a circular orbit or land at a latitude/longitude on Tellus or Luma, mouse-wheel zoom speed. Drag the window by its title |
| **Right-click a part** | Part actions: EVA, deploy / cut parachute, activate / shut down engine, decouple, legs, enable / disable reaction wheel or RCS block, undock |

**Navball**: blue sky, brown ground, headings clockwise from north (facing east with the sky up, 45° is on the left and
135° on the right). **Markers**: yellow = prograde/retrograde, magenta = normal/anti-normal, cyan = radial, blue = manoeuvre,
pink = target. The speed display above the navball cycles **Surface / Orbit / Target** when clicked; SAS prograde and
retrograde follow the selected speed mode.

## EVA (rocketeer outside the vessel)

| Key | Action |
|---|---|
| **W A S D** | Walk (camera relative); with the jetpack on: translate |
| **Shift** | Run (jetpack: up) |
| **Ctrl** | Jetpack: down |
| **Space** | Jump |
| **R** | Jetpack on / off (up to 10 kg of EVA propellant taken from the vessel's monopropellant and returned on boarding: about 100 m/s, a minute of hovering on Luma; too weak to lift off on Tellus) |
| **B** | Board a hatch within 3 m while moving slower than 2.5 m/s relative to it |
| **G** | Plant a flag (must stand on the ground) |

## Map view

| Input | Action |
|---|---|
| **M** | Back to the flight view |
| **Right drag / scroll** | Orbit / zoom the map camera (down to just above the focused body, or close to the focused vessel) |
| **Tab / Backspace** | Cycle focus between bodies and vessels / focus the active vessel |
| **Click the orbit line** | Pop-up for that point of the orbit: **Add maneuver node** or **Warp here** |
| **Drag a node** (its marker) | Move the node along the orbit (a later node moves along the planned path between its neighbours) |
| **Drag node handles** | Prograde / retrograde, normal / anti-normal, radial in / out Δv (the further you pull, the faster it changes) |
| **Right-click a node, Del, or its × button** | Delete the node |
| **Node panel** | Type or step (0.1 / 1 / 10 / 100 m/s) each Δv component; move the node ±10 s, ±1 min, ±10 min or ±1 orbit; delete; warp to burn; SAS → node; burn info (Δv, burn time, time to burn) |
| **Click a body or vessel** | Focus it, or **Set as target** (closest approach and relative speed are shown) |

## Assembly building (vehicle editor)

| Input | Action |
|---|---|
| **Click the parts list** | Pick up a new part (categories + search box; hover for full part stats) |
| **Left click** | Attach the held part / pick up a placed part together with everything attached to it; clicking the root part picks up the whole craft |
| **Click empty space** (holding parts) | Set them aside: they stay there greyed out, are not part of the craft (not launched, not counted in the readouts) and can be built on, picked up and attached again |
| **Alt + click** | Copy a placed part (with its attached parts) |
| **W A S D Q E** | Rotate the held part (90° steps; Shift for 5° steps); **Space** resets the rotation |
| **X / Shift + X** | Radial symmetry up / down (1, 2, 3, 4, 6, 8) |
| **C** | Angle snap on / off (15° surface positions, 90° rotation steps) |
| **Del** | Delete the hovered part (and what is attached to it), or the held parts |
| **Esc** | Put picked-up parts back where they were (a new part from the list is dropped) |
| **Click the parts list** (holding parts) | Delete the held parts |
| **Right click a part** | Part details and settings (engine / booster thrust limiter), **Make root part** (re-root the craft on a stack-attached part), delete |
| **Ctrl + Z / Ctrl + Y** | Undo / redo |
| **Ctrl + S** | Save the craft |
| **Right drag / scroll** | Orbit / zoom the camera; **Shift + scroll** or middle drag moves up / down; arrow keys also work |
| **F** | Frame the craft |
| **V** | Show / hide the centre of mass (yellow), thrust (purple) and pressure (cyan) markers |
| **Staging panel** | Drag stage icons between stages, or onto the gap between two stages to create a new one; **Reset** restores automatic staging |
| **F1** | Help |
