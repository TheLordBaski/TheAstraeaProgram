# 11 · Space center (HUB)

**Have:**
- The launch complex: pad deck, flame trench, service tower and the assembly building ~420 m west of the pad. These
  are surface objects in flight.
- The main menu opens the assembly building directly, and flights return there.

**KSP 1.0 had a hub scene** with clickable facilities:
- vehicle assembly, spaceplane hangar, launch pad, runway;
- tracking station, Mission Control, Astronaut Complex, R&D, Administration.

The buildings changed looks with their upgrade level and could be destroyed by crashes.

**TAP's hub** is rockets only, with no hangar or runway (they could come later, D-15). It has seven facilities:

| Facility | Status |
|---|---|
| Assembly Building | existing |
| Launch Pad | existing |
| Tracking Station | new |
| Mission Control | new |
| Astronaut Complex | new |
| R&D | new |
| Administration | new |

The hub is the root of every game loop (FND-07).

---

### HUB-01 · Site plan
**Milestone** M1 · **Claude** 1 d · **You** 1 d · **Needs** —

- A layout around the existing complex: roads, buildings, landscaping and the flattened area (the generic
  `flatAreas`, FND-02). The launch pad stays where it is, so saves and pad coordinates don't change.
- Silhouettes readable from the hub camera. Room for a runway and hangar later without moving anything (D-15).
- Placeholder buildings (procedural boxes) for M1; real art is ART-06.

**Done when**
- [ ] The plan image is in this folder.
- [ ] The placeholders are in the flight scene and the hub.
- [ ] Nothing overlaps the pad's approach or the rocket's fall-back zone.

### HUB-02 · Hub scene, camera and facility screens
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** FND-07, HUB-01

- The hub camera: orbit, pan and zoom around the site, bounded, with smooth moves between buildings.
- Hovering highlights a building and shows its name and level. A click opens its screen:

  | Building | Screen |
  |---|---|
  | Assembly | assembly scene |
  | Tracking Station | NAV-06 |
  | Mission Control | CAR-06 |
  | Astronaut Complex | CRW-02 |
  | R&D | TEC-03 |
  | Administration | CAR-08 |
  | Pad | vessels on the pad, launch |

- Facilities without a function in the current mode are shown but disabled, with an explanation (for example, no
  Mission Control in sandbox).
- The top bar shows currencies (career), the date and settings.

**Done when**
- [ ] Every facility opens its screen, or explains why it can't in the current mode.
- [ ] Keyboard navigation works.
- [ ] 60 fps on the reference PC.

### HUB-03 · Day, night and time in the hub
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** HUB-02, FND-01

- The hub is lit by the real star position, so there is night at the space center with lights on the buildings.
- Time passes in the hub. Warp in the hub like the tracking station, stopping for alarms (NAV-10).

**Done when**
- [ ] The day/night cycle matches Tellus's rotation.
- [ ] Warping to an alarm from the hub stops on time.

### HUB-04 · Facility levels and upgrades
**Milestone** EA3 · **Claude** 2 d · **You** 1 d · **Needs** CAR-03, ART-06

- Each building shows its level. The upgrade button (career) shows cost and new limits. The upgrade swaps the model
  with a short transition.
- Placeholder tiers until the art (ART-06) lands.

**Done when**
- [ ] Upgrading each facility in career changes its look and its limits.
- [ ] The level is saved.

### HUB-05 · Destructible facilities
**Milestone** EA5 · **Claude** 3 d · **You** 1 d · **Needs** HUB-04, ART-06

- A crash above an impact threshold breaks the building: it collapses into ruins and its functions are disabled.
  Repair costs funds in career and is free in sandbox (or buildings are indestructible, CAR-09).
- The launch pad can be damaged too. Launching is blocked until it is repaired.

**Done when**
- [ ] A rocket crashing into Mission Control breaks it.
- [ ] Repair restores it.
- [ ] The difficulty toggle makes buildings immune.

### HUB-06 · Launch and return flow
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** HUB-02, ASM-07, SCI-06

- **Launching**:
  - assembly building → the pad is checked: occupied by another vessel? offer to recover it;
  - crew assignment (ASM-07) → flight.
- **Returning**: recovery → summary dialog (science, refunds, experience) → hub. Revert options survive this flow.

**Done when**
- [ ] Launch, recovery and the summary work in all three modes.
- [ ] A vessel left on the pad blocks the next launch until it is recovered or moved.

### HUB-07 · Agency identity
**Milestone** EA3 · **Claude** 1 d · **You** 1 d · **Needs** UX-11, CAR-07

- Agency name and flag chosen at new game, and changeable. The flag appears on buildings, planted flags and vessels
  (decals where parts support them).
- The agency history from the world firsts (CAR-07), shown in the Administration building.

**Done when**
- [ ] A custom flag appears on the hub buildings and on a planted flag.
- [ ] The history lists every first.
