# 04 · Flight systems and physics (FLT)

**Have (verified in the slice):**

- **Vessel model**: vessels are single rigid bodies with analytic joint loads. Parts break at joints; the loads
  come from the side without ground contact, with per-point contact impulses.
- **Engines**: liquid engines and solid boosters with sea-level vs vacuum Isp, gimbal, throttle and restarts.
- **Flow and control**: stack fuel flow with decouplers blocking crossfeed; RCS, reaction wheels and every SAS mode.
- **Atmosphere**: per-part drag with occlusion, Mach effects, fin and body lift; heating and ablation.
- **Recovery hardware**: parachutes with safety states; landing legs with suspension; buoyancy and splashdown.
- **EVA and docking**: an EVA jetpack; docking with magnetic capture.
- **Simulation**: rails and physics warp, floating origin and Krakensbane.

The design pillar stays: one rigid body per vessel, no wobble (D-14).

**Missing for KSP 1.0 parity:**
- propellant types for nuclear and ion engines;
- electricity (generation, consumption, starvation);
- probe control rules;
- heat conduction and radiators;
- aero parts for rockets (fairings, control fins, airbrakes);
- resource transfer and flow control;
- action groups;
- rovers;
- launch clamps;
- docking port sizes;
- the grabbing unit;
- tweakables;
- struts and fuel lines;
- performance at KSP-scale part counts.

---

### FLT-01 · Propellant model
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** D-01

Today there is one `LiquidFuel` for every liquid engine. Nuclear-thermal and ion engines need their own propellants.
Pick the model in D-01. The recommendation is to keep one chemical **Propellant** (renamed from `LiquidFuel`) and add:

- **Hydrogen** for nuclear-thermal engines: light, bulky, it boils off slowly unless insulated (boil-off optional);
- **Xenon** for ion engines;
- **Ore** for ISRU (RES).

The work:
- Resource definitions (density, unit volume, cost for career).
- Tank contents per part, the engines' propellant mix, and flow rules per resource (xenon and ore flow everywhere,
  like electricity; stack propellants flow down stacks).
- A migration of craft files and saves (`LiquidFuel` → `Propellant`), and the part stats and descriptions updated.

**Done when**
- [ ] Every starter craft and save loads with an unchanged Δv (within 0.5%).
- [ ] A nuclear engine burns Hydrogen only.
- [ ] Unit tests cover the new flow rules.

### FLT-02 · Electric system: generation, consumption, starvation
**Milestone** EA1 · **Claude** 5 d · **You** 1 d · **Needs** SYS-02, PRT-03

- **Generation**:
  - Solar panels scale with sun angle and 1/r² flux. Tracking panels rotate towards the star. Bodies eclipse them
    and the vessel's own parts shade them (a few raycasts, updated at a low rate).
  - RTGs produce a constant output.
  - Fuel cells turn Propellant into electricity.
  - Launch clamps supply power on the pad.
- **Consumption**:
  - probe cores;
  - reaction wheels, in proportion to torque used;
  - antennas while transmitting;
  - experiments while running;
  - lights;
  - ion engines;
  - ISRU;
  - wheel motors.
- **Starvation**: a probe with no charge loses control (staging and SAS off); wheels and ion engines stop. The HUD
  warns before it happens.
- **On rails**: charge is integrated over the warp step (generation minus idle use, with eclipses), so a probe can
  run flat during a long warp through a night side.
- **HUD**: net flow (+/− per second) and time to empty or full.

**Done when**
- [ ] A probe in Luma's shadow runs its battery down at the expected rate and recovers in sunlight.
- [ ] A panel shaded by a tank produces nothing.
- [ ] Unit tests cover the rails integration.

### FLT-03 · Command, control and probe rules
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FLT-02

- Control points: *Control from here* on pods, probe cores and docking ports; the navball follows.
- A vessel is **controllable** when it has a crewed pod, an external seat with a rocketeer, or a probe core with
  charge. Otherwise it is **uncontrolled**: no SAS, throttle, steering or staging.
- SAS modes come from the best probe-core level on board or the best pilot level (CRW-03). Sandbox allows all.

**Done when**
- [ ] Uncrewed debris can't be flown.
- [ ] A basic probe core offers stability assist only.
- [ ] Switching the control point re-orients the navball.

### FLT-04 · Thermal system v2
**Milestone** EA2 · **Claude** 5 d · **You** 1 d · **Needs** SYS-02

- Heat conducts between connected parts (per joint, scaled by contact area).
- Engines put heat into their own part and neighbours (the nuclear engine a lot).
- Radiators: fixed panels and deployable ones that pull heat from nearby parts, plus active radiators that use
  electricity.
- The star heats parts by flux and exposed area (Ignis), and planets radiate heat.
- Drills and ISRU converters generate heat (RES).
- Thermal readouts: temperature bars near hot parts (optional overlay), and warnings before a part is lost.
- On rails, the temperatures jump to their steady state, so a warp can't hide an overheating nuclear engine.

**Done when**
- [ ] The `failures` mission still passes.
- [ ] A nuclear engine at full thrust needs radiators to run beyond N minutes.
- [ ] A probe near Ignis balances at the predicted temperature.

### FLT-05 · Aerodynamics for rockets: fairings, bays, other atmospheres
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** PRT-04

- Parts inside a closed fairing or service bay get no drag and no heating. The shell carries them instead (occlusion
  by enclosure, not only by the part in front).
- Every atmosphere uses its own composition (molar mass, adiabatic index, temperature curve), so Caligo's dense haze
  and Rubra's thin air behave differently.
- An aero overlay (F12 debug and a player option): drag and lift vectors per part, CoP vs CoM.

**Done when**
- [ ] A payload in a fairing reaches orbit with less drag than the same payload bare (measured).
- [ ] Rubra's terminal velocity under the same chute is several times Tellus's, matching the density ratio.

### FLT-06 · Resource management: transfer, locks, crossfeed
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** FLT-01

- Transfer between tanks from the part menu: select source and destination, in or out, stop. It works across a
  docked vessel.
- Lock a tank (no flow), and toggle crossfeed on decouplers and docking ports.
- Stack vs radial flow priority as documented in the handbook.
- The resource panel shows current stage or whole vessel.

**Done when**
- [ ] Moving propellant from a docked tanker into a lander works.
- [ ] A locked tank is untouched by engines.
- [ ] Unit tests cover the flow priority.

### FLT-07 · Action groups
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** ASM-03, FND-09

- Standard groups: Stage, Legs, Lights, RCS, SAS, Brakes, Abort. Custom groups 1–10.
- Parts expose their actions: toggle, extend, retract, activate, shut down, deploy, transmit…
- Assigned in the assembly building (ASM-03), fired by keys in flight, saved with craft and vessels.

**Done when**
- [ ] Custom group 1 fires an experiment and a panel deploy together.
- [ ] Abort stages a launch-escape setup.
- [ ] Groups survive save/load and docking.

### FLT-08 · Rovers and wheels
**Milestone** EA2 · **Claude** 7 d · **You** 2 d · **Needs** PRT-12b

- A wheel module: raycast suspension (like the legs), a motor that uses electricity, steering, brakes, traction
  from surface and gravity, and slip.
- Rover driving mode: W/S throttle and A/D steer when the controlling part points forward. Brakes on B, a speed
  readout and optional cruise control.
- Damage: a wheel breaks on overload or impact, and an engineer can repair it (CRW-03).
- Stable at low gravity (Pruina, Mica): no flip-outs at low speed.

**Done when**
- [ ] Autotest `rover-luma`: drive 2 km across Luma and return to the lander.
- [ ] A wheel breaks from a hard drop and is repaired on EVA.
- [ ] The rover stays upright on a 15° slope at 5 m/s.

### FLT-09 · Launch clamps
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Clamp parts hold the craft on the pad. They replace today's implicit `PadHold`, release on the first stage, and
  supply power (and optionally propellant top-up) until release.
- Clamp towers stretch to the ground under the attach point.
- Existing craft without clamps still launch (the pad still holds them for compatibility).

**Done when**
- [ ] Engines ignite with the rocket held, and it releases on staging.
- [ ] A tall rocket on clamps doesn't sway.
- [ ] Every mission passes.

### FLT-10 · Docking v2: sizes and docking mode
**Milestone** EA2 · **Claude** 3 d · **You** 0.5 d · **Needs** PRT-12b

- Port sizes S0, S1 and S2. Only matching sizes capture.
- An inline shielded port that opens and closes (drag while closed).
- *Decouple node* and *Undock* for any chosen port on a stack docked several times.
- **Docking mode**: a HUD toggle that turns WASD/QE into translation and keeps rotation on the arrow keys.

**Done when**
- [ ] A station of 4 modules on 3 port sizes builds, undocks in any order and re-docks.
- [ ] The `docking` mission passes.

### FLT-11 · Grabbing unit
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** PRT-12c

- An arm that grabs any part, debris or asteroid surface on contact. It merges the two vessels at the contact point.
- Free-pivot mode, crossfeed through the grab (when grabbing a tank or an asteroid with ore), and release with a
  push.

**Done when**
- [ ] Autotest: grab a spent upper stage, de-orbit it and release.
- [ ] Grab a 500 t asteroid and push it with the combined vessel (RES-06).

### FLT-12 · Physics range and many vessels
**Milestone** EA2 · **Claude** 3 d · **You** 0.5 d · **Needs** —

- Loading and unloading distances set per situation: larger for landed vessels, packing ranges that avoid landed
  vessels sinking or jumping.
- Debris settings: maximum persistent debris, and auto-clean on rails.
- Landed bases of up to 10 vessels near each other stay stable when loaded.

**Done when**
- [ ] 10 landed vessels at a Luma base stay put over 10 load/unload cycles.
- [ ] Debris is capped as configured.

### FLT-13 · Precision at solar distances
**Milestone** EA1 · **Claude** 2 d · **You** — · **Needs** FND-01

A regression suite for the classic "Kraken" failures:
- 30 days at maximum warp in solar orbit, then docking at the far end;
- a high-speed flyby of Magnus at periapsis;
- a landing on Remota at aphelion;
- a vessel near the star.

Each checks position/velocity continuity, energy drift and joint stability.

**Done when**
- [ ] Every case passes nightly: no jumps over 1 m, and no energy drift beyond expectation.

### FLT-14 · Part-count performance
**Milestone** EA1 · **Claude** 5 d · **You** 1 d · **Needs** FND-13

- Profile the per-part costs: aerodynamics, thermal, structural analysis, resource flow, part modules, effects,
  UI.
- Batch the hot loops (Burst jobs for aero and thermal per part), cut allocations in `FixedUpdate`, and use effect
  LODs.
- Meet the FND-13 budgets: 60 fps at 150 parts, 30 fps at 300 parts.

**Done when**
- [ ] The benchmark meets the budgets on the reference PC.
- [ ] No per-frame allocations in flight.

### FLT-15 · Tweakables
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** ASM-02

- Thrust limiter; gimbal limit and lock; resource fill per tank (assembly building only); crossfeed toggles;
  reaction-wheel authority; RCS thruster limiter.
- Parachute deploy pressure and altitude.
- Leg and wheel spring and damper.
- Everything is editable in the assembly building and, where it makes sense, in flight. Saved with craft and
  vessels.

**Done when**
- [ ] A thrust limiter at 50% halves the thrust and the Δv readouts agree.
- [ ] A chute set to 2 km opens at 2 km.

### FLT-16 · Struts and fuel lines in a single-rigid-body world
**Milestone** EA4 · **Claude** 3 d · **You** 1 d · **Needs** PRT-05

TAP has no wobble, so a strut can't stiffen anything. It **reinforces** instead: the joint-load analysis
(`StructuralAnalysis`) treats a strut as a second load path between its end parts. It carries a share of the forces
along the tree path it spans, up to its own strength, and it can break.

- Fuel lines add a flow edge from source to target (for asparagus staging).
- The assembly building shows which joints a strut relieves, and the Engineer's Report counts struts.

**Done when**
- [ ] Heavy side boosters that snap off at max-Q without struts survive with two struts each.
- [ ] Asparagus staging with fuel lines drains the outer boosters first (autotest).

### FLT-17 · Control fins, airbrakes and lifting reentry
**Milestone** EA2 · **Claude** 3 d · **You** 1 d · **Needs** PRT-12b

- Control fins and canards deflect with pitch, yaw and roll input (and SAS), and their authority scales with dynamic
  pressure.
- Airbrakes deploy for drag (the Brakes action group).
- Capsules generate body lift when offset (lifting reentry lowers the g-load). SAS can hold a reentry attitude.
- Booster recovery aids are optional: grid fins (PRT catalogue).

**Done when**
- [ ] A finned rocket follows a gravity turn with fins only after the engines are out.
- [ ] A lifting reentry from Luma return peaks below 5 g, against 6.4 g ballistic in the `lunar` mission.

### FLT-18 · Engine families
**Milestone** EA2 · **Claude** 4 d · **You** 1 d · **Needs** FLT-01, FLT-02, FLT-04

- **Aerospike**: nearly flat Isp across altitude.
- **Nuclear thermal**: Hydrogen, high Isp, low thrust, lots of heat, a slow spool.
- **Ion**: Xenon plus electricity, tiny thrust. It can't thrust on rails, so physics warp only, with a "burn
  duration" readout and a rails-safe completion.
- **Cluster and radial engines**; **verniers** (small attitude-control thrusters on Propellant).
- Thrust curves for solid boosters (tunable per booster).

**Done when**
- [ ] Each engine type has a unit test for its thrust/Isp curve.
- [ ] An ion probe completes a 2,000 m/s transfer with physics warp in reasonable real time (under 10 min at 4×).
