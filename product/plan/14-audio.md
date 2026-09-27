# 14 · Audio (AUD)

**Have:** no audio at all.

**KSP 1.0 had:** engine and staging sounds, explosions, wind, parachutes, interface clicks, space-center ambience
and a soundtrack of calm music in space and lively music at the center.

TAP needs a complete sound design. The sourcing decision is D-06 (library sounds processed by you, commissioned or
licensed music).

---

### AUD-01 · Audio architecture
**Milestone** M1 · **Claude** 3 d · **You** 0.5 d · **Needs** FND-08

- A mixer with groups: master, music, effects, interface, ambience (volumes from FND-08).
- **Atmosphere filter**: sound through the air falls off with pressure. In vacuum only sounds carried through the
  vessel are heard: a low-passed, quieter version of your own vessel.
- 3D positioning, distance attenuation, a voice limit with priorities, and pooling.
- Nothing plays twice for one event (staging a symmetry group plays one sound).

**Done when**
- [ ] A rocket climbing through 30 km loses its outside sound smoothly.
- [ ] 100 simultaneous events don't exceed the voice limit or allocate per frame.

### AUD-02 · Engine sounds
**Milestone** EA1 · **Claude** 3 d · **You** 2 d · **Needs** AUD-01

- Loops per family and size:
  - small, medium and large liquid engines;
  - solid boosters;
  - RCS puffs;
  - later the nuclear engine's rumble, the ion drive's hum and the aerospike.
- Ignition, shutdown, flame-out, gimbal whine (subtle). Pitch and volume follow throttle and chamber state. Clusters
  mix without phasing.
- Sourcing: library sounds processed by you (D-06).

**Done when**
- [ ] Every EA1 engine has sound.
- [ ] A 7-engine cluster sounds big, not louder-and-phasey (your judgement).

### AUD-03 · Vehicle sounds
**Milestone** EA1 · **Claude** 2 d · **You** 1.5 d · **Needs** AUD-01

- Decoupling, staging, docking capture and undock, parachute deploy and snap-open.
- Legs deploying and touching down, fairings opening, panels and antennas deploying.
- Explosions (small, large), structural stress creaks before a joint fails, heat roar and plasma on reentry, wind by
  airspeed and density, splashdown.

**Done when**
- [ ] Every event in the list has a sound.
- [ ] A stress creak plays before at least 80% of structural failures in the `failures` mission.

### AUD-04 · Rocketeer sounds
**Milestone** EA1 · **Claude** 1 d · **You** 1 d · **Needs** AUD-01

- Footsteps on dust, ice, rock and metal (ladders); jetpack bursts; helmet breathing and suit ambience; the flag
  plant; boarding; swimming (EA5).

**Done when**
- [ ] Walking on Luma, climbing a ladder and a jetpack burst each sound right in the helmet mix.

### AUD-05 · Interface sounds
**Milestone** EA1 · **Claude** 1 d · **You** 0.5 d · **Needs** AUD-01

- Clicks, hovers, confirmations, errors, notifications by type (science, contract, warning, first), warp steps,
  staging clicks. Subtle, never tiring.

**Done when**
- [ ] Every button and notification type has a sound.
- [ ] You play one hour with UI sounds on without fatigue.

### AUD-06 · Music
**Milestone** EA1 · **Claude** 2 d · **You** 2 d · **Needs** AUD-01, D-06

- **The music system**: contexts (main menu, hub, assembly, flight in atmosphere, space, map, tracking station,
  credits) with crossfades and silences between tracks. Space music is sparse.
- **Tracks**: about 10 for 1.0, and 4–5 for Early Access (D-06: commission or license; placeholders from a
  royalty-free library until then).

**Done when**
- [ ] Every context has music.
- [ ] Transitions never cut abruptly.
- [ ] Every track's licence is recorded (REL-02).

### AUD-07 · Ambience
**Milestone** EA1 · **Claude** 1 d · **You** 1 d · **Needs** AUD-01

- The hub (wind, distant machinery, birds by day), the assembly hangar (work sounds), and planet surfaces by
  atmosphere (Tellus breeze, Rubra's thin hiss, Caligo's heavy rumble; silence on airless worlds, only suit
  sounds).

**Done when**
- [ ] Every scene and every EA1 atmospheric body has an ambience.

### AUD-08 · Mix and polish
**Milestone** EA2 · **Claude** 1 d · **You** 2 d · **Needs** AUD-02, AUD-03, AUD-06

- A loudness pass (target −16 LUFS integrated for music), priorities, a ducking rule (music under loud events), and
  a check on speakers and headphones on Windows and Linux.

**Done when**
- [ ] A 30-minute flight recording has no clipping and no masking of warnings (your review).
