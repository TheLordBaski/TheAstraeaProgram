# Walkthrough: to Luma and back with the Luma Pathfinder

This is the complete mission: build → launch → orbit → transfer → Luma orbit → landing → EVA and flag → boarding →
lunar ascent → return → reentry → parachute landing → recovery. The in-flight **mission guide** (left side of the
flight screen) tracks every step and shows a hint for the current one. Quicksave (**F5**) before each big step.

The Luma Pathfinder stages (Space presses):

| Press | Stage | What happens |
|---|---|---|
| 1 | 5 | Goliath core engine + four Kodiak solid boosters ignite, the pad releases the rocket |
| 2 | 4 | Radial decouplers drop the burnt-out boosters (~45 s) |
| 3 | 3 | Core stage drops, the transfer stage's Hornet engine ignites (~110 s, ~37 km) |
| 4 | 2 | Transfer stage drops (when empty, during the transfer burn), the lander's Ember engine ignites |
| 5 | 1 | Lander drops: only the capsule, heat shield and parachute remain (before reentry) |
| 6 | 0 | Parachute armed: it opens when the air is thick enough and fully at 1 km above ground |

## 1. Build

1. Main menu → **New sandbox** → **Start**.
2. In the assembly building press **Load** → *Luma Pathfinder* → **Load** (or build your own: parts on the left,
   click to pick up, click a green node or a surface to attach; see `CONTROLS.md`).
3. Check the engineer's report on the right: launch TWR ≈ 2.0, ≈ 7,640 m/s vacuum Δv, no red warnings.
   The transfer stage (≈ 1,560 m/s) finishes the orbit and starts the transfer to Luma; the squat 2.5 m lander
   (≈ 3,480 m/s, legs spread 5 m) completes it and does the capture, the landing, the lunar ascent and the burn home.
   Its low, wide stance keeps it standing on slopes up to ~25°.
   Switch the readout to **Luma surface** to see the lander's local TWR.
4. Press **LAUNCH**.

## 2. Launch and orbit

1. **T** turns SAS on, **Z** sets full throttle, **Space** lifts off.
2. At about 60 m/s tap **D** a few times to tip ~10° east (the navball heading reads 090°), then press **2**
   (SAS prograde). The rocket now follows its own velocity vector in a gravity turn.
3. When the boosters flame out press **Space** (drop boosters); when the core is empty press **Space** again.
4. Watch **Ap** in the orbit panel (top right). At ~80 km press **X** (cut throttle). Above ~38 km click the speed display
   above the navball to switch it to **ORBIT** so prograde follows the orbital velocity.
5. Coast out of the atmosphere (70 km). Press **M** for the map, click your orbit near the apoapsis marker and choose
   **Add maneuver node**, then drag the yellow prograde handle (or press + in the node panel) until **Pe** is above 70 km.
6. Press **0** (SAS → manoeuvre), **Warp to burn** in the node panel, and start the burn when the time to burn reaches
   zero (it begins half the burn time before the node). Cut the throttle when the remaining Δv reaches 0.
   *Mission guide: "Achieve a stable orbit" ✔.*

## 3. Transfer to Luma

1. In the map click **Luma** → **Set as target**.
2. Click your orbit, choose **Add maneuver node** and give it ≈ **860 m/s prograde**. Move the node along the orbit (drag
   the node marker, or use the ±time buttons) until the trajectory turns into a **Luma encounter** (a green patch inside Luma's sphere of
   influence with an encounter periapsis marker). Aim for a Luma periapsis of 20–50 km.
3. Execute the burn as before. Time warp (**.**) to Luma — warp automatically drops out before the sphere of influence
   change and before the periapsis.

## 4. Luma orbit

1. Near the Luma periapsis add a node there and drag **retrograde** (≈ 250–320 m/s) until the orbit closes
   around Luma (apoapsis and periapsis both shown). Execute it.
2. If the transfer stage runs dry during a burn, press **Space**: it drops and the lander engine takes over.

## 5. Landing

1. Press **Space** if the transfer stage is still attached, then **G** to deploy the landing legs.
2. Burn **retrograde** until the periapsis is below the surface.
3. Click the speed display to **SURFACE** and select SAS retrograde (**3**). As the ground approaches, throttle up so the
   vertical speed drops; touch down below ~3 m/s (legs break above 12 m/s). The radar altitude is shown above the
   navball together with the vertical speed.
4. Pick level ground: Luma's crater walls are steep. Hover 30–50 m up (throttle ≈ 25%), tilt gently with **W A S D**
   to slide over flat terrain, then settle straight down. The wide lander stands on slopes up to ~25°.
   *Mission guide: "Land on Luma" ✔.*

## 6. EVA, flag, boarding

1. Right-click the capsule → **EVA: Ada Kestrel** (or whoever is aboard). The rocketeer climbs out beside the hatch,
   slides down the lander's tapered tank and drops to the ground (slow in Luma's gravity; the suit tolerates 12 m/s).
2. **W A S D** walk in low gravity (0.16 g), **Space** jumps, **R** toggles the jetpack (**Shift**/**Ctrl** up/down).
3. **G** plants a flag (a permanent flag vessel, visible in the map).
4. There is no ladder: walk back under the hatch, switch the jetpack on (**R**), rise to the capsule (hold **Shift**,
   ease off near the top) and press **B** within 3 m of the hatch to board. A fresh jetpack holds ~100 m/s.

## 7. Lunar ascent and the way home

1. **G** retracts the legs. Full throttle (**Z**), tip east with **W** after lifting clear, then SAS prograde.
2. Cut the throttle when the Luma apoapsis reaches ~20 km and circularise with a node at the apoapsis.
3. Add a node about a quarter orbit *before* your orbit's point that faces Luma's direction of travel and drag
   prograde (≈ 250–300 m/s) until the predicted path leaves Luma's sphere of influence and shows a **Tellus periapsis
   of 25–40 km**. Execute it and time warp home.
4. If needed, make a small correction burn after leaving Luma's sphere of influence to put the Tellus periapsis back into
   25–40 km.

## 8. Reentry and landing

1. Before reaching 70 km press **Space** to drop the lander (throttle at zero: a stage dropped while its engine is still
   winding down can push itself into the capsule). Select SAS retrograde (surface) so the **heat shield** faces the
   airflow (the capsule glows; the shield ablates — watch the ablator bar).
2. After the heating peak (below ~30 km, speed falling) watch the parachute's bar in the staging stack: it turns
   **green** when deployment is safe (amber = risky, red = would tear or burn; hover it for the reason). Then press
   **Space** to arm it. It semi-deploys at safe pressure and opens fully 1 km above the ground. Deploying at high speed
   tears it.
3. Touch down or splash down below ~6 m/s. **Esc → Recover vessel** returns the crew to the roster.
   *Mission guide: "Mission complete — welcome home!"*

## Automated version

The same mission can be flown by the built-in scripted pilot, which uses only player controls
(throttle, attitude keys, SAS modes, staging, manoeuvre nodes, time warp, EVA inputs):

```
TAP.exe -autotest lunar -report lunar_report.md
```

The report lists every check (orbit reached, encounter found, capture, landing tilt and leg damage, EVA walk/jump,
flag, boarding, ascent orbit, return periapsis, reentry temperatures, touchdown speed) with measured values.
