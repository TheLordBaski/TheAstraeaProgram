# The part catalogue

PRT-01 (2026-10-16). `Assets/TAP/Resources/Data/parts.json` holds every part of the plan, 168 in all
([product/plan/05-parts.md](../product/plan/05-parts.md)). `Data/part_templates.json` holds the conventions they follow, and
`PartTemplates.Check` measures every part against them (`PartCatalogueTests`). The rocketeer and the flag are two more entries that are
never in the parts list.

## Releases

Each part has a `milestone`: `M0` is the vertical slice (36 parts), then `EA1` to `EA4` add 40, 63, 9 and 20.

`part_templates.json` says which milestone is out (`"release"`, now `M0`). A part of a later milestone is **hidden**: it is not in the
parts list, `PartDatabase.Get` doesn't find it and a craft that names it says "arrives with EA1". Parts of later milestones have
placeholder models (the nearest existing shape) and, where the system isn't built yet, no module (solar panels, antennas, wheels, drills…).
Raising `release` shows the next set. PRT-12a to PRT-12c do that after each batch has models, modules, icons and a test that flies it.
`PartDatabase.ShowUnreleased = true` shows everything (tools and tests).

## Conventions

**Ids** are lower case with a prefix by kind: `pod_`, `probe_`, `tank_`, `adapter_`, `coupler_`, `mono_`, `hydro_`, `xenon_`, `ore_`, `eng_`, `srb_`,
`dec_`, `sep_`, `dock_`, `nose_`, `fairing_`, `rw_`, `rcs_`, `battery_`, `solar_`, `antenna_`, `chute_`, `shield_`, `leg_`, `fin_`, `sci_`, `wheel_`…
(a role's `idPrefixes`). A size follows where there is one: `tank_s1_long`.

**Size classes** (`size`): `S0` 0.625 m, `S1` 1.25 m, `S2` 2.5 m, `S3` 3.75 m (attach-node sizes 0 to 3), `radial` (attaches to a surface and has no
stack nodes), `none` (a truss, a chute) or the two sizes an adapter spans, `S1/S2`. A part's diameter is its class's within 12% (a heat shield
is a little wider), its nodes are its class's size (a command pod may taper to a smaller one), and its joints hold a force in the class's band.

**Names** follow the slice: birds for pods and probes (Kestrel, Albatross, Wren, Mayfly, Finch…), insects and animals for engines (Hornet, Ember,
Wasp, Lantern…), codes for structure (`SD-1`, `TA-21`, `LT-400`). A tank's number is its propellant in kg (`LT-400` holds 2,000 units of the chemical
mix, which weigh 2,000 kg), and a hydrogen or xenon tank's is the kg it holds (`HY-140`, `XG-40`). The names are the plan's working names; D-02 is
still open, and renaming a part is an edit of its `title` and its description.

**Manufacturers** are six fictional makers, each with a personality that the descriptions of its parts will carry (PRT-10):

| Maker | Makes | Personality |
|---|---|---|
| Astraea Works | tanks of 1.25 m, couplings, ladders, batteries, legs | the agency's own shop: plain, sturdy, cheap |
| Corvid Aerospace | pods, probes, fins, chutes, nose cones, small tanks | light, clever, a little too optimistic |
| Vance Propulsion | engines and boosters | thrust first, manners later; always a point ahead of the brochure |
| Lumen Instruments | science, panels, antennas, lights | precise to a fault; gives error bars |
| Vantor Heavy Works | big tanks, heavy engines, structure, rovers | slow, massive, conservative |
| Quiet Star Labs | nuclear, ion, power, mining | experimental and quietly brilliant |

## Stat templates

A **role** is a kind of part (`engine.lifter`, `tank.chemical`, `power.solar`…). `part_templates.json` gives each role its category, its id prefixes and
a band `[min, max]` for the numbers that define it. A part must stay inside its role's bands; PRT-11 balances by moving parts inside them (or
the bands). The numbers (`PartTemplates.Metrics`):

| Metric | Meaning | Examples of bands |
|---|---|---|
| `twrAsl`, `twrVac` | engine thrust (sea level, vacuum) over its own weight | lifter TWR 8 to 30 at sea level; nuclear 1 to 8; ion 0.1 to 3 |
| `ispVac`, `ispAsl`, `ispRatio` | specific impulse, and sea level over vacuum | lifter Isp 290 to 335 s, ratio 0.78 to 0.93; vacuum engine 330 to 380 s, ratio 0.1 to 0.5; aerospike ratio 0.85 to 1; nuclear 750 to 950 s; ion 3,000 to 5,500 s |
| `dryFraction` | dry mass over full mass | chemical tank 0.09 to 0.14 (the tank is 1/9 of the full weight); hydrogen tank 0.3 to 0.8; solid booster 0.08 to 0.3; heat shield 0.2 to 0.6 |
| `twrWet` | booster thrust over its full weight | solid booster 3 to 12; separation motor 20 to 150 |
| `massPerSeat`, `torquePerKg` | crew modules; reaction wheels | pod 250 to 1,000 kg per seat; wheel 60 to 200 N·m per kg |
| `dryMass`, `costPerKg` | every part | by role |
| `breakingForce` | joint strength, by size class | S0 30 to 120 kN, S1 100 to 800 kN, S2 300 to 1,200 kN, S3 800 to 3,000 kN |

`cost` is the dry mass times the role's rate per kg, without propellant (the launch site supplies that, see [RESOURCES.md](RESOURCES.md)). The rates
and the bands are placeholders for the economy of CAR-10. Entry costs for the research tree are not set here: the science and tech rework decides
them.

## Adding a part

Copy a part of the same role, give it an id with the role's prefix, a `milestone`, a `size`, a `role` and a `manufacturer` from the list, and set its
numbers. Run the EditMode tests: `PartCatalogueTests` names every number that is out of its band.

`tools/catalogue/build_catalogue.py` made the first catalogue from the plan's table. It is a one-off: the two JSON files are the source of truth
from now on, and rerunning the script into the project would overwrite hand-made balance (`--out DIR` writes elsewhere).
