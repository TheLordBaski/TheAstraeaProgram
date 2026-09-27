# 07 · Crew, EVA and characters (CRW)

**Have (verified):**
- A roster of named rocketeers: status and flight count (`CrewRecord`), assigned automatically at launch.
- EVA: exit and board, walking, jumping, a limited jetpack, planting flags, and states preserved over switching and
  saving.
- A procedural rocketeer model.

**Missing for KSP 1.0 parity:**
- traits (pilot, engineer, scientist) with abilities;
- experience and levels;
- hiring;
- tourists and rescues;
- ladders and climbing, swimming and falling;
- EVA science (reports, samples, carrying data);
- external seats;
- crew portraits;
- interior (IVA) views;
- men and women (character art is ART-05).

Crew personality is TAP's own. KSP's kerbals have courage and stupidity; TAP's rocketeers get **Nerve** and
**Whimsy**, which drive portrait expressions and flavour text only.

---

### CRW-01 · Crew data model v2
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** FND-05

- Record fields:
  - name, gender, trait (pilot, engineer, scientist, tourist), level;
  - experience and a flight log (body × achievement: flyby, orbit, landing, flag, return);
  - Nerve and Whimsy;
  - status: available, assigned, EVA, missing, lost;
  - hire date, and the contract for tourists and rescued crew.
- Migration of slice saves: the existing crew become pilots at level 0 with their flight count kept.

**Done when**
- [ ] Old saves load with sensible defaults.
- [ ] The fields round-trip through save and load (unit test).

### CRW-02 · Astronaut Complex
**Milestone** EA3 · **Claude** 3 d · **You** 1 d · **Needs** CRW-01, HUB-02

- The roster with portraits, trait, level, status and flight log.
- **Applicants**: a rotating list with traits and a hire cost that rises with roster size (career).
- Dismiss, and the memorial (CRW-12).
- The roster size limit depends on the facility level (CAR-03). Sandbox and science hire for free.

**Done when**
- [ ] Hiring deducts funds.
- [ ] The limit blocks hiring at level 1.
- [ ] Portraits (placeholders until CRW-10) show for everyone.

### CRW-03 · Traits and abilities
**Milestone** EA3 · **Claude** 4 d · **You** 1 d · **Needs** CRW-01, FLT-03

| Trait | Abilities by level (0–5) |
|---|---|
| **Pilot** | SAS modes: 0 stability only · 1 prograde and retrograde · 2 normal and radial · 3 target and manoeuvre · higher levels give smoother control |
| **Engineer** | repair broken legs and wheels (level 1), repack parachutes (level 2), ISRU efficiency bonus, faster fuel transfer |
| **Scientist** | reset single-use experiments (level 1), more science from EVA work, better lab processing |
| **Tourist** | cannot control, cannot EVA outside the contract; pays per destination (CAR-05) |

- Probe cores keep their own SAS levels. The best source aboard wins.

**Done when**
- [ ] Each ability has a test.
- [ ] Sandbox still gives every SAS mode (difficulty option).

### CRW-04 · Experience and levels
**Milestone** EA3 · **Claude** 2 d · **You** 1 d · **Needs** CRW-01, FND-14

- The flight log fills from game events: flyby, orbit, landing, flag, per body. Experience is weighted by body (Luma
  more than Tellus orbit, Magnus moons more still).
- Experience is credited when the rocketeer is recovered at Tellus, as in KSP, so it isn't lost on a mission that
  never returns.
- Level thresholds give 0–5 stars, with a level-up notice at recovery.

**Done when**
- [ ] The `lunar` mission's pilot levels up on recovery by the designed amount.
- [ ] The flight log shows every achievement.

### CRW-05 · Rescue and tourists
**Milestone** EA3 · **Claude** 2 d · **You** 0.5 d · **Needs** CRW-01, CAR-04

- **Rescue**: a contract spawns a stranded rocketeer, in a capsule or alone on EVA in orbit or on a surface. They
  join the roster once recovered.
- **Tourists**: board as passengers with a list of destinations. They pay on recovery for the destinations visited.

**Done when**
- [ ] Autotest: rescue from Tellus orbit adds a rocketeer to the roster.
- [ ] A tourist who visited Luma orbit pays the contract amount.

### CRW-06 · Ladders and climbing
**Milestone** EA1 · **Claude** 3 d · **You** 1 d · **Needs** PRT-02

- Ladder parts, and ladder zones on pods and lander cans.
- Grab a ladder near the rocketeer, climb up and down, reach the hatch, let go. The jetpack is off while on the
  ladder.
- Climbing works in any gravity, and stepping off onto the ground or a landing leg is safe.

**Done when**
- [ ] A rocketeer climbs down a Luma lander's ladder to the ground and back up to the hatch.
- [ ] Autotest step in `lunar-surface`.

### CRW-07 · Swimming, falling and ragdoll
**Milestone** EA5 · **Claude** 3 d · **You** 1 d · **Needs** ART-05

- **Swimming**: floating on Tellus's, Caligo's and Maris's liquids with slow swimming, and boarding from the water.
- **Ragdoll**: a hard landing or a steep slope trips the rocketeer into ragdoll physics, then they get up. Lethal
  impacts still kill.

**Done when**
- [ ] A rocketeer can swim from a splashed capsule, climb onto it and board.
- [ ] Tripping on a 30° slope recovers without clipping into the terrain.

### CRW-08 · EVA science
**Milestone** EA1 · **Claude** 2 d · **You** 0.5 d · **Needs** SCI-03

- **Crew Observation** (from inside) and **EVA Field Notes** (outside), **Surface Sample** (on the ground; in
  career it needs the research facility at level 2).
- **Carrying data**: take data from instruments on EVA, store it in any crewed pod or the lab, and carry it back.

**Done when**
- [ ] A rocketeer collects a sample on Luma, stores it in the pod, and it is credited on recovery.

### CRW-09 · External seats
**Milestone** EA2 · **Claude** 2 d · **You** 0.5 d · **Needs** PRT-06

- Sit in a seat part, which gives the vessel control and SAS from the pilot, and stand up to continue the EVA.
- A seat-flown lander or rover.

**Done when**
- [ ] A seat-flown Pruina hopper lands and takes off.
- [ ] The rover of FLT-08 can be driven from a seat.

### CRW-10 · Portraits in flight
**Milestone** EA5 · **Claude** 3 d · **You** 1 d · **Needs** ART-05

- Live head-and-shoulders portraits for everyone aboard, rendered from the character model.
- Expressions react to g-load, spin, landing and staging, flavoured by Nerve and Whimsy.
- Click a portrait for IVA (CRW-11) or EVA.

**Done when**
- [ ] Portraits show in flight at under 1 ms per frame for 4 crew.
- [ ] Expressions change with g-load.

### CRW-11 · Interior views
**Milestone** EA5 · **Claude** 4 d · **You** 3 d · **Needs** CRW-10, ART-05, D-04

- A first-person interior for the main pods (Kestrel, Albatross, Mayfly), with windows onto the real scene.
- Basic props and a working navball in the dashboard; you can look around. Toggle with C.
- Scope is decided in D-04. The recommendation is these 3 pods first; the other crew parts show a view from their
  window.

**Done when**
- [ ] You can fly a landing on Luma from inside the Mayfly using the dashboard navball.

### CRW-12 · Crew loss and the memorial
**Milestone** EA3 · **Claude** 1 d · **You** 0.5 d · **Needs** CRW-02

- Difficulty option: lost crew come back after N days, or never.
- The memorial lists the lost with their flight logs, in the Astronaut Complex.
- Reputation penalty for losses in career (CAR-01).

**Done when**
- [ ] A crew loss is listed.
- [ ] Respawn after the configured time works.

### CRW-13 · Names
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** —

- Original lists of first names and surnames, varied and pronounceable, with no repeats while alive.
- You can rename anyone in the Astronaut Complex. Tourists get different surname pools.

**Done when**
- [ ] 200 generated names contain no duplicates and no real-world celebrity names (a check list).
