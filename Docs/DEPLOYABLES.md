# Deployables

PRT-03 (2026-10-16). One module, `DeployableModule`, moves everything that unfolds, extends or opens: solar panels, antennas, radiators, bay doors,
ladders, airbrakes, drills, scanners and the shielded docking port. A part gets it by having a `deployable` block in `parts.json`; there is no
code per part. The state machine is `DeployableLogic` (`Parts/Deployable.cs`), the module only reads the vessel and moves the model.

## States

`Retracted` → `Extending` → `Extended` → `Retracting` → `Retracted`, and `Broken`. A command in mid-motion reverses it. `Broken` is final:
the moving pieces are hidden (torn off), the part's body stays.

## The data

```json
"deployable": {
  "extendLabel": "Deploy panel", "retractLabel": "Retract panel",
  "extendTime": 6, "retractTime": 5,
  "breakPressure": 15000, "breakImpact": 6,
  "motions": [
    { "transform": "WingA", "kind": "rotate", "to": [0, 0, -90], "curve": "smooth", "start": 0.0, "end": 0.6 },
    { "transform": "WingB", "kind": "rotate", "to": [0, 0, 90],  "curve": "smooth", "start": 0.4, "end": 1.0 }
  ]
}
```

| Field | Meaning |
|---|---|
| `extendLabel`, `retractLabel` | The part menu's buttons ("Open bay", "Close bay"…) |
| `extendTime`, `retractTime` | Seconds each way. `retractTime` 0 means it can't be taken in again (an inflatable shield) |
| `startState` | `retracted` (default) or `extended` (a part that starts out) |
| `breakPressure` | Dynamic pressure in Pa above which the part tears off while it is out or moving. 0 never. Panels 6 to 15 kPa, dishes 8 to 20, bay doors 30, an airbrake 80 |
| `breakImpact` | The impact speed in m/s the part survives while out or moving (a part menu shows its own tolerance when folded). 0 keeps the part's own |
| `ecPerSecond` | Electric charge per second while it moves; with no charge it stalls |
| `motions` | The transforms of the model it drives |

A **motion** moves one transform of the part's model, found by name or path under `Model` ("WingA", "Boom/Dish"), from its rest pose:

- `kind`: `rotate` (euler degrees added to the rest rotation), `translate` (metres added to the rest position) or `scale` (factors times the rest scale; `from` defaults to 1, 1, 1).
- `from`, `to`: the value at the start and at the end (`from` defaults to no change).
- `curve`: `linear`, `smooth` (default), `easeIn`, `easeOut` or `overshoot`; or `keys`, a list of `[progress, value]` pairs of the blend from 0 to 1 that replaces it.
- `start`, `end`: the part of the deployment, 0 to 1, in which it moves. Staggered windows fold a panel in stages.

The model's **rest pose is the retracted one.** A hand-made model ([PART_MODELS.md](PART_MODELS.md)) works if it keeps the transform names of the part's
motions; a model without them still keeps the state, the actions and the limits, it just doesn't move. The procedural models of the catalogue's
panels and antennas (PRT-02) provide the names listed in `parts.json`.

## What it connects to

- **The part menu:** `CollectActions` offers the extend button when it can extend and the retract button when it can retract.
- **Action groups (FLT-07):** the calls are `Extend()`, `Retract()` and `Toggle()` on the module.
- **Saves:** the state and the progress are saved with the vessel (`modules["Deployable"]`).
- **Impacts:** `Part.ImpactTolerance` takes `breakImpact` while the part is out.
- **What the part does while out** (power from a panel, signal from a dish, drag from an airbrake, mining by a drill) is the business of those systems (FLT-02,
  FLT-04, FLT-17, RES-03); they read `DeployableModule.State` and `Progress`.

Tests: `DeployableTests` (the state machine, curves, a panel, an antenna, a bay door and a ladder on model hierarchies, the data checks) and the `deploy` autotest
mission (a real vessel: unfold in orbit, save, tear off at 24 kPa).
