# 06 · Assembly building (ASM)

**Have (verified):**

- **Building:**
  - vertical construction with stack snapping and surface attachment on the true part profile;
  - radial and mirror symmetry (1–8);
  - angle snap and free rotation;
  - pick up, copy, delete, undo/redo;
  - set parts aside (greyed, not launched) and re-root the craft.
- **Staging**: staging editor with automatic order and drag and drop.
- **Readouts**:
  - mass, TWR and Δv per stage (sea level, vacuum, Luma);
  - CoM, CoT and CoP markers;
  - design warnings;
  - save/load of craft files, and 3 starter rockets.

**Missing for KSP 1.0 parity:**
- a part browser that scales to 168 parts with research locks and costs;
- tweakables;
- an action group editor;
- subassemblies;
- a craft browser with thumbnails;
- offset and rotate gizmos;
- crew assignment;
- the Engineer's Report with facility limits and costs;
- a Δv readout for any destination;
- a stock craft library for the whole system.

---

### ASM-01 · Part browser v2
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** PRT-09

- Browsing:
  - categories: pods, propulsion, tanks, control, structure, coupling, aero, utility, science, ground, ISRU;
  - filters by size, manufacturer, resource and research state;
  - text search and sort by name, mass or cost;
  - sections that can be collapsed.
- **Research state**: unresearched parts are hidden or shown locked. In career, researched but not purchased parts
  show their price (TEC-02).
- Hover shows the part info panel (PRT-09). Icons come from the thumbnail renderer.

**Done when**
- [ ] Any of the 168 parts can be found by search in under 3 s.
- [ ] Locked parts are correct in each mode.
- [ ] The browser stays responsive (under 1 ms per frame idle).

### ASM-02 · Tweakables in the assembly building
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- The part menu gains sliders and toggles from FLT-15:
  - fill levels per resource;
  - thrust limiter, gimbal;
  - chute deploy pressure and altitude;
  - crossfeed;
  - reaction-wheel authority.
- Symmetry counterparts follow unless you hold Alt. Totals and Δv update live.

**Done when**
- [ ] Halving a tank's fill updates mass and Δv at once.
- [ ] Settings are saved in the craft file.

### ASM-03 · Action group editor
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FLT-07

- A mode in the assembly building:
  - pick a group, then click parts to add their actions to it;
  - a list shows the actions per group;
  - symmetry is respected;
  - presets set sensible defaults (legs, lights, chutes).

**Done when**
- [ ] Building the Luma lander with custom groups is possible without the keyboard.
- [ ] The groups fire correctly in flight (FLT-07).

### ASM-04 · Subassemblies
**Milestone** EA2 · **Claude** 3 d · **You** 0.5 d · **Needs** —

- Save a picked-up subtree as a named subassembly with a thumbnail. A library panel lists them, and dragging one
  into the craft attaches it like a part.
- Stored in the user folder, shared across saves, and respecting research locks.

**Done when**
- [ ] A lander saved as a subassembly is re-attached to a new lifter with the same staging within the subtree.

### ASM-05 · Craft browser v2
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Lists stock craft and your craft, with a thumbnail rendered on save, description, part count, cost, mass and a
  Δv summary.
- Load, merge into the current craft (as a set-aside group, since set-aside groups already exist), rename, delete,
  open the folder.
- Craft with locked or missing parts show which ones.

**Done when**
- [ ] Merging a craft brings it in as a set-aside group.
- [ ] A craft with a modded part that is missing loads with a clear notice.

### ASM-06 · Offset and rotate gizmos
**Milestone** EA2 · **Claude** 3 d · **You** 1 d · **Needs** —

- Translate and rotate handles on a selected part, with snap steps and local/absolute axes.
- Offset stays within a limit (KSP-like) so parts can't float away from their attach point. It works with symmetry.

**Done when**
- [ ] A fin can be nudged and rotated precisely with snap.
- [ ] The offset survives launch and the physics stay unchanged (the joint stays at the attach node).

### ASM-07 · Crew assignment
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** CRW-01

- A panel listing every seat. Assign crew from the roster (trait and level shown from EA3), auto-fill or leave
  seats empty.
- Tourists go only to seats their contract allows.
- The flight uses the assignment; today crew are assigned automatically at launch.

**Done when**
- [ ] Launching with a chosen crew puts exactly them aboard.
- [ ] An uncrewed craft launches with empty seats and warns if nothing can control it.

### ASM-08 · Engineer's Report and facility limits
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** FND-06

- Extends `DesignAnalysis`. Checks:
  - control;
  - electricity balance;
  - parachutes vs heat shield;
  - staging mistakes;
  - fuel flow blocked;
  - engines without fuel;
  - landing legs;
  - fairing clearance.
- **Career**: part count, mass and height against the facility levels (CAR-03), launch cost against funds.
- A traffic-light summary, and one click to jump to the offending part.

**Done when**
- [ ] Each check has a failing test craft.
- [ ] Launch is blocked, with the reason, when a hard limit is exceeded in career.

### ASM-09 · Δv readout for any destination
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** FND-14, SYS-18

- Choose the body and situation for the stage readouts: sea level or vacuum on any body, instead of today's fixed
  Luma column.
- A "mission Δv" hint from the Δv map (for example Tellus → Rubra surface) next to the total.

**Done when**
- [ ] Readouts for Rubra agree with the in-flight readout on Rubra's surface within 1%.

### ASM-10 · Assembly building camera and interaction polish
**Milestone** EA1 · **Claude** 2 d · **You** 1 d · **Needs** FND-09

- Camera:
  - pan vertically along the craft, and horizontally for wide craft;
  - focus on a part;
  - zoom limits by craft size;
  - smooth orbit.
- Interaction:
  - clear symmetry and snap indicators;
  - hover highlight;
  - undo history visible;
  - rebindable keys.

**Done when**
- [ ] Working on a 40 m rocket is comfortable (you judge it).
- [ ] Every action is reachable by mouse or key.

### ASM-11 · Stock craft library
**Milestone** EA2 · **Claude** 3 d · **You** 2 d · **Needs** PRT-12b, SYS-17

- Reference craft for every rung of the ladder:
  - a sounding rocket and an orbital probe;
  - a Luma lander;
  - a Pruina hopper;
  - a Rubra lander;
  - a Caligo lander and its heavy launcher;
  - a Magnus moons explorer;
  - a station core, a rover and a heavy lifter;
  - later: an ISRU miner and an asteroid tug (EA4).
- Each one is flown by an autotest (SYS-17) and ships with a description.

**Done when**
- [ ] Every stock craft flies its described mission in the nightly tests.
- [ ] Each one appears in the craft browser with a thumbnail.
