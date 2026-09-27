# 03 · Navigation, map and tracking station (NAV)

**Have:** a map view for Tellus and Luma with orbit lines, Ap/Pe, encounters and targets (Luma or a vessel), plus
closest approach and relative velocity.

Manoeuvre nodes work in KSP style:

- drag handles, numeric steps and time shift;
- click an orbit for **Add maneuver node** or **Warp here**;
- drag a node along its orbit;
- delete a node with right-click, Del or ×.

The patched-conic predictor (`PatchedConics`) is already generic over the body hierarchy. The navball has every
marker.

**Missing for KSP 1.0 parity:**
- a map at solar-system scale;
- conics across several SOIs;
- rendezvous aids (AN/DN);
- a tracking station;
- vessel types and names;
- waypoints for contracts;
- body information.

Beyond KSP 1.0 (but cheap and valuable in a game with interplanetary travel): transfer-window help and an alarm
clock.

---

### NAV-01 · Map view at solar-system scale
**Milestone** EA1 · **Claude** 4 d · **You** 1 d · **Needs** FND-01, FND-03

- Zoom smoothly from a vessel (10 m) out to the whole system (200 Gm), with the camera clipping handled in scaled
  space.
- Focus cycling: Tab / Shift+Tab through the vessel, its bodies and its targets. Double-click a body or vessel to
  focus it.
- Orbit lines for every body, and optional SOI spheres.
- Labels declutter: labels fade by screen size, and moons hide when their parent is tiny.
- Vessel-type icons (NAV-07), with debris hidden by default.
- It stays fast with 40 vessels and 17 bodies.

**Done when**
- [ ] Zooming from a docking port to Remota's orbit is smooth and nothing jitters.
- [ ] Focus cycling reaches every relevant object.
- [ ] The map stays under 2 ms per frame at 40 vessels.

### NAV-02 · Patched conics across several SOIs
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** NAV-01

- The predictor follows Tellus → star → Rubra → Vigil and draws each patch, with SOI change markers and times.
- Draw modes as in KSP: each patch relative to its own body, or relative to the target body at encounter time. The
  second makes the arrival orbit around a planet readable.
- In career, the number of patches drawn depends on the tracking station level (CAR-03).
- Encounter info (periapsis, inclination, time) for each chained encounter.

**Done when**
- [ ] A Tellus → Rubra transfer shows escape, the heliocentric arc and the Rubra periapsis.
- [ ] The prediction matches the flown trajectory within 1% of periapsis (autotest).

### NAV-03 · Rendezvous aids
**Milestone** EA1 · **Claude** 3 d · **You** 0.5 d · **Needs** NAV-02

- Ascending/descending node markers with relative inclination to a target (vessel or body).
- Closest-approach markers for the next two orbits, with separation and time (this extends today's closest
  approach).
- Relative speed at closest approach. Target and anti-target markers already exist on the navball; add a docking
  alignment view (optional).
- A "match planes" node helper: a button that creates a node at AN/DN with the normal Δv to zero the relative
  inclination.

**Done when**
- [ ] A rendezvous flown by hand (you) is possible with the map aids only.
- [ ] The docking autotest uses the AN/DN helper to align planes from 5°.

### NAV-04 · Manoeuvre planning precision
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- A node editor panel: numeric prograde, normal and radial Δv fields, step size, jump ±1 orbit, move to Ap/Pe/AN/DN.
- Burn time estimated across stages (using the Δv calculator).
- *Warp to node* with a lead time setting. It stops warp at T minus (half the burn plus the margin).

**Done when**
- [ ] A node can be placed to 0.1 m/s with fields.
- [ ] The warp stops at the configured lead.
- [ ] A burn across a staging event shows the right total time.

### NAV-05 · Transfer windows and planning help
**Milestone** EA2 · **Claude** 4 d · **You** 1 d · **Needs** NAV-02, SYS-01

KSP 1.0 had none; its players used web tools. TAP makes it part of the game.

- Readouts for the selected target planet: phase angle now vs ideal, time to the next window, ejection angle.
- A transfer planner window: departure-date × travel-time grid coloured by Δv (a porkchop plot). Clicking a cell
  creates the ejection node from the current orbit.
- The planner needs a Lambert solver; the scripted pilot reuses it (SYS-17).

**Done when**
- [ ] For Tellus → Rubra, the planner's best window is within 5% of the Δv the autotest flies.
- [ ] Clicking it creates a node that produces an encounter.

### NAV-06 · Tracking station
**Milestone** EA1 · **Claude** 5 d · **You** 1 d · **Needs** NAV-01, FND-07

- A scene (or the hub's building view) with the whole-system map and a list of every vessel, grouped and filterable
  by type and body.
- Actions: **Fly**, **Recover** (landed or splashed on Tellus, with the recovery dialog), **Terminate** (with
  confirmation), **Rename**.
- Time warp from the tracking station, stopping for alarms (NAV-10) and SOI changes of vessels.
- Asteroids appear here as unknown objects to track or ignore (RES-05). In career the facility level limits the
  tracking.

**Done when**
- [ ] Every action works from the tracking station.
- [ ] Warping days in the tracking station keeps every vessel's orbit exact: the `persistence`-style check passes.

### NAV-07 · Vessel types, names and icons
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** —

- Types: probe, lander, rover, ship, station, base, debris, flag, rocketeer, asteroid, plus relay (reserved for
  comm networks later).
- Chosen automatically (debris for broken-off parts, flag, rocketeer) and editable. A rename dialog from the
  assembly building, flight and the tracking station.
- Icons and colours per type, used on the map, in the tracking station and in vessel switching.

**Done when**
- [ ] Every object on the map has the right icon.
- [ ] A rename survives saving.
- [ ] Debris can be filtered out.

### NAV-08 · Waypoints
**Milestone** EA3 · **Claude** 3 d · **You** 0.5 d · **Needs** NAV-01

- A waypoint has a body, latitude, longitude and optional altitude. Contracts create them (survey, landmarks), and
  you can place your own in the map.
- Shown on the map, as a world marker in flight (with distance) and on the navball as a heading marker. One can be
  set as the target.
- Saved, and removed when their contract ends.

**Done when**
- [ ] Flying a rover to a waypoint 10 km away works using only the navball marker.
- [ ] A survey contract's waypoints appear and disappear correctly.

### NAV-09 · Body information panel
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SYS-18

- From the map and the tracking station:
  - radius, gravity, escape velocity, day length;
  - atmosphere (height, pressure);
  - SOI, orbit and moons;
  - biomes discovered;
  - science collected per situation;
  - the synchronous orbit altitude (useful for satellite contracts).

**Done when**
- [ ] Every body shows correct values.
- [ ] The synchronous altitude for Tellus matches the orbital formula.

### NAV-10 · Alarm clock
**Milestone** EA2 · **Claude** 2 d · **You** 0.5 d · **Needs** NAV-02

(Added to KSP only years after 1.0, but essential with many missions in flight.)

- Alarms for a manoeuvre node, an SOI change, closest approach, an in-game date, or a transfer window.
- Warp stops at an alarm with a margin, and the alarm can switch you to its vessel. The list is in the tracking
  station and in flight.

**Done when**
- [ ] Warping in the tracking station stops at an alarm and offers to switch to its vessel.
- [ ] Alarms are saved.
