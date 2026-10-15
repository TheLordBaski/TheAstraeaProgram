# Resources, propellant mixes and tanks

Decision D-01 (2026-10-15), built in FLT-01. Everything is data in `Assets/TAP/Resources/Data/parts.json`: a new resource,
a new mix, or a new tank or engine needs no code.

## Resources

```json
{ "id": "Hydrogen", "name": "Hydrogen", "unit": "L", "density": 0.07, "flow": "Stack", "color": [0.78, 0.9, 1.0] }
```

| Field | Meaning |
|---|---|
| `density` | Mass in kg of one **unit**. A unit is a volume unit: a tank holds a number of units, and its mass is units × density. A hydrogen tank holds fewer kg than a fuel tank of the same part. It is not the size of the model. Electric charge has density 0 |
| `flow` | `Part` (only from the part itself: solid fuel, ablator), `Stack` (from every tank reachable without crossing a decoupler) or `Vessel` (from anywhere: monopropellant, xenon, ore, electric charge) |
| `price` | Funds per unit when a launch draws it from the site's stock (career, CAR-02). 0 is free |
| `boilOffPerDay` | A hook for cryogenic boil-off. Nothing reads it yet |
| `unit`, `name`, `color` | What the player sees |

The shipped resources are Fuel, Oxidiser, Hydrogen, Xenon, Ore (all in L), Solid propellant, Monopropellant, Electric charge and
Ablator.

## Propellant mixes

A **mix** is a named recipe of resources by *volume* ratio:

```json
"mixes": [
  { "id": "chemical", "name": "Liquid Propellant", "color": [0.95, 0.72, 0.25],
    "components": [ { "resource": "Fuel", "ratio": 14 }, { "resource": "Oxidiser", "ratio": 19 } ] }
]
```

- **A part carries a mix** by naming it among its resources: `"resources": [ { "id": "chemical", "amount": 500 } ]` holds 500 units
  of the mix, which the catalogue check splits by the ratio into Fuel and Oxidiser (212.1 and 287.9 units). The part menu lists the
  two; the flight display shows one bar, in kg.
- **An engine burns a mix** by naming it: `"propellant": "chemical"`. It draws the components together at the mix's *mass* ratio
  (the volume ratio times the densities). It flames out when the scarcest component is gone.
- **A single resource is a propellant too**: `"propellant": "Hydrogen"` burns hydrogen only.
- The chemical mix weighs 1 kg per unit (14 × 0.81 + 19 × 1.14 = 33 kg for 33 units), so a tank's number of units is its propellant
  mass in kg.
- A mix needs resources with a density above 0 and ratios above 0. Mistakes are reported like any other ([CONTENT_VALIDATION.md](CONTENT_VALIDATION.md)).

## The launch site supplies the tanks

The player never picks a propellant. A launch fills the vessel's tanks from the site's stock (`LaunchSupply`):

- The stock is unlimited for now: free in sandbox and science mode; in career the cost is the resource's `price` (`LaunchSupply.Price`),
  to be charged by CAR-02. Bases elsewhere will hold what they make or receive.
- A craft part's setting `fill` (0 to 1, default full) launches its tanks part-filled. The assembly building will expose it (ASM-02).
- Supplied: liquid and vessel-wide resources. Not supplied: solid fuel and ablator (a motor is sold full) and electric charge.

## Saves

The save format did not change: a vessel record lists each part's resources by id and amount, so tanks save as Fuel and Oxidiser.
Saves made before D-01 hold the old `LiquidFuel`, which no longer exists, so their tanks load empty. They are not migrated
(there were no saves worth keeping before Early Access).
